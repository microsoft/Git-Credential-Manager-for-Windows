using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Bitbucket.Authentication;
using GitHub.Authentication;
using Microsoft.Alm.Authentication;
using Microsoft.Alm.Git;

namespace Microsoft.Alm.Cli
{
    internal partial class Program
    {
        public const string SourceUrl = "https://github.com/Microsoft/Git-Credential-Manager-for-Windows";
        public const string EventSource = "Git Credential Manager";

        internal const string ConfigAuthortyKey = "authority";
        internal const string ConfigHttpProxyKey = "httpProxy";
        internal const string ConfigInteractiveKey = "interactive";
        internal const string ConfigNamespaceKey = "namespace";
        internal const string ConfigPreserveCredentialsKey = "preserve";
        internal const string ConfigUseHttpPathKey = "useHttpPath";
        internal const string ConfigUseModalPromptKey = "modalPrompt";
        internal const string ConfigValidateKey = "validate";
        internal const string ConfigWritelogKey = "writelog";

        internal const string EnvironInteractiveKey = "GCM_INTERACTIVE";
        internal const string EnvironPreserveCredentialsKey = "GCM_PRESERVE_CREDS";
        internal const string EnvironModalPromptKey = "GCM_MODAL_PROMPT";
        internal const string EnvironValidateKey = "GCM_VALIDATE";
        internal const string EnvironWritelogKey = "GCM_WRITELOG";
        internal const string EnvironConfigNoLocalKey = "GCM_CONFIG_NOLOCAL";
        internal const string EnvironConfigNoSystemKey = "GCM_CONFIG_NOSYSTEM";

        private const string ConfigPrefix = "credential";
        private const string SecretsNamespace = "git";

        internal static readonly StringComparer ConfigKeyComparer = StringComparer.OrdinalIgnoreCase;
        internal static readonly StringComparer ConfigValueComparer = StringComparer.InvariantCultureIgnoreCase;

        internal static readonly StringComparer EnvironKeyComparer = StringComparer.OrdinalIgnoreCase;

        internal static readonly char[] NewLineChars = Environment.NewLine.ToCharArray();

        private static readonly VstsTokenScope VstsCredentialScope = VstsTokenScope.CodeWrite |
                                                                     VstsTokenScope.PackagingRead;

        private static readonly GitHubTokenScope GitHubCredentialScope = GitHubTokenScope.Gist | GitHubTokenScope.Repo;
        private static string _executablePath;
        private static string _location;
        private static string _name;
        private static Version _version;

        /// <summary>
        ///     Gets the path to the executable.
        /// </summary>
        public static string ExecutablePath
        {
            get
            {
                if (_executablePath == null)
                    LoadAssemblyInformation();
                return _executablePath;
            }
        }

        /// <summary>
        ///     Gets the directory where the executable is contained.
        /// </summary>
        public static string Location
        {
            get
            {
                if (_location == null)
                    LoadAssemblyInformation();
                return _location;
            }
        }

        /// <summary>
        ///     Gets the name of the application.
        /// </summary>
        public static string Name
        {
            get
            {
                if (_name == null)
                    LoadAssemblyInformation();
                return _name;
            }
        }

        /// <summary>
        ///     <para>Gets <see langword="true" /> if stderr is a TTY device; otherwise <see langword="false" />.</para>
        ///     <para>If TTY, then it is very likely stderr is attached to a console and ineractions with the user are possible.</para>
        /// </summary>
        public static bool StandardErrorIsTty
        {
            get { return StandardHandleIsTty(NativeMethods.StandardHandleType.Error); }
        }

        /// <summary>
        ///     <para>Gets <see langword="true" /> if stdin is a TTY device; otherwise <see langword="false" />.</para>
        ///     <para>If TTY, then it is very likely stdin is attached to a console and ineractions with the user are possible.</para>
        /// </summary>
        public static bool StandardInputIsTty
        {
            get { return StandardHandleIsTty(NativeMethods.StandardHandleType.Input); }
        }

        /// <summary>
        ///     <para>Gets <see langword="true" /> if stdout is a TTY device; otherwise <see langword="false" />.</para>
        ///     <para>If TTY, then it is very likely stdout is attached to a console and ineractions with the user are possible.</para>
        /// </summary>
        public static bool StandardOutputIsTty
        {
            get { return StandardHandleIsTty(NativeMethods.StandardHandleType.Output); }
        }

        /// <summary>
        ///     Gets the version of the application.
        /// </summary>
        internal static Version Version
        {
            get
            {
                if (_version == null)
                    LoadAssemblyInformation();
                return _version;
            }
        }

        internal static void LogEvent(string message, EventLogEntryType eventType)
        {
            /*** commented out due to UAC issues which require a proper installer to work around ***/

            try
            {
                Trace.WriteLine("Program::LogEvent");

                if (!EventLog.SourceExists(EventSource))
                {
                    EventLog.CreateEventSource(EventSource, "Application");

                    Trace.WriteLine("   event source created");
                }

                EventLog.WriteEntry(EventSource, message, eventType);

                Trace.WriteLine("   " + eventType + "event written");
            }
            catch
            {
                Trace.WriteLine("   failed ot log event.");
            }
        }

