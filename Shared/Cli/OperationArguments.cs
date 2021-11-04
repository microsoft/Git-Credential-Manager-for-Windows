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
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Alm.Authentication;
using static System.Globalization.CultureInfo;
using AzureDev = AzureDevOps.Authentication;
using Git = Microsoft.Alm.Authentication.Git;

namespace Microsoft.Alm.Cli
{
    [System.Diagnostics.DebuggerDisplay("{DebuggerDisplay, nq}")]
    internal class OperationArguments : Base
    {
        /// <summary>
        /// Creates a new instance of `<see cref="OperationArguments"/>` with default values.
        /// <para/>
        /// Use `<see cref="ReadInput(Stream)"/>` to populate the instance's properties.
        /// </summary>
        public OperationArguments(RuntimeContext context)
            : base(context)
        {
            _authorityType = AuthorityType.Auto;
            _interactivity = Interactivity.Auto;
            _useLocalConfig = true;
            _useHttpPath = true;
            _useModalUi = true;
            _useSystemConfig = true;
            _validateCredentials = true;
            _devopsTokenScope = Program.DevOpsCredentialScope;
        }

        /// <summary>
        /// Creates a new instance of `<see cref="OperationArguments"/>` using `<seealso cref="RuntimeContext.Default"/>`, and with default values.
        /// <para/>
        /// Use `<see cref="ReadInput(Stream)"/>` to populate the instance's properties.
        /// </summary>
        public OperationArguments()
            : this(RuntimeContext.Default)
        { }

        private AuthorityType _authorityType;
        private Git.Configuration _configuration;
        private Credential _credentials;
        private string _customNamespace;
        private AzureDev.TokenScope _devopsTokenScope;
        private Dictionary<string, string> _environmentVariables;
        private string _gitRemoteHttpCommandLine;
        private Interactivity _interactivity;
        private IntPtr _parentHwnd;
        private bool _preserveCredentials;
        private Uri _proxyUri;
        private string _queryHost;
        private string _queryPath;
        private string _queryProtocol;
        private TargetUri _targetUri;
        private TimeSpan? _tokenDuration;
        private string _urlOverride;
        private bool _useHttpPath;
        private bool _useLocalConfig;
        private bool _useModalUi;
        private string _username;
        private bool _useSystemConfig;
        private bool _validateCredentials;
        private bool _writeLog;

        /// <summary>
        /// Gets or sets the authority to be used during the current operation.
        /// <para/>
        /// Default value is `<seealso cref="AuthorityType.Auto"/>`.
        /// </summary>
        public virtual AuthorityType Authority
        {
            get { return _authorityType; }
            set { _authorityType = value; }
        }

        /// <summary>
        /// Gets or sets credentials to be used by, or returned by the current operation.
        /// <para/>
        /// Default value is `<see langword="null"/>`.
        /// </summary>
        public virtual Credential Credentials
        {
            get { return _credentials; }
            set { _credentials = value; }
        }

        /// <summary>
        /// Gets or sets a custom namespace, which overrides `<seealso cref="Program.SecretsNamespace"/>`, used when generating secret-keys.
        /// <para/>
        /// Default value is `<see langword="null"/>`.
        /// </summary>
        public virtual string CustomNamespace
        {
            get { return _customNamespace; }
            set { _customNamespace = value; }
        }

        /// <summary>
        /// Gets or sets the scope, or permissions, when requesting new access tokens from Azure DevOps.
        /// <para/>
        /// Default value is `<seealso cref="Program.DevOpsCredentialScope"/>`.
        /// </summary>
        public virtual AzureDev.TokenScope DevOpsTokenScope
        {
            get { return _devopsTokenScope; }
            set { _devopsTokenScope = value; }
        }

        /// <summary>
        /// Gets a map of the process's environmental variables keyed on case-insensitive names.
        /// </summary>
        public virtual IReadOnlyDictionary<string, string> EnvironmentVariables
        {
            get
            {
                if (_environmentVariables is null)
                {
                    _environmentVariables = new Dictionary<string, string>(Settings.GetEnvironmentVariables(), Program.EnvironKeyComparer);
                }
                return _environmentVariables;
            }
        }

