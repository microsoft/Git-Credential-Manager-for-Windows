/**** Git Credential Manager for Windows ****
 *
 * Copyright (c) Microsoft Corporation
 * All rights reserved.
 *
 * MIT License
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the """"Software""""), to deal
 * in the Software without restriction, including without limitation the rights to
 * use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
 * the Software, and to permit persons to whom the Software is furnished to do so,
 * subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED *AS IS*, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
 * FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
 * COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN
 * AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
 * WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE."
**/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.Alm.Authentication;
using Bitbucket = Atlassian.Bitbucket.Authentication;
using Git = Microsoft.Alm.Authentication.Git;
using Github = GitHub.Authentication;

namespace Microsoft.Alm.Cli
{
    enum KeyType
    {
        Authority,
        ConfigNoLocal,
        ConfigNoSystem,
        HttpPath,
        HttpProxy,
        HttpsProxy,
        HttpUserAgent,
        Interactive,
        ModalPrompt,
        Namespace,
        PreserveCredentials,
        TokenDuration,
        Username,
        Validate,
        VstsScope,
        Writelog,
    }

    partial class Program
    {
        public const string SourceUrl = "https://github.com/Microsoft/Git-Credential-Manager-for-Windows";
        public const string EventSource = "Git Credential Manager";

        internal static readonly StringComparer ConfigKeyComparer = StringComparer.OrdinalIgnoreCase;
        internal static readonly StringComparer ConfigValueComparer = StringComparer.OrdinalIgnoreCase;

        internal const string EnvironConfigDebugKey = "GCM_DEBUG";
        internal const string EnvironConfigTraceKey = "GCM_TRACE";

        internal static readonly StringComparer EnvironKeyComparer = StringComparer.OrdinalIgnoreCase;

        internal const string ConfigPrefix = "credential";
        internal const string SecretsNamespace = "git";

        internal static readonly char[] NewLineChars = Environment.NewLine.ToCharArray();

        internal static readonly VstsTokenScope VstsCredentialScope = VstsTokenScope.CodeWrite | VstsTokenScope.PackagingRead;
        internal static readonly Github.TokenScope GitHubCredentialScope = Github.TokenScope.Gist | Github.TokenScope.Repo;

        internal BasicCredentialPromptDelegate _basicCredentialPrompt = ConsoleFunctions.CredentialPrompt;
        internal BitbucketCredentialPromptDelegate _bitbucketCredentialPrompt = BitbucketFunctions.CredentialPrompt;
        internal BitbucketOAuthPromptDelegate _bitbucketOauthPrompt = BitbucketFunctions.OAuthPrompt;
        internal CreateAuthenticationDelegate _createAuthentication = CommonFunctions.CreateAuthentication;
        internal DeleteCredentialsDelegate _deleteCredentials = CommonFunctions.DeleteCredentials;
        internal DieExceptionDelegate _dieException = CommonFunctions.DieException;
        internal DieMessageDelegate _dieMessage = CommonFunctions.DieMessage;
        internal EnableTraceLoggingDelegate _enableTraceLogging = CommonFunctions.EnableTraceLogging;
        internal EnableTraceLoggingFileDelegate _enableTraceLoggingFile = CommonFunctions.EnableTraceLoggingFile;
        internal ExitDelegate _exit = ConsoleFunctions.Exit;
        internal GitHubAuthCodePromptDelegate _gitHubAuthCodePrompt = GitHubFunctions.AuthCodePrompt;
        internal GitHubCredentialPromptDelegate _gitHubCredentialPrompt = GitHubFunctions.CredentialPrompt;
        internal LoadOperationArgumentsDelegate _loadOperationArguments = CommonFunctions.LoadOperationArguments;
        internal LogEventDelegate _logEvent = CommonFunctions.LogEvent;
        internal ModalPromptDisplayDialogDelegate _modalPromptDisplayDialog = DialogFunctions.DisplayModal;
        internal ModalPromptForCredentialsDelegate _modalPromptForCredentials = DialogFunctions.CredentialPrompt;
        internal ModalPromptForPasswordDelegate _modalPromptForPassword = DialogFunctions.PasswordPrompt;
        internal OpenStandardHandleDelegate _openStandardErrorStream = ConsoleFunctions.OpenStandardErrorStream;
        internal GetStandardWriterDelegate _openStandardErrorWriter = ConsoleFunctions.OpenStandardErrorWriter;
        internal OpenStandardHandleDelegate _openStandardInputStream = ConsoleFunctions.OpenStandardInputStream;
        internal GetStandardReaderDelegate _openStandardInputReader = ConsoleFunctions.OpenStandardInputReader;
        internal OpenStandardHandleDelegate _openStandardOutputStream = ConsoleFunctions.OpenStandardOutputStream;
        internal GetStandardWriterDelegate _openStandardOutputWriter = ConsoleFunctions.OpenStandardOutputWriter;
        internal PrintArgsDelegate _printArgs = CommonFunctions.PrintArgs;
        internal QueryCredentialsDelegate _queryCredentials = CommonFunctions.QueryCredentials;
        internal ReadKeyDelegate _readKey = ConsoleFunctions.ReadKey;
        internal SetStandardWriterDelegate _setStandardErrorWriter = ConsoleFunctions.SetStandardErrorWriter;
        internal SetStandardReaderDelegate _setStandardInputReader = ConsoleFunctions.SetStandardInputReader;
        internal SetStandardWriterDelegate _setStandardOutputWriter = ConsoleFunctions.SetStandardOutputWriter;
        internal StandardHandleIsTtyDelegate _standardHandleIsTty = ConsoleFunctions.StandardHandleIsTty;
        internal TryReadBooleanDelegate _tryReadBoolean = CommonFunctions.TryReadBoolean;
        internal TryReadStringDelegate _tryReadString = CommonFunctions.TryReadString;
        internal WriteDelegate _write = ConsoleFunctions.Write;
        internal WriteLineDelegate _writeLine = ConsoleFunctions.WriteLine;

