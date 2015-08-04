using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.TeamFoundation.Authentication;
// couple of local re-delcaration to make code easier to read
using ConfigEntry = System.Collections.Generic.KeyValuePair<string, string>;
using Configuration = System.Collections.Generic.Dictionary<string, string>;

namespace Microsoft.TeamFoundation.CredentialHelper
{
    class Program
    {
        private const string ConfigPrefix = "credential";
        private const string SecretsNamespace = "git";
        private static readonly VsoTokenScope CredentialScope = VsoTokenScope.CodeWrite;
        private const char HostSplitCharacter = '.';

        static void Main(string[] args)
        {
            try
            {
                EnableDebugTrace();

                if (args.Length == 0 || args[0].Contains('?'))
                {
                    PrintHelpMessage();
                    return;
                }

                // parse the operations arguments from stdin (this is how git sends commands)
                // see: https://www.kernel.org/pub/software/scm/git/docs/technical/api-credentials.html
                // see: https://www.kernel.org/pub/software/scm/git/docs/git-credential.html
                OperationArguments operationArguments = new OperationArguments(Console.In);

                LoadOperationArguments(operationArguments);
                EnableTraceLogging(operationArguments);

                // list of arg => method associations (case-insensitive)
                Dictionary<string, Action<OperationArguments>> actions =
                    new Dictionary<string, Action<OperationArguments>>(StringComparer.OrdinalIgnoreCase)
                {
                    { "approve", Store },
                    { "erase", Erase },
                    { "fill", Get },
                    { "get", Get },
                    { "reject", Erase },
                    { "store", Store },
                };

                foreach (string arg in args)
                {
                    if (actions.ContainsKey(arg))
                    {
                        actions[arg](operationArguments);
                    }
                }
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine("Fatal: " + exception.GetType().Name + " encountered.");
                LogEvent(exception.Message, EventLogEntryType.Error);
            }

            Trace.Flush();
        }

        private static void PrintHelpMessage()
        {
            Trace.WriteLine("Program::PrintHelpMessage");

            Console.Out.WriteLine("usage: git credential <command> [<args>]");
            Console.Out.WriteLine();
            Console.Out.WriteLine("   authority      Defines the type of authentication to be used.");
            Console.Out.WriteLine("                  Supportd Auto, Basic, AAD, and MSA. Default is Auto.");
            Console.Out.WriteLine();
            Console.Out.WriteLine("      `git config --global credential.microsoft.visualstudio.com.authority AAD`");
            Console.Out.WriteLine();
            Console.Out.WriteLine("   interactive    Specifies if user can be prompted for credentials or not.");
            Console.Out.WriteLine("                  Supports Auto, Always, or Never. Defaults to Auto.");
            Console.Out.WriteLine("                  Only used by AAD and MSA authority.");
            Console.Out.WriteLine();
            Console.Out.WriteLine("      `git config --global credential.microsoft.visualstudio.com.interactive never`");
            Console.Out.WriteLine();
            Console.Out.WriteLine("   validate       Causes validation of credentials before supplying them");
            Console.Out.WriteLine("                  to Git. Invalid credentials are attemped to refreshed");
            Console.Out.WriteLine("                  before failing. Incurs some minor overhead.");
            Console.Out.WriteLine("                  Defaults to TRUE. Ignore by Basic authority.");
            Console.Out.WriteLine();
            Console.Out.WriteLine("      `git config --global credential.microsoft.visualstudio.com.validate false`");
            Console.Out.WriteLine();
            Console.Out.WriteLine("   writelog       Enables trace logging of all activities. Logs are written to");
            Console.Out.WriteLine("                  the .git/ folder at the root of the repository.");
            Console.Out.WriteLine("                  Defaults to FALSE.");
            Console.Out.WriteLine();
            Console.Out.WriteLine("      `git config --global credential.writelog true`");
            Console.Out.WriteLine();
            Console.Out.WriteLine("Sample Configuration:");
            Console.Out.WriteLine(@"   [credential ""microsoft.visualstudio.com""]");
            Console.Out.WriteLine(@"       authority = AAD");
            Console.Out.WriteLine(@"   [credential ""visualstudio.com""]");
            Console.Out.WriteLine(@"       authority = MSA");
            Console.Out.WriteLine(@"   [credential]");
            Console.Out.WriteLine(@"       helper = manager");
        }