        private static bool BasicCredentialPrompt(TargetUri targetUri, string titleMessage, out string username,
            out string password)
        {
            // ReadConsole 32768 fail, 32767 ok
            // @linquize [https://github.com/Microsoft/Git-Credential-Manager-for-Windows/commit/a62b9a19f430d038dcd85a610d97e5f763980f85]
            const int BufferReadSize = 16*1024;

            Debug.Assert(targetUri != null);

            Trace.WriteLine("Program::BasicCredentialPrompt");

            username = null;
            password = null;

            if (!StandardErrorIsTty || !StandardInputIsTty)
            {
                Trace.WriteLine("  not a tty detected, abandoning prompt.");
                return false;
            }

            titleMessage = titleMessage ?? "Please enter your credentials for ";

            var buffer = new StringBuilder(BufferReadSize);
            uint read = 0;
            uint written = 0;

            var fileAccessFlags = NativeMethods.FileAccess.GenericRead | NativeMethods.FileAccess.GenericWrite;
            var fileAttributes = NativeMethods.FileAttributes.Normal;
            var fileCreationDisposition = NativeMethods.FileCreationDisposition.OpenExisting;
            var fileShareFlags = NativeMethods.FileShare.Read | NativeMethods.FileShare.Write;

            using (
                var stdout = NativeMethods.CreateFile(NativeMethods.ConsoleOutName, fileAccessFlags, fileShareFlags,
                    IntPtr.Zero, fileCreationDisposition, fileAttributes, IntPtr.Zero))
            {
                using (
                    var stdin = NativeMethods.CreateFile(NativeMethods.ConsoleInName, fileAccessFlags, fileShareFlags,
                        IntPtr.Zero, fileCreationDisposition, fileAttributes, IntPtr.Zero))
                {
                    // read the current console mode
                    NativeMethods.ConsoleMode consoleMode;
                    if (!NativeMethods.GetConsoleMode(stdin, out consoleMode))
                    {
                        var error = Marshal.GetLastWin32Error();
                        throw new Win32Exception(error,
                            "Unable to determine console mode (" + NativeMethods.Win32Error.GetText(error) + ").");
                    }

                    Trace.WriteLine("   console mode = " + consoleMode);

                    // instruct the user as to what they are expected to do
                    buffer.Append(titleMessage)
                        .Append(targetUri)
                        .AppendLine();
                    if (!NativeMethods.WriteConsole(stdout, buffer, (uint) buffer.Length, out written, IntPtr.Zero))
                    {
                        var error = Marshal.GetLastWin32Error();
                        throw new Win32Exception(error,
                            "Unable to write to standard output (" + NativeMethods.Win32Error.GetText(error) + ").");
                    }

                    // clear the buffer for the next operation
                    buffer.Clear();

                    // prompt the user for the username wanted
                    buffer.Append("username: ");
                    if (!NativeMethods.WriteConsole(stdout, buffer, (uint) buffer.Length, out written, IntPtr.Zero))
                    {
                        var error = Marshal.GetLastWin32Error();
                        throw new Win32Exception(error,
                            "Unable to write to standard output (" + NativeMethods.Win32Error.GetText(error) + ").");
                    }

                    // clear the buffer for the next operation
                    buffer.Clear();

                    // read input from the user
                    if (!NativeMethods.ReadConsole(stdin, buffer, BufferReadSize, out read, IntPtr.Zero))
                    {
                        var error = Marshal.GetLastWin32Error();
                        throw new Win32Exception(error,
                            "Unable to read from standard input (" + NativeMethods.Win32Error.GetText(error) + ").");
                    }

                    // record input from the user into local storage, stripping any eol chars
                    username = buffer.ToString(0, (int) read);
                    username = username.Trim(Environment.NewLine.ToCharArray());

                    // clear the buffer for the next operation
                    buffer.Clear();

                    // set the console mode to current without echo input
                    var consoleMode2 = consoleMode ^ NativeMethods.ConsoleMode.EchoInput;

                    try
                    {
                        if (!NativeMethods.SetConsoleMode(stdin, consoleMode2))
                        {
                            var error = Marshal.GetLastWin32Error();
                            throw new Win32Exception(error,
                                "Unable to set console mode (" + NativeMethods.Win32Error.GetText(error) + ").");
                        }

                        Trace.WriteLine("   console mode = " + consoleMode2);

                        // prompt the user for password
                        buffer.Append("password: ");
                        if (!NativeMethods.WriteConsole(stdout, buffer, (uint) buffer.Length, out written, IntPtr.Zero))
                        {
                            var error = Marshal.GetLastWin32Error();
                            throw new Win32Exception(error,
                                "Unable to write to standard output (" + NativeMethods.Win32Error.GetText(error) + ").");
                        }

                        // clear the buffer for the next operation
                        buffer.Clear();

                        // read input from the user
                        if (!NativeMethods.ReadConsole(stdin, buffer, BufferReadSize, out read, IntPtr.Zero))
                        {
                            var error = Marshal.GetLastWin32Error();
                            throw new Win32Exception(error,
                                "Unable to read from standard input (" + NativeMethods.Win32Error.GetText(error) + ").");
                        }

                        // record input from the user into local storage, stripping any eol chars
                        password = buffer.ToString(0, (int) read);
                        password = password.Trim(Environment.NewLine.ToCharArray());
                    }
                    catch
                    {
                        throw;
                    }
                    finally
                    {
                        // restore the console mode to its original value
                        if (!NativeMethods.SetConsoleMode(stdin, consoleMode))
                        {
                            var error = Marshal.GetLastWin32Error();
                            throw new Win32Exception(error,
                                "Unable to set console mode (" + NativeMethods.Win32Error.GetText(error) + ").");
                        }

                        Trace.WriteLine("   console mode = " + consoleMode);
                    }
                }
            }

            return (username != null)
                   && (password != null);
        }