        /// <summary>
        /// Gets the process's Git configuration based on current working directory, user's folder, and Git's system directory.
        /// <para/>
        /// Value is `<see langword="null"/>`, until explicitly loaded.
        /// <para/>
        /// Use `<seealso cref="LoadConfiguration()"/>` to populate the property.
        /// </summary>
        public virtual Git.Configuration GitConfiguration
        {
            get { return _configuration; }
        }

        /// <summary>
        /// Gets or sets the command line used to invoke "git-remote-http(s).exe".
        /// <para/>
        /// Setting the value will recreate the `<seealso cref="TargetUri"/>` property value.
        /// <para/>
        /// The default value is `<see langword="null"/>`.
        /// </summary>
        public virtual string GitRemoteHttpCommandLine
        {
            get { return _gitRemoteHttpCommandLine; }
            set
            {
                _gitRemoteHttpCommandLine = value;

                // Re-create the target Uri.
                CreateTargetUri();
            }
        }

        /// <summary>
        /// Gets or sets if the expected interactivity level of the command line interface.
        /// <para/>
        /// Default value is `<seealso cref="Interactivity.Auto"/>`.
        /// </summary>
        public virtual Interactivity Interactivity
        {
            get { return _interactivity; }
            set { _interactivity = value; }
        }

        /// <summary>
        /// Gets or sets the handle of the parent UX window.
        /// <para/>
        /// Primarily used by IDE which have primary window handles, and a need for any UX presented by the GCM to appear relative to their UX.
        /// <para/>
        /// Returns the parent window's handle; otherwise `<seealso cref="IntPtr.Zero"/>`.
        /// </summary>
        public virtual IntPtr ParentHwnd
        {
            get { return _parentHwnd; }
            set { _parentHwnd = value; }
        }

        /// <summary>
        /// Gets the password value of `<seealso cref="Credentials"/>` if available; otherwise `<see langword="null"/>`.
        /// </summary>
        public virtual string Password
        {
            get { return _credentials?.Password; }
        }

        /// <summary>
        /// Gets or sets the expected credential erasure behavior expectation.
        /// <para/>
        /// Default value is `<see langword="false"/>`.
        /// </summary>
        public virtual bool PreserveCredentials
        {
            get { return _preserveCredentials; }
            set { _preserveCredentials = value; }
        }

        /// <summary>
        /// Gets or sets the `<seealso cref="Uri"/>` of the proxy network operation should use if available; otherwise `<see langword="null"/>`.
        /// </summary>
        public virtual Uri ProxyUri
        {
            get { return _targetUri.ProxyUri; }
            internal set
            {
                _proxyUri = value;

                // Re-create the target Uri.
                CreateTargetUri();
            }
        }

        /// <summary>
        /// Gets or sets the host (aka domain name) portion of `<seealso cref="QueryUri"/>`.
        /// </summary>
        /// <exception cref="ArgumentNullException">When set `<see langword="null"/>`.</exception>
        public virtual string QueryHost
        {
            get { return _queryHost; }
            set
            {
                if (value is null)
                    throw new ArgumentNullException(nameof(QueryHost));

                _queryHost = value;

                // Re-create the target Uri.
                CreateTargetUri();
            }
        }

        /// <summary>
        /// Gets or sets the path portion of `<see cref="QueryUri"/>`.
        /// </summary>
        public virtual string QueryPath
        {
            get { return _queryPath; }
            set
            {
                _queryPath = value;

                // Re-create the target Uri.
                CreateTargetUri();
            }
        }

        /// <summary>
        /// Gets or sets the protocol portion of `<seealso cref="QueryUri"/>`.
        /// </summary>
        /// <exception cref="ArgumentNullException">When set `<see langword="null"/>`.</exception>
        public virtual string QueryProtocol
        {
            get { return _queryProtocol; }
            set
            {
                if (value is null)
                    throw new ArgumentNullException(nameof(QueryProtocol));

                _queryProtocol = value;

                // Re-create the target Uri.
                CreateTargetUri();
            }
        }