        private static void Erase(OperationArguments operationArguments)
        {
            Debug.Assert(operationArguments != null, "The operationArguments is null");
            Debug.Assert(operationArguments.TargetUri != null, "The operationArgument.TargetUri is null");

            Trace.WriteLine("Program::Erase");
            Trace.WriteLine("   targetUri = " + operationArguments.TargetUri);

            BaseAuthentication authentication = CreateAuthentication(operationArguments);

            switch (operationArguments.Authority)
            {
                default:
                case AuthorityType.Basic:
                    authentication.DeleteCredentials(operationArguments.TargetUri);
                    break;

                case AuthorityType.AzureDirectory:
                case AuthorityType.MicrosoftAccount:
                    BaseVsoAuthentication vsoAuth = authentication as BaseVsoAuthentication;
                    vsoAuth.DeleteCredentials(operationArguments.TargetUri);
                    break;
            }
        }

        private static void Get(OperationArguments operationArguments)
        {
            const string AadMsaAuthFailureMessage = "Logon failed, use ctrl+c to cancel basic credential prompt.";

            Debug.Assert(operationArguments != null, "The operationArguments is null");
            Debug.Assert(operationArguments.TargetUri != null, "The operationArgument.TargetUri is null");

            Trace.WriteLine("Program::Get");
            Trace.WriteLine("   targetUri = " + operationArguments.TargetUri);

            BaseAuthentication authentication = CreateAuthentication(operationArguments);
            Credential credentials = null;

            switch (operationArguments.Authority)
            {
                default:
                case AuthorityType.Basic:
                    if (authentication.GetCredentials(operationArguments.TargetUri, out credentials))
                    {
                        Trace.WriteLine("   credentials found");
                        operationArguments.SetCredentials(credentials);
                    }
                    break;

                case AuthorityType.AzureDirectory:
                    VsoAadAuthentication aadAuth = authentication as VsoAadAuthentication;

                    Task.Run(async () =>
                    {
                        // attmempt to get cached creds -> refresh creds -> non-interactive logon -> interactive logon
                        // note that AAD "credentials" are always scoped access tokens
                        if (((operationArguments.Interactivity != Interactivity.Always
                                && aadAuth.GetCredentials(operationArguments.TargetUri, out credentials)
                                && (!operationArguments.ValidateCredentials
                                    || await aadAuth.ValidateCredentials(operationArguments.TargetUri, credentials)))
                            || (operationArguments.Interactivity != Interactivity.Always
                                && await aadAuth.RefreshCredentials(operationArguments.TargetUri, true)
                                && aadAuth.GetCredentials(operationArguments.TargetUri, out credentials)
                                && (!operationArguments.ValidateCredentials
                                    || await aadAuth.ValidateCredentials(operationArguments.TargetUri, credentials)))
                            || (operationArguments.Interactivity != Interactivity.Always
                                && await aadAuth.NoninteractiveLogon(operationArguments.TargetUri, true)
                                && aadAuth.GetCredentials(operationArguments.TargetUri, out credentials)
                                && (!operationArguments.ValidateCredentials
                                    || await aadAuth.ValidateCredentials(operationArguments.TargetUri, credentials)))
                            || (operationArguments.Interactivity != Interactivity.Never
                                && aadAuth.InteractiveLogon(operationArguments.TargetUri, true))
                                && aadAuth.GetCredentials(operationArguments.TargetUri, out credentials)
                                && (!operationArguments.ValidateCredentials
                                    || await aadAuth.ValidateCredentials(operationArguments.TargetUri, credentials))))
                        {
                            Trace.WriteLine("   credentials found");
                            operationArguments.SetCredentials(credentials);
                            LogEvent("Azure Directory credentials for " + operationArguments.TargetUri + " successfully retrieved.", EventLogEntryType.SuccessAudit);
                        }
                        else
                        {
                            Console.Error.WriteLine(AadMsaAuthFailureMessage);
                            LogEvent("Failed to retrieve Azure Directory credentials for " + operationArguments.TargetUri + ".", EventLogEntryType.FailureAudit);
                        }
                    }).Wait();
                    break;

                case AuthorityType.MicrosoftAccount:
                    VsoMsaAuthentication msaAuth = authentication as VsoMsaAuthentication;

                    Task.Run(async () =>
                    {
                        // attmempt to get cached creds -> refresh creds -> interactive logon
                        // note that MSA "credentials" are always scoped access tokens
                        if (((operationArguments.Interactivity != Interactivity.Always
                                && msaAuth.GetCredentials(operationArguments.TargetUri, out credentials)
                                && (!operationArguments.ValidateCredentials
                                    || await msaAuth.ValidateCredentials(operationArguments.TargetUri, credentials)))
                            || (operationArguments.Interactivity != Interactivity.Always
                                && await msaAuth.RefreshCredentials(operationArguments.TargetUri, true)
                                && msaAuth.GetCredentials(operationArguments.TargetUri, out credentials)
                                && (!operationArguments.ValidateCredentials
                                    || await msaAuth.ValidateCredentials(operationArguments.TargetUri, credentials)))
                            || (operationArguments.Interactivity != Interactivity.Never
                                && msaAuth.InteractiveLogon(operationArguments.TargetUri, true))
                                && msaAuth.GetCredentials(operationArguments.TargetUri, out credentials)
                                && (!operationArguments.ValidateCredentials
                                    || await msaAuth.ValidateCredentials(operationArguments.TargetUri, credentials))))
                        {
                            Trace.WriteLine("   credentials found");
                            operationArguments.SetCredentials(credentials);
                            LogEvent("Microsoft Live credentials for " + operationArguments.TargetUri + " successfully retrieved.", EventLogEntryType.SuccessAudit);
                        }
                        else
                        {
                            Console.Error.WriteLine(AadMsaAuthFailureMessage);
                            LogEvent("Failed to retrieve Microsoft Live credentials for " + operationArguments.TargetUri + ".", EventLogEntryType.FailureAudit);
                        }
                    }).Wait();
                    break;
            }

            Console.Out.Write(operationArguments);
        }