        private static BaseAuthentication CreateAuthentication(OperationArguments operationArguments)
        {
            Debug.Assert(operationArguments != null, "The operationArguments is null");
            Debug.Assert(operationArguments.TargetUri != null, "The operationArgument.TargetUri is null");

            Trace.WriteLine("Program::CreateAuthentication");

            var secretsNamespace = operationArguments.CustomNamespace ?? SecretsNamespace;
            var secrets = new SecretStore(secretsNamespace, null, null, Secret.UriToName);
            BaseAuthentication authority = null;

            switch (operationArguments.Authority)
            {
                case AuthorityType.Auto:
                    Trace.WriteLine("   detecting authority type");

                    // detect the authority
                    authority = BaseVstsAuthentication.GetAuthentication(operationArguments.TargetUri,
                                    VstsCredentialScope,
                                    secrets)
                                ?? GitHubAuthentication.GetAuthentication(operationArguments.TargetUri,
                                    GitHubCredentialScope,
                                    secrets,
                                    operationArguments.UseModalUi
                                        ? GitHub.Authentication.AuthenticationPrompts.CredentialModalPrompt
                                        : new GitHubAuthentication.AcquireCredentialsDelegate(GitHubCredentialPrompt),
                                    operationArguments.UseModalUi
                                        ? GitHub.Authentication.AuthenticationPrompts.AuthenticationCodeModalPrompt
                                        : new GitHubAuthentication.AcquireAuthenticationCodeDelegate(
                                            GitHubAuthCodePrompt),
                                    null)
                                ?? BitbucketAuthentication.GetAuthentication(operationArguments.TargetUri, secrets,
                                operationArguments.UseModalUi
                                        ? Bitbucket.Authentication.AuthenticationPrompts.CredentialModalPrompt
                                        : new BitbucketAuthentication.AcquireCredentialsDelegate(CredentialPrompt),
                                operationArguments.UseModalUi
                                        ? Bitbucket.Authentication.AuthenticationPrompts.AuthenticationOAuthModalPrompt
                                        : new BitbucketAuthentication.AcquireAuthenticationOAuthDelegate(OAuthPrompt));

                    if (authority != null)
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
                        else if (authority is GitHubAuthentication)
                        {
                            operationArguments.Authority = AuthorityType.GitHub;
                            goto case AuthorityType.GitHub;
                        }
                        else if (authority is BitbucketAuthentication)
                        {
                            operationArguments.Authority = AuthorityType.Bitbucket;
                            goto case AuthorityType.Bitbucket;
                        }

                    operationArguments.Authority = AuthorityType.Basic;
                    goto case AuthorityType.Basic;

                case AuthorityType.AzureDirectory:
                    Trace.WriteLine("   authority is Azure Directory");

                    var tenantId = Guid.Empty;
                    // return the allocated authority or a generic AAD backed VSTS authentication object
                    return authority ?? new VstsAadAuthentication(Guid.Empty, VstsCredentialScope, secrets);

                case AuthorityType.Basic:
                default:
                    Trace.WriteLine("   authority is basic");

                    // return a generic username + password authentication object
                    return authority ?? new BasicAuthentication(secrets);

                case AuthorityType.GitHub:
                    Trace.WriteLine("   authority it GitHub");

                    // return a GitHub authentication object
                    return authority ?? new GitHubAuthentication(GitHubCredentialScope,
                               secrets,
                               operationArguments.UseModalUi
                                   ? GitHub.Authentication.AuthenticationPrompts.CredentialModalPrompt
                                   : new GitHubAuthentication.AcquireCredentialsDelegate(GitHubCredentialPrompt),
                               operationArguments.UseModalUi
                                   ? GitHub.Authentication.AuthenticationPrompts.AuthenticationCodeModalPrompt
                                   : new GitHubAuthentication.AcquireAuthenticationCodeDelegate(GitHubAuthCodePrompt),
                               null);

                case AuthorityType.Bitbucket:
                    Trace.WriteLine("   authority is Bitbucket");

                    // return a Bitbucket authentication object
                    return authority ?? new BitbucketAuthentication(/*GitHubCredentialScope,*/
                               secrets,
                               operationArguments.UseModalUi
                                   ? Bitbucket.Authentication.AuthenticationPrompts.CredentialModalPrompt
                                   : new BitbucketAuthentication.AcquireCredentialsDelegate(CredentialPrompt),
                                operationArguments.UseModalUi
                                        ? Bitbucket.Authentication.AuthenticationPrompts.AuthenticationOAuthModalPrompt
                                        : new BitbucketAuthentication.AcquireAuthenticationOAuthDelegate(OAuthPrompt));

                case AuthorityType.MicrosoftAccount:
                    Trace.WriteLine("   authority is Microsoft Live");

                    // return the allocated authority or a generic MSA backed VSTS authentication object
                    return authority ?? new VstsMsaAuthentication(VstsCredentialScope, secrets);
            }
        }

        private static void DeleteCredentials(OperationArguments operationArguments)
        {
            if (ReferenceEquals(operationArguments, null))
                throw new ArgumentNullException("operationArguments");

            Trace.WriteLine("Program::DeleteCredentials");
            Trace.WriteLine("   targetUri = " + operationArguments.TargetUri);

            var authentication = CreateAuthentication(operationArguments);

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
                    var vstsAuth = authentication as BaseVstsAuthentication;
                    vstsAuth.DeleteCredentials(operationArguments.TargetUri);
                    break;

                case AuthorityType.GitHub:
                    Trace.WriteLine("   deleting GitHub credentials");
                    var ghAuth = authentication as GitHubAuthentication;
                    ghAuth.DeleteCredentials(operationArguments.TargetUri);
                    break;

                case AuthorityType.Bitbucket:
                    Trace.WriteLine("   deleting Bitbucket credentials");
                    var bbAuth = authentication as BitbucketAuthentication;
                    bbAuth.DeleteCredentials(operationArguments.TargetUri, operationArguments.CredUsername);
                    break;
            }
        }

