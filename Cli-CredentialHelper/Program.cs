using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Alm.Authentication;
using Microsoft.Alm.Git;
using Microsoft.Win32.SafeHandles;

namespace Microsoft.Alm.CredentialHelper
{
    class Program
    {
        public const string Title = "Git Credential Manager for Windows";
        public const string SourceUrl = "https://github.com/Microsoft/Git-Credential-Manager-for-Windows";

        internal const string CommandApprove = "approve";
        internal const string CommandErase = "erase";
        internal const string CommandDeploy = "deploy";
        internal const string CommandFill = "fill";
        internal const string CommandGet = "get";
        internal const string CommandInstall = "install";
        internal const string CommandReject = "reject";
        internal const string CommandRemove = "remove";
        internal const string CommandStore = "store";
        internal const string CommandUninstall = "uninstall";
        internal const string CommandVersion = "version";

        internal const string ConfigAuthortyKey = "authority";
        internal const string ConfigInteractiveKey = "interactive";
        internal const string ConfigUseModalUi = "modalprompt";
        internal const string ConfigValidateKey = "validate";
        internal const string ConfigWritelogKey = "writelog";

        private const string ConfigPrefix = "credential";
        private const string SecretsNamespace = "git";
        private static readonly VsoTokenScope VsoCredentialScope = VsoTokenScope.CodeWrite | VsoTokenScope.PackagingRead;
        private static readonly GithubTokenScope GithubCredentialScope = GithubTokenScope.Gist | GithubTokenScope.PublicKeyRead | GithubTokenScope.Repo;
        private static readonly List<string> CommandList = new List<string>
        {
            CommandApprove,
            CommandDeploy,
            CommandErase,
            CommandFill,
            CommandGet,
            CommandInstall,
            CommandReject,
            CommandRemove,
            CommandStore,
            CommandUninstall,
            CommandVersion
        };

