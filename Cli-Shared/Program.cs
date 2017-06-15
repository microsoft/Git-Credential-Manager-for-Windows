using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.Alm.Authentication;
using Bitbucket = Atlassian.Bitbucket.Authentication;
using Github = GitHub.Authentication;

namespace Microsoft.Alm.Cli
{
    partial class Program
    {
        public const string SourceUrl = "https://github.com/Microsoft/Git-Credential-Manager-for-Windows";
        public const string EventSource = "Git Credential Manager";

        internal const string ConfigAuthorityKey = "authority";
        internal const string ConfigHttpProxyKey = "httpProxy";
        internal const string ConfigInteractiveKey = "interactive";
        internal const string ConfigLoginHintKey = "loginhint";
        internal const string ConfigNamespaceKey = "namespace";
        internal const string ConfigPreserveCredentialsKey = "preserve";
        internal const string ConfigUseHttpPathKey = "useHttpPath";
        internal const string ConfigUseModalPromptKey = "modalPrompt";
        internal const string ConfigValidateKey = "validate";
        internal const string ConfigWritelogKey = "writelog";

        internal static readonly StringComparer ConfigKeyComparer = StringComparer.OrdinalIgnoreCase;
        internal static readonly StringComparer ConfigValueComparer = StringComparer.OrdinalIgnoreCase;

        internal const string EnvironAuthorityKey = "GCM_AUTHORITY";
        internal const string EnvironConfigNoLocalKey = "GCM_CONFIG_NOLOCAL";
        internal const string EnvironConfigNoSystemKey = "GCM_CONFIG_NOSYSTEM";
        internal const string EnvironHttpProxyKey = "GCM_HTTP_PROXY";
        internal const string EnvironHttpUserAgent = "GCM_HTTP_USER_AGENT";
        internal const string EnvironInteractiveKey = "GCM_INTERACTIVE";
        internal const string EnvironLoginHintKey = "GCM_LOGINHINT";
        internal const string EnvironModalPromptKey = "GCM_MODAL_PROMPT";
        internal const string EnvironNamespaceKey = "GCM_NAMESPACE";
        internal const string EnvironPreserveCredentialsKey = "GCM_PRESERVE_CREDS";
        internal const string EnvironValidateKey = "GCM_VALIDATE";
        internal const string EnvironWritelogKey = "GCM_WRITELOG";

        internal const string EnvironConfigTraceKey = Git.Trace.EnvironmentVariableKey;

        internal static readonly StringComparer EnvironKeyComparer = StringComparer.OrdinalIgnoreCase;

        internal const string ConfigPrefix = "credential";
        internal const string SecretsNamespace = "git";

        internal static readonly char[] NewLineChars = Environment.NewLine.ToCharArray();

        internal static readonly VstsTokenScope VstsCredentialScope = VstsTokenScope.CodeWrite | VstsTokenScope.PackagingRead;
        internal static readonly Github.TokenScope GitHubCredentialScope = Github.TokenScope.Gist | Github.TokenScope.Repo;

        internal static BasicCredentialPromptDelegate _basicCredentialPrompt = ConsoleFunctions.CredentialPrompt;
        internal static BitbucketCredentialPromptDelegate _bitbucketCredentialPrompt = BitbucketFunctions.CredentialPrompt;
        internal static BitbucketOAuthPromptDelegate _bitbucketOauthPrompt = BitbucketFunctions.OAuthPrompt;
        internal static CreateAuthenticationDelegate _createAuthentication = CommonFunctions.CreateAuthentication;
        internal static DeleteCredentialsDelegate _deleteCredentials = CommonFunctions.DeleteCredentials;
        internal static DieExceptionDelegate _dieException = CommonFunctions.DieException;
        internal static DieMessageDelegate _dieMessage = CommonFunctions.DieMessage;
        internal static EnableTraceLoggingDelegate _enableTraceLogging = CommonFunctions.EnableTraceLogging;
        internal static EnableTraceLoggingFileDelegate _enableTraceLoggingFile = CommonFunctions.EnableTraceLoggingFile;
        internal static ExitDelegate _exit = ConsoleFunctions.Exit;
        internal static GitHubAuthCodePromptDelegate _gitHubAuthCodePrompt = GitHubFunctions.AuthCodePrompt;
        internal static GitHubCredentialPromptDelegate _gitHubCredentialPrompt = GitHubFunctions.CredentialPrompt;
        internal static LoadOperationArgumentsDelegate _loadOperationArguments = CommonFunctions.LoadOperationArguments;
        internal static LogEventDelegate _logEvent = CommonFunctions.LogEvent;
        internal static ModalPromptDisplayDialogDelegate _modalPromptDisplayDialog = DialogFunctions.DisplayModal;
        internal static ModalPromptForCredentialsDelegate _modalPromptForCredentials = DialogFunctions.CredentialPrompt;
        internal static ModalPromptForPasswordDelegate _modalPromptForPassword = DialogFunctions.PasswordPrompt;
        internal static PrintArgsDelegate _printArgs = CommonFunctions.PrintArgs;
        internal static QueryCredentialsDelegate _queryCredentials = CommonFunctions.QueryCredentials;
        internal static ReadKeyDelegate _readKey = ConsoleFunctions.ReadKey;
        internal static StandardHandleIsTtyDelegate _standardHandleIsTty = ConsoleFunctions.StandardHandleIsTty;
        internal static TryReadBooleanDelegate _tryReadBoolean = CommonFunctions.TryReadBoolean;
        internal static TryReadStringDelegate _tryReadString = CommonFunctions.TryReadString;
        internal static WriteDelegate _write = ConsoleFunctions.Write;
        internal static WriteLineDelegate _writeLine = ConsoleFunctions.WriteLine;