        internal readonly Dictionary<KeyType, string> _configurationKeys = new Dictionary<KeyType, string>()
        {
            { KeyType.Authority,  "authority" },
            { KeyType.HttpProxy,  "httpProxy" },
            { KeyType.HttpsProxy,  "httpsProxy" },
            { KeyType.Interactive,  "interactive" },
            { KeyType.ModalPrompt,  "modalPrompt" },
            { KeyType.Namespace,  "namespace" },
            { KeyType.PreserveCredentials,  "preserve" },
            { KeyType.TokenDuration,  "tokenDuration" },
            { KeyType.HttpPath,  "useHttpPath" },
            { KeyType.Username,  "username" },
            { KeyType.Validate,  "validate" },
            { KeyType.VstsScope, "vstsScope" },
            { KeyType.Writelog,  "writeLog" },
        };
        internal readonly Dictionary<KeyType, string> _environmentKeys = new Dictionary<KeyType, string>()
        {
            { KeyType.Authority, "GCM_AUTHORITY" },
            { KeyType.ConfigNoLocal, "GCM_CONFIG_NOLOCAL" },
            { KeyType.ConfigNoSystem, "GCM_CONFIG_NOSYSTEM" },
            { KeyType.HttpProxy, "HTTP_PROXY" },
            { KeyType.HttpsProxy, "HTTPS_PROXY" },
            { KeyType.HttpUserAgent, "GCM_HTTP_USER_AGENT" },
            { KeyType.Interactive, "GCM_INTERACTIVE" },
            { KeyType.ModalPrompt, "GCM_MODAL_PROMPT" },
            { KeyType.Namespace, "GCM_NAMESPACE" },
            { KeyType.PreserveCredentials, "GCM_PRESERVE" },
            { KeyType.TokenDuration, "GCM_TOKEN_DURATION" },
            { KeyType.Validate, "GCM_VALIDATE" },
            { KeyType.VstsScope, "GCM_VSTS_SCOPE" },
            { KeyType.Writelog, "GCM_WRITELOG" },
        };

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1810:InitializeReferenceTypeStaticFieldsInline")]
        static Program()
        {
            ServicePointManager.SecurityProtocol = ServicePointManager.SecurityProtocol | SecurityProtocolType.Tls12;
        }

        private string _executablePath;
        private readonly RuntimeContext _context;
        private string _location;
        private string _name;
        private Stream _stdErrStream;
        private TextWriter _stdErrWriter;
        private Stream _stdInStream;
        private TextReader _stdInReader;
        private Stream _stdOutStream;
        private TextWriter _stdOutWriter;
        private readonly object _syncpoint = new object();
        private string _title;
        private Version _version;

        public IReadOnlyDictionary<KeyType, string> ConfigurationKeys
            => _configurationKeys;

        public IReadOnlyDictionary<KeyType, string> EnvironmentKeys
            => _environmentKeys;

        /// <summary>
        /// Gets the path to the executable.
        /// </summary>
        public string ExecutablePath
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
        public string Location
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
        public string Name
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
        public bool StandardErrorIsTty
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
        public bool StandardInputIsTty
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
        public bool StandardOutputIsTty
        {
            get { return StandardHandleIsTty(NativeMethods.StandardHandleType.Output); }
        }