        internal static string ExecutablePath
        {
            get
            {
                if (_exeutablePath == null)
                {
                    LoadAssemblyInformation();
                }
                return _exeutablePath;
            }
        }
        private static string _exeutablePath;
        internal static string Location
        {
            get
            {
                if (_location == null)
                {
                    LoadAssemblyInformation();
                }
                return _location;
            }
        }
        private static string _location;
        internal static string Name
        {
            get
            {
                if (_name == null)
                {
                    LoadAssemblyInformation();
                }
                return _name;
            }
        }
        private static string _name;
        internal static Version Version
        {
            get
            {
                if (_version == null)
                {
                    LoadAssemblyInformation();
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

                if (args.Length == 0
                    || String.Equals(args[0], "--help", StringComparison.OrdinalIgnoreCase)
                    || String.Equals(args[0], "-h", StringComparison.OrdinalIgnoreCase)
                    || args[0].Contains('?'))
                {
                    PrintHelpMessage();
                    return;
                }

                // list of arg => method associations (case-insensitive)
                Dictionary<string, Action> actions = new Dictionary<string, Action>(StringComparer.OrdinalIgnoreCase)
                {
                    { "approve", Store },
                    { "erase", Erase },
                    { "deploy", Deploy },
                    { "fill", Get },
                    { "get", Get },
                    { "install", Deploy },
                    { "reject", Erase },
                    { "remove", Remove },
                    { "store", Store },
                    { "uninstall", Remove },
                    { "version", PrintVersion },
                };

                // invoke action specified by arg0
                if (actions.ContainsKey(args[0]))
                {
                    actions[args[0]]();
                }
                else
                {
                    // display unknown command error
                    Console.Error.WriteLine("Unknown command '{0}'. Please use {1} ? to display help.", args[0], Program.Name);
                }
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine("Fatal: " + exception.GetType().Name + " encountered.");
                Trace.WriteLine("Fatal: " + exception.ToString());
                LogEvent(exception.Message, EventLogEntryType.Error);
            }

            Trace.Flush();
        }

        private static void PrintHelpMessage()
        {
            Trace.WriteLine("Program::PrintHelpMessage");

            Console.Out.WriteLine("usage: git credential-manager [" + String.Join("|", CommandList) + "] [<args>]");
            Console.Out.WriteLine();
            Console.Out.WriteLine("Command Line Options:");
            Console.Out.WriteLine();
            Console.Out.WriteLine("  " + CommandDeploy + "       Deploys the " + Title);
            Console.Out.WriteLine("               package and sets Git configuration to use the helper.");
            Console.Out.WriteLine();
            Console.Out.WriteLine("    " + Installer.ParamPathKey + "     Specifies a path for the installer to deploy to.");
            Console.Out.WriteLine("               If a path is provided, the installer will not seek additional");
            Console.Out.WriteLine("               Git installations to modify.");
            Console.Out.WriteLine();
            Console.Out.WriteLine("    " + Installer.ParamPassiveKey + "  Instructs the installer to not prompt the user for input");
            Console.Out.WriteLine("               during deployment and restricts output to error messages only.");
            Console.Out.WriteLine("               When combined with " + Installer.ParamForceKey + " all output is eliminated; only the");
            Console.Out.WriteLine("               return code can be used to validate success.");
            Console.Out.WriteLine();
            Console.Out.WriteLine("    " + Installer.ParamForceKey + "    Instructs the installer to proceed with deployment even if");
            Console.Out.WriteLine("               prerequisites are not met or errors are encountered.");
            Console.Out.WriteLine("               When combined with " + Installer.ParamPassiveKey + " all output is eliminated; only the");
            Console.Out.WriteLine("               return code can be used to validate success.");
            Console.Out.WriteLine();
            Console.Out.WriteLine("  " + CommandRemove + "       Removes the " + Title);
            Console.Out.WriteLine("               package and unsets Git configuration to no longer use the helper.");
            Console.Out.WriteLine();
            Console.Out.WriteLine("    " + Installer.ParamPathKey + "     Specifies a path for the installer to remove from.");
            Console.Out.WriteLine("               If a path is provided, the installer will not seek additional");
            Console.Out.WriteLine("               Git installations to modify.");
            Console.Out.WriteLine();
            Console.Out.WriteLine("    " + Installer.ParamPassiveKey + "  Instructs the installer to not prompt the user for input");
            Console.Out.WriteLine("               during removal and restricts output to error messages only.");
            Console.Out.WriteLine("               When combined with " + Installer.ParamForceKey + " all output is eliminated; only the");
            Console.Out.WriteLine("               return code can be used to validate success.");
            Console.Out.WriteLine();
            Console.Out.WriteLine("    " + Installer.ParamForceKey + "    Instructs the installer to proceed with removal even if");
            Console.Out.WriteLine("               prerequisites are not met or errors are encountered.");
            Console.Out.WriteLine("               When combined with " + Installer.ParamPassiveKey + " all output is eliminated; only the");
            Console.Out.WriteLine("               return code can be used to validate success.");
            Console.Out.WriteLine();
            Console.Out.WriteLine("  " + CommandVersion + "       Displays the current version.");
            Console.Out.WriteLine();
            Console.Out.WriteLine("Git Configuration Options:");
            Console.Out.WriteLine();
            Console.Out.WriteLine("  " + ConfigAuthortyKey + "    Defines the type of authentication to be used.");
            Console.Out.WriteLine("               Supports Auto, Basic, AAD, MSA, and Integrated.");
            Console.Out.WriteLine("               Default is Auto.");
            Console.Out.WriteLine();
            Console.Out.WriteLine("      `git config --global credential.microsoft.visualstudio.com." + ConfigAuthortyKey + " AAD`");
            Console.Out.WriteLine();
            Console.Out.WriteLine("  " + ConfigInteractiveKey + "  Specifies if user can be prompted for credentials or not.");
            Console.Out.WriteLine("               Supports Auto, Always, or Never. Defaults to Auto.");
            Console.Out.WriteLine("               Only used by AAD, MSA, and Github authority.");
            Console.Out.WriteLine();
            Console.Out.WriteLine("      `git config --global credential.microsoft.visualstudio.com." + ConfigInteractiveKey + " never`");
            Console.Out.WriteLine();
            Console.Out.WriteLine("  " + ConfigUseModalUi + "  Forces authentication to use a modal dialog instead of");
            Console.Out.WriteLine("               asking for credentials at the command prompt.");
            Console.Out.WriteLine("               Only used by Basic authority.");
            Console.Out.WriteLine("               Defaults to FALSE.");
            Console.Out.WriteLine();
            Console.Out.WriteLine("      `git config --global credential." + ConfigUseModalUi + " true`");
            Console.Out.WriteLine();
            Console.Out.WriteLine("  " + ConfigValidateKey + "     Causes validation of credentials before supplying them");
            Console.Out.WriteLine("               to Git. Invalid credentials get a refresh attempt");
            Console.Out.WriteLine("               before failing. Incurs some minor overhead.");
            Console.Out.WriteLine("               Defaults to TRUE. Ignored by Basic authority.");
            Console.Out.WriteLine();
            Console.Out.WriteLine("      `git config --global credential.microsoft.visualstudio.com." + ConfigValidateKey + " false`");
            Console.Out.WriteLine();
            Console.Out.WriteLine("  " + ConfigWritelogKey + "     Enables trace logging of all activities. Logs are written to");
            Console.Out.WriteLine("               the local .git/ folder at the root of the repository.");
            Console.Out.WriteLine("               Defaults to FALSE.");
            Console.Out.WriteLine();
            Console.Out.WriteLine("      `git config --global credential." + ConfigWritelogKey + " true`");
            Console.Out.WriteLine();
            Console.Out.WriteLine("Sample Configuration:");
            Console.Out.WriteLine();
            Console.Out.WriteLine(@"  [credential ""microsoft.visualstudio.com""]");
            Console.Out.WriteLine(@"      " + ConfigAuthortyKey + " = AAD");
            Console.Out.WriteLine(@"      " + ConfigInteractiveKey + " = never");
            Console.Out.WriteLine(@"      " + ConfigValidateKey + " = false");
            Console.Out.WriteLine(@"  [credential ""visualstudio.com""]");
            Console.Out.WriteLine(@"      " + ConfigAuthortyKey + " = MSA");
            Console.Out.WriteLine(@"  [credential]");
            Console.Out.WriteLine(@"      helper = manager");
            Console.Out.WriteLine(@"      " + ConfigWritelogKey + " = true");
            Console.Out.WriteLine();
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
                    else if (operationArguments.UseModalUi)
                    {
                        // display the modal dialog
                        string username;
                        string password;
                        if (PromptForCredentials(operationArguments.TargetUri, out username, out password))
                        {
                            // set the credentials object
                            // no need to save the credentials explicitly, as Git will call back
                            // with a store command if the credentials are valid.
                            credentials = new Credential(username, password);
                        }
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
            Credential credentials = new Credential(operationArguments.Username, operationArguments.Password);
            authentication.SetCredentials(operationArguments.TargetUri, credentials);
        }

        private static void PrintVersion()
        {
            Trace.WriteLine("Program::Version");

            Console.Out.WriteLine("{0} version {1}", Title, Version.ToString(3));
        }

        private static void Deploy()
        {
            Trace.WriteLine("Program::Deploy");

            var installer = new Installer();
            installer.DeployConsole();

            Trace.WriteLine(String.Format("   Installer result = {0}.", installer.Result));
            Trace.WriteLine(String.Format("   Installer exit code = {0}.", installer.ExitCode));

            Environment.Exit(installer.ExitCode);
        }

        private static void Remove()
        {
            Trace.WriteLine("Program::Remove");

            var installer = new Installer();
            installer.RemoveConsole();

            Trace.WriteLine(String.Format("   Installer result = {0}.", installer.Result));
            Trace.WriteLine(String.Format("   Installer exit code = {0}.", installer.ExitCode));

            Environment.Exit(installer.ExitCode);
        }

        private static BaseAuthentication CreateAuthentication(OperationArguments operationArguments)
        {
            Debug.Assert(operationArguments != null, "The operationArguments is null");

            Trace.WriteLine("Program::CreateAuthentication");

            var secrets = new SecretStore(SecretsNamespace);
            BaseAuthentication authority = null;

            switch (operationArguments.Authority)
            {
                case AuthorityType.Auto:
                    Trace.WriteLine("   detecting authority type");

                    // detect the authority
                    if (BaseVsoAuthentication.GetAuthentication(operationArguments.TargetUri,
                                                                VsoCredentialScope,
                                                                secrets,
                                                                null,
                                                                out authority)
                        || GithubAuthentication.GetAuthentication(operationArguments.TargetUri,
                                                                  GithubCredentialScope,
                                                                  secrets,
                                                                  GithubCredentialPrompt, 
                                                                  GithubAuthCodePrompt,
                                                                  null,
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
                    // return the allocated authority or a generic AAD backed VSO authentication object
                    return authority ?? new VsoAadAuthentication(Guid.Empty, VsoCredentialScope, secrets);

                case AuthorityType.Basic:
                default:
                    Trace.WriteLine("   authority is basic");

                    // return a generic username + password authentication object
                    return authority ?? new BasicAuthentication(secrets);

                case AuthorityType.GitHub:
                    Trace.WriteLine("   authority it GitHub");

                    // return a GitHub authenitcation object
                    return authority ?? new GithubAuthentication(GithubCredentialScope, 
                                                                 secrets, 
                                                                 GithubCredentialPrompt, 
                                                                 GithubAuthCodePrompt,
                                                                 null);

                case AuthorityType.MicrosoftAccount:
                    Trace.WriteLine("   authority is Microsoft Live");

                    // return the allocated authority or a generic MSA backed VSO authentication object
                    return authority ?? new VsoMsaAuthentication(VsoCredentialScope, secrets);
            }
        }

        private static void LoadAssemblyInformation()
        {
            var assembly = System.Reflection.Assembly.GetEntryAssembly();
            var asseName = assembly.GetName();

            _exeutablePath = assembly.Location;
            _location = Path.GetDirectoryName(_exeutablePath);
            _name = asseName.Name;
            _version = asseName.Version;
        }

        private static void LoadOperationArguments(OperationArguments operationArguments)
        {
            Debug.Assert(operationArguments != null, "The operationsArguments parameter is null.");

            Trace.WriteLine("Program::LoadOperationArguments");

            Configuration config = new Configuration();
            Configuration.Entry entry;

            if (config.TryGetEntry(ConfigPrefix, operationArguments.TargetUri, ConfigAuthortyKey, out entry))
            {
                Trace.WriteLine("   " + ConfigAuthortyKey + " = " + entry.Value);

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

            if (config.TryGetEntry(ConfigPrefix, operationArguments.TargetUri, ConfigInteractiveKey, out entry))
            {
                Trace.WriteLine("   " + ConfigInteractiveKey + " = " + entry.Value);

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

            if (config.TryGetEntry(ConfigPrefix, operationArguments.TargetUri, ConfigValidateKey, out entry))
            {
                Trace.WriteLine("   " + ConfigValidateKey + " = " + entry.Value);

                bool validate = operationArguments.ValidateCredentials;
                if (Boolean.TryParse(entry.Value, out validate))
                {
                    operationArguments.ValidateCredentials = validate;
                }
            }

            if (config.TryGetEntry(ConfigPrefix, operationArguments.TargetUri, ConfigWritelogKey, out entry))
            {
                Trace.WriteLine("   " + ConfigWritelogKey + " = " + entry.Value);

                bool writelog = operationArguments.WriteLog;
                if (Boolean.TryParse(entry.Value, out writelog))
                {
                    operationArguments.WriteLog = writelog;
                }
            }

            if (config.TryGetEntry(ConfigPrefix, operationArguments.TargetUri, ConfigUseModalUi, out entry))
            {
                Trace.WriteLine("   " + ConfigUseModalUi + " = " + entry.Value);

                bool usemodel = operationArguments.WriteLog;
                if (Boolean.TryParse(entry.Value, out usemodel))
                {
                    operationArguments.UseModalUi = usemodel;
                }
            }
        }

        private static void LogEvent(string message, EventLogEntryType eventType)
        {
            //const string EventSource = "Git Credential Manager";

            ///*** commented out due to UAC issues which require a proper installer to work around ***/

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
                    listener.WriteLine(String.Format("Microsoft {0} version {0}", Program.Title, Version.ToString(3)));
                }
            }
        }

        private static bool GithubCredentialPrompt(Uri targetUri, out string username, out string password)
        {
            // ReadConsole 32768 fail, 32767 ok 
            // @linquize [https://github.com/Microsoft/Git-Credential-Manager-for-Windows/commit/a62b9a19f430d038dcd85a610d97e5f763980f85]
            const int BufferReadSize = 32 * 1024 - 7;

            StringBuilder buffer = new StringBuilder(BufferReadSize);
            uint read = 0;
            uint written = 0;

            username = null;
            password = null;

            NativeMethods.FileAccess fileAccessFlags = NativeMethods.FileAccess.GenericRead | NativeMethods.FileAccess.GenericWrite;
            NativeMethods.FileAttributes fileAttributes = NativeMethods.FileAttributes.Normal;
            NativeMethods.FileCreationDisposition fileCreationDisposition = NativeMethods.FileCreationDisposition.OpenExisting;
            NativeMethods.FileShare fileShareFlags = NativeMethods.FileShare.Read | NativeMethods.FileShare.Write;

            using (SafeFileHandle stdout = NativeMethods.CreateFile(NativeMethods.ConsoleOutName, fileAccessFlags, fileShareFlags, IntPtr.Zero, fileCreationDisposition, fileAttributes, IntPtr.Zero))
            using (SafeFileHandle stdin = NativeMethods.CreateFile(NativeMethods.ConsoleInName, fileAccessFlags, fileShareFlags, IntPtr.Zero, fileCreationDisposition, fileAttributes, IntPtr.Zero))
            {
                // read the current console mode
                NativeMethods.ConsoleMode consoleMode;
                if (!NativeMethods.GetConsoleMode(stdin, out consoleMode))
                {
                    int error = Marshal.GetLastWin32Error();
                    throw new Win32Exception(error, "Unable to determine console mode (" + error + ").");
                }

                // instruct the user as to what they are expected to do
                buffer.Append("Please enter your GitHub credentials for ")
                      .Append(targetUri.Scheme)
                      .Append("://")
                      .Append(targetUri.DnsSafeHost)
                      .Append(targetUri.PathAndQuery)
                      .AppendLine();
                if (!NativeMethods.WriteConsole(stdout, buffer, (uint)buffer.Length, out written, IntPtr.Zero))
                {
                    int error = Marshal.GetLastWin32Error();
                    throw new Win32Exception(error, "Unable to write to standard output (" + error + ").");
                }

                // clear the buffer for the next operation
                buffer.Clear();

                // prompt the user for the username wanted
                buffer.Append("username: ");
                if (!NativeMethods.WriteConsole(stdout, buffer, (uint)buffer.Length, out written, IntPtr.Zero))
                {
                    int error = Marshal.GetLastWin32Error();
                    throw new Win32Exception(error, "Unable to write to standard output (" + error + ").");
                }

                // clear the buffer for the next operation
                buffer.Clear();

                // read input from the user
                if (!NativeMethods.ReadConsole(stdin, buffer, BufferReadSize, out read, IntPtr.Zero))
                {
                    int error = Marshal.GetLastWin32Error();
                    throw new Win32Exception(error, "Unable to read from standard input (" + error + ").");
                }

                // record input from the user into local storage, stripping any eol chars
                username = buffer.ToString(0, (int)read);
                username = username.Trim(Environment.NewLine.ToCharArray());

                // clear the buffer for the next operation
                buffer.Clear();

                // set the console mode to current without echo input
                NativeMethods.ConsoleMode consoleMode2 = consoleMode ^ NativeMethods.ConsoleMode.EchoInput;

                try
                {
                    if (!NativeMethods.SetConsoleMode(stdin, consoleMode2))
                    {
                        int error = Marshal.GetLastWin32Error();
                        throw new Win32Exception(error, "Unable to set console mode (" + error + ").");
                    }

                    // prompt the user for password
                    buffer.Append("password: ");
                    if (!NativeMethods.WriteConsole(stdout, buffer, (uint)buffer.Length, out written, IntPtr.Zero))
                    {
                        int error = Marshal.GetLastWin32Error();
                        throw new Win32Exception(error, "Unable to write to standard output (" + error + ").");
                    }

                    // clear the buffer for the next operation
                    buffer.Clear();

                    // read input from the user
                    if (!NativeMethods.ReadConsole(stdin, buffer, BufferReadSize, out read, IntPtr.Zero))
                    {
                        int error = Marshal.GetLastWin32Error();
                        throw new Win32Exception(error, "Unable to read from standard input (" + error + ").");
                    }

                    // record input from the user into local storage, stripping any eol chars
                    password = buffer.ToString(0, (int)read);
                    password = password.Trim(Environment.NewLine.ToCharArray());
                }
                catch { throw; }
                finally
                {
                    // restore the console mode to its original value
                    if (!NativeMethods.SetConsoleMode(stdin, consoleMode))
                    {
                        int error = Marshal.GetLastWin32Error();
                        throw new Win32Exception(error, "Unable to set console mode (" + error + ").");
                    }
                }
            }

            return username != null
                && password != null;
        }

        private static bool GithubAuthCodePrompt(Uri targetUri, GithubAuthenticationResultType resultType, out string authenticationCode)
        {
            // ReadConsole 32768 fail, 32767 ok 
            // @linquize [https://github.com/Microsoft/Git-Credential-Manager-for-Windows/commit/a62b9a19f430d038dcd85a610d97e5f763980f85]
            const int BufferReadSize = 32 * 1024 - 7;

            StringBuilder buffer = new StringBuilder(BufferReadSize);
            uint read = 0;
            uint written = 0;

            authenticationCode = null;

            NativeMethods.FileAccess fileAccessFlags = NativeMethods.FileAccess.GenericRead | NativeMethods.FileAccess.GenericWrite;
            NativeMethods.FileAttributes fileAttributes = NativeMethods.FileAttributes.Normal;
            NativeMethods.FileCreationDisposition fileCreationDisposition = NativeMethods.FileCreationDisposition.OpenExisting;
            NativeMethods.FileShare fileShareFlags = NativeMethods.FileShare.Read | NativeMethods.FileShare.Write;

            using (SafeFileHandle stdout = NativeMethods.CreateFile(NativeMethods.ConsoleOutName, fileAccessFlags, fileShareFlags, IntPtr.Zero, fileCreationDisposition, fileAttributes, IntPtr.Zero))
            using (SafeFileHandle stdin = NativeMethods.CreateFile(NativeMethods.ConsoleInName, fileAccessFlags, fileShareFlags, IntPtr.Zero, fileCreationDisposition, fileAttributes, IntPtr.Zero))
            {
                string type = resultType == GithubAuthenticationResultType.TwoFactorApp 
                    ? "app" 
                    : "sms";
                buffer.AppendLine()
                      .Append("authcode (")
                      .Append(type)
                      .Append("): ");
                if (!NativeMethods.WriteConsole(stdout, buffer, (uint)buffer.Length, out written, IntPtr.Zero))
                {
                    int error = Marshal.GetLastWin32Error();
                    throw new Win32Exception(error, "Unable to write to standard output (" + error + ").");
                }
                buffer.Clear();

                // read input from the user
                if (!NativeMethods.ReadConsole(stdin, buffer, BufferReadSize, out read, IntPtr.Zero))
                {
                    int error = Marshal.GetLastWin32Error();
                    throw new Win32Exception(error, "Unable to read from standard input (" + error + ").");
                }

                authenticationCode = buffer.ToString(0, (int)read);
                authenticationCode = authenticationCode.Trim(Environment.NewLine.ToCharArray());
            }

            return authenticationCode != null;
        }

        private static bool PromptForCredentials(Uri targetUri, out string username, out string password)
        {
            NativeMethods.CredentialUiInfo credUiInfo = new NativeMethods.CredentialUiInfo
            {
                BannerArt = IntPtr.Zero,
                CaptionText = "Git Credentials",
                MessageText = String.Format("Enter you credentials for {0}://{1}.", targetUri.Scheme, targetUri.DnsSafeHost),
                Parent = IntPtr.Zero,
                Size = Marshal.SizeOf(typeof(NativeMethods.CredentialUiInfo))
            };

            bool saveCredentials = false;
            NativeMethods.CredentialPackFlags packFlags = NativeMethods.CredentialPackFlags.GenericCredentials;
            IntPtr packedAuthBufferPtr = IntPtr.Zero;
            uint packedAuthBufferSize = 0;
            NativeMethods.CredentialUiWindowsFlags flags = NativeMethods.CredentialUiWindowsFlags.Generic
                                                         | NativeMethods.CredentialUiWindowsFlags.Pack32Wow;

            try
            {
                int result;
                if ((result = NativeMethods.CredUIPromptForWindowsCredentials(ref credUiInfo,
                                                                              0,
                                                                              ref packFlags,
                                                                              IntPtr.Zero,
                                                                              0,
                                                                              out packedAuthBufferPtr,
                                                                              out packedAuthBufferSize,
                                                                              ref saveCredentials,
                                                                              flags)) != NativeMethods.Win32Error.Success)
                {
                    username = null;
                    password = null;

                    return false;
                }

                StringBuilder usernameBuffer = new StringBuilder(512);
                StringBuilder domainBuffer = new StringBuilder(256);
                StringBuilder passwordBuffer = new StringBuilder(512);
                int usernameLen = usernameBuffer.Capacity;
                int passwordLen = passwordBuffer.Capacity;
                int domainLen = domainBuffer.Capacity;

                if (!NativeMethods.CredUnPackAuthenticationBuffer(packFlags,
                                                                  packedAuthBufferPtr,
                                                                  packedAuthBufferSize,
                                                                  usernameBuffer,
                                                                  ref usernameLen,
                                                                  domainBuffer,
                                                                  ref domainLen,
                                                                  passwordBuffer,
                                                                  ref passwordLen))
                {
                    username = null;
                    password = null;

                    return false;
                }

                username = usernameBuffer.ToString();
                password = passwordBuffer.ToString();

                return true;
            }
            catch { throw; }
            finally
            {
                if (packedAuthBufferPtr != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(packedAuthBufferPtr);
                }
            }
        }

        [Conditional("DEBUG")]
        private static void EnableDebugTrace()
        {
            // use the stderr stream for the trace as stdout is used in the cross-process communications protocol
            Trace.Listeners.Add(new ConsoleTraceListener(useErrorStream: true));
        }
    }
}