        private static string _executablePath;
        private static string _location;
        private static string _name;
        private static Version _version;

        /// <summary>
        /// Gets the path to the executable.
        /// </summary>
        public static string ExecutablePath
        {
            get
            {
                if (_executablePath == null)
                {
                    LoadAssemblyInformation();
                }
                return _executablePath;
            }
        }

        /// <summary>
        /// Gets the directory where the executable is contained.
        /// </summary>
        public static string Location
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

        /// <summary>
        /// Gets the name of the application.
        /// </summary>
        public static string Name
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

        /// <summary>
        /// <para>Gets <see langword="true"/> if stderr is a TTY device; otherwise <see langword="false"/>.</para>
        /// <para>
        /// If TTY, then it is very likely stderr is attached to a console and interactions with the
        /// user are possible.
        /// </para>
        /// </summary>
        public static bool StandardErrorIsTty
        {
            get { return StandardHandleIsTty(NativeMethods.StandardHandleType.Error); }
        }

        /// <summary>
        /// <para>Gets <see langword="true"/> if stdin is a TTY device; otherwise <see langword="false"/>.</para>
        /// <para>
        /// If TTY, then it is very likely stdin is attached to a console and interactions with the
        /// user are possible.
        /// </para>
        /// </summary>
        public static bool StandardInputIsTty
        {
            get { return StandardHandleIsTty(NativeMethods.StandardHandleType.Input); }
        }

        /// <summary>
        /// <para>Gets <see langword="true"/> if stdout is a TTY device; otherwise <see langword="false"/>.</para>
        /// <para>
        /// If TTY, then it is very likely stdout is attached to a console and interaction with the
        /// user are possible.
        /// </para>
        /// </summary>
        public static bool StandardOutputIsTty
        {
            get { return StandardHandleIsTty(NativeMethods.StandardHandleType.Output); }
        }

        /// <summary>
        /// Gets the version of the application.
        /// </summary>
        public static Version Version
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

        internal static void Die(Exception exception,
                                [CallerFilePath] string path = "",
                                [CallerLineNumber] int line = 0,
                                [CallerMemberName] string name = "")
            => _dieException(exception, path, line, name);

        internal static void Die(string message,
                                [CallerFilePath] string path = "",
                                [CallerLineNumber] int line = 0,
                                [CallerMemberName] string name = "")
            => _dieMessage(message, path, line, name);

        internal static void Exit(int exitcode = 0,
                                  string message = null,
                                 [CallerFilePath] string path = "",
                                 [CallerLineNumber] int line = 0,
                                 [CallerMemberName] string name = "")
            => _exit(exitcode, message, path, line, name);

        internal static void LoadOperationArguments(OperationArguments operationArguments)
            => _loadOperationArguments(operationArguments);

        internal static void LogEvent(string message, EventLogEntryType eventType)
            => _logEvent(message, eventType);

        internal static Credential QueryCredentials(OperationArguments operationArguments)
            => _queryCredentials(operationArguments);

        internal static ConsoleKeyInfo ReadKey(bool intercept = true)
            => _readKey(intercept);

        internal static void Write(string message)
            => _write(message);

        internal static void WriteLine(string message = null)
            => _writeLine(message);