        private static void LoadOperationArguments(OperationArguments operationArguments)
        {
            Debug.Assert(operationArguments != null, "The operationsArguments parameter is null.");

            Trace.WriteLine("Program::LoadOperationArguments");

            if (operationArguments.TargetUri == null)
            {
                Console.Error.WriteLine("fatal: no host information, unable to continue.");
                Environment.Exit(-1);
            }

            var envars = operationArguments.EnvironmentVariables;

            string value;
            operationArguments.UseConfigLocal = !envars.TryGetValue(EnvironConfigNoLocalKey, out value)
                                                || string.IsNullOrWhiteSpace(value)
                                                || ConfigValueComparer.Equals(value, "0")
                                                || ConfigValueComparer.Equals(value, "false")
                                                || ConfigValueComparer.Equals(value, "no");

            operationArguments.UseConfigSystem = !envars.TryGetValue(EnvironConfigNoSystemKey, out value)
                                                 || string.IsNullOrWhiteSpace(value)
                                                 || ConfigValueComparer.Equals(value, "0")
                                                 || ConfigValueComparer.Equals(value, "false")
                                                 || ConfigValueComparer.Equals(value, "no");

            // load/re-load the Git configuration after setting the use local/system config values
            operationArguments.LoadConfiguration();

            var config = operationArguments.GitConfiguration;
            Configuration.Entry entry;

            // look for authority config settings
            if (config.TryGetEntry(ConfigPrefix, operationArguments.QueryUri, ConfigAuthortyKey, out entry))
            {
                Trace.WriteLine("   " + ConfigAuthortyKey + " = " + entry.Value);

                if (ConfigKeyComparer.Equals(entry.Value, "MSA")
                    || ConfigKeyComparer.Equals(entry.Value, "Microsoft")
                    || ConfigKeyComparer.Equals(entry.Value, "MicrosoftAccount")
                    || ConfigKeyComparer.Equals(entry.Value, "Live")
                    || ConfigKeyComparer.Equals(entry.Value, "LiveConnect")
                    || ConfigKeyComparer.Equals(entry.Value, "LiveID"))
                    operationArguments.Authority = AuthorityType.MicrosoftAccount;
                else if (ConfigKeyComparer.Equals(entry.Value, "AAD")
                         || ConfigKeyComparer.Equals(entry.Value, "Azure")
                         || ConfigKeyComparer.Equals(entry.Value, "AzureDirectory"))
                    operationArguments.Authority = AuthorityType.AzureDirectory;
                else if (ConfigKeyComparer.Equals(entry.Value, "Integrated")
                         || ConfigKeyComparer.Equals(entry.Value, "Windows")
                         || ConfigKeyComparer.Equals(entry.Value, "TFS")
                         || ConfigKeyComparer.Equals(entry.Value, "Kerberos")
                         || ConfigKeyComparer.Equals(entry.Value, "NTLM")
                         || ConfigKeyComparer.Equals(entry.Value, "SSO"))
                    operationArguments.Authority = AuthorityType.Integrated;
                else if (ConfigKeyComparer.Equals(entry.Value, "GitHub"))
                    operationArguments.Authority = AuthorityType.GitHub;
                else
                    operationArguments.Authority = AuthorityType.Basic;
            }

            // look for interactivity config settings
            string interativeValue = null;
            if (envars.TryGetValue(EnvironInteractiveKey, out interativeValue)
                && !string.IsNullOrWhiteSpace(interativeValue))
            {
                Trace.WriteLine("   " + EnvironInteractiveKey + " = " + interativeValue);
            }
            else if (config.TryGetEntry(ConfigPrefix, operationArguments.QueryUri, ConfigInteractiveKey, out entry))
            {
                Trace.WriteLine("   " + ConfigInteractiveKey + " = " + entry.Value);

                interativeValue = entry.Value;
            }

            if (!string.IsNullOrWhiteSpace(interativeValue))
                if (ConfigKeyComparer.Equals(interativeValue, "always")
                    || ConfigKeyComparer.Equals(interativeValue, "true")
                    || ConfigKeyComparer.Equals(interativeValue, "force"))
                    operationArguments.Interactivity = Interactivity.Always;
                else if (ConfigKeyComparer.Equals(interativeValue, "never")
                         || ConfigKeyComparer.Equals(interativeValue, "false"))
                    operationArguments.Interactivity = Interactivity.Never;

            // look for credential validation config settings
            bool? validateCredentials;
            if (TryReadBoolean(operationArguments, ConfigValidateKey, EnvironValidateKey,
                operationArguments.ValidateCredentials, out validateCredentials))
                operationArguments.ValidateCredentials = validateCredentials.Value;

            // look for write log config settings
            bool? writeLog;
            if (TryReadBoolean(operationArguments, ConfigWritelogKey, EnvironWritelogKey, operationArguments.WriteLog,
                out writeLog))
                operationArguments.WriteLog = writeLog.Value;

            // look for modal prompt config settings
            bool? useModalUi = null;
            if (TryReadBoolean(operationArguments, ConfigUseModalPromptKey, EnvironModalPromptKey,
                operationArguments.UseModalUi, out useModalUi))
                operationArguments.UseModalUi = useModalUi.Value;

            // look for credential preservation config settings
            bool? preserveCredentials;
            if (TryReadBoolean(operationArguments, ConfigPreserveCredentialsKey, EnvironPreserveCredentialsKey,
                operationArguments.PreserveCredentials, out preserveCredentials))
                operationArguments.PreserveCredentials = preserveCredentials.Value;

            // look for http path usage config settings
            bool? useHttpPath;
            if (TryReadBoolean(operationArguments, ConfigUseHttpPathKey, null, operationArguments.UseHttpPath,
                out useHttpPath))
                operationArguments.UseHttpPath = useHttpPath.Value;

            // look for http proxy config settings
            if (config.TryGetEntry(ConfigPrefix, operationArguments.QueryUri, ConfigHttpProxyKey, out entry)
                || config.TryGetEntry("http", operationArguments.QueryUri, "proxy", out entry))
            {
                Trace.WriteLine("   " + ConfigHttpProxyKey + " = " + entry.Value);

                operationArguments.SetProxy(entry.Value);
            }

            // look for custom namespace config settings
            if (config.TryGetEntry(ConfigPrefix, operationArguments.QueryUri, ConfigNamespaceKey, out entry))
            {
                Trace.WriteLine("   " + ConfigNamespaceKey + " = " + entry.Value);

                operationArguments.CustomNamespace = entry.Value;
            }
        }

        [Conditional("DEBUG")]
        private static void EnableDebugTrace()
        {
            // use the stderr stream for the trace as stdout is used in the cross-process communications protocol
            Trace.Listeners.Add(new ConsoleTraceListener(true));
        }

        private static void EnableTraceLogging(OperationArguments operationArguments)
        {
            Trace.WriteLine("Program::EnableTraceLogging");

            if (operationArguments.WriteLog)
            {
                Trace.WriteLine("   trace logging enabled");

                string gitConfigPath;
                if (Where.GitLocalConfig(out gitConfigPath))
                {
                    Trace.WriteLine("   git local config found at " + gitConfigPath);

                    var gitDirPath = Path.GetDirectoryName(gitConfigPath);

                    if (Directory.Exists(gitDirPath))
                        EnableTraceLogging(operationArguments, gitDirPath);
                }
                else if (Where.GitGlobalConfig(out gitConfigPath))
                {
                    Trace.WriteLine("   git global config found at " + gitConfigPath);

                    var homeDirPath = Path.GetDirectoryName(gitConfigPath);

                    if (Directory.Exists(homeDirPath))
                        EnableTraceLogging(operationArguments, homeDirPath);
                }
            }
        }

        private static void EnableTraceLogging(OperationArguments operationArguments, string logFilePath)
        {
            const int LogFileMaxLength = 8*1024*1024; // 8 MB

            var logFileName = Path.Combine(logFilePath, Path.ChangeExtension(ConfigPrefix, ".log"));

            var logFileInfo = new FileInfo(logFileName);
            if (logFileInfo.Exists && (logFileInfo.Length > LogFileMaxLength))
                for (var i = 1; i < int.MaxValue; i++)
                {
                    var moveName = string.Format("{0}{1:000}.log", ConfigPrefix, i);
                    var movePath = Path.Combine(logFilePath, moveName);

                    if (!File.Exists(movePath))
                    {
                        logFileInfo.MoveTo(movePath);
                        break;
                    }
                }

            Trace.WriteLine("   trace log destination is " + logFilePath);

            var listener = new TextWriterTraceListener(logFilePath, logFileName);
            Trace.Listeners.Add(listener);

            // write a small header to help with identifying new log entries
            listener.WriteLine(Environment.NewLine);
            listener.WriteLine(string.Format("Log Start ({0:u})", DateTimeOffset.Now));
            listener.WriteLine(string.Format("Microsoft {0} version {1}", Title, Version.ToString(3)));
        }

