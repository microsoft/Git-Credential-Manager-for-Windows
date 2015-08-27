using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.TeamFoundation.Authentication;

namespace Microsoft.TeamFoundation.CredentialHelper
{
    class Program
    {
        private const string ConfigPrefix = "credential";
        private const string SecretsNamespace = "git";
        private static readonly VsoTokenScope VsoCredentialScope = VsoTokenScope.CodeWrite;
        private static readonly GithubTokenScope GithubCredentialScope = GithubTokenScope.Gist | GithubTokenScope.PublicKeyRead | GithubTokenScope.Repo;

        internal static Version Version
        {
            get
            {
                if (_version==null)
                {
                    _version = System.Reflection.Assembly.GetEntryAssembly().GetName().Version; ;
                }
                return _version;
            }
        }
        private static Version _version;

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

                // list of arg => method associations (case-insensitive)
                Dictionary<string, Action> actions = new Dictionary<string, Action>(StringComparer.OrdinalIgnoreCase)
                {
                    { "approve", Store },
                    { "erase", Erase },
                    { "fill", Get },
                    { "get", Get },
                    { "reject", Erase },
                    { "store", Store },
                    { "version", PrintVersion }
                };

                foreach (string arg in args)
                {
                    if (actions.ContainsKey(arg))
                    {
                        actions[arg]();
                    }
                }
            }
            catch (Exception exception)
            {
                Trace.WriteLine("Fatal: " + exception.ToString());
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
            Console.Out.WriteLine("                  Supportd Auto, Basic, AAD, MSA, and Integrated.");
            Console.Out.WriteLine("                  Default is Auto.");
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

        private static void Erase()
        {
            // parse the operations arguments from stdin (this is how git sends commands)
            // see: https://www.kernel.org/pub/software/scm/git/docs/technical/api-credentials.html
            // see: https://www.kernel.org/pub/software/scm/git/docs/git-credential.html
            OperationArguments operationArguments = new OperationArguments(Console.In);

            Debug.Assert(operationArguments != null, "The operationArguments is null");
            Debug.Assert(operationArguments.TargetUri != null, "The operationArgument.TargetUri is null");

            LoadOperationArguments(operationArguments);
            EnableTraceLogging(operationArguments);

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

        private static void Get()
        {
            const string AadMsaAuthFailureMessage = "Logon failed, use ctrl+c to cancel basic credential prompt.";
            const string GitHubAuthFailureMessage = "Logon failed, use ctrl+c to cancel basic credential prompt.";

            // parse the operations arguments from stdin (this is how git sends commands)
            // see: https://www.kernel.org/pub/software/scm/git/docs/technical/api-credentials.html
            // see: https://www.kernel.org/pub/software/scm/git/docs/git-credential.html
            OperationArguments operationArguments = new OperationArguments(Console.In);

            Debug.Assert(operationArguments != null, "The operationArguments is null");
            Debug.Assert(operationArguments.TargetUri != null, "The operationArgument.TargetUri is null");

            LoadOperationArguments(operationArguments);
            EnableTraceLogging(operationArguments);

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

                case AuthorityType.GitHub:
                    GithubAuthentication ghAuth = authentication as GithubAuthentication;

                    Task.Run(async () =>
                    {
                        if ((operationArguments.Interactivity != Interactivity.Always
                                && ghAuth.GetCredentials(operationArguments.TargetUri, out credentials)
                                && (!operationArguments.ValidateCredentials
                                    || await ghAuth.ValidateCredentials(operationArguments.TargetUri, credentials)))
                            || (operationArguments.Interactivity != Interactivity.Never
                                && ghAuth.InteractiveLogon(operationArguments.TargetUri, out credentials)
                                && ghAuth.GetCredentials(operationArguments.TargetUri, out credentials)
                                && (!operationArguments.ValidateCredentials
                                    || await ghAuth.ValidateCredentials(operationArguments.TargetUri, credentials))))
                        {
                            Trace.WriteLine("   credentials found");
                            operationArguments.SetCredentials(credentials);
                            LogEvent("GitHub credentials for " + operationArguments.TargetUri + " successfully retrieved.", EventLogEntryType.SuccessAudit);
                        }
                        else
                        {
                            Console.Error.WriteLine(GitHubAuthFailureMessage);
                            LogEvent("Failed to retrieve GitHub credentials for " + operationArguments.TargetUri + ".", EventLogEntryType.FailureAudit);
                        }
                    }).Wait();
                    break;

                case AuthorityType.Integrated:
                    credentials = new Credential(String.Empty, String.Empty);
                    operationArguments.SetCredentials(credentials);
                    break;
            }

            Console.Out.Write(operationArguments);
        }

        private static void Store()
        {
            // parse the operations arguments from stdin (this is how git sends commands)
            // see: https://www.kernel.org/pub/software/scm/git/docs/technical/api-credentials.html
            // see: https://www.kernel.org/pub/software/scm/git/docs/git-credential.html
            OperationArguments operationArguments = new OperationArguments(Console.In);

            Debug.Assert(operationArguments != null, "The operationArguments is null");
            Debug.Assert(operationArguments.Username != null, "The operaionArgument.Username is null");
            Debug.Assert(operationArguments.TargetUri != null, "The operationArgument.TargetUri is null");

            LoadOperationArguments(operationArguments);
            EnableTraceLogging(operationArguments);

            Trace.WriteLine("Program::Store");
            Trace.WriteLine("   targetUri = " + operationArguments.TargetUri);

            BaseAuthentication authentication = CreateAuthentication(operationArguments);
            Credential credentials = new Credential(operationArguments.Username, operationArguments.Password ?? String.Empty);
            authentication.SetCredentials(operationArguments.TargetUri, credentials);
        }

        private static void PrintVersion()
        {
            Trace.WriteLine("Program::Version");

            Console.Out.WriteLine("Microsoft Git Credential Manager for Windows version {0}", Version.ToString(3));
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
                    BaseAuthentication authority;
                    if (BaseVsoAuthentication.GetAuthentication(operationArguments.TargetUri,
                                                                VsoCredentialScope,
                                                                secrets,
                                                                null,
                                                                out authority)
                        || GithubAuthentication.GetAuthentication(operationArguments.TargetUri,
                                                                  GithubCredentialScope,
                                                                  secrets,
                                                                  out authority))
                    {
                        // set the authority type based on the returned value
                        if (authority is VsoMsaAuthentication)
                        {
                            operationArguments.Authority = AuthorityType.MicrosoftAccount;
                            goto case AuthorityType.MicrosoftAccount;
                        }
                        else if (authority is VsoAadAuthentication)
                        {
                            operationArguments.Authority = AuthorityType.AzureDirectory;
                            goto case AuthorityType.AzureDirectory;
                        }
                        else if (authority is GithubAuthentication)
                        {
                            operationArguments.Authority = AuthorityType.GitHub;
                            goto case AuthorityType.GitHub;
                        }
                    }

                    operationArguments.Authority = AuthorityType.Basic;
                    goto case AuthorityType.Basic;

                case AuthorityType.AzureDirectory:
                    Trace.WriteLine("   authority is Azure Directory");

                    Guid tenantId = Guid.Empty;
                    // return a generic AAD backed VSO authentication object
                    return new VsoAadAuthentication(Guid.Empty, VsoCredentialScope, secrets);

                case AuthorityType.Basic:
                default:
                    Trace.WriteLine("   authority is basic");

                    // return a generic username + password authentication object
                    return new BasicAuthentication(secrets);

                case AuthorityType.GitHub:
                    Trace.WriteLine("    authority it GitHub");

                    return new GithubAuthentication(GithubCredentialScope, secrets);

                case AuthorityType.MicrosoftAccount:
                    Trace.WriteLine("   authority is Microsoft Live");

                    // return a generic MSA backed VSO authentication object
                    return new VsoMsaAuthentication(VsoCredentialScope, secrets);
            }
        }

