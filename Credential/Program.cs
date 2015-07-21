using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
// couple of local re-delcaration to make code easier to read
using ConfigEntry = System.Collections.Generic.KeyValuePair<string, string>;
using Configuration = System.Collections.Generic.Dictionary<string, string>;

namespace Microsoft.TeamFoundation.Git.Helpers.Authentication
{
    class Program
    {
        private const string ConfigPrefix = "credential";
        private const string SecretsNamespace = "git";
        private static readonly VsoTokenScope CredentialScope = VsoTokenScope.CodeWrite | VsoTokenScope.ProfileRead;
        private const char HostSplitCharacter = '.';

        static void Main(string[] args)
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
            Console.Out.WriteLine("Sample Configuration:");
            Console.Out.WriteLine(@"   [credential ""microsoft.visualstudio.com""]");
            Console.Out.WriteLine(@"       authority = AAD");
            Console.Out.WriteLine(@"   [credential ""visualstudio.com""]");
            Console.Out.WriteLine(@"       authority = MSA");
            Console.Out.WriteLine(@"   [credential]");
            Console.Out.WriteLine(@"       helper = !'C:\\Tools\\Git\\git-credential-man.exe'");
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
                        }
                        else
                        {
                            Console.Error.WriteLine(AadMsaAuthFailureMessage);
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
                        }
                        else
                        {
                            Console.Error.WriteLine(AadMsaAuthFailureMessage);
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
            Trace.WriteLine("   authority = " + operationArguments.Authority);

            var secrets = new SecretStore(SecretsNamespace);

            switch (operationArguments.Authority)
            {
                case AuthorityType.Auto:
                    const string VsoBaseUrlHost = "visualstudio.com";
                    const string VsoResourceTenantHeader = "X-VSS-ResourceTenant";

                    Trace.WriteLine("   detecting authority type");

                    if (operationArguments.Host.EndsWith(VsoBaseUrlHost, StringComparison.OrdinalIgnoreCase))
                    {
                        Trace.WriteLine("   detected visualstudio.com, checking AAD vs MSA");

                        try
                        {
                            var request = WebRequest.CreateHttp(operationArguments.TargetUri);
                            request.Method = "HEAD";
                            request.AllowAutoRedirect = false;
                            var response = request.GetResponse();
                            if (response != null && response.SupportsHeaders)
                            {
                                Trace.WriteLine("   server has responded");

                                var tenant = response.Headers[VsoResourceTenantHeader];
                                Guid tenantId;
                                if (!String.IsNullOrWhiteSpace(tenant) && Guid.TryParse(tenant, out tenantId))
                                {
                                    Trace.WriteLine("   tenant '" + tenantId + "' detected");

                                    if (tenantId == Guid.Empty)
                                    {
                                        operationArguments.Authority = AuthorityType.MicrosoftAccount;
                                        goto case AuthorityType.MicrosoftAccount;
                                    }
                                    else
                                    {
                                        operationArguments.Authority = AuthorityType.AzureDirectory;
                                        goto case AuthorityType.AzureDirectory;
                                    }
                                }
                                else
                                {
                                    Trace.WriteLine("   server reponded with:");
                                    foreach(string key in response.Headers.Keys)
                                    {
                                        Trace.WriteLine("   => " + key + " = " + response.Headers[key]);
                                    }
                                }
                            }
                        }
                        catch (Exception exception)
                        {
                            Trace.WriteLine("   failed detection");
                            Debug.WriteLine(exception);
                        }
                    }

                    // default to basic authority
                    goto case AuthorityType.Basic;

                case AuthorityType.AzureDirectory:
                    Trace.WriteLine("   authority is Azure Directory");

                    // return a generic AAD backed VSO authentication object
                    return new VsoAadAuthentication(CredentialScope, secrets);

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
                operationArguments.SetScheme(entry.Value);
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
        }

        private static Configuration LoadGitConfiguation()
        {
            string systemConfig = null;
            string globalConfig = null;
            string localConfig = null;

            Trace.WriteLine("Program::LoadGitConfiguation");

            Configuration values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            if (Where.GitSystemConfig(out systemConfig))
            {
                ParseGitConfig(systemConfig, values);
            }

            if (Where.GitGlobalConfig(out globalConfig))
            {
                ParseGitConfig(globalConfig, values);
            }

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

            foreach (var line in File.ReadLines(configPath))
            {
                if (String.IsNullOrWhiteSpace(line))
                    continue;
                if (Regex.IsMatch(line, @"^\s*[#;]", RegexOptions.Compiled | RegexOptions.CultureInvariant))
                    continue;

                if ((match = Regex.Match(line, @"^\s*\[\s*(\w+)\s*(\""[^\""]+\""){0,1}\]", RegexOptions.Compiled | RegexOptions.CultureInvariant)).Success)
                {
                    if (match.Groups.Count >= 2 && !String.IsNullOrWhiteSpace(match.Groups[1].Value))
                    {
                        section = match.Groups[1].Value.Trim();

                        if (match.Groups.Count >= 3 && !String.IsNullOrWhiteSpace(match.Groups[2].Value))
                        {
                            string val = match.Groups[2].Value.Trim();

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
                else if ((match = Regex.Match(line, @"^\s*(\w+)\s*=\s*(.+)", RegexOptions.Compiled | RegexOptions.CultureInvariant)).Success)
                {
                    if (match.Groups.Count >= 3
                        && !String.IsNullOrEmpty(match.Groups[1].Value)
                        && !String.IsNullOrEmpty(match.Groups[2].Value))
                    {
                        string key = section + HostSplitCharacter + match.Groups[1].Value.Trim();
                        string val = match.Groups[2].Value.Trim();

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

            if (GetGitConfigEntry(config, ConfigPrefix, String.Empty, key, out entry))
                return true;

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

            foreach (var candidate in config)
            {
                if (String.Equals(candidate.Key, match, StringComparison.OrdinalIgnoreCase))
                {
                    entry = candidate;
                    return true;
                }
            }

            entry = default(ConfigEntry);
            return false;
        }

        [Conditional("DEBUG")]
        [Conditional("TRACE")]
        private static void EnableDebugTrace()
        {
            // use the stderr stream for the trace as stdout is used in the cross-process communcations protocol
            Trace.Listeners.Add(new ConsoleTraceListener(useErrorStream: true));
        }
    }
}