        private static void Store(OperationArguments operationArguments)
        {
            Debug.Assert(operationArguments != null, "The operationArguments is null");
            Debug.Assert(operationArguments.Username != null, "The operaionArgument.Username is null");
            Debug.Assert(operationArguments.TargetUri != null, "The operationArgument.TargetUri is null");

            Trace.WriteLine("Program::Store");
            Trace.WriteLine("   targetUri = " + operationArguments.TargetUri);

            BaseAuthentication authentication = CreateAuthentication(operationArguments);
            Credential credentials = new Credential(operationArguments.Username, operationArguments.Password ?? String.Empty);
            authentication.SetCredentials(operationArguments.TargetUri, credentials);
        }

        private static BaseAuthentication CreateAuthentication(OperationArguments operationArguments)
        {
            Debug.Assert(operationArguments != null, "The operationArguments is null");

            Trace.WriteLine("Program::CreateAuthentication");

            var secrets = new SecretStore(SecretsNamespace);

            switch (operationArguments.Authority)
            {
                case AuthorityType.Auto:
                    Trace.WriteLine("   detecting authority type");

                    // detect the authority
                    var authority = BaseVsoAuthentication.GetAuthentication(operationArguments.TargetUri,
                                                                            CredentialScope,
                                                                            secrets);
                    // set the authority type based on the returned value
                    if (authority is VsoMsaAuthentication)
                    {
                        operationArguments.Authority = AuthorityType.MicrosoftAccount;
                    }
                    else if (authority is VsoAadAuthentication)
                    {
                        operationArguments.Authority = AuthorityType.AzureDirectory;
                    }
                    else
                    {
                        operationArguments.Authority = AuthorityType.Basic;
                    }

                    return authority;

                case AuthorityType.AzureDirectory:
                    Trace.WriteLine("   authority is Azure Directory");

                    Guid tenantId = Guid.Empty;
                    // return a generic AAD backed VSO authentication object
                    return new VsoAadAuthentication(Guid.Empty, CredentialScope, secrets);

                case AuthorityType.Basic:
                default:
                    Trace.WriteLine("   authority is basic");

                    // return a generic username + password authentication object
                    return new BasicAuthentication(secrets);

                case AuthorityType.MicrosoftAccount:
                    Trace.WriteLine("   authority is Microsoft Live");

                    // return a generic MSA backed VSO authentication object
                    return new VsoMsaAuthentication(CredentialScope, secrets);
            }
        }