        public string Title
        {
            get { return _title; }
            private set { _title = value; }
        }

        /// <summary>
        /// Gets the version of the application.
        /// </summary>
        public Version Version
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

        internal RuntimeContext Context
            => _context;

        internal TextWriter Error
        {
            get
            {
                lock (_syncpoint)
                {
                    if (_stdErrWriter is null)
                    {
                        _stdErrWriter = _openStandardErrorWriter(this);
                    }

                    return _stdErrWriter;
                }
            }
        }

        internal Stream ErrorStream
        {
            get
            {
                lock (_syncpoint)
                {
                    if (_stdErrStream is null)
                    {
                        _stdErrStream = _openStandardErrorStream(this);
                    }

                    return _stdErrStream;
                }
            }
        }

        internal IFileSystem FileSystem
            => _context.FileSystem;

        internal TextReader In
        {
            get
            {
                lock (_syncpoint)
                {
                    if (_stdInReader is null)
                    {
                        _stdInReader = _openStandardInputReader(this);
                    }

                    return _stdInReader;
                }
            }
        }

        internal Stream InStream
        {
            get
            {
                lock (_syncpoint)
                {
                    if (_stdInStream is null)
                    {
                        _stdInStream = _openStandardInputStream(this);
                    }

                    return _stdInStream;
                }
            }
        }

        internal TextWriter Out
        {
            get
            {
                lock (_syncpoint)
                {
                    if (_stdOutWriter is null)
                    {
                        _stdOutWriter = _openStandardOutputWriter(this);
                    }

                    return _stdOutWriter;
                }
            }
        }

        internal Stream OutStream
        {
            get
            {
                lock (_syncpoint)
                {
                    if (_stdOutStream is null)
                    {
                        _stdOutStream = _openStandardOutputStream(this);
                    }

                    return _stdOutStream;
                }
            }
        }

        internal static void DebuggerLaunch(Program program)
        {
            if (program is null)
                throw new ArgumentNullException(nameof(program));

            if (Debugger.IsAttached)
                return;

            string debug = Environment.GetEnvironmentVariable(EnvironConfigDebugKey);
            if (debug != null
                && (StringComparer.OrdinalIgnoreCase.Equals(debug, "true")
                    || StringComparer.OrdinalIgnoreCase.Equals(debug, "1")
                    || StringComparer.OrdinalIgnoreCase.Equals(debug, "debug")))
            {
                program.Context.Trace.WriteLine($"'{EnvironConfigDebugKey}': '{debug}', launching debugger...");

                Debugger.Launch();
            }
        }

        internal void Die(Exception exception,
                                [CallerFilePath] string path = "",
                                [CallerLineNumber] int line = 0,
                                [CallerMemberName] string name = "")
            => _dieException(this, exception, path, line, name);

        internal void Die(string message,
                                [CallerFilePath] string path = "",
                                [CallerLineNumber] int line = 0,
                                [CallerMemberName] string name = "")
            => _dieMessage(this, message, path, line, name);

        internal void Exit(int exitcode = 0,
                                  string message = null,
                                 [CallerFilePath] string path = "",
                                 [CallerLineNumber] int line = 0,
                                 [CallerMemberName] string name = "")
            => _exit(this, exitcode, message, path, line, name);

        internal Task LoadOperationArguments(OperationArguments operationArguments)
            => _loadOperationArguments(this, operationArguments);

        internal void LogEvent(string message, EventLogEntryType eventType)
            => _logEvent(this, message, eventType);

        internal Task<Credential> QueryCredentials(OperationArguments operationArguments)
            => _queryCredentials(this, operationArguments);

        internal ConsoleKeyInfo ReadKey(bool intercept = true)
            => _readKey(this, intercept);

        internal void Write(string message)
            => _write(this, message);

        internal void WriteLine(string message = null)
            => _writeLine(this, message);

        internal Credential BasicCredentialPrompt(TargetUri targetUri)
        {
            string message = "Please enter your credentials for ";
            return BasicCredentialPrompt(targetUri, message);
        }

        internal Credential BasicCredentialPrompt(TargetUri targetUri, string titleMessage)
            => _basicCredentialPrompt(this, targetUri, titleMessage);

        internal Task<BaseAuthentication> CreateAuthentication(OperationArguments operationArguments)
            => _createAuthentication(this, operationArguments);

        internal Task<bool> DeleteCredentials(OperationArguments operationArguments)
            => _deleteCredentials(this, operationArguments);

        internal void PrintArgs(string[] args)
            => _printArgs(this, args);

