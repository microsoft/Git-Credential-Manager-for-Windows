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
    internal class Program
    {
        public const string Title = "Git Credential Manager for Windows";
        public const string SourceUrl = "https://github.com/Microsoft/Git-Credential-Manager-for-Windows";
        public static readonly StringComparer ConfigStringComparer = StringComparer.OrdinalIgnoreCase;

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
        internal const string CommandDelete = "delete";

        internal const string ConfigAuthortyKey = "authority";
        internal const string ConfigHttpProxyKey = "httpProxy";
        internal const string ConfigInteractiveKey = "interactive";
        internal const string ConfigPreserveCredentialsKey = "preserve";
        internal const string ConfigUseHttpPathKey = "useHttpPath";
        internal const string ConfigUseModalPromptKey = "modalPrompt";
        internal const string ConfigValidateKey = "validate";
        internal const string ConfigWritelogKey = "writelog";


        private const string ConfigPrefix = "credential";
        private const string SecretsNamespace = "git";
        private static readonly VstsTokenScope VstsCredentialScope = VstsTokenScope.CodeWrite | VstsTokenScope.PackagingRead;
        private static readonly GithubTokenScope GithubCredentialScope = GithubTokenScope.Gist | GithubTokenScope.Repo;
        private static readonly List<string> CommandList = new List<string>
        {
            CommandApprove,
            CommandDelete,
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
        private static readonly char[] NewLineChars = Environment.NewLine.ToCharArray();

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

        private static void Main(string[] args)
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
                    { CommandApprove, Store },
                    { CommandErase, Erase },
                    { CommandDeploy, Deploy },
                    { CommandFill, Get },
                    { CommandGet, Get },
                    { CommandInstall, Deploy },
                    { CommandReject, Erase },
                    { CommandRemove, Remove },
                    { CommandStore, Store },
                    { CommandUninstall, Remove },
                    { CommandVersion, PrintVersion },
                    { CommandDelete, Delete },
                };

                // invoke action specified by arg0
                if (actions.ContainsKey(args[0]))
                {
                    actions[args[0]]();
                }
                else
                {
                    // display unknown command error
                    Console.Error.WriteLine("Unknown command '{0}'. Please use `{1} ?` to display help.", args[0], Program.Name);
                }
            }
            catch (AggregateException exception)
            {
                // print out more useful information when an `AggregateException` is encountered
                Console.Error.WriteLine("Fatal: " + exception.InnerExceptions[0].GetType().Name + " encountered.");
                Trace.WriteLine("Fatal: " + exception.ToString());
                LogEvent(exception.Message, EventLogEntryType.Error);
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
            Console.Out.WriteLine("  " + CommandDeploy + "       Deploys the " + Title + " package and sets");
            Console.Out.WriteLine("               Git configuration to use the helper.");
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
            Console.Out.WriteLine("  " + CommandRemove + "       Removes the " + Title + " package");
            Console.Out.WriteLine("               and unsets Git configuration to no longer use the helper.");
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
            Console.Out.WriteLine("  " + CommandDelete + "       Removes stored credentials for a given URL.");
            Console.Out.WriteLine("               Any future attempts to authenticate with the remote will require");
            Console.Out.WriteLine("               authenitcation steps to be completed again.");
            Console.Out.WriteLine();
            Console.Out.WriteLine("      `git credential-manager clear <url>`");
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
            Console.Out.WriteLine("  " + ConfigUseModalPromptKey + "  Forces authentication to use a modal dialog instead of");
            Console.Out.WriteLine("               asking for credentials at the command prompt.");
            Console.Out.WriteLine("               Defaults to TRUE.");
            Console.Out.WriteLine();
            Console.Out.WriteLine("      `git config --global credential." + ConfigUseModalPromptKey + " true`");
            Console.Out.WriteLine();
            Console.Out.WriteLine("  " + ConfigValidateKey + "     Causes validation of credentials before supplying them");
            Console.Out.WriteLine("               to Git. Invalid credentials get a refresh attempt");
            Console.Out.WriteLine("               before failing. Incurs some minor overhead.");
            Console.Out.WriteLine("               Defaults to TRUE. Ignored by Basic authority.");
            Console.Out.WriteLine();
            Console.Out.WriteLine("      `git config --global credential.microsoft.visualstudio.com." + ConfigValidateKey + " false`");
            Console.Out.WriteLine();
            Console.Out.WriteLine("  " + ConfigPreserveCredentialsKey + "     Prevents the deletion of credentials even when they are");
            Console.Out.WriteLine("               reported as invlaid by Git. Can lead to lockout situations once credentials");
            Console.Out.WriteLine("               expire and until those credentials are manually removed.");
            Console.Out.WriteLine("               Defaults to FALSE.");
            Console.Out.WriteLine();
            Console.Out.WriteLine("      `git config --global credential.visualstudio.com." + ConfigPreserveCredentialsKey + " true`");
            Console.Out.WriteLine();
            Console.Out.WriteLine("  " + ConfigUseHttpPathKey + "     Causes the path portion of the target URI to considered meaningful.");
            Console.Out.WriteLine("               By default the path portion of the target URI is ignore, if this is set to true");
            Console.Out.WriteLine("               the path is considered meaningful and credentials will be store for each path.");
            Console.Out.WriteLine("               Defaults to FALSE.");
            Console.Out.WriteLine();
            Console.Out.WriteLine("      `git config --global credential.bitbucket.com." + ConfigUseHttpPathKey + " true`");
            Console.Out.WriteLine();
            Console.Out.WriteLine("  " + ConfigHttpProxyKey + "     Causes the proxy value to be considered when evaluating.");
            Console.Out.WriteLine("               credential target information. A proxy setting should established if use of a");
            Console.Out.WriteLine("               proxy is required to interact with Git remotes.");
            Console.Out.WriteLine("               The value should the url of the proxy server.");
            Console.Out.WriteLine();
            Console.Out.WriteLine("      `git config --global credential.github.com." + ConfigUseHttpPathKey + " https://myproxy:8080`");
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

        private static void Delete()
        {
            Trace.WriteLine("Program::Erase");

            string[] args = Environment.GetCommandLineArgs();

            if (args.Length < 3)
                goto error_parse;

            string url = args[2];
            Uri uri = null;

            if (Uri.IsWellFormedUriString(url, UriKind.Absolute))
            {
                if (!Uri.TryCreate(url, UriKind.Absolute, out uri))
                    goto error_parse;
            }
            else
            {
                url = String.Format("{0}://{1}", Uri.UriSchemeHttps, url);
                if (!Uri.TryCreate(url, UriKind.Absolute, out uri))
                    goto error_parse;
            }

            OperationArguments operationArguments = new OperationArguments(TextReader.Null);
            operationArguments.QueryUri = uri;

            LoadOperationArguments(operationArguments);

            BaseAuthentication authentication = CreateAuthentication(operationArguments);

            switch (operationArguments.Authority)
            {
                default:
                case AuthorityType.Basic:
                    Trace.WriteLine("   deleting basic credentials");
                    authentication.DeleteCredentials(operationArguments.TargetUri);
                    break;

                case AuthorityType.AzureDirectory:
                case AuthorityType.MicrosoftAccount:
                    Trace.WriteLine("   deleting VSTS credentials");
                    BaseVstsAuthentication vstsAuth = authentication as BaseVstsAuthentication;
                    vstsAuth.DeleteCredentials(operationArguments.TargetUri);
                    // call delete twice to purge any stored ADA tokens
                    vstsAuth.DeleteCredentials(operationArguments.TargetUri);
                    break;

                case AuthorityType.GitHub:
                    Trace.WriteLine("   deleting GitHub credentials");
                    GithubAuthentication ghAuth = authentication as GithubAuthentication;
                    ghAuth.DeleteCredentials(operationArguments.TargetUri);
                    break;
            }

            return;

            error_parse:
            Console.Out.WriteLine("Fatal: unable to parse target uri.");
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

            if (operationArguments.PreserveCredentials)
            {
                Trace.WriteLine("   " + ConfigPreserveCredentialsKey + " = true");
                Trace.WriteLine("   cancelling erase request.");
                return;
            }

            BaseAuthentication authentication = CreateAuthentication(operationArguments);

            switch (operationArguments.Authority)
            {
                default:
                case AuthorityType.Basic:
                    Trace.WriteLine("   deleting basic credentials");
                    authentication.DeleteCredentials(operationArguments.TargetUri);
                    break;

                case AuthorityType.AzureDirectory:
                case AuthorityType.MicrosoftAccount:
                    Trace.WriteLine("   deleting VSTS credentials");
                    BaseVstsAuthentication vstsAuth = authentication as BaseVstsAuthentication;
                    vstsAuth.DeleteCredentials(operationArguments.TargetUri);
                    break;

                case AuthorityType.GitHub:
                    Trace.WriteLine("   deleting GitHub credentials");
                    GithubAuthentication ghAuth = authentication as GithubAuthentication;
                    ghAuth.DeleteCredentials(operationArguments.TargetUri);
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
                        if (ModalPromptForCredentials(operationArguments.TargetUri, out username, out password))
                        {
                            // set the credentials object
                            // no need to save the credentials explicitly, as Git will call back
                            // with a store command if the credentials are valid.
                            credentials = new Credential(username, password);
                            operationArguments.SetCredentials(credentials);
                        }
                    }
                    break;

                case AuthorityType.AzureDirectory:
                    VstsAadAuthentication aadAuth = authentication as VstsAadAuthentication;

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
                    VstsMsaAuthentication msaAuth = authentication as VstsMsaAuthentication;

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
            Debug.Assert(operationArguments.CredUsername != null, "The operaionArgument.Username is null");
            Debug.Assert(operationArguments.TargetUri != null, "The operationArgument.TargetUri is null");

            LoadOperationArguments(operationArguments);
            EnableTraceLogging(operationArguments);

            Trace.WriteLine("Program::Store");
            Trace.WriteLine("   targetUri = " + operationArguments.TargetUri);

            BaseAuthentication authentication = CreateAuthentication(operationArguments);
            Credential credentials = new Credential(operationArguments.CredUsername, operationArguments.CredPassword);
            authentication.SetCredentials(operationArguments.TargetUri, credentials);
        }

        private static void PrintVersion()
        {
            Trace.WriteLine("Program::PrintVersion");

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

            Secret.UriNameConversion getTargetName = Secret.UriToSimpleName;
            if (operationArguments.UseHttpPath)
            {
                getTargetName = Secret.UriToPathedName;
            }
            var secrets = new SecretStore(SecretsNamespace, null, null, getTargetName);
            BaseAuthentication authority = null;

            switch (operationArguments.Authority)
            {
                case AuthorityType.Auto:
                    Trace.WriteLine("   detecting authority type");

                    // detect the authority
                    if (BaseVstsAuthentication.GetAuthentication(operationArguments.TargetUri,
                                                                VstsCredentialScope,
                                                                secrets,
                                                                null,
                                                                out authority)
                        || GithubAuthentication.GetAuthentication(operationArguments.TargetUri,
                                                                  GithubCredentialScope,
                                                                  secrets,
                                                                  operationArguments.UseModalUi
                                                                    ? new GithubAuthentication.AcquireCredentialsDelegate(GithubCredentialModalPrompt)
                                                                    : new GithubAuthentication.AcquireCredentialsDelegate(GithubCredentialPrompt),
                                                                  operationArguments.UseModalUi
                                                                    ? new GithubAuthentication.AcquireAuthenticationCodeDelegate(GithubAuthcodeModalPrompt)
                                                                    : new GithubAuthentication.AcquireAuthenticationCodeDelegate(GithubAuthCodePrompt),
                                                                  null,
                                                                  out authority))
                    {
                        // set the authority type based on the returned value
                        if (authority is VstsMsaAuthentication)
                        {
                            operationArguments.Authority = AuthorityType.MicrosoftAccount;
                            goto case AuthorityType.MicrosoftAccount;
                        }
                        else if (authority is VstsAadAuthentication)
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
                    // return the allocated authority or a generic AAD backed VSTS authentication object
                    return authority ?? new VstsAadAuthentication(Guid.Empty, VstsCredentialScope, secrets);

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
                                                                 operationArguments.UseModalUi
                                                                    ? new GithubAuthentication.AcquireCredentialsDelegate(GithubCredentialModalPrompt)
                                                                    : new GithubAuthentication.AcquireCredentialsDelegate(GithubCredentialPrompt),
                                                                 operationArguments.UseModalUi
                                                                    ? new GithubAuthentication.AcquireAuthenticationCodeDelegate(GithubAuthcodeModalPrompt)
                                                                    : new GithubAuthentication.AcquireAuthenticationCodeDelegate(GithubAuthCodePrompt),
                                                                 null);

                case AuthorityType.MicrosoftAccount:
                    Trace.WriteLine("   authority is Microsoft Live");

                    // return the allocated authority or a generic MSA backed VSTS authentication object
                    return authority ?? new VstsMsaAuthentication(VstsCredentialScope, secrets);
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

            if (config.TryGetEntry(ConfigPrefix, operationArguments.QueryUri, ConfigAuthortyKey, out entry))
            {
                Trace.WriteLine("   " + ConfigAuthortyKey + " = " + entry.Value);

                if (ConfigStringComparer.Equals(entry.Value, "MSA")
                    || ConfigStringComparer.Equals(entry.Value, "Microsoft")
                    || ConfigStringComparer.Equals(entry.Value, "MicrosoftAccount")
                    || ConfigStringComparer.Equals(entry.Value, "Live")
                    || ConfigStringComparer.Equals(entry.Value, "LiveConnect")
                    || ConfigStringComparer.Equals(entry.Value, "LiveID"))
                {
                    operationArguments.Authority = AuthorityType.MicrosoftAccount;
                }
                else if (ConfigStringComparer.Equals(entry.Value, "AAD")
                         || ConfigStringComparer.Equals(entry.Value, "Azure")
                         || ConfigStringComparer.Equals(entry.Value, "AzureDirectory"))
                {
                    operationArguments.Authority = AuthorityType.AzureDirectory;
                }
                else if (ConfigStringComparer.Equals(entry.Value, "Integrated")
                         || ConfigStringComparer.Equals(entry.Value, "NTLM")
                         || ConfigStringComparer.Equals(entry.Value, "Kerberos")
                         || ConfigStringComparer.Equals(entry.Value, "SSO"))
                {
                    operationArguments.Authority = AuthorityType.Integrated;
                }
                else
                {
                    operationArguments.Authority = AuthorityType.Basic;
                }
            }

            if (config.TryGetEntry(ConfigPrefix, operationArguments.QueryUri, ConfigInteractiveKey, out entry))
            {
                Trace.WriteLine("   " + ConfigInteractiveKey + " = " + entry.Value);

                if (ConfigStringComparer.Equals(entry.Value, "always")
                    || ConfigStringComparer.Equals(entry.Value, "true")
                    || ConfigStringComparer.Equals(entry.Value, "force"))
                {
                    operationArguments.Interactivity = Interactivity.Always;
                }
                else if (ConfigStringComparer.Equals(entry.Value, "never")
                         || ConfigStringComparer.Equals(entry.Value, "false"))
                {
                    operationArguments.Interactivity = Interactivity.Never;
                }
            }

            if (config.TryGetEntry(ConfigPrefix, operationArguments.QueryUri, ConfigValidateKey, out entry))
            {
                Trace.WriteLine("   " + ConfigValidateKey + " = " + entry.Value);

                bool validate = operationArguments.ValidateCredentials;
                if (Boolean.TryParse(entry.Value, out validate))
                {
                    operationArguments.ValidateCredentials = validate;
                }
                else
                {
                    if (ConfigStringComparer.Equals(validate, "no"))
                    {
                        operationArguments.ValidateCredentials = false;
                    }
                    else if (ConfigStringComparer.Equals(validate, "yes"))
                    {
                        operationArguments.ValidateCredentials = true;
                    }
                }
            }

            if (config.TryGetEntry(ConfigPrefix, operationArguments.QueryUri, ConfigWritelogKey, out entry))
            {
                Trace.WriteLine("   " + ConfigWritelogKey + " = " + entry.Value);

                bool writelog = operationArguments.WriteLog;
                if (Boolean.TryParse(entry.Value, out writelog))
                {
                    operationArguments.WriteLog = writelog;
                }
                else
                {
                    if (ConfigStringComparer.Equals(writelog, "no"))
                    {
                        operationArguments.WriteLog = false;
                    }
                    else if (ConfigStringComparer.Equals(writelog, "yes"))
                    {
                        operationArguments.WriteLog = true;
                    }
                }
            }

            if (config.TryGetEntry(ConfigPrefix, operationArguments.QueryUri, ConfigUseModalPromptKey, out entry))
            {
                Trace.WriteLine("   " + ConfigUseModalPromptKey + " = " + entry.Value);

                bool usemodel = operationArguments.UseModalUi;
                if (Boolean.TryParse(entry.Value, out usemodel))
                {
                    operationArguments.UseModalUi = usemodel;
                }
                else
                {
                    if (ConfigStringComparer.Equals(usemodel, "no"))
                    {
                        operationArguments.UseModalUi = false;
                    }
                    else if (ConfigStringComparer.Equals(usemodel, "yes"))
                    {
                        operationArguments.UseModalUi = true;
                    }
                }
            }

            if (config.TryGetEntry(ConfigPrefix, operationArguments.QueryUri, ConfigPreserveCredentialsKey, out entry))
            {
                Trace.WriteLine("   " + ConfigPreserveCredentialsKey + " = " + entry.Value);

                bool preserveCredentials = operationArguments.UseModalUi;
                if (Boolean.TryParse(entry.Value, out preserveCredentials))
                {
                    operationArguments.PreserveCredentials = preserveCredentials;
                }
                else
                {
                    if (ConfigStringComparer.Equals(preserveCredentials, "no"))
                    {
                        operationArguments.PreserveCredentials = false;
                    }
                    else if (ConfigStringComparer.Equals(preserveCredentials, "yes"))
                    {
                        operationArguments.PreserveCredentials = true;
                    }
                }
            }

            if (config.TryGetEntry(ConfigPrefix, operationArguments.QueryUri, ConfigUseHttpPathKey, out entry))
            {
                Trace.WriteLine("   " + ConfigUseHttpPathKey + " = " + entry.Value);

                bool useHttPath = operationArguments.UseHttpPath;
                if (Boolean.TryParse(entry.Value, out useHttPath))
                {
                    operationArguments.UseHttpPath = true;
                }
            }

            if (config.TryGetEntry(ConfigPrefix, operationArguments.QueryUri, ConfigHttpProxyKey, out entry)
                || config.TryGetEntry("http", operationArguments.QueryUri, "proxy", out entry))
            {
                Trace.WriteLine("   " + ConfigHttpProxyKey + " = " + entry.Value);

                operationArguments.SetProxy(entry.Value);
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
                    listener.WriteLine(String.Format("Microsoft {0} version {1}", Program.Title, Version.ToString(3)));
                }
            }
        }

        private static bool GithubCredentialModalPrompt(TargetUri targetUri, out string username, out string password)
        {
            Trace.WriteLine("Program::GithubCredentialModalPrompt");

            return ModalPromptForCredentials(targetUri, out username, out password);
        }

        private static bool GithubAuthcodeModalPrompt(TargetUri targetUri, GithubAuthenticationResultType resultType, string username, out string authenticationCode)
        {
            Trace.WriteLine("Program::GithubAuthcodeModalPrompt");

            authenticationCode = null;

            string type =
                resultType == GithubAuthenticationResultType.TwoFactorApp
                    ? "app"
                    : "sms";
            string message = String.Format("Enter {0} authentication code for {1}.", type, targetUri);

            Trace.WriteLine("   prompting user for authentication code.");

            return ModalPromptForPassword(targetUri, message, username, out authenticationCode);
        }

        private static bool GithubCredentialPrompt(TargetUri targetUri, out string username, out string password)
        {
            // ReadConsole 32768 fail, 32767 ok
            // @linquize [https://github.com/Microsoft/Git-Credential-Manager-for-Windows/commit/a62b9a19f430d038dcd85a610d97e5f763980f85]
            const int BufferReadSize = 16 * 1024;

            Debug.Assert(targetUri != null);

            Trace.WriteLine("Program::GithubCredentialPrompt");

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
                    throw new Win32Exception(error, "Unable to determine console mode (" + NativeMethods.Win32Error.GetText(error) + ").");
                }

                Trace.WriteLine("   console mode = " + consoleMode);

                // instruct the user as to what they are expected to do
                buffer.Append("Please enter your GitHub credentials for ")
                      .Append(targetUri)
                      .AppendLine();
                if (!NativeMethods.WriteConsole(stdout, buffer, (uint)buffer.Length, out written, IntPtr.Zero))
                {
                    int error = Marshal.GetLastWin32Error();
                    throw new Win32Exception(error, "Unable to write to standard output (" + NativeMethods.Win32Error.GetText(error) + ").");
                }

                // clear the buffer for the next operation
                buffer.Clear();

                // prompt the user for the username wanted
                buffer.Append("username: ");
                if (!NativeMethods.WriteConsole(stdout, buffer, (uint)buffer.Length, out written, IntPtr.Zero))
                {
                    int error = Marshal.GetLastWin32Error();
                    throw new Win32Exception(error, "Unable to write to standard output (" + NativeMethods.Win32Error.GetText(error) + ").");
                }

                // clear the buffer for the next operation
                buffer.Clear();

                // read input from the user
                if (!NativeMethods.ReadConsole(stdin, buffer, BufferReadSize, out read, IntPtr.Zero))
                {
                    int error = Marshal.GetLastWin32Error();
                    throw new Win32Exception(error, "Unable to read from standard input (" + NativeMethods.Win32Error.GetText(error) + ").");
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
                        throw new Win32Exception(error, "Unable to set console mode (" + NativeMethods.Win32Error.GetText(error) + ").");
                    }

                    Trace.WriteLine("   console mode = " + consoleMode2);

                    // prompt the user for password
                    buffer.Append("password: ");
                    if (!NativeMethods.WriteConsole(stdout, buffer, (uint)buffer.Length, out written, IntPtr.Zero))
                    {
                        int error = Marshal.GetLastWin32Error();
                        throw new Win32Exception(error, "Unable to write to standard output (" + NativeMethods.Win32Error.GetText(error) + ").");
                    }

                    // clear the buffer for the next operation
                    buffer.Clear();

                    // read input from the user
                    if (!NativeMethods.ReadConsole(stdin, buffer, BufferReadSize, out read, IntPtr.Zero))
                    {
                        int error = Marshal.GetLastWin32Error();
                        throw new Win32Exception(error, "Unable to read from standard input (" + NativeMethods.Win32Error.GetText(error) + ").");
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
                        throw new Win32Exception(error, "Unable to set console mode (" + NativeMethods.Win32Error.GetText(error) + ").");
                    }

                    Trace.WriteLine("   console mode = " + consoleMode);
                }
            }

            return username != null
                && password != null;
        }

        private static bool GithubAuthCodePrompt(TargetUri targetUri, GithubAuthenticationResultType resultType, string username, out string authenticationCode)
        {
            // ReadConsole 32768 fail, 32767 ok
            // @linquize [https://github.com/Microsoft/Git-Credential-Manager-for-Windows/commit/a62b9a19f430d038dcd85a610d97e5f763980f85]
            const int BufferReadSize = 16 * 1024;

            Debug.Assert(targetUri != null);

            Trace.WriteLine("Program::GithubAuthCodePrompt");

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

                Trace.WriteLine("   2fa type = " + type);

                buffer.AppendLine()
                      .Append("authcode (")
                      .Append(type)
                      .Append("): ");

                if (!NativeMethods.WriteConsole(stdout, buffer, (uint)buffer.Length, out written, IntPtr.Zero))
                {
                    int error = Marshal.GetLastWin32Error();
                    throw new Win32Exception(error, "Unable to write to standard output (" + NativeMethods.Win32Error.GetText(error) + ").");
                }
                buffer.Clear();

                // read input from the user
                if (!NativeMethods.ReadConsole(stdin, buffer, BufferReadSize, out read, IntPtr.Zero))
                {
                    int error = Marshal.GetLastWin32Error();
                    throw new Win32Exception(error, "Unable to read from standard input (" + NativeMethods.Win32Error.GetText(error) + ").");
                }

                authenticationCode = buffer.ToString(0, (int)read);
                authenticationCode = authenticationCode.Trim(NewLineChars);
            }

            return authenticationCode != null;
        }

        private static bool ModalPromptForCredentials(TargetUri targetUri, string message, out string username, out string password)
        {
            Debug.Assert(targetUri != null);
            Debug.Assert(message != null);

            Trace.WriteLine("Program::ModalPromptForCredemtials");

            NativeMethods.CredentialUiInfo credUiInfo = new NativeMethods.CredentialUiInfo
            {
                BannerArt = IntPtr.Zero,
                CaptionText = Title,
                MessageText = message,
                Parent = IntPtr.Zero,
                Size = Marshal.SizeOf(typeof(NativeMethods.CredentialUiInfo))
            };
            NativeMethods.CredentialUiWindowsFlags flags = NativeMethods.CredentialUiWindowsFlags.Generic;
            NativeMethods.CredentialPackFlags authPackage = NativeMethods.CredentialPackFlags.None;
            IntPtr packedAuthBufferPtr = IntPtr.Zero;
            IntPtr inBufferPtr = IntPtr.Zero;
            uint packedAuthBufferSize = 0;
            bool saveCredentials = false;
            int inBufferSize = 0;

            return ModalPromptDisplayDialog(ref credUiInfo,
                                            ref authPackage,
                                            packedAuthBufferPtr,
                                            packedAuthBufferSize,
                                            inBufferPtr,
                                            inBufferSize,
                                            saveCredentials,
                                            flags,
                                            out username,
                                            out password);
        }

        private static bool ModalPromptForCredentials(TargetUri targetUri, out string username, out string password)
        {
            Trace.WriteLine("Program::ModalPromptForCredemtials");

            string message = String.Format("Enter your credentials for {0}.", targetUri);
            return ModalPromptForCredentials(targetUri, message, out username, out password);
        }

        private static bool ModalPromptForPassword(TargetUri targetUri, string message, string username, out string password)
        {
            Debug.Assert(targetUri != null);
            Debug.Assert(message != null);
            Debug.Assert(username != null);

            Trace.WriteLine("Program::ModalPromptForPassword");

            NativeMethods.CredentialUiInfo credUiInfo = new NativeMethods.CredentialUiInfo
            {
                BannerArt = IntPtr.Zero,
                CaptionText = Title,
                MessageText = message,
                Parent = IntPtr.Zero,
                Size = Marshal.SizeOf(typeof(NativeMethods.CredentialUiInfo))
            };
            NativeMethods.CredentialUiWindowsFlags flags = NativeMethods.CredentialUiWindowsFlags.Generic;
            NativeMethods.CredentialPackFlags authPackage = NativeMethods.CredentialPackFlags.None;
            IntPtr packedAuthBufferPtr = IntPtr.Zero;
            IntPtr inBufferPtr = IntPtr.Zero;
            uint packedAuthBufferSize = 0;
            bool saveCredentials = false;
            int inBufferSize = 0;

            try
            {
                int error;

                // execute with `null` to determine buffer size
                // always returns false when determining size, only fail if `inBufferSize` looks bad
                NativeMethods.CredPackAuthenticationBuffer(flags: authPackage,
                                                           username: username,
                                                           password: String.Empty,
                                                           packedCredentials: inBufferPtr,
                                                           packedCredentialsSize: ref inBufferSize);
                if (inBufferSize <= 0)
                {
                    error = Marshal.GetLastWin32Error();
                    Trace.WriteLine("   unable to determine credential buffer size (" + NativeMethods.Win32Error.GetText(error) + ").");

                    username = null;
                    password = null;

                    return false;
                }

                inBufferPtr = Marshal.AllocHGlobal(inBufferSize);

                if (!NativeMethods.CredPackAuthenticationBuffer(flags: authPackage,
                                                                username: username,
                                                                password: String.Empty,
                                                                packedCredentials: inBufferPtr,
                                                                packedCredentialsSize: ref inBufferSize))
                {
                    error = Marshal.GetLastWin32Error();
                    Trace.WriteLine("   unable to write to credential buffer (" + NativeMethods.Win32Error.GetText(error) + ").");

                    username = null;
                    password = null;

                    return false;
                }

                return ModalPromptDisplayDialog(ref credUiInfo,
                                                ref authPackage,
                                                packedAuthBufferPtr,
                                                packedAuthBufferSize,
                                                inBufferPtr,
                                                inBufferSize,
                                                saveCredentials,
                                                flags,
                                                out username,
                                                out password);
            }
            finally
            {
                if (inBufferPtr != IntPtr.Zero)
                {
                    Marshal.FreeCoTaskMem(inBufferPtr);
                }
            }
        }

        private static bool ModalPromptDisplayDialog(
            ref NativeMethods.CredentialUiInfo credUiInfo,
            ref NativeMethods.CredentialPackFlags authPackage,
            IntPtr packedAuthBufferPtr,
            uint packedAuthBufferSize,
            IntPtr inBufferPtr,
            int inBufferSize,
            bool saveCredentials,
            NativeMethods.CredentialUiWindowsFlags flags,
            out string username,
            out string password)
        {
            Trace.WriteLine("Program::ModalPromptDisplayDialog");

            int error;

            try
            {
                // open a standard Windows authentication dialog to acquire username + password credentials
                if ((error = NativeMethods.CredUIPromptForWindowsCredentials(credInfo: ref credUiInfo,
                                                                             authError: 0,
                                                                             authPackage: ref authPackage,
                                                                             inAuthBuffer: inBufferPtr,
                                                                             inAuthBufferSize: (uint)inBufferSize,
                                                                             outAuthBuffer: out packedAuthBufferPtr,
                                                                             outAuthBufferSize: out packedAuthBufferSize,
                                                                             saveCredentials: ref saveCredentials,
                                                                             flags: flags)) != NativeMethods.Win32Error.Success)
                {
                    Trace.WriteLine("   credential prompt failed (" + NativeMethods.Win32Error.GetText(error) + ").");

                    username = null;
                    password = null;

                    return false;
                }

                // use `StringBuilder` references instead of string so that they can be written to
                StringBuilder usernameBuffer = new StringBuilder(512);
                StringBuilder domainBuffer = new StringBuilder(256);
                StringBuilder passwordBuffer = new StringBuilder(512);
                int usernameLen = usernameBuffer.Capacity;
                int passwordLen = passwordBuffer.Capacity;
                int domainLen = domainBuffer.Capacity;

                // unpack the result into locally useful data
                if (!NativeMethods.CredUnPackAuthenticationBuffer(flags: authPackage,
                                                                  authBuffer: packedAuthBufferPtr,
                                                                  authBufferSize: packedAuthBufferSize,
                                                                  username: usernameBuffer,
                                                                  maxUsernameLen: ref usernameLen,
                                                                  domainName: domainBuffer,
                                                                  maxDomainNameLen: ref domainLen,
                                                                  password: passwordBuffer,
                                                                  maxPasswordLen: ref passwordLen))
                {
                    username = null;
                    password = null;

                    error = Marshal.GetLastWin32Error();
                    Trace.WriteLine("   failed to unpack buffer (" + NativeMethods.Win32Error.GetText(error) + ").");

                    return false;
                }

                Trace.WriteLine("   successfully acquired credentials from user.");

                username = usernameBuffer.ToString();
                password = passwordBuffer.ToString();

                return true;
            }
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