        private static void LoadOperationArguments(OperationArguments operationArguments)
        {
            Debug.Assert(operationArguments != null, "The operationsArguments parameter is null.");

            Trace.WriteLine("Program::LoadOperationArguments");

            Configuration config = LoadGitConfiguation();
            ConfigEntry entry;

            if (GetGitConfigEntry(config, operationArguments, "authority", out entry))
            {
                Trace.WriteLine("   authority = " + entry.Value);

                if (String.Equals(entry.Value, "MSA", StringComparison.OrdinalIgnoreCase)
                    || String.Equals(entry.Value, "Microsoft", StringComparison.OrdinalIgnoreCase)
                    || String.Equals(entry.Value, "MicrosoftAccount", StringComparison.OrdinalIgnoreCase)
                    || String.Equals(entry.Value, "Live", StringComparison.OrdinalIgnoreCase)
                    || String.Equals(entry.Value, "LiveConnect", StringComparison.OrdinalIgnoreCase)
                    || String.Equals(entry.Value, "LiveID", StringComparison.OrdinalIgnoreCase))
                {
                    operationArguments.Authority = AuthorityType.MicrosoftAccount;
                }
                else if (String.Equals(entry.Value, "AAD", StringComparison.OrdinalIgnoreCase)
                         || String.Equals(entry.Value, "Azure", StringComparison.OrdinalIgnoreCase)
                         || String.Equals(entry.Value, "AzureDirectory", StringComparison.OrdinalIgnoreCase))
                {
                    operationArguments.Authority = AuthorityType.AzureDirectory;
                }
                else
                {
                    operationArguments.Authority = AuthorityType.Basic;
                }
            }

            if (GetGitConfigEntry(config, operationArguments, "interactive", out entry))
            {
                Trace.WriteLine("   interactive = " + entry.Value);

                if (String.Equals("always", entry.Value, StringComparison.OrdinalIgnoreCase)
                    || String.Equals("true", entry.Value, StringComparison.OrdinalIgnoreCase)
                    || String.Equals("force", entry.Value, StringComparison.OrdinalIgnoreCase))
                {
                    operationArguments.Interactivity = Interactivity.Always;
                }
                else if (String.Equals("never", entry.Value, StringComparison.OrdinalIgnoreCase)
                         || String.Equals("false", entry.Value, StringComparison.OrdinalIgnoreCase))
                {
                    operationArguments.Interactivity = Interactivity.Never;
                }
            }

            if (GetGitConfigEntry(config, operationArguments, "validate", out entry))
            {
                Trace.WriteLine("   validate = " + entry.Value);

                bool validate = operationArguments.ValidateCredentials;
                if (Boolean.TryParse(entry.Value, out validate))
                {
                    operationArguments.ValidateCredentials = validate;
                }
            }

            if (GetGitConfigEntry(config, operationArguments, "writelog", out entry))
            {
                Trace.WriteLine("   writelog = " + entry.Value);

                bool writelog = operationArguments.WriteLog;
                if (Boolean.TryParse(entry.Value, out writelog))
                {
                    operationArguments.WriteLog = writelog;
                }
            }
        }

        private static Configuration LoadGitConfiguation()
        {
            string systemConfig = null;
            string globalConfig = null;
            string localConfig = null;

            Trace.WriteLine("Program::LoadGitConfiguation");

            // read Git's three configs from lowest priority to highest, overwriting values as
            // higher prirority configurations are parsed, storing them in a handy lookup table
            Configuration values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            // find and parse Git's system config
            if (Where.GitSystemConfig(out systemConfig))
            {
                ParseGitConfig(systemConfig, values);
            }

            // find and parse Git's global config
            if (Where.GitGlobalConfig(out globalConfig))
            {
                ParseGitConfig(globalConfig, values);
            }

            // find and parse Git's local config
            if (Where.GitLocalConfig(out localConfig))
            {
                ParseGitConfig(localConfig, values);
            }

            foreach (var pair in values)
            {
                Trace.WriteLine(String.Format("   {0} = {1}", pair.Key, pair.Value));
            }

            return values;
        }