        private static Credential BasicCredentialPrompt(TargetUri targetUri)
        {
            string message = "Please enter your credentials for ";
            return BasicCredentialPrompt(targetUri, message);
        }

        private static Credential BasicCredentialPrompt(TargetUri targetUri, string titleMessage)
            => _basicCredentialPrompt(targetUri, titleMessage);

        private static Task<BaseAuthentication> CreateAuthentication(OperationArguments operationArguments)
            => _createAuthentication(operationArguments);

        private static void DeleteCredentials(OperationArguments operationArguments)
            => _deleteCredentials(operationArguments);

        private static void PrintArgs(string[] args)
            => _printArgs(args);

        [Conditional("DEBUG")]
        private static void EnableDebugTrace()
        {
            // use the stderr stream for the trace as stdout is used in the cross-process
            // communications protocol
            Git.Trace.AddListener(Console.Error);
        }

        private static void EnableTraceLogging(OperationArguments operationArguments)
            => _enableTraceLogging(operationArguments);

        private static void EnableTraceLogging(OperationArguments operationArguments, string logFilePath)
            => _enableTraceLoggingFile(operationArguments, logFilePath);

        private static bool BitbucketCredentialPrompt(string titleMessage, TargetUri targetUri, out string username, out string password)
            => _bitbucketCredentialPrompt(titleMessage, targetUri, out username, out password);

        private static bool BitbucketOAuthPrompt(string title, TargetUri targetUri, Bitbucket.AuthenticationResultType resultType, string username)
            => _bitbucketOauthPrompt(title, targetUri, resultType, username);

        private static bool GitHubAuthCodePrompt(TargetUri targetUri, Github.GitHubAuthenticationResultType resultType, string username, out string authenticationCode)
            => _gitHubAuthCodePrompt(targetUri, resultType, username, out authenticationCode);

        private static bool GitHubCredentialPrompt(TargetUri targetUri, out string username, out string password)
            => _gitHubCredentialPrompt(targetUri, out username, out password);

        private static void LoadAssemblyInformation()
        {
            var assembly = System.Reflection.Assembly.GetEntryAssembly();
            var asseName = assembly.GetName();

            _executablePath = assembly.Location;
            _location = Path.GetDirectoryName(_executablePath);
            _name = asseName.Name;
            _version = asseName.Version;
        }

        private static bool ModalPromptDisplayDialog(ref NativeMethods.CredentialUiInfo credUiInfo,
                                                     ref NativeMethods.CredentialPackFlags authPackage,
                                                     IntPtr packedAuthBufferPtr,
                                                     uint packedAuthBufferSize,
                                                     IntPtr inBufferPtr,
                                                     int inBufferSize,
                                                     bool saveCredentials,
                                                     NativeMethods.CredentialUiWindowsFlags flags,
                                                     out string username,
                                                     out string password)
            => _modalPromptDisplayDialog(ref credUiInfo,
                                         ref authPackage,
                                         packedAuthBufferPtr,
                                         packedAuthBufferSize,
                                         inBufferPtr,
                                         inBufferSize,
                                         saveCredentials,
                                         flags,
                                         out username,
                                         out password);

        private static Credential ModalPromptForCredentials(TargetUri targetUri, string message)
            => _modalPromptForCredentials(targetUri, message);

        private static Credential ModalPromptForCredentials(TargetUri targetUri)
        {
            string message = string.Format("Enter your credentials for {0}.", targetUri.ToString(port: true, path: true));

            if (!string.IsNullOrEmpty(targetUri.ActualUri.UserInfo))
            {
                string username = targetUri.ActualUri.UserInfo;

                if (!targetUri.ActualUri.UserEscaped)
                {
                    username = Uri.UnescapeDataString(username);
                }

                return ModalPromptForPassword(targetUri, message, username);
            }

            return ModalPromptForCredentials(targetUri, message);
        }

        private static Credential ModalPromptForPassword(TargetUri targetUri, string message, string username)
            => _modalPromptForPassword(targetUri, message, username);

        private static void PrintVersion()
        {
            WriteLine($"{Title} version {Version.ToString(3)}");
        }

        private static bool StandardHandleIsTty(NativeMethods.StandardHandleType handleType)
            => _standardHandleIsTty(handleType);

        private static bool TryReadBoolean(OperationArguments operationArguments, string configKey, string environKey, out bool? value)
            => _tryReadBoolean(operationArguments, configKey, environKey, out value);

        private static bool TryReadString(OperationArguments operationArguments, string configKey, string environKey, out string value)
            => _tryReadString(operationArguments, configKey, environKey, out value);
    }
}