        private static void LoadOperationArguments(OperationArguments operationArguments)
        {
            Debug.Assert(operationArguments != null, "The operationsArguments parameter is null.");

            Trace.WriteLine("Program::LoadOperationArguments");

            Configuration config = new Configuration();
            Configuration.Entry entry;

            if (config.TryGetEntry(ConfigPrefix, operationArguments.TargetUri, "authority", out entry))
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
                else if (String.Equals(entry.Value, "Integrated", StringComparison.OrdinalIgnoreCase)
                         || String.Equals(entry.Value, "NTLM", StringComparison.OrdinalIgnoreCase)
                         || String.Equals(entry.Value, "Kerberos", StringComparison.OrdinalIgnoreCase)
                         || String.Equals(entry.Value, "SSO", StringComparison.OrdinalIgnoreCase))
                {
                    operationArguments.Authority = AuthorityType.Integrated;
                }
                else
                {
                    operationArguments.Authority = AuthorityType.Basic;
                }
            }

            if (config.TryGetEntry(ConfigPrefix, operationArguments.TargetUri, "interactive", out entry))
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

            if (config.TryGetEntry(ConfigPrefix, operationArguments.TargetUri, "validate", out entry))
            {
                Trace.WriteLine("   validate = " + entry.Value);

                bool validate = operationArguments.ValidateCredentials;
                if (Boolean.TryParse(entry.Value, out validate))
                {
                    operationArguments.ValidateCredentials = validate;
                }
            }

            if (config.TryGetEntry(ConfigPrefix, operationArguments.TargetUri, "writelog", out entry))
            {
                Trace.WriteLine("   writelog = " + entry.Value);

                bool writelog = operationArguments.WriteLog;
                if (Boolean.TryParse(entry.Value, out writelog))
                {
                    operationArguments.WriteLog = writelog;
                }
            }
        }

        private static void LogEvent(string message, EventLogEntryType eventType)
        {
            const string EventSource = "Git Credential Manager";

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
                    listener.WriteLine(String.Format("Microsoft Git Credential Manager for Windows version {0}", Version.ToString(3)));
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