        private static bool GitHubAuthCodePrompt(TargetUri targetUri, GitHubAuthenticationResultType resultType,
            string username, out string authenticationCode)
        {
            // ReadConsole 32768 fail, 32767 ok
            // @linquize [https://github.com/Microsoft/Git-Credential-Manager-for-Windows/commit/a62b9a19f430d038dcd85a610d97e5f763980f85]
            const int BufferReadSize = 16*1024;

            Debug.Assert(targetUri != null);

            Trace.WriteLine("Program::GitHubAuthCodePrompt");

            var buffer = new StringBuilder(BufferReadSize);
            uint read = 0;
            uint written = 0;

            authenticationCode = null;

            var fileAccessFlags = NativeMethods.FileAccess.GenericRead | NativeMethods.FileAccess.GenericWrite;
            var fileAttributes = NativeMethods.FileAttributes.Normal;
            var fileCreationDisposition = NativeMethods.FileCreationDisposition.OpenExisting;
            var fileShareFlags = NativeMethods.FileShare.Read | NativeMethods.FileShare.Write;

            using (
                var stdout = NativeMethods.CreateFile(NativeMethods.ConsoleOutName, fileAccessFlags, fileShareFlags,
                    IntPtr.Zero, fileCreationDisposition, fileAttributes, IntPtr.Zero))
            {
                using (
                    var stdin = NativeMethods.CreateFile(NativeMethods.ConsoleInName, fileAccessFlags, fileShareFlags,
                        IntPtr.Zero, fileCreationDisposition, fileAttributes, IntPtr.Zero))
                {
                    var type = resultType == GitHubAuthenticationResultType.TwoFactorApp
                        ? "app"
                        : "sms";

                    Trace.WriteLine("   2fa type = " + type);

                    buffer.AppendLine()
                        .Append("authcode (")
                        .Append(type)
                        .Append("): ");

                    if (!NativeMethods.WriteConsole(stdout, buffer, (uint) buffer.Length, out written, IntPtr.Zero))
                    {
                        var error = Marshal.GetLastWin32Error();
                        throw new Win32Exception(error,
                            "Unable to write to standard output (" + NativeMethods.Win32Error.GetText(error) + ").");
                    }
                    buffer.Clear();

                    // read input from the user
                    if (!NativeMethods.ReadConsole(stdin, buffer, BufferReadSize, out read, IntPtr.Zero))
                    {
                        var error = Marshal.GetLastWin32Error();
                        throw new Win32Exception(error,
                            "Unable to read from standard input (" + NativeMethods.Win32Error.GetText(error) + ").");
                    }

                    authenticationCode = buffer.ToString(0, (int) read);
                    authenticationCode = authenticationCode.Trim(NewLineChars);
                }
            }

            return authenticationCode != null;
        }

        private static bool OAuthPrompt(string title, TargetUri targetUri, BitbucketAuthenticationResultType resultType,
            string username)
        {
            const int BufferReadSize = 16 * 1024;

            Debug.Assert(targetUri != null);

            Trace.WriteLine("Program::BitbucketOAuthPrompt");

            var buffer = new StringBuilder(BufferReadSize);
            uint read = 0;
            uint written = 0;

            string accessToken = null;

            var fileAccessFlags = NativeMethods.FileAccess.GenericRead | NativeMethods.FileAccess.GenericWrite;
            var fileAttributes = NativeMethods.FileAttributes.Normal;
            var fileCreationDisposition = NativeMethods.FileCreationDisposition.OpenExisting;
            var fileShareFlags = NativeMethods.FileShare.Read | NativeMethods.FileShare.Write;

            using (
                var stdout = NativeMethods.CreateFile(NativeMethods.ConsoleOutName, fileAccessFlags, fileShareFlags,
                    IntPtr.Zero, fileCreationDisposition, fileAttributes, IntPtr.Zero))
            {
                using (
                    var stdin = NativeMethods.CreateFile(NativeMethods.ConsoleInName, fileAccessFlags, fileShareFlags,
                        IntPtr.Zero, fileCreationDisposition, fileAttributes, IntPtr.Zero))
                {
                    Trace.WriteLine("   OAuth");

                    buffer.AppendLine()
                        .Append(title)
                        .Append(" OAuth Access Token: ");

                    if (!NativeMethods.WriteConsole(stdout, buffer, (uint)buffer.Length, out written, IntPtr.Zero))
                    {
                        var error = Marshal.GetLastWin32Error();
                        throw new Win32Exception(error,
                            "Unable to write to standard output (" + NativeMethods.Win32Error.GetText(error) + ").");
                    }
                    buffer.Clear();

                    // read input from the user
                    if (!NativeMethods.ReadConsole(stdin, buffer, BufferReadSize, out read, IntPtr.Zero))
                    {
                        var error = Marshal.GetLastWin32Error();
                        throw new Win32Exception(error,
                            "Unable to read from standard input (" + NativeMethods.Win32Error.GetText(error) + ").");
                    }

                    accessToken = buffer.ToString(0, (int)read);
                    accessToken = accessToken.Trim(NewLineChars);
                }
            }
            return accessToken != null;
        }

        private static bool GitHubCredentialPrompt(TargetUri targetUri, out string username, out string password)
        {
            const string TitleMessage = "Please enter your GitHub credentials for ";

            Trace.WriteLine("Program::GitHubCredentialPrompt");

            return BasicCredentialPrompt(targetUri, TitleMessage, out username, out password);
        }

        public static bool CredentialPrompt(string titleMessage, TargetUri targetUri, out string username, out string password)
        {
            Trace.WriteLine("Program::CredentialPrompt");

            return BasicCredentialPrompt(targetUri, titleMessage, out username, out password);
        }

        private static void LoadAssemblyInformation()
        {
            var assembly = Assembly.GetEntryAssembly();
            var asseName = assembly.GetName();

            _executablePath = assembly.Location;
            _location = Path.GetDirectoryName(_executablePath);
            _name = asseName.Name;
            _version = asseName.Version;
        }