        [Conditional("DEBUG")]
        internal void EnableDebugTrace()
        {
            // use the stderr stream for the trace as stdout is used in the cross-process
            // communications protocol
            _context.Trace.AddListener(Error);
        }

        internal void EnableTraceLogging(OperationArguments operationArguments)
            => _enableTraceLogging(this, operationArguments);

        internal void EnableTraceLogging(OperationArguments operationArguments, string logFilePath)
            => _enableTraceLoggingFile(this, operationArguments, logFilePath);

        internal bool BitbucketCredentialPrompt(string titleMessage, TargetUri targetUri, out string username, out string password)
            => _bitbucketCredentialPrompt(this, titleMessage, targetUri, out username, out password);

        internal bool BitbucketOAuthPrompt(string title, TargetUri targetUri, Bitbucket.AuthenticationResultType resultType, string username)
            => _bitbucketOauthPrompt(this, title, targetUri, resultType, username);

        internal string KeyTypeName(KeyType type)
        {
            string value = "UNKNOWN";

            if (!_configurationKeys.TryGetValue(type, out value)
                && !_environmentKeys.TryGetValue(type, out value))
            {
                _context.Trace.WriteLine($"BUG: unknown {nameof(KeyType)}: '{type}' encountered.");
            }

            return value;
        }

        internal bool GitHubAuthCodePrompt(TargetUri targetUri, Github.GitHubAuthenticationResultType resultType, string username, out string authenticationCode)
            => _gitHubAuthCodePrompt(this, targetUri, resultType, username, out authenticationCode);

        internal bool GitHubCredentialPrompt(TargetUri targetUri, out string username, out string password)
            => _gitHubCredentialPrompt(this, targetUri, out username, out password);

        private void LoadAssemblyInformation()
        {
            var assembly = System.Reflection.Assembly.GetEntryAssembly();
            var asseName = assembly.GetName();

            _executablePath = assembly.Location;
            _location = Path.GetDirectoryName(_executablePath);
            _name = asseName.Name;
            _version = asseName.Version;
        }

        internal bool ModalPromptDisplayDialog(ref NativeMethods.CredentialUiInfo credUiInfo,
                                               ref NativeMethods.CredentialPackFlags authPackage,
                                               IntPtr packedAuthBufferPtr,
                                               uint packedAuthBufferSize,
                                               IntPtr inBufferPtr,
                                               int inBufferSize,
                                               bool saveCredentials,
                                               NativeMethods.CredentialUiWindowsFlags flags,
                                               out string username,
                                               out string password)
            => _modalPromptDisplayDialog(this,
                                         ref credUiInfo,
                                         ref authPackage,
                                         packedAuthBufferPtr,
                                         packedAuthBufferSize,
                                         inBufferPtr,
                                         inBufferSize,
                                         saveCredentials,
                                         flags,
                                         out username,
                                         out password);

        internal Credential ModalPromptForCredentials(TargetUri targetUri, string message)
            => _modalPromptForCredentials(this, targetUri, message);

        internal Credential ModalPromptForCredentials(TargetUri targetUri)
        {
            string message = string.Format("Enter your credentials for {0}.", targetUri.ToString(username: false, port: true, path: true));

            if (!string.IsNullOrEmpty(targetUri.QueryUri.UserInfo))
            {
                string username = targetUri.QueryUri.UserInfo;

                if (!targetUri.QueryUri.UserEscaped)
                {
                    username = Uri.UnescapeDataString(username);
                }

                return ModalPromptForPassword(targetUri, message, username);
            }

            return ModalPromptForCredentials(targetUri, message);
        }

        private Credential ModalPromptForPassword(TargetUri targetUri, string message, string username)
            => _modalPromptForPassword(this, targetUri, message, username);

        internal void PrintVersion()
        {
#if DEBUG
            WriteLine($"{Title} version {Version.ToString(4)}");
#else
            WriteLine($"{Title} version {Version.ToString(3)}");
#endif
        }

        internal void SetError(TextWriter writer)
        {
            _setStandardErrorWriter(this, writer);
        }

        internal void SetIn(TextReader reader)
        {
            _setStandardInputReader(this, reader);
        }

        internal void SetOut(TextWriter writer)
        {
            _setStandardOutputWriter(this, writer);
        }

        internal bool StandardHandleIsTty(NativeMethods.StandardHandleType handleType)
            => _standardHandleIsTty(this, handleType);

        internal bool TryReadBoolean(OperationArguments operationArguments, KeyType key, out bool? value)
            => _tryReadBoolean(this, operationArguments, key, out value);

        internal bool TryReadString(OperationArguments operationArguments, KeyType key, out string value)
            => _tryReadString(this, operationArguments, key, out value);
    }
}