        /// <summary>
        /// Gets the `<seealso cref="Uri"/>` used for all network operations. 
        /// </summary>
        /// <exception cref="ArgumentNullException">When set `<see langword="null"/>`.</exception>
        public virtual Uri QueryUri
        {
            get { return _targetUri?.QueryUri; }
            set
            {
                if (value is null)
                    throw new ArgumentNullException(nameof(QueryUri));

                _queryHost = value.DnsSafeHost;
                _queryPath = value.AbsolutePath;
                _queryProtocol = value.Scheme;

                // If there's a non-default port value, retain it.
                if (!value.IsDefaultPort)
                {
                    _queryHost = $"{_queryHost}:{value.Port}";
                }

                // If there's a query segment, retain it.
                if (!string.IsNullOrWhiteSpace(value.Query))
                {
                    _queryHost = $"{_queryHost}?{value.Query}";
                }

                // Re-create the target Uri.
                CreateTargetUri();
            }
        }

        /// <summary>
        /// Gets the `<seealso cref="Authentication.TargetUri"/>` used by the GCM for all network operations.
        /// </summary>
        public virtual TargetUri TargetUri
        {
            get { return _targetUri; }
        }

        /// <summary>
        /// Gets or sets the option token duration.
        /// <para/>
        /// Used during new token generation for authorities which support limiting tokens by lifetime.
        /// <para/>
        /// Default value is `<see langword="null"/>`.
        /// </summary>
        public virtual TimeSpan? TokenDuration
        {
            get { return _tokenDuration; }
            set { _tokenDuration = value; }
        }

        /// <summary>
        /// Gets or sets the override value for the `<seealso cref="TargetUri.ActualUri"/>` value.
        /// <para/>
        /// Default value is `<see langword="null"/>`.
        /// </summary>
        public virtual string UrlOverride
        {
            get { return _urlOverride; }
            set
            {
                _urlOverride = value;

                // Re-create the target Uri.
                CreateTargetUri();
            }
        }

        /// <summary>
        /// Gets or sets `<see langword="false"/>` if `<seealso cref="LoadConfiguration"/>` ignores local Git configuration values; otherwise `<see langword="true"/>`.
        /// <para/>
        /// Default value is `<see langword="true"/>`.
        /// </summary>
        public virtual bool UseConfigLocal
        {
            get { return _useLocalConfig; }
            set { _useLocalConfig = value; }
        }

        /// <summary>
        /// Gets or sets `<see langword="false"/>` if `<seealso cref="LoadConfiguration"/>` ignores system Git configuration values; otherwise `<see langword="true"/>`.
        /// <para/>
        /// Default value is `<see langword="true"/>`.
        /// </summary>
        public virtual bool UseConfigSystem
        {
            get { return _useSystemConfig; }
            set { _useSystemConfig = value; }
        }

        /// <summary>
        /// Gets or sets `<see langword="false"/>` if GCM ignored supplied URL-path information; otherwise `<see langword="true"/>`.
        /// <para/>
        /// Default value is `<see langword="true"/>`.
        /// </summary>
        public virtual bool UseHttpPath
        {
            get { return _useHttpPath; }
            set { _useHttpPath = value; }
        }

        /// <summary>
        /// Gets or sets `<see langword="false"/>` if GCM cannot present modal UX windows; otherwise `<see langword="true"/>`.
        /// <para/>
        /// Default value is `<see langword="true"/>`.
        /// </summary>
        public virtual bool UseModalUi
        {
            get { return _useModalUi; }
            set { _useModalUi = value; }
        }

        /// <summary>
        /// Gets or sets the user information portion of `<seealso cref="QueryUri"/>`.
        /// </summary>
        public virtual string Username
        {
            get { return _username; }
            set
            {
                _username = value;

                // Re-create the target Uri.
                CreateTargetUri();
            }
        }

        /// <summary>
        /// Sets or sets `<see langword="false"/>` if credential validation should be skipped before returning; otherwise `<see langword="true"/>`.
        /// <para/>
        /// Only applies to authorities which support credential validation.
        /// <para/>
        /// Default value is `<see langword="true"/>`.
        /// </summary>
        public virtual bool ValidateCredentials
        {
            get { return _validateCredentials; }
            set { _validateCredentials = value; }
        }

        /// <summary>
        /// Gets or sets `<see langword="true"/>` if the GCM should write trace events; otherwise `<see langword="false"/>`.
        /// <para/>
        /// Default value is `<see langword="false"/>`.
        /// </summary>
        public virtual bool WriteLog
        {
            get { return _writeLog; }
            set { _writeLog = value; }
        }

