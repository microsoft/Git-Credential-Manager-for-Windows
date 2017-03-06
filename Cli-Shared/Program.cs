using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Alm.Authentication;
using Microsoft.Alm.Git;
using Microsoft.Win32.SafeHandles;
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

        private const string ConfigPrefix = "credential";
        private const string SecretsNamespace = "git";

        internal static readonly char[] NewLineChars = Environment.NewLine.ToCharArray();

        private static readonly VstsTokenScope VstsCredentialScope = VstsTokenScope.CodeWrite | VstsTokenScope.PackagingRead;
        private static readonly Github.TokenScope GitHubCredentialScope = Github.TokenScope.Gist | Github.TokenScope.Repo;

        internal static Action<Exception> _dieExceptionCallback = (Exception exception) =>
        {
            Git.Trace.WriteLine(exception.ToString());
            LogEvent(exception.ToString(), EventLogEntryType.Error);

            string message;
            if (!String.IsNullOrWhiteSpace(exception.Message))
            {
                message = $"{exception.GetType().Name} encountered.\n   {exception.Message}";
            }
            else
            {
                message = $"{exception.GetType().Name}  encountered.";
            }

            Die(message);
        };
        internal static Action<string> _dieMessageCallback = (string message) =>
        {
            Git.Trace.WriteLine($"fatal: {message}");
            Program.WriteLine($"fatal: {message}");

            Git.Trace.Flush();

            Environment.Exit(-1);
        };
        internal static Action<int, string> _exitCallback = (int exitcode, string message) =>
        {
            if (!String.IsNullOrWhiteSpace(message))
            {
                Git.Trace.WriteLine(message);
                Program.WriteLine(message);
            }

            Environment.Exit(exitcode);
        };

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
        /// <para>If TTY, then it is very likely stderr is attached to a console and ineractions with the user are possible.</para>
        /// </summary>
        public static bool StandardErrorIsTty
        {
            get { return StandardHandleIsTty(NativeMethods.StandardHandleType.Error); }
        }

        /// <summary>
        /// <para>Gets <see langword="true"/> if stdin is a TTY device; otherwise <see langword="false"/>.</para>
        /// <para>If TTY, then it is very likely stdin is attached to a console and ineractions with the user are possible.</para>
        /// </summary>
        public static bool StandardInputIsTty
        {
            get { return StandardHandleIsTty(NativeMethods.StandardHandleType.Input); }
        }

        /// <summary>
        /// <para>Gets <see langword="true"/> if stdout is a TTY device; otherwise <see langword="false"/>.</para>
        /// <para>If TTY, then it is very likely stdout is attached to a console and ineractions with the user are possible.</para>
        /// </summary>
        public static bool StandardOutputIsTty
        {
            get { return StandardHandleIsTty(NativeMethods.StandardHandleType.Output); }
        }

        /// <summary>
        /// Gets the version of the application.
        /// </summary>
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

        internal static void Die(Exception exception)
            => _dieExceptionCallback(exception);

        internal static void Die(string message)
            => _dieMessageCallback(message);

        internal static void Exit(int exitcode = 0, string message = null)
            => _exitCallback(exitcode, message);

        internal static void LogEvent(string message, EventLogEntryType eventType)
        {
            /*** try-squelch due to UAC issues which require a proper installer to work around ***/

            Git.Trace.WriteLine(message);

            try
            {
                EventLog.WriteEntry(EventSource, message, eventType);
            }
            catch { /* squelch */ }
        }

        internal static ConsoleKeyInfo ReadKey(bool intercept = true)
        {
            return (StandardInputIsTty)
                ? Console.ReadKey(intercept)
                : new ConsoleKeyInfo(' ', ConsoleKey.Escape, false, false, false);
        }

        internal static void Write(string message)
        {
            if (message == null)
                return;

            Console.Error.WriteLine(message);
        }

        internal static void WriteLine(string message = null)
        {
            Console.Error.WriteLine(message);
        }

        private static Credential BasicCredentialPrompt(TargetUri targetUri)
        {
            string message = "Please enter your credentials for ";
            return BasicCredentialPrompt(targetUri, message);
        }

        private static Credential BasicCredentialPrompt(TargetUri targetUri, string titleMessage)
        {
            // ReadConsole 32768 fail, 32767 ok
            // @linquize [https://github.com/Microsoft/Git-Credential-Manager-for-Windows/commit/a62b9a19f430d038dcd85a610d97e5f763980f85]
            const int BufferReadSize = 16 * 1024;

            Debug.Assert(targetUri != null);

            if (!StandardErrorIsTty || !StandardInputIsTty)
            {
                Git.Trace.WriteLine("not a tty detected, abandoning prompt.");
                return null;
            }

            titleMessage = titleMessage ?? "Please enter your credentials for ";

            StringBuilder buffer = new StringBuilder(BufferReadSize);
            uint read = 0;
            uint written = 0;

            NativeMethods.FileAccess fileAccessFlags = NativeMethods.FileAccess.GenericRead | NativeMethods.FileAccess.GenericWrite;
            NativeMethods.FileAttributes fileAttributes = NativeMethods.FileAttributes.Normal;
            NativeMethods.FileCreationDisposition fileCreationDisposition = NativeMethods.FileCreationDisposition.OpenExisting;
            NativeMethods.FileShare fileShareFlags = NativeMethods.FileShare.Read | NativeMethods.FileShare.Write;

            using (SafeFileHandle stdout = NativeMethods.CreateFile(NativeMethods.ConsoleOutName, fileAccessFlags, fileShareFlags, IntPtr.Zero, fileCreationDisposition, fileAttributes, IntPtr.Zero))
            using (SafeFileHandle stdin = NativeMethods.CreateFile(NativeMethods.ConsoleInName, fileAccessFlags, fileShareFlags, IntPtr.Zero, fileCreationDisposition, fileAttributes, IntPtr.Zero))
            {
                string username = null;
                string password = null;

                // read the current console mode
                NativeMethods.ConsoleMode consoleMode;
                if (!NativeMethods.GetConsoleMode(stdin, out consoleMode))
                {
                    int error = Marshal.GetLastWin32Error();
                    throw new Win32Exception(error, "Unable to determine console mode (" + NativeMethods.Win32Error.GetText(error) + ").");
                }

                Git.Trace.WriteLine($"console mode = '{consoleMode}'.");

                // instruct the user as to what they are expected to do
                buffer.Append(titleMessage)
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

                    Git.Trace.WriteLine($"console mode = '{consoleMode2}'.");

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
                    NativeMethods.SetConsoleMode(stdin, consoleMode);

                    Git.Trace.WriteLine($"console mode = '{consoleMode}'.");
                }

                if (username != null && password != null)
                    return new Credential(username, password);
            }

            return null;
        }

        private static async Task<BaseAuthentication> CreateAuthentication(OperationArguments operationArguments)
        {
            Debug.Assert(operationArguments != null, "The operationArguments is null");
            Debug.Assert(operationArguments.TargetUri != null, "The operationArgument.TargetUri is null");

            var secretsNamespace = operationArguments.CustomNamespace ?? SecretsNamespace;
            var secrets = new SecretStore(secretsNamespace, null, null, Secret.UriToName);
            BaseAuthentication authority = null;

            var basicCredentialCallback = (operationArguments.UseModalUi)
                    ? new AcquireCredentialsDelegate(Program.ModalPromptForCredentials)
                    : new AcquireCredentialsDelegate(Program.BasicCredentialPrompt);

            var bitbucketCredentialCallback = (operationArguments.UseModalUi)
                    ? Bitbucket.AuthenticationPrompts.CredentialModalPrompt
                    : new Bitbucket.Authentication.AcquireCredentialsDelegate(BitbucketCredentialPrompt);

            var bitbucketOauthCallback = (operationArguments.UseModalUi)
                    ? Bitbucket.AuthenticationPrompts.AuthenticationOAuthModalPrompt
                    : new Bitbucket.Authentication.AcquireAuthenticationOAuthDelegate(BitbucketOAuthPrompt);

            var githubCredentialCallback = (operationArguments.UseModalUi)
                    ? new Github.Authentication.AcquireCredentialsDelegate(Github.AuthenticationPrompts.CredentialModalPrompt)
                    : new Github.Authentication.AcquireCredentialsDelegate(Program.GitHubCredentialPrompt);

            var githubAuthcodeCallback = (operationArguments.UseModalUi)
                    ? new Github.Authentication.AcquireAuthenticationCodeDelegate(Github.AuthenticationPrompts.AuthenticationCodeModalPrompt)
                    : new Github.Authentication.AcquireAuthenticationCodeDelegate(Program.GitHubAuthCodePrompt);

            NtlmSupport basicNtlmSupport = NtlmSupport.Auto;

            switch (operationArguments.Authority)
            {
                case AuthorityType.Auto:
                    Git.Trace.WriteLine($"detecting authority type for '{operationArguments.TargetUri}'.");

                    // detect the authority
                    authority = await BaseVstsAuthentication.GetAuthentication(operationArguments.TargetUri,
                                                                         VstsCredentialScope,
                                                                         secrets)
                             ?? Github.Authentication.GetAuthentication(operationArguments.TargetUri,
                                                                       GitHubCredentialScope,
                                                                       secrets,
                                                                       githubCredentialCallback,
                                                                       githubAuthcodeCallback,
                                                                       null)
							?? Bitbucket.Authentication.GetAuthentication(operationArguments.TargetUri, secrets,
                                                                          bitbucketCredentialCallback,
                                                                          bitbucketOauthCallback);

                    if (authority != null)
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
                        else if (authority is Github.Authentication)
                        {
                            operationArguments.Authority = AuthorityType.GitHub;
                            goto case AuthorityType.GitHub;
                        }
						else if (authority is Bitbucket.Authentication)
                        {
                            operationArguments.Authority = AuthorityType.Bitbucket;
                            goto case AuthorityType.Bitbucket;
                        }
                    }
                    goto default;

                case AuthorityType.AzureDirectory:
                    Git.Trace.WriteLine($"authority for '{operationArguments.TargetUri}' is Azure Directory.");

                    Guid tenantId = Guid.Empty;

                    // Get the identity of the tenant.
                    var result = await BaseVstsAuthentication.DetectAuthority(operationArguments.TargetUri);

                    if (result.Key)
                    {
                        tenantId = result.Value;
                    }

                    // return the allocated authority or a generic AAD backed VSTS authentication object
                    return authority ?? new VstsAadAuthentication(tenantId, VstsCredentialScope, secrets);

                case AuthorityType.Basic:
                    // enforce basic authentication only
                    basicNtlmSupport = NtlmSupport.Never;
                    goto default;

                case AuthorityType.GitHub:
                    Git.Trace.WriteLine($"authority for '{operationArguments.TargetUri}' is GitHub.");

                    // return a GitHub authentication object
                    return authority ?? new Github.Authentication(GitHubCredentialScope,
                                                                 secrets,
                                                                 githubCredentialCallback,
                                                                 githubAuthcodeCallback,
                                                                 null);

                case AuthorityType.Bitbucket:
                    Git.Trace.WriteLine($"authority for '{operationArguments.TargetUri}'  is Bitbucket");

                    // return a Bitbucket authentication object
                    return authority ?? new Bitbucket.Authentication(secrets,
                                                                     bitbucketCredentialCallback,
                                                                     bitbucketOauthCallback);

                case AuthorityType.MicrosoftAccount:
                    Git.Trace.WriteLine($"authority for '{operationArguments.TargetUri}' is Microsoft Live.");

                    // return the allocated authority or a generic MSA backed VSTS authentication object
                    return authority ?? new VstsMsaAuthentication(VstsCredentialScope, secrets);

                case AuthorityType.Ntlm:
                    // enforce NTLM authentication only
                    basicNtlmSupport = NtlmSupport.Always;
                    goto default;

                default:
                    Git.Trace.WriteLine($"authority for '{operationArguments.TargetUri}' is basic with NTLM={basicNtlmSupport}.");

                    // return a generic username + password authentication object
                    return authority ?? new BasicAuthentication(secrets, basicNtlmSupport, basicCredentialCallback, null);
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        private static void DeleteCredentials(OperationArguments operationArguments)
        {
            if (ReferenceEquals(operationArguments, null))
                throw new ArgumentNullException("operationArguments");

            var task = Task.Run(async () => { return await CreateAuthentication(operationArguments); });

            BaseAuthentication authentication = task.Result;

            switch (operationArguments.Authority)
            {
                default:
                case AuthorityType.Basic:
                    Git.Trace.WriteLine($"deleting basic credentials for '{operationArguments.TargetUri}'.");
                    authentication.DeleteCredentials(operationArguments.TargetUri);
                    break;

                case AuthorityType.AzureDirectory:
                case AuthorityType.MicrosoftAccount:
                    Git.Trace.WriteLine($"deleting VSTS credentials for '{operationArguments.TargetUri}'.");
                    BaseVstsAuthentication vstsAuth = authentication as BaseVstsAuthentication;
                    vstsAuth.DeleteCredentials(operationArguments.TargetUri);
                    break;

                case AuthorityType.GitHub:
                    Git.Trace.WriteLine($"deleting GitHub credentials for '{operationArguments.TargetUri}'.");
                    Github.Authentication ghAuth = authentication as Github.Authentication;
                    ghAuth.DeleteCredentials(operationArguments.TargetUri);
                    break;

                case AuthorityType.Bitbucket:
                    Git.Trace.WriteLine($"deleting Bitbucket credentials for '{operationArguments.TargetUri}'.");
                    var bbAuth = authentication as Bitbucket.Authentication;
                    bbAuth.DeleteCredentials(operationArguments.TargetUri, operationArguments.CredUsername);
                    break;
            }
        }

        internal static void LoadOperationArguments(OperationArguments operationArguments)
        {
            if (operationArguments.TargetUri == null)
            {
                Die("No host information, unable to continue.");
            }

            string value;
            bool? yesno;

            if (TryReadBoolean(operationArguments, null, EnvironConfigNoLocalKey, out yesno))
            {
                operationArguments.UseConfigLocal = yesno.Value;
            }

            if (TryReadBoolean(operationArguments, null, EnvironConfigNoSystemKey, out yesno))
            {
                operationArguments.UseConfigSystem = yesno.Value;
            }

            // load/re-load the Git configuration after setting the use local/system config values
            operationArguments.LoadConfiguration();

            // if a user-agent has been specified in the environment, set it globally
            if (TryReadString(operationArguments, null, EnvironHttpUserAgent, out value))
            {
                Global.UserAgent = value;
            }

            // look for authority settings
            if (TryReadString(operationArguments, ConfigAuthorityKey, EnvironAuthorityKey, out value))
            {
                Git.Trace.WriteLine($"{ConfigAuthorityKey} = '{value}'.");

                if (ConfigKeyComparer.Equals(value, "MSA")
                        || ConfigKeyComparer.Equals(value, "Microsoft")
                        || ConfigKeyComparer.Equals(value, "MicrosoftAccount")
                        || ConfigKeyComparer.Equals(value, "Live")
                        || ConfigKeyComparer.Equals(value, "LiveConnect")
                        || ConfigKeyComparer.Equals(value, "LiveID"))
                {
                    operationArguments.Authority = AuthorityType.MicrosoftAccount;
                }
                else if (ConfigKeyComparer.Equals(value, "AAD")
                         || ConfigKeyComparer.Equals(value, "Azure")
                         || ConfigKeyComparer.Equals(value, "AzureDirectory"))
                {
                    operationArguments.Authority = AuthorityType.AzureDirectory;
                }
                else if (ConfigKeyComparer.Equals(value, "Integrated")
                         || ConfigKeyComparer.Equals(value, "Windows")
                         || ConfigKeyComparer.Equals(value, "TFS")
                         || ConfigKeyComparer.Equals(value, "Kerberos")
                         || ConfigKeyComparer.Equals(value, "NTLM")
                         || ConfigKeyComparer.Equals(value, "SSO"))
                {
                    operationArguments.Authority = AuthorityType.Ntlm;
                }
                else if (ConfigKeyComparer.Equals(value, "GitHub"))
                {
                    operationArguments.Authority = AuthorityType.GitHub;
                }
                else
                {
                    operationArguments.Authority = AuthorityType.Basic;
                }
            }

            // look for interactivity config settings
            if (TryReadString(operationArguments, ConfigInteractiveKey, EnvironInteractiveKey, out value))
            {
                Git.Trace.WriteLine($"{EnvironInteractiveKey} = '{value}'.");

                if (ConfigKeyComparer.Equals(value, "always")
                    || ConfigKeyComparer.Equals(value, "true")
                    || ConfigKeyComparer.Equals(value, "force"))
                {
                    operationArguments.Interactivity = Interactivity.Always;
                }
                else if (ConfigKeyComparer.Equals(value, "never")
                         || ConfigKeyComparer.Equals(value, "false"))
                {
                    operationArguments.Interactivity = Interactivity.Never;
                }
            }

            // look for credential validation config settings
            if (TryReadBoolean(operationArguments, ConfigValidateKey, EnvironValidateKey, out yesno))
            {
                operationArguments.ValidateCredentials = yesno.Value;
            }

            // look for write log config settings
            if (TryReadBoolean(operationArguments, ConfigWritelogKey, EnvironWritelogKey, out yesno))
            {
                operationArguments.WriteLog = yesno.Value;
            }

            // look for modal prompt config settings
            if (TryReadBoolean(operationArguments, ConfigUseModalPromptKey, EnvironModalPromptKey, out yesno))
            {
                operationArguments.UseModalUi = yesno.Value;
            }

            // look for credential preservation config settings
            if (TryReadBoolean(operationArguments, ConfigPreserveCredentialsKey, EnvironPreserveCredentialsKey, out yesno))
            {
                operationArguments.PreserveCredentials = yesno.Value;
            }

            // look for http path usage config settings
            if (TryReadBoolean(operationArguments, ConfigUseHttpPathKey, null, out yesno))
            {
                operationArguments.UseHttpPath = yesno.Value;
            }

            // look for http proxy config settings
            if (TryReadString(operationArguments, ConfigHttpProxyKey, EnvironHttpProxyKey, out value))
            {
                Git.Trace.WriteLine($"{ConfigHttpProxyKey} = '{value}'.");

                operationArguments.SetProxy(value);
            }
            else
            {
                // check the git-config http.proxy setting just-in-case
                Configuration.Entry entry;
                if (operationArguments.GitConfiguration.TryGetEntry("http", operationArguments.QueryUri, "proxy", out entry)
                    && !String.IsNullOrWhiteSpace(entry.Value))
                {
                    Git.Trace.WriteLine($"http.proxy = '{entry.Value}'.");

                    operationArguments.SetProxy(entry.Value);
                }
            }

            // look for custom namespace config settings
            if (TryReadString(operationArguments, ConfigNamespaceKey, EnvironNamespaceKey, out value))
            {
                Git.Trace.WriteLine($"{ConfigNamespaceKey} = '{value}'.");

                operationArguments.CustomNamespace = value;
            }
        }

        private static void PrintArgs(string[] args)
        {
            Debug.Assert(args != null, $"The `{nameof(args)}` parameter is null.");

            StringBuilder builder = new StringBuilder();
            builder.Append(Program.Name)
                   .Append(" (v")
                   .Append(Program.Version.ToString(3))
                   .Append(")");

            for (int i = 0; i < args.Length; i += 1)
            {
                builder.Append(" '")
                       .Append(args[i])
                       .Append("'");

                if (i + 1 < args.Length)
                {
                    builder.Append(",");
                }
            }

            // fake being part of the Main method for clarity
            Git.Trace.WriteLine(builder.ToString(), memberName: nameof(Main));
            builder = null;
        }

        [Conditional("DEBUG")]
        private static void EnableDebugTrace()
        {
            // use the stderr stream for the trace as stdout is used in the cross-process communications protocol
            Git.Trace.AddListener(Console.Error);
        }

        private static void EnableTraceLogging(OperationArguments operationArguments)
        {
            if (operationArguments.WriteLog)
            {
                Git.Trace.WriteLine("trace logging enabled.");

                string gitConfigPath;
                if (Where.GitLocalConfig(out gitConfigPath))
                {
                    Git.Trace.WriteLine($"git local config found at '{gitConfigPath}'.");

                    string gitDirPath = Path.GetDirectoryName(gitConfigPath);

                    if (Directory.Exists(gitDirPath))
                    {
                        EnableTraceLogging(operationArguments, gitDirPath);
                    }
                }
                else if (Where.GitGlobalConfig(out gitConfigPath))
                {
                    Git.Trace.WriteLine($"git global config found at '{gitConfigPath}'.");

                    string homeDirPath = Path.GetDirectoryName(gitConfigPath);

                    if (Directory.Exists(homeDirPath))
                    {
                        EnableTraceLogging(operationArguments, homeDirPath);
                    }
                }
            }
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "operationArguments")]
        private static void EnableTraceLogging(OperationArguments operationArguments, string logFilePath)
        {
            const int LogFileMaxLength = 8 * 1024 * 1024; // 8 MB

            string logFileName = Path.Combine(logFilePath, Path.ChangeExtension(ConfigPrefix, ".log"));

            FileInfo logFileInfo = new FileInfo(logFileName);
            if (logFileInfo.Exists && logFileInfo.Length > LogFileMaxLength)
            {
                for (int i = 1; i < Int32.MaxValue; i++)
                {
                    string moveName = String.Format("{0}{1:000}.log", ConfigPrefix, i);
                    string movePath = Path.Combine(logFilePath, moveName);

                    if (!File.Exists(movePath))
                    {
                        logFileInfo.MoveTo(movePath);
                        break;
                    }
                }
            }

            Git.Trace.WriteLine($"trace log destination is '{logFilePath}'.");

            using (var fileStream = File.Open(logFileName, FileMode.Append, FileAccess.Write, FileShare.ReadWrite))
            {
                var listener = new StreamWriter(fileStream, Encoding.UTF8);
                Git.Trace.AddListener(listener);

                // write a small header to help with identifying new log entries
                listener.Write('\n');
                listener.Write($"{DateTime.Now:yyyy.MM.dd HH:mm:ss} Microsoft {Program.Title} version {Version.ToString(3)}\n");
            }
        }

        private static bool BitbucketCredentialPrompt(string titleMessage, TargetUri targetUri, out string username, out string password)
        {
            Credential credential;
            if ((credential = BasicCredentialPrompt(targetUri, titleMessage)) != null)
            {
                username = credential.Username;
                password = credential.Password;

                return true;
            }

            username = null;
            password = null;

            return false;
        }

        private static bool BitbucketOAuthPrompt(string title, TargetUri targetUri, Bitbucket.AuthenticationResultType resultType,
            string username)
        {
            const int BufferReadSize = 16 * 1024;

            Debug.Assert(targetUri != null);

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

        private static bool GitHubAuthCodePrompt(TargetUri targetUri, Github.GitHubAuthenticationResultType resultType, string username, out string authenticationCode)
        {
            // ReadConsole 32768 fail, 32767 ok
            // @linquize [https://github.com/Microsoft/Git-Credential-Manager-for-Windows/commit/a62b9a19f430d038dcd85a610d97e5f763980f85]
            const int BufferReadSize = 16 * 1024;

            Debug.Assert(targetUri != null);

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
                string type = resultType == Github.GitHubAuthenticationResultType.TwoFactorApp
                    ? "app"
                    : "sms";

                Git.Trace.WriteLine($"2fa type = '{type}'.");

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

        private static bool GitHubCredentialPrompt(TargetUri targetUri, out string username, out string password)
        {
            const string TitleMessage = "Please enter your GitHub credentials for ";

            Credential credential;
            if ((credential = BasicCredentialPrompt(targetUri, TitleMessage)) != null)
            {
                username = credential.Username;
                password = credential.Password;

                return true;
            }

            username = null;
            password = null;

            return false;
        }

        private static void LoadAssemblyInformation()
        {
            var assembly = System.Reflection.Assembly.GetEntryAssembly();
            var asseName = assembly.GetName();

            _executablePath = assembly.Location;
            _location = Path.GetDirectoryName(_executablePath);
            _name = asseName.Name;
            _version = asseName.Version;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "targetUri")]
        private static Credential ModalPromptForCredentials(TargetUri targetUri, string message)
        {
            Debug.Assert(targetUri != null);
            Debug.Assert(message != null);

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
            string username;
            string password;

            if (ModalPromptDisplayDialog(ref credUiInfo,
                                         ref authPackage,
                                         packedAuthBufferPtr,
                                         packedAuthBufferSize,
                                         inBufferPtr,
                                         inBufferSize,
                                         saveCredentials,
                                         flags,
                                         out username,
                                         out password))
            {
                return new Credential(username, password);
            }

            return null;
        }

        private static Credential ModalPromptForCredentials(TargetUri targetUri)
        {
            string message = String.Format("Enter your credentials for {0}.", targetUri);
            return ModalPromptForCredentials(targetUri, message);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "targetUri")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        private static Credential ModalPromptForPassword(TargetUri targetUri, string message, string username)
        {
            Debug.Assert(targetUri != null);
            Debug.Assert(message != null);
            Debug.Assert(username != null);

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
            string password;

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
                    Git.Trace.WriteLine($"unable to determine credential buffer size ('{NativeMethods.Win32Error.GetText(error)}').");

                    return null;
                }

                inBufferPtr = Marshal.AllocHGlobal(inBufferSize);

                if (!NativeMethods.CredPackAuthenticationBuffer(flags: authPackage,
                                                                username: username,
                                                                password: String.Empty,
                                                                packedCredentials: inBufferPtr,
                                                                packedCredentialsSize: ref inBufferSize))
                {
                    error = Marshal.GetLastWin32Error();
                    Git.Trace.WriteLine($"unable to write to credential buffer ('{NativeMethods.Win32Error.GetText(error)}').");

                    return null;
                }

                if (ModalPromptDisplayDialog(ref credUiInfo,
                                             ref authPackage,
                                             packedAuthBufferPtr,
                                             packedAuthBufferSize,
                                             inBufferPtr,
                                             inBufferSize,
                                             saveCredentials,
                                             flags,
                                             out username,
                                             out password))
                {
                    return new Credential(username, password);
                }
            }
            finally
            {
                if (inBufferPtr != IntPtr.Zero)
                {
                    Marshal.FreeCoTaskMem(inBufferPtr);
                }
            }

            return null;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        private static void PrintVersion()
        {
            Program.WriteLine($"{Title} version {Version}");
        }

        private static bool QueryCredentials(OperationArguments operationArguments)
        {
            const string AadMsaAuthFailureMessage = "Logon failed, use ctrl+c to cancel basic credential prompt.";
            const string BasicAuthFaulureMessage = "Logon failed, use ctrl+c to cancel basic credential prompt.";
            const string GitHubAuthFailureMessage = "Logon failed, use ctrl+c to cancel basic credential prompt.";
            const string BitbucketAuthFailureMessage = "Logon failed, use ctrl+c to cancel basic credential prompt.";

            if (ReferenceEquals(operationArguments, null))
                throw new ArgumentNullException(nameof(operationArguments));
            if (ReferenceEquals(operationArguments.TargetUri, null))
                throw new ArgumentException("TargetUri property returned null", nameof(operationArguments));

            bool credentialsFound = false;
            var task = Task.Run(async () => { return await CreateAuthentication(operationArguments); });
            BaseAuthentication authentication = task.Result;
            Credential credentials = null;

            switch (operationArguments.Authority)
            {
                default:
                case AuthorityType.Basic:
                    {
                        BasicAuthentication basicAuth = authentication as BasicAuthentication;

                        Task.Run(async () =>
                        {
                            // attempt to get cached creds or acquire creds if interactivity is allowed
                            if ((operationArguments.Interactivity != Interactivity.Always
                                    && (credentials = authentication.GetCredentials(operationArguments.TargetUri)) != null)
                                || (operationArguments.Interactivity != Interactivity.Never
                                    && (credentials = await basicAuth.AcquireCredentials(operationArguments.TargetUri)) != null))
                            {
                                Git.Trace.WriteLine("credentials found.");
                                // set the credentials object
                                // no need to save the credentials explicitly, as Git will call back
                                // with a store command if the credentials are valid.
                                operationArguments.SetCredentials(credentials);
                                credentialsFound = true;
                            }
                            else
                            {
                                Git.Trace.WriteLine($"credentials for '{operationArguments.TargetUri}' not found.");
                                Program.WriteLine(BasicAuthFaulureMessage);
                                LogEvent($"Failed to retrieve credentials for '{operationArguments.TargetUri}'.", EventLogEntryType.FailureAudit);
                            }
                        }).Wait();
                    }
                    break;

                case AuthorityType.AzureDirectory:
                    {
                        VstsAadAuthentication aadAuth = authentication as VstsAadAuthentication;

                        Task.Run(async () =>
                        {
                            // attempt to get cached creds -> non-interactive logon -> interactive logon
                            // note that AAD "credentials" are always scoped access tokens
                            if (((operationArguments.Interactivity != Interactivity.Always
                                    && ((credentials = aadAuth.GetCredentials(operationArguments.TargetUri)) != null)
                                    && (!operationArguments.ValidateCredentials
                                        || await aadAuth.ValidateCredentials(operationArguments.TargetUri, credentials))))
                                || (operationArguments.Interactivity != Interactivity.Always
                                    && ((credentials = await aadAuth.NoninteractiveLogon(operationArguments.TargetUri, true)) != null)
                                    && (!operationArguments.ValidateCredentials
                                        || await aadAuth.ValidateCredentials(operationArguments.TargetUri, credentials)))
                                || (operationArguments.Interactivity != Interactivity.Never
                                    && ((credentials = await aadAuth.InteractiveLogon(operationArguments.TargetUri, true)) != null)
                                    && (!operationArguments.ValidateCredentials
                                        || await aadAuth.ValidateCredentials(operationArguments.TargetUri, credentials))))
                            {
                                Git.Trace.WriteLine($"credentials for '{operationArguments.TargetUri}' found.");
                                operationArguments.SetCredentials(credentials);
                                credentialsFound = true;
                                LogEvent($"Azure Directory credentials  for '{operationArguments.TargetUri}' successfully retrieved.", EventLogEntryType.SuccessAudit);
                            }
                            else
                            {
                                Git.Trace.WriteLine($"credentials for '{operationArguments.TargetUri}' not found.");
                                Program.WriteLine(AadMsaAuthFailureMessage);
                                LogEvent($"Failed to retrieve Azure Directory credentials for '{operationArguments.TargetUri}'.", EventLogEntryType.FailureAudit);
                            }
                        }).Wait();
                    }
                    break;

                case AuthorityType.MicrosoftAccount:
                    {
                        VstsMsaAuthentication msaAuth = authentication as VstsMsaAuthentication;

                        Task.Run(async () =>
                        {
                            // attempt to get cached creds -> interactive logon
                            // note that MSA "credentials" are always scoped access tokens
                            if (((operationArguments.Interactivity != Interactivity.Always
                                    && ((credentials = msaAuth.GetCredentials(operationArguments.TargetUri)) != null)
                                    && (!operationArguments.ValidateCredentials
                                        || await msaAuth.ValidateCredentials(operationArguments.TargetUri, credentials))))
                                || (operationArguments.Interactivity != Interactivity.Never
                                    && ((credentials = await msaAuth.InteractiveLogon(operationArguments.TargetUri, true)) != null)
                                    && (!operationArguments.ValidateCredentials
                                        || await msaAuth.ValidateCredentials(operationArguments.TargetUri, credentials))))
                            {
                                Git.Trace.WriteLine($"credentials for '{operationArguments.TargetUri}' found.");
                                operationArguments.SetCredentials(credentials);
                                credentialsFound = true;
                                LogEvent($"Microsoft Live credentials for '{operationArguments.TargetUri}' successfully retrieved.", EventLogEntryType.SuccessAudit);
                            }
                            else
                            {
                                Git.Trace.WriteLine($"credentials for '{operationArguments.TargetUri}' not found.");
                                Program.WriteLine(AadMsaAuthFailureMessage);
                                LogEvent($"Failed to retrieve Microsoft Live credentials for '{operationArguments.TargetUri}'.", EventLogEntryType.FailureAudit);
                            }
                        }).Wait();
                    }
                    break;

                case AuthorityType.GitHub:
                    {
                        Github.Authentication ghAuth = authentication as Github.Authentication;

                        Task.Run(async () =>
                        {
                            if ((operationArguments.Interactivity != Interactivity.Always
                                    && ((credentials = ghAuth.GetCredentials(operationArguments.TargetUri)) != null)
                                    && (!operationArguments.ValidateCredentials
                                        || await ghAuth.ValidateCredentials(operationArguments.TargetUri, credentials)))
                                || (operationArguments.Interactivity != Interactivity.Never
                                    && ((credentials = await ghAuth.InteractiveLogon(operationArguments.TargetUri)) != null)
                                    && (!operationArguments.ValidateCredentials
                                        || await ghAuth.ValidateCredentials(operationArguments.TargetUri, credentials))))
                            {
                                Git.Trace.WriteLine($"credentials for '{operationArguments.TargetUri}' found.");
                                operationArguments.SetCredentials(credentials);
                                credentialsFound = true;
                                LogEvent($"GitHub credentials for '{operationArguments.TargetUri}' successfully retrieved.", EventLogEntryType.SuccessAudit);
                            }
                            else
                            {
                                Git.Trace.WriteLine($"credentials for '{operationArguments.TargetUri}' not found.");
                                Program.WriteLine(GitHubAuthFailureMessage);
                                LogEvent($"Failed to retrieve GitHub credentials for '{operationArguments.TargetUri}'.", EventLogEntryType.FailureAudit);
                            }
                        }).Wait();
                    }
                    break;

                case AuthorityType.Bitbucket:
					{
                    var bbcAuth = authentication as Bitbucket.Authentication;

                    Task.Run(async () =>
                    {
                        if (((operationArguments.Interactivity != Interactivity.Always)
                             && ((credentials = bbcAuth.GetCredentials(operationArguments.TargetUri, operationArguments.CredUsername)) != null)
                             && (!operationArguments.ValidateCredentials
                                 || ((credentials = await bbcAuth.ValidateCredentials(operationArguments.TargetUri, operationArguments.CredUsername, credentials)) != null)))
                            || ((operationArguments.Interactivity != Interactivity.Never)
                                && ((credentials = await bbcAuth.InteractiveLogon(operationArguments.TargetUri, operationArguments.CredUsername)) != null)
                                && (!operationArguments.ValidateCredentials
                                    || ((credentials = await bbcAuth.ValidateCredentials(operationArguments.TargetUri, operationArguments.CredUsername, credentials)) != null))))
                        {
                                Git.Trace.WriteLine($"credentials for '{operationArguments.TargetUri}' found.");
                                // Bitbucket relies on a username + secret, so make sure there is a username to return
                                if (operationArguments.CredUsername != null)
                                {
                                    var c2 = new Credential(operationArguments.CredUsername, credentials.Password);
                                    operationArguments.SetCredentials(c2);
                                }
                                else
                                {
                                    operationArguments.SetCredentials(credentials);
                                }
                                credentialsFound = true;
                                LogEvent($"Bitbucket credentials for '{operationArguments.TargetUri}' successfully retrieved.", EventLogEntryType.SuccessAudit);

                        }
                        else
                        {
                            Program.WriteLine(BitbucketAuthFailureMessage);
                            LogEvent($"Failed to retrieve Bitbucket credentials for '{operationArguments.TargetUri}'.", EventLogEntryType.FailureAudit);
                        }
                    }).Wait();
					}
                    break;
                case AuthorityType.Ntlm:
                    {
                        Git.Trace.WriteLine($"'{operationArguments.TargetUri}' is NTLM.");
                        operationArguments.SetCredentials(BasicAuthentication.NtlmCredentials);
                        credentialsFound = true;
                    }
                    break;
            }

            return credentialsFound;
        }

        private static bool StandardHandleIsTty(NativeMethods.StandardHandleType handleType)
        {
            var standardHandle = NativeMethods.GetStdHandle(handleType);
            var handleFileType = NativeMethods.GetFileType(standardHandle);
            return handleFileType == NativeMethods.FileType.Char;
        }

        internal static bool TryReadBoolean(OperationArguments operationArguments, string configKey, string environKey, out bool? value)
        {
            if (ReferenceEquals(operationArguments, null))
                throw new ArgumentNullException(nameof(operationArguments));

            var envars = operationArguments.EnvironmentVariables;

            // look for an entry in the environment variables
            string localVal = null;
            if (!String.IsNullOrWhiteSpace(environKey)
                && envars.TryGetValue(environKey, out localVal))
            {
                goto parse_localval;
            }

            var config = operationArguments.GitConfiguration;

            // look for an entry in the git config
            Configuration.Entry entry;
            if (!String.IsNullOrWhiteSpace(configKey)
                && config.TryGetEntry(ConfigPrefix, operationArguments.QueryUri, configKey, out entry))
            {
                goto parse_localval;
            }

            // parse the value into a bool
            parse_localval:

            // An empty value is unset / should not be there, so treat it as if it isn't.
            if (String.IsNullOrWhiteSpace(localVal))
            {
                value = null;
                return false;
            }

            // Test `localValue` for a Git 'true' equivalent value
            if (ConfigValueComparer.Equals(localVal, "yes")
                || ConfigValueComparer.Equals(localVal, "true")
                || ConfigValueComparer.Equals(localVal, "1")
                || ConfigValueComparer.Equals(localVal, "on"))
            {
                value = true;
                return true;
            }

            // Test `localValue` for a Git 'false' equivalent value
            if (ConfigValueComparer.Equals(localVal, "no")
                || ConfigValueComparer.Equals(localVal, "false")
                || ConfigValueComparer.Equals(localVal, "0")
                || ConfigValueComparer.Equals(localVal, "off"))
            {
                value = false;
                return true;
            }

            value = null;
            return false;
        }

        private static bool TryReadString(OperationArguments operationArguments, string configKey, string environKey, out string value)
        {
            if (ReferenceEquals(operationArguments, null))
                throw new ArgumentNullException(nameof(operationArguments));

            var envars = operationArguments.EnvironmentVariables;

            // look for an entry in the environment variables
            string localVal;
            if (!String.IsNullOrWhiteSpace(environKey)
                && envars.TryGetValue(environKey, out localVal)
                && !String.IsNullOrWhiteSpace(localVal))
            {
                value = localVal;
                return true;
            }

            var config = operationArguments.GitConfiguration;

            // look for an entry in the git config
            Configuration.Entry entry;
            if (!String.IsNullOrWhiteSpace(configKey)
                && config.TryGetEntry(ConfigPrefix, operationArguments.QueryUri, configKey, out entry)
                && !String.IsNullOrWhiteSpace(entry.Value))
            {
                value = entry.Value;
                return true;
            }

            value = null;
            return false;
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
                    Git.Trace.WriteLine($"credential prompt failed ('{NativeMethods.Win32Error.GetText(error)}').");

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
                    Git.Trace.WriteLine($"failed to unpack buffer ('{NativeMethods.Win32Error.GetText(error)}').");

                    return false;
                }

                Git.Trace.WriteLine("successfully acquired credentials from user.");

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
    }
}