        private static bool ModalPromptForCredentials(TargetUri targetUri, string message, out string username,
            out string password)
        {
            Debug.Assert(targetUri != null);
            Debug.Assert(message != null);

            Trace.WriteLine("Program::ModalPromptForCredemtials");

            var credUiInfo = new NativeMethods.CredentialUiInfo
            {
                BannerArt = IntPtr.Zero,
                CaptionText = Title,
                MessageText = message,
                Parent = IntPtr.Zero,
                Size = Marshal.SizeOf(typeof(NativeMethods.CredentialUiInfo))
            };
            var flags = NativeMethods.CredentialUiWindowsFlags.Generic;
            var authPackage = NativeMethods.CredentialPackFlags.None;
            var packedAuthBufferPtr = IntPtr.Zero;
            var inBufferPtr = IntPtr.Zero;
            uint packedAuthBufferSize = 0;
            var saveCredentials = false;
            var inBufferSize = 0;

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

            var message = string.Format("Enter your credentials for {0}.", targetUri);
            return ModalPromptForCredentials(targetUri, message, out username, out password);
        }

        private static bool ModalPromptForPassword(TargetUri targetUri, string message, string username,
            out string password)
        {
            Debug.Assert(targetUri != null);
            Debug.Assert(message != null);
            Debug.Assert(username != null);

            Trace.WriteLine("Program::ModalPromptForPassword");

            var credUiInfo = new NativeMethods.CredentialUiInfo
            {
                BannerArt = IntPtr.Zero,
                CaptionText = Title,
                MessageText = message,
                Parent = IntPtr.Zero,
                Size = Marshal.SizeOf(typeof(NativeMethods.CredentialUiInfo))
            };
            var flags = NativeMethods.CredentialUiWindowsFlags.Generic;
            var authPackage = NativeMethods.CredentialPackFlags.None;
            var packedAuthBufferPtr = IntPtr.Zero;
            var inBufferPtr = IntPtr.Zero;
            uint packedAuthBufferSize = 0;
            var saveCredentials = false;
            var inBufferSize = 0;

            try
            {
                int error;

                // execute with `null` to determine buffer size
                // always returns false when determining size, only fail if `inBufferSize` looks bad
                NativeMethods.CredPackAuthenticationBuffer(authPackage,
                    username,
                    string.Empty,
                    inBufferPtr,
                    ref inBufferSize);
                if (inBufferSize <= 0)
                {
                    error = Marshal.GetLastWin32Error();
                    Trace.WriteLine("   unable to determine credential buffer size (" +
                                    NativeMethods.Win32Error.GetText(error) + ").");

                    username = null;
                    password = null;

                    return false;
                }

                inBufferPtr = Marshal.AllocHGlobal(inBufferSize);

                if (!NativeMethods.CredPackAuthenticationBuffer(authPackage,
                    username,
                    string.Empty,
                    inBufferPtr,
                    ref inBufferSize))
                {
                    error = Marshal.GetLastWin32Error();
                    Trace.WriteLine("   unable to write to credential buffer (" +
                                    NativeMethods.Win32Error.GetText(error) + ").");

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
                    Marshal.FreeCoTaskMem(inBufferPtr);
            }
        }

        private static void PrintConfigurationHelp()
        {
            Console.Out.WriteLine("Git Configuration Options:");
            Console.Out.WriteLine();
            Console.Out.WriteLine("  " + ConfigAuthortyKey + "    Defines the type of authentication to be used.");
            Console.Out.WriteLine("               Supports Auto, Basic, AAD, MSA, and Integrated.");
            Console.Out.WriteLine("               Default is Auto.");
            Console.Out.WriteLine();
            Console.Out.WriteLine("      `git config --global credential.microsoft.visualstudio.com." +
                                  ConfigAuthortyKey + " AAD`");
            Console.Out.WriteLine();
            Console.Out.WriteLine("  " + ConfigInteractiveKey +
                                  "  Specifies if user can be prompted for credentials or not.");
            Console.Out.WriteLine("               Supports Auto, Always, or Never. Defaults to Auto.");
            Console.Out.WriteLine("               Only used by AAD, MSA, and GitHub authority.");
            Console.Out.WriteLine();
            Console.Out.WriteLine("      `git config --global credential.microsoft.visualstudio.com." +
                                  ConfigInteractiveKey + " never`");
            Console.Out.WriteLine();
            Console.Out.WriteLine("  " + ConfigUseModalPromptKey +
                                  "  Forces authentication to use a modal dialog instead of");
            Console.Out.WriteLine("               asking for credentials at the command prompt.");
            Console.Out.WriteLine("               Defaults to TRUE.");
            Console.Out.WriteLine();
            Console.Out.WriteLine("      `git config --global credential." + ConfigUseModalPromptKey + " true`");
            Console.Out.WriteLine();
            Console.Out.WriteLine("  " + ConfigValidateKey +
                                  "     Causes validation of credentials before supplying them");
            Console.Out.WriteLine("               to Git. Invalid credentials get a refresh attempt");
            Console.Out.WriteLine("               before failing. Incurs some minor overhead.");
            Console.Out.WriteLine("               Defaults to TRUE. Ignored by Basic authority.");
            Console.Out.WriteLine();
            Console.Out.WriteLine("      `git config --global credential.microsoft.visualstudio.com." +
                                  ConfigValidateKey + " false`");
            Console.Out.WriteLine();
            Console.Out.WriteLine("  " + ConfigPreserveCredentialsKey +
                                  "     Prevents the deletion of credentials even when they are");
            Console.Out.WriteLine(
                "               reported as invalid by Git. Can lead to lockout situations once credentials");
            Console.Out.WriteLine("               expire and until those credentials are manually removed.");
            Console.Out.WriteLine("               Defaults to FALSE.");
            Console.Out.WriteLine();
            Console.Out.WriteLine("      `git config --global credential.visualstudio.com." +
                                  ConfigPreserveCredentialsKey + " true`");
            Console.Out.WriteLine();
            Console.Out.WriteLine("  " + ConfigUseHttpPathKey +
                                  "     Causes the path portion of the target URI to considered meaningful.");
            Console.Out.WriteLine(
                "               By default the path portion of the target URI is ignore, if this is set to true");
            Console.Out.WriteLine(
                "               the path is considered meaningful and credentials will be store for each path.");
            Console.Out.WriteLine("               Defaults to FALSE.");
            Console.Out.WriteLine();
            Console.Out.WriteLine("      `git config --global credential.bitbucket.com." + ConfigUseHttpPathKey +
                                  " true`");
            Console.Out.WriteLine();
            Console.Out.WriteLine("  " + ConfigHttpProxyKey +
                                  "     Causes the proxy value to be considered when evaluating.");
            Console.Out.WriteLine(
                "               credential target information. A proxy setting should established if use of a");
            Console.Out.WriteLine("               proxy is required to interact with Git remotes.");
            Console.Out.WriteLine("               The value should the URL of the proxy server.");
            Console.Out.WriteLine();
            Console.Out.WriteLine("      `git config --global credential.github.com." + ConfigUseHttpPathKey +
                                  " https://myproxy:8080`");
            Console.Out.WriteLine();
            Console.Out.WriteLine("  " + ConfigWritelogKey +
                                  "     Enables trace logging of all activities. Logs are written to");
            Console.Out.WriteLine("               the local .git/ folder at the root of the repository.");
            Console.Out.WriteLine("               Defaults to FALSE.");
            Console.Out.WriteLine();
            Console.Out.WriteLine("      `git config --global credential." + ConfigWritelogKey + " true`");
            Console.Out.WriteLine();
            Console.Out.WriteLine("  " + ConfigNamespaceKey + "     Sets the namespace for stored credentials.");
            Console.Out.WriteLine(
                "               By default the GCM uses the 'git' namespace for all stored credentials, setting this");
            Console.Out.WriteLine(
                "               configuration value allows for control of the namespace used globally, or per host.");
            Console.Out.WriteLine();
            Console.Out.WriteLine("      `git config --global credential." + ConfigNamespaceKey + " name`");
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
        }

        private static void PrintVersion()
        {
            Trace.WriteLine("Program::PrintVersion");

            Console.Out.WriteLine("{0} version {1}", Title, Version.ToString(3));
        }

        private static void QueryCredentials(OperationArguments operationArguments)
        {
            const string AadMsaAuthFailureMessage = "Logon failed, use ctrl+c to cancel basic credential prompt.";
            const string GitHubAuthFailureMessage = "Logon failed, use ctrl+c to cancel basic credential prompt.";
            // TODO bitbucket
            const string BitbucketAuthFailureMessage = "TODO review wording: Logon failed, use ctrl+c to cancel basic credential prompt.";

            if (ReferenceEquals(operationArguments, null))
                throw new ArgumentNullException("operationArguments");
            if (ReferenceEquals(operationArguments.TargetUri, null))
                throw new ArgumentNullException("operationArguments.TargetUri");

            Trace.WriteLine("Program::QueryCredentials");
            Trace.WriteLine("   targetUri = " + operationArguments.TargetUri);

            var authentication = CreateAuthentication(operationArguments);
            Credential credentials = null;

            switch (operationArguments.Authority)
            {
                default:
                case AuthorityType.Basic:
                    if ((credentials = authentication.GetCredentials(operationArguments.TargetUri)) != null)
                    {
                        Trace.WriteLine("   credentials found");
                        operationArguments.SetCredentials(credentials);
                    }
                    else if (operationArguments.Interactivity != Interactivity.Never)
                    {
                        string username;
                        string password;

                        // either use modal UI or command line to query for credentials
                        if ((operationArguments.UseModalUi
                             && ModalPromptForCredentials(operationArguments.TargetUri, out username, out password))
                            || (!operationArguments.UseModalUi
                                && BasicCredentialPrompt(operationArguments.TargetUri, null, out username, out password)))
                        {
                            Trace.WriteLine("   credentials found");
                            // set the credentials object
                            // no need to save the credentials explicitly, as Git will call back
                            // with a store command if the credentials are valid.
                            credentials = new Credential(username, password);
                            operationArguments.SetCredentials(credentials);
                        }
                    }
                    break;

                case AuthorityType.AzureDirectory:
                    var aadAuth = authentication as VstsAadAuthentication;

                    Task.Run(async () =>
                    {
                        // attempt to get cached creds -> refresh creds -> non-interactive logon -> interactive logon
                        // note that AAD "credentials" are always scoped access tokens
                        if (((operationArguments.Interactivity != Interactivity.Always)
                             && ((credentials = aadAuth.GetCredentials(operationArguments.TargetUri)) != null)
                             && (!operationArguments.ValidateCredentials
                                 || await aadAuth.ValidateCredentials(operationArguments.TargetUri, credentials)))
                            || ((operationArguments.Interactivity != Interactivity.Always)
                                &&
                                ((credentials = await aadAuth.NoninteractiveLogon(operationArguments.TargetUri, true)) !=
                                 null)
                                && (!operationArguments.ValidateCredentials
                                    || await aadAuth.ValidateCredentials(operationArguments.TargetUri, credentials)))
                            || ((operationArguments.Interactivity != Interactivity.Never)
                                &&
                                ((credentials = await aadAuth.InteractiveLogon(operationArguments.TargetUri, true)) !=
                                 null)
                                && (!operationArguments.ValidateCredentials
                                    || await aadAuth.ValidateCredentials(operationArguments.TargetUri, credentials))))
                        {
                            Trace.WriteLine("   credentials found");
                            operationArguments.SetCredentials(credentials);
                            LogEvent(
                                "Azure Directory credentials for " + operationArguments.TargetUri +
                                " successfully retrieved.", EventLogEntryType.SuccessAudit);
                        }
                        else
                        {
                            Console.Error.WriteLine(AadMsaAuthFailureMessage);
                            LogEvent(
                                "Failed to retrieve Azure Directory credentials for " + operationArguments.TargetUri +
                                ".", EventLogEntryType.FailureAudit);
                        }
                    }).Wait();
                    break;

                case AuthorityType.MicrosoftAccount:
                    var msaAuth = authentication as VstsMsaAuthentication;

                    Task.Run(async () =>
                    {
                        // attempt to get cached creds -> refresh creds -> interactive logon
                        // note that MSA "credentials" are always scoped access tokens
                        if (((operationArguments.Interactivity != Interactivity.Always)
                             && ((credentials = msaAuth.GetCredentials(operationArguments.TargetUri)) != null)
                             && (!operationArguments.ValidateCredentials
                                 || await msaAuth.ValidateCredentials(operationArguments.TargetUri, credentials)))
                            || ((operationArguments.Interactivity != Interactivity.Never)
                                &&
                                ((credentials = await msaAuth.InteractiveLogon(operationArguments.TargetUri, true)) !=
                                 null)
                                && (!operationArguments.ValidateCredentials
                                    || await msaAuth.ValidateCredentials(operationArguments.TargetUri, credentials))))
                        {
                            Trace.WriteLine("   credentials found");
                            operationArguments.SetCredentials(credentials);
                            LogEvent(
                                "Microsoft Live credentials for " + operationArguments.TargetUri +
                                " successfully retrieved.", EventLogEntryType.SuccessAudit);
                        }
                        else
                        {
                            Console.Error.WriteLine(AadMsaAuthFailureMessage);
                            LogEvent(
                                "Failed to retrieve Microsoft Live credentials for " + operationArguments.TargetUri +
                                ".", EventLogEntryType.FailureAudit);
                        }
                    }).Wait();
                    break;

                case AuthorityType.GitHub:
                    var ghAuth = authentication as GitHubAuthentication;

                    Task.Run(async () =>
                    {
                        if (((operationArguments.Interactivity != Interactivity.Always)
                             && ((credentials = ghAuth.GetCredentials(operationArguments.TargetUri)) != null)
                             && (!operationArguments.ValidateCredentials
                                 || await ghAuth.ValidateCredentials(operationArguments.TargetUri, credentials)))
                            || ((operationArguments.Interactivity != Interactivity.Never)
                                && ((credentials = await ghAuth.InteractiveLogon(operationArguments.TargetUri)) != null)
                                && (!operationArguments.ValidateCredentials
                                    || await ghAuth.ValidateCredentials(operationArguments.TargetUri, credentials))))
                        {
                            Trace.WriteLine("   credentials found");
                            operationArguments.SetCredentials(credentials);
                            LogEvent(
                                "GitHub credentials for " + operationArguments.TargetUri + " successfully retrieved.",
                                EventLogEntryType.SuccessAudit);
                        }
                        else
                        {
                            Console.Error.WriteLine(GitHubAuthFailureMessage);
                            LogEvent("Failed to retrieve GitHub credentials for " + operationArguments.TargetUri + ".",
                                EventLogEntryType.FailureAudit);
                        }
                    }).Wait();
                    break;

                case AuthorityType.Bitbucket:
                    var bbcAuth = authentication as BitbucketAuthentication;

                    Task.Run(async () =>
                    {
                        if (((operationArguments.Interactivity != Interactivity.Always)
                             && ((credentials = bbcAuth.GetCredentials(operationArguments.TargetUri, operationArguments.CredUsername)) != null)
                             && (!operationArguments.ValidateCredentials
                                 || await bbcAuth.ValidateCredentials(operationArguments.TargetUri, operationArguments.CredUsername, credentials)))
                            || ((operationArguments.Interactivity != Interactivity.Never)
                                && ((credentials = await bbcAuth.InteractiveLogon(operationArguments.TargetUri, operationArguments.CredUsername)) != null)
                                && (!operationArguments.ValidateCredentials
                                    || await bbcAuth.ValidateCredentials(operationArguments.TargetUri, operationArguments.CredUsername, credentials))))
                        {
                            Trace.WriteLine("   credentials found");
                            var c2 = new Credential(operationArguments.CredUsername, credentials.Password);
                            operationArguments.SetCredentials(c2);
                            LogEvent(
                                "Bitbucket credentials for " + operationArguments.TargetUri + " successfully retrieved.",
                                EventLogEntryType.SuccessAudit);
                        }
                        else
                        {
                            Console.Error.WriteLine(BitbucketAuthFailureMessage);
                            LogEvent("Failed to retrieve Bitbucket credentials for " + operationArguments.TargetUri + ".",
                                EventLogEntryType.FailureAudit);
                        }
                    }).Wait();
                    break;

                case AuthorityType.Integrated:
                    credentials = new Credential(string.Empty, string.Empty);
                    operationArguments.SetCredentials(credentials);
                    break;
            }
        }

        private static bool StandardHandleIsTty(NativeMethods.StandardHandleType handleType)
        {
            var standardHandle = NativeMethods.GetStdHandle(NativeMethods.StandardHandleType.Output);
            var handleFileType = NativeMethods.GetFileType(standardHandle);
            return handleFileType == NativeMethods.FileType.Char;
        }

        private static bool TryReadBoolean(OperationArguments operationArguments, string configKey, string environKey,
            bool defaultValue, out bool? value)
        {
            if (ReferenceEquals(operationArguments, null))
                throw new ArgumentNullException(nameof(operationArguments));
            if (ReferenceEquals(configKey, null))
                throw new ArgumentNullException(nameof(configKey));

            Trace.WriteLine("Program::TryReadBoolean");

            var config = operationArguments.GitConfiguration;
            var envars = operationArguments.EnvironmentVariables;

            var entry = new Configuration.Entry();
            value = null;

            string valueString = null;
            if (!string.IsNullOrWhiteSpace(environKey)
                && envars.TryGetValue(environKey, out valueString))
                Trace.WriteLine("   " + environKey + " = " + valueString);

            if (!string.IsNullOrWhiteSpace(valueString)
                || config.TryGetEntry(ConfigPrefix, operationArguments.QueryUri, configKey, out entry))
            {
                Trace.WriteLine("   " + configKey + " = " + entry.Value);
                valueString = entry.Value;
            }

            if (!string.IsNullOrWhiteSpace(valueString))
            {
                var result = defaultValue;
                if (bool.TryParse(valueString, out result))
                {
                    value = result;
                }
                else
                {
                    if (ConfigValueComparer.Equals(valueString, "no"))
                        value = false;
                    else if (ConfigValueComparer.Equals(valueString, "yes"))
                        value = true;
                }
            }

            return value.HasValue;
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
                if ((error = NativeMethods.CredUIPromptForWindowsCredentials(ref credUiInfo,
                        0,
                        ref authPackage,
                        inBufferPtr,
                        (uint) inBufferSize,
                        out packedAuthBufferPtr,
                        out packedAuthBufferSize,
                        ref saveCredentials,
                        flags)) != NativeMethods.Win32Error.Success)
                {
                    Trace.WriteLine("   credential prompt failed (" + NativeMethods.Win32Error.GetText(error) + ").");

                    username = null;
                    password = null;

                    return false;
                }

                // use `StringBuilder` references instead of string so that they can be written to
                var usernameBuffer = new StringBuilder(512);
                var domainBuffer = new StringBuilder(256);
                var passwordBuffer = new StringBuilder(512);
                var usernameLen = usernameBuffer.Capacity;
                var passwordLen = passwordBuffer.Capacity;
                var domainLen = domainBuffer.Capacity;

                // unpack the result into locally useful data
                if (!NativeMethods.CredUnPackAuthenticationBuffer(authPackage,
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
                    Marshal.FreeHGlobal(packedAuthBufferPtr);
            }
        }
    }
}