        /// <summary>
        /// KeyVaultUrl for storing credentials and PAT tokens in KeyVault. 
        /// If specified, KeyVault is used as a storage mechanism for secrets.
        /// </summary>
        public virtual string KeyVaultUrl { get; set; }

        public virtual bool? KeyVaultUseMsi { get; set; }

        public virtual string KeyVaulyAuthCertificateStoreType { get; set; }

        public virtual string KeyVaultAuthCertificateThumbprint { get; set; }

        public virtual string KeyVaultAuthClientId { get; set; }

        internal string DebuggerDisplay
        {
            get { return $"{nameof(OperationArguments)}: TargetUri: {TargetUri}, Authority: {Authority}, Credentials: {Credentials}"; }
        }

        /// <summary>
        /// Loads the Git configuration based on `<seealso cref="Environment.CurrentDirectory"/>`.
        /// </summary>
        public virtual async Task LoadConfiguration()
        {
            _configuration = await Git.Configuration.ReadConfiuration(Context, Settings.CurrentDirectory, UseConfigLocal, UseConfigSystem);
        }

        /// <summary>
        /// Reads git-credential formatted input from `<paramref name="readableStream"/>`, parses the data, and populates `<seealso cref="TargetUri"/>`.
        /// </summary>
        /// <param name="readableStream">
        /// Readable stream with credential protocol formatted information.
        /// <para/>
        /// Please see 'https://www.kernel.org/pub/software/scm/git/docs/git-credential.html' for information about credential protocol format.
        /// </param>
        /// <exception cref="ArgumentNullException">When `<paramref name="readableStream"/>` is `<see langword="null"/>`.</exception>
        /// <exception cref="ArgumentException">When `<paramref name="readableStream"/>` is not readable.</exception>
        /// <exception cref="InvalidOperationException">When data read from `<paramref name="readableStream"/>` is in an unexpected format.</exception>
        public virtual async Task ReadInput(Stream readableStream)
        {
            if (readableStream is null)
                throw new ArgumentNullException(nameof(readableStream));

            if (readableStream == Stream.Null || !readableStream.CanRead)
            {
                var inner = new InvalidDataException("Stream must be readable.");
                throw new ArgumentException(inner.Message, nameof(readableStream), inner);
            }

            byte[] buffer = new byte[4096];
            int read = 0;

            int r;
            while ((r = await readableStream.ReadAsync(buffer, read, buffer.Length - read)) > 0)
            {
                read += r;

                // If we've filled the buffer, make it larger this could hit an out of memory
                // condition, but that'd require the called to be attempting to do so, since
                // that's not a security threat we can safely ignore that and allow NetFx to
                // handle it.
                if (read == buffer.Length)
                {
                    Array.Resize(ref buffer, buffer.Length * 2);
                }

                if ((read > 0 && read < 3 && buffer[read - 1] == '\n'))
                {
                    var inner = new InvalidDataException("Invalid input, please see 'https://www.kernel.org/pub/software/scm/git/docs/git-credential.html'.");
                    throw new InvalidOperationException(inner.Message, inner);
                }

                // The input ends with LFLF, check for that and break the read loop unless
                // input is coming from CLRF system, in which case it'll be CLRFCLRF.
                if ((buffer[read - 2] == '\n'
                        && buffer[read - 1] == '\n')
                    || (buffer[read - 4] == '\r'
                        && buffer[read - 3] == '\n'
                        && buffer[read - 2] == '\r'
                        && buffer[read - 1] == '\n'))
                    break;
            }

            // Git uses UTF-8 for string, don't let the OS decide how to decode it instead
            // we'll actively decode the UTF-8 block ourselves.
            string input = Encoding.UTF8.GetString(buffer, 0, read);

            string username = null;
            string password = null;

            // The `StringReader` is just useful.
            using (var reader = new StringReader(input))
            {
                string line;
                while (!string.IsNullOrWhiteSpace((line = reader.ReadLine())))
                {
                    string[] pair = line.Split(new[] { '=' }, 2);

                    if (pair.Length == 2)
                    {
                        if ("protocol".Equals(pair[0], StringComparison.Ordinal))
                        {
                            _queryProtocol = pair[1];
                        }
                        else if ("host".Equals(pair[0], StringComparison.Ordinal))
                        {
                            _queryHost = pair[1];
                        }
                        else if ("path".Equals(pair[0], StringComparison.Ordinal))
                        {
                            _queryPath = pair[1];
                        }
                        else if ("username".Equals(pair[0], StringComparison.Ordinal))
                        {
                            username = pair[1];
                        }
                        else if ("password".Equals(pair[0], StringComparison.Ordinal))
                        {
                            password = pair[1];
                        }
                        // This is a GCM only addition to the Git-Credential specification. The intent is to
                        // facilitate debugging without the need for running git.exe to debug git-remote-http(s)
                        // command line values.
                        else if ("_url".Equals(pair[0], StringComparison.Ordinal))
                        {
                            _gitRemoteHttpCommandLine = pair[1];
                        }
                    }
                }
            }

            // Cache the username and password provided by the caller (Git).
            if (username != null)
            {
                _username = username;

                // Only bother checking the password if there was a username credentials
                // cannot be only a password (though passwords are optional).
                if (password != null)
                {
                    _credentials = new Credential(username, password);
                }
            }

            CreateTargetUri();
        }