        private static void ParseGitConfig(string configPath, Configuration values)
        {
            Debug.Assert(!String.IsNullOrWhiteSpace(configPath), "The configPath parameter is null or invalid.");
            Debug.Assert(File.Exists(configPath), "The configPath parameter references a non-existant file.");
            Debug.Assert(values != null, "The configPath parameter is null or invalid.");

            Trace.WriteLine("Program::ParseGitConfig");

            if (!File.Exists(configPath))
                return;

            Match match = null;
            string section = null;

            // parse each line in the config independently - Git's configs do not accept multi-line values
            foreach (var line in File.ReadLines(configPath))
            {
                // skip empty and commented lines
                if (String.IsNullOrWhiteSpace(line))
                    continue;
                if (Regex.IsMatch(line, @"^\s*[#;]", RegexOptions.Compiled | RegexOptions.CultureInvariant))
                    continue;

                // sections begin with values like [section] or [section "section name"]. All subsequent lines,
                // until a new section is encountered, are children of the section
                if ((match = Regex.Match(line, @"^\s*\[\s*(\w+)\s*(\""[^\""]+\""){0,1}\]", RegexOptions.Compiled | RegexOptions.CultureInvariant)).Success)
                {
                    if (match.Groups.Count >= 2 && !String.IsNullOrWhiteSpace(match.Groups[1].Value))
                    {
                        section = match.Groups[1].Value.Trim();

                        // check if the section is named, if so: process the name
                        if (match.Groups.Count >= 3 && !String.IsNullOrWhiteSpace(match.Groups[2].Value))
                        {
                            string val = match.Groups[2].Value.Trim();

                            // triming off enclosing quotes makes usage easier, only trim in pairs
                            if (val[0] == '"')
                            {
                                if (val[val.Length - 1] == '"')
                                {
                                    val = val.Substring(1, val.Length - 2);
                                }
                                else
                                {
                                    val = val.Substring(1, val.Length - 1);
                                }
                            }

                            section += HostSplitCharacter + val;
                        }
                    }
                }
                // section children should be in the format of name = value pairs
                else if ((match = Regex.Match(line, @"^\s*(\w+)\s*=\s*(.+)", RegexOptions.Compiled | RegexOptions.CultureInvariant)).Success)
                {
                    if (match.Groups.Count >= 3
                        && !String.IsNullOrEmpty(match.Groups[1].Value)
                        && !String.IsNullOrEmpty(match.Groups[2].Value))
                    {
                        string key = section + HostSplitCharacter + match.Groups[1].Value.Trim();
                        string val = match.Groups[2].Value.Trim();

                        // triming off enclosing quotes makes usage easier, only trim in pairs
                        if (val[0] == '"')
                        {
                            if (val[val.Length - 1] == '"')
                            {
                                val = val.Substring(1, val.Length - 2);
                            }
                            else
                            {
                                val = val.Substring(1, val.Length - 1);
                            }
                        }

                        // add or update the (key, value)
                        if (values.ContainsKey(key))
                        {
                            values[key] = val;
                        }
                        else
                        {
                            values.Add(key, val);
                        }
                    }
                }
            }
        }

        private static bool GetGitConfigEntry(Configuration config, OperationArguments operationArguments, string key, out ConfigEntry entry)
        {
            Debug.Assert(config != null, "The config parameter is null");
            Debug.Assert(operationArguments != null, "The operationArguments parameter is null");
            Debug.Assert(operationArguments.Protocol != null, "The operationArguments.Protocol parameter is null");
            Debug.Assert(operationArguments.Host != null, "The operationArguments.Host parameter is null");
            Debug.Assert(key != null, "The key parameter is null");

            Trace.WriteLine("Program::GetGitConfigEntry");

            // return match seeking from most specific (credential.<schema>://<uri>.<key>) to least specific (credential.<key>)
            if (GetGitConfigEntry(config, ConfigPrefix, String.Format("{0}://{1}", operationArguments.Protocol, operationArguments.Host), key, out entry)
                || GetGitConfigEntry(config, ConfigPrefix, operationArguments.Host, key, out entry))
                return true;

            if (!String.IsNullOrWhiteSpace(operationArguments.Host))
            {
                string[] fragments = operationArguments.Host.Split(HostSplitCharacter);
                string host = null;

                // look for host matches stripping a single sub-domain at a time off
                // don't match against a top-level domain (aka ".com")
                for (int i = 1; i < fragments.Length - 1; i++)
                {
                    host = String.Join(".", fragments, i, fragments.Length - i);
                    if (GetGitConfigEntry(config, ConfigPrefix, host, key, out entry))
                        return true;
                }
            }

            // try to find an unadorned match as a complete fallback
            if (GetGitConfigEntry(config, ConfigPrefix, String.Empty, key, out entry))
                return true;

            // nothing found
            entry = default(ConfigEntry);
            return false;
        }

        private static bool GetGitConfigEntry(Configuration config, string prefix, string key, string suffix, out ConfigEntry entry)
        {
            Debug.Assert(config != null, "The config parameter is null");
            Debug.Assert(prefix != null, "The prefix parameter is null");
            Debug.Assert(suffix != null, "The suffic parameter is null");

            string match = String.IsNullOrEmpty(key)
                ? String.Format("{0}.{1}", prefix, suffix)
                : String.Format("{0}.{1}.{2}", prefix, key, suffix);

            // if there's a match, return it
            if (config.ContainsKey(match))
            {
                entry = new ConfigEntry(match, config[match]);
                return true;
            }

            // nothing found
            entry = default(ConfigEntry);
            return false;
        }

        private static void LogEvent(string message, EventLogEntryType eventType)
        {
            //const string EventSource = "TFS Git Credential Helper";

            /*** commented out due to UAC issues which require a proper installer to work around ***/

            //Trace.WriteLine("Program::LogEvent");

            //if (!EventLog.SourceExists(EventSource))
            //{
            //    EventLog.CreateEventSource(EventSource, "Application");

            //    Trace.WriteLine("   event source created");
            //}

            //EventLog.WriteEntry(EventSource, message, eventType);

            //Trace.WriteLine("   " + eventType + "event written");
        }

        private static void EnableTraceLogging(OperationArguments operationArguments)
        {
            const int LogFileMaxLength = 8 * 1024 * 1024; // 8 MB

            Trace.WriteLine("Program::EnableTraceLogging");

            if (operationArguments.WriteLog)
            {
                Trace.WriteLine("   trace logging enabled");

                string gitConfigPath;
                if (Where.GitLocalConfig(out gitConfigPath))
                {
                    Trace.WriteLine("   git local config found at " + gitConfigPath);

                    string dotGitPath = Path.GetDirectoryName(gitConfigPath);
                    string logFilePath = Path.Combine(dotGitPath, Path.ChangeExtension(ConfigPrefix, ".log"));
                    string logFileName = operationArguments.TargetUri.ToString();

                    FileInfo logFileInfo = new FileInfo(logFilePath);
                    if (logFileInfo.Exists && logFileInfo.Length > LogFileMaxLength)
                    {
                        for (int i = 1; i < Int32.MaxValue; i++)
                        {
                            string moveName = String.Format("{0}{1:000}.log", ConfigPrefix, i);
                            string movePath = Path.Combine(dotGitPath, moveName);

                            if (!File.Exists(movePath))
                            {
                                logFileInfo.MoveTo(movePath);
                                break;
                            }
                        }
                    }

                    Trace.WriteLine("   trace log destination is " + logFilePath);

                    var listener = new TextWriterTraceListener(logFilePath, logFileName);
                    Trace.Listeners.Add(listener);

                    // write a small header to help with identifying new log entries
                    listener.WriteLine(Environment.NewLine);
                    listener.WriteLine(String.Format("Log Start ({0:u})", DateTimeOffset.Now));
                }
            }
        }

        [Conditional("DEBUG")]
        private static void EnableDebugTrace()
        {
            // use the stderr stream for the trace as stdout is used in the cross-process communcations protocol
            Trace.Listeners.Add(new ConsoleTraceListener(useErrorStream: true));
        }
    }
}