        /// <summary>
        /// Sets the value of `<seealso cref="Credentials"/>`.
        /// </summary>
        /// <param name="username">The username of the newly set credentials.</param>
        /// <param name="password">The password of the newly set credentials.</param>
        /// <exception cref="ArgumentNullException">When `<paramref name="username"/>` is `<see langword="null"/>`.</exception>
        public virtual void SetCredentials(string username, string password)
        {
            if (username is null)
                throw new ArgumentNullException(nameof(username));

            _credentials = new Credential(username, password);
        }

        /// <summary>
        /// Sets the URL of the proxy to be used during network operations.
        /// <para/>
        /// When `<paramref name="url"/>` is `<see langword="null"/>` the proxy value is cleared.
        /// </summary>
        /// <param name="url">The uniform-resource-locater of the proxy.</param>
        public virtual void SetProxy(string url)
        {
            if (Uri.TryCreate(url, UriKind.Absolute, out Uri tmp))
            {
                Trace.WriteLine($"successfully set proxy to '{tmp.AbsoluteUri}'.");
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(url))
                {
                    Trace.WriteLine($"failed to parse '{url}'.");
                }

                Trace.WriteLine("proxy cleared.");
            }

            ProxyUri = tmp;
        }

        /// <summary>
        /// Sets the `<seealso cref="TargetUri"/>` to be used during network operations.
        /// </summary>
        /// <param name="targetUri">The value to set.</param>
        /// <exception cref="ArgumentNullException">When `<paramref name="targetUri"/>` is `<see langword="null"/>`.</exception>
        public void SetTargetUri(Uri targetUri)
        {
            if (targetUri is null)
                throw new ArgumentNullException(nameof(targetUri));

            _queryProtocol = targetUri.Scheme;
            _queryHost = (targetUri.IsDefaultPort)
                ? targetUri.Host
                : string.Format(InvariantCulture, "{0}:{1}", targetUri.Host, targetUri.Port);
            _queryPath = targetUri.AbsolutePath;

            CreateTargetUri();
        }

        /// <summary>
        /// Returns the current value of this instance in credential protocol format.
        /// <para/>
        /// Please see 'https://www.kernel.org/pub/software/scm/git/docs/git-credential.html' for information about credential protocol format.
        /// </summary>
        public override string ToString()
        {
            var builder = new StringBuilder();

            builder.Append("protocol=")
                   .Append(_queryProtocol ?? string.Empty)
                   .Append('\n');
            builder.Append("host=")
                   .Append(_queryHost ?? string.Empty)
                   .Append('\n');
            builder.Append("path=")
                   .Append(_queryPath ?? string.Empty)
                   .Append('\n');

            if (_credentials == null)
            {
                builder.Append("username=")
                   .Append(_username ?? string.Empty)
                   .Append('\n');
            }
            else
            {
                // Only write out username if we know it.
                if (_credentials.Username != null)
                {
                    builder.Append("username=")
                           .Append(_credentials.Username)
                           .Append('\n');
                }

                // Only write out password if we know it.
                if (_credentials.Password != null)
                {
                    builder.Append("password=")
                           .Append(_credentials.Password)
                           .Append('\n');
                }
            }

            builder.Append('\n');

            return builder.ToString();
        }

        /// <summary>
        /// Writes the UTF-8 encoded value of `<see cref="ToString"/>` directly to `<paramref name="writableStream"/>`.
        /// </summary>
        /// <param name="writableStream">A `<see cref="Stream"/>` to write to.</param>
        /// <exception cref="ArgumentNullException">When `<paramref name="writableStream"/>` is `<see langword="null"/>`.</exception>
        /// <exception cref="ArgumentException">When `<paramref name="writableStream"/>` not writable.</exception>
        public virtual void WriteToStream(Stream writableStream)
        {
            if (writableStream is null)
                throw new ArgumentNullException(nameof(writableStream));

            if (!writableStream.CanWrite)
            {
                var inner = new InvalidDataException("Stream must be writable.");
                throw new ArgumentException(inner.Message, nameof(writableStream), inner);
            }

            // Git reads/writes UTF-8, we'll explicitly encode to Utf-8 to avoid NetFx or the
            // operating system making the wrong encoding decisions.
            string output = ToString();
            byte[] bytes = Encoding.UTF8.GetBytes(output);

            // write the bytes.
            writableStream.Write(bytes, 0, bytes.Length);
        }

        internal virtual void CreateTargetUri()
        {
            string queryUrl = null;
            string proxyUrl = _proxyUri?.OriginalString;
            string actualUrl = null;

            var buffer = new StringBuilder();

            // URI format is {protocol}://{username}@{host}/{path] with
            // everything optional except for {host}.

            // Protocol.
            if (!string.IsNullOrWhiteSpace(_queryProtocol))
            {
                buffer.Append(_queryProtocol)
                      .Append("://");
            }

            // Username.
            if (!string.IsNullOrWhiteSpace(_username))
            {
                var username = NeedsToBeEscaped(_username)
                    ? Uri.EscapeDataString(_username)
                    : _username;

                buffer.Append(username)
                      .Append('@');
            }

            // Host.
            buffer.Append(_queryHost)
                  .Append('/');

            // Path
            if (!string.IsNullOrWhiteSpace(_queryPath))
            {
                buffer.Append(_queryPath);
            }

            queryUrl = buffer.ToString();

            // if path is specified we should give the host/path priority
            if (!string.IsNullOrWhiteSpace(_queryPath))
            {
                actualUrl = queryUrl;
            }
            // If the actual-url override has been set, honor it.
            else if (!string.IsNullOrEmpty(_urlOverride))
            {
                if (Uri.TryCreate(_urlOverride, UriKind.Absolute, out Uri uri))
                {
                    actualUrl = uri.ToString();
                }
                else
                {
                    Trace.WriteLine($"failed to parse \"{_urlOverride}\", unable to set URL override.");
                }
            }
            // If the git-remote-http(s) command line has been captured,
            // try and parse it and provide the command-url .
            else if (!string.IsNullOrEmpty(_gitRemoteHttpCommandLine))
            {
                string[] parts = _gitRemoteHttpCommandLine.Split(' ');

                switch (parts.Length)
                {
                    case 1:
                    {
                        if (Uri.TryCreate(parts[0], UriKind.Absolute, out Uri uri))
                        {
                            actualUrl = uri.ToString();
                        }
                        else
                        {
                            Trace.WriteLine($"failed to parse \"{parts[0]}\", unable to set URL override.");
                        }
                    }
                    break;

                    case 3:
                    {
                        if (Uri.TryCreate(parts[2], UriKind.Absolute, out Uri uri))
                        {
                            actualUrl = uri.ToString();
                        }
                        else
                        {
                            Trace.WriteLine($"failed to parse \"{parts[2]}\", unable to set URL override.");
                        }
                    }
                    break;
                }
            }

            // Create the target URI object.
            _targetUri = new TargetUri(queryUrl, proxyUrl, actualUrl);
        }

        internal static bool NeedsToBeEscaped(string value)
        {
            for (int i = 0; i < value.Length; i += 1)
            {
                switch (value[i])
                {
                    case ':':
                    case '/':
                    case '\\':
                    case '?':
                    case '#':
                    case '[':
                    case ']':
                    case '@':
                    case '!':
                    case '$':
                    case '&':
                    case '\'':
                    case '(':
                    case ')':
                    case '*':
                    case '+':
                    case ',':
                    case ';':
                    case '=':
                    return true;
                }
            }
            return false;
        }
    }
}
