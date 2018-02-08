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
using Microsoft.Alm.Authentication;

namespace Microsoft.Alm.Cli
{
    internal class OperationArguments
    {
        private static readonly char[] SeperatorCharacters = { '/', '\\' };

        public OperationArguments(Stream readableStream)
                : this()
        {
            if (readableStream is null)
                throw new ArgumentNullException(nameof(readableStream));

            if (readableStream == Stream.Null || !readableStream.CanRead)
                throw new InvalidOperationException("Unable to read input.");

            byte[] buffer = new byte[4096];
            int read = 0;

            int r;
            while ((r = readableStream.Read(buffer, read, buffer.Length - read)) > 0)
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
                    throw new InvalidDataException("Invalid input, please see 'https://www.kernel.org/pub/software/scm/git/docs/git-credential.html'.");
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

            // The `StringReader` is just useful.
            using (StringReader reader = new StringReader(input))
            {
                string line;
                while (!string.IsNullOrWhiteSpace((line = reader.ReadLine())))
                {
                    string[] pair = line.Split(new[] { '=' }, 2);

                    if (pair.Length == 2)
                    {
                        switch (pair[0])
                        {
                            case "protocol":
                                _queryProtocol = pair[1];
                                break;

                            case "host":
                                _queryHost = pair[1];
                                break;

                            case "path":
                                _queryPath = pair[1];
                                break;

                            case "username":
                                _username = pair[1];
                                break;

                            case "password":
                                _password = pair[1];
                                break;
                        }
                    }
                }
            }

            CreateTargetUri();
        }

        public OperationArguments(Uri targetUri)
            : this()
        {
            if (targetUri is null)
                throw new ArgumentNullException("targetUri");

            _queryProtocol = targetUri.Scheme;
            _queryHost = (targetUri.IsDefaultPort)
                ? targetUri.Host
                : string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}:{1}", targetUri.Host, targetUri.Port);
            _queryPath = targetUri.AbsolutePath;

            CreateTargetUri();
        }

        public OperationArguments()
        {
            _authorityType = AuthorityType.Auto;
            _interactivity = Interactivity.Auto;
            _useLocalConfig = true;
            _useModalUi = true;
            _useSystemConfig = true;
            _validateCredentials = true;
            _vstsTokenScope = Program.VstsCredentialScope;
        }

        private AuthorityType _authorityType;
        private Git.Configuration _configuration;
        private string _customNamespace;
        private Dictionary<string, string> _environmentVariables;
        private Interactivity _interactivity;
        private string _password;
        private bool _preserveCredentials;
        private Uri _proxyUri;
        private string _queryHost;
        private string _queryPath;
        private string _queryProtocol;
        private TargetUri _targetUri;
        private TimeSpan? _tokenDuration;
        private bool _useHttpPath;
        private bool _useLocalConfig;
        private bool _useModalUi;
        private string _username;
        private bool _useSystemConfig;
        private bool _validateCredentials;
        private VstsTokenScope _vstsTokenScope;
        private bool _writeLog;

        public virtual AuthorityType Authority
        {
            get { return _authorityType; }
            set { _authorityType = value; }
        }

        public virtual string CredPassword
        {
            get { return _password; }
        }

        public virtual string CredUsername
        {
            get { return _username; }
        }

        public virtual string CustomNamespace
        {
            get { return _customNamespace; }
            set { _customNamespace = value; }
        }

        /// <summary>
        /// Gets a map of the process's environmental variables keyed on case-insensitive names.
        /// </summary>
        public virtual IReadOnlyDictionary<string, string> EnvironmentVariables
        {
            get
            {
                if (_environmentVariables == null)
                {
                    _environmentVariables = new Dictionary<string, string>(Program.EnvironKeyComparer);
                    var iter = Environment.GetEnvironmentVariables().GetEnumerator();
                    while (iter.MoveNext())
                    {
                        _environmentVariables[iter.Key as string] = iter.Value as string;
                    }
                }
                return _environmentVariables;
            }
        }

        /// <summary>
        /// Gets the process's Git configuration based on current working directory, user's folder,
        /// and Git's system directory.
        /// </summary>
        public virtual Git.Configuration GitConfiguration
        {
            get
            {
                if (_configuration == null)
                {
                    LoadConfiguration();
                }
                return _configuration;
            }
        }

        public virtual Interactivity Interactivity
        {
            get { return _interactivity; }
            set { _interactivity = value; }
        }

        public virtual bool PreserveCredentials
        {
            get { return _preserveCredentials; }
            set { _preserveCredentials = value; }
        }

        public virtual Uri ProxyUri
        {
            get { return _targetUri.ProxyUri; }
            internal set
            {
                _proxyUri = value;
                CreateTargetUri();
            }
        }

        public virtual string QueryHost
        {
            get { return _queryHost; }
            set
            {
                _queryHost = value;
                CreateTargetUri();
            }
        }

        public virtual string QueryPath
        {
            get { return _queryPath; }
            set
            {
                _queryPath = value;
                CreateTargetUri();
            }
        }

        public virtual string QueryProtocol
        {
            get { return _queryProtocol; }
            set
            {
                _queryProtocol = value;
                CreateTargetUri();
            }
        }

        public virtual Uri QueryUri
        {
            get { return _targetUri.QueryUri; }
            set
            {
                if (value == null)
                {
                    _queryHost = null;
                    _queryPath = null;
                    _queryProtocol = null;
                }
                else
                {
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
                }
                CreateTargetUri();
            }
        }

        public virtual bool UseConfigLocal
        {
            get { return _useLocalConfig; }
            set { _useLocalConfig = value; }
        }

        public virtual bool UseConfigSystem
        {
            get { return _useSystemConfig; }
            set { _useSystemConfig = value; }
        }

        public virtual TargetUri TargetUri
        {
            get { return _targetUri; }
        }

        public virtual TimeSpan? TokenDuration
        {
            get { return _tokenDuration; }
            set { _tokenDuration = value; }
        }

        public virtual bool UseHttpPath
        {
            get { return _useHttpPath; }
            set
            {
                _useHttpPath = value;
                CreateTargetUri();
            }
        }

        public virtual bool UseModalUi
        {
            get { return _useModalUi; }
            set { _useModalUi = value; }
        }

        public virtual bool ValidateCredentials
        {
            get { return _validateCredentials; }
            set { _validateCredentials = value; }
        }

        public virtual VstsTokenScope VstsTokenScope
        {
            get { return _vstsTokenScope; }
            set { _vstsTokenScope = value; }
        }

        public virtual bool WriteLog
        {
            get { return _writeLog; }
            set { _writeLog = true; }
        }

        public virtual void LoadConfiguration()
        {
            _configuration = Git.Configuration.ReadConfiuration(Environment.CurrentDirectory, UseConfigLocal, UseConfigSystem);
        }

        public virtual void SetCredentials(Credential credentials)
        {
            _username = credentials.Username;
            _password = credentials.Password;

            CreateTargetUri();
        }

        public virtual void SetCredentials(string username, string password)
        {
            var credentials = new Credential(username, password);

            SetCredentials(credentials);
        }

        public virtual void SetProxy(string url)
        {
            Uri tmp = null;
            if (Uri.TryCreate(url, UriKind.Absolute, out tmp))
            {
                Git.Trace.WriteLine($"successfully set proxy to '{tmp.AbsoluteUri}'.");
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(url))
                {
                    Git.Trace.WriteLine($"failed to parse '{url}'.");
                }

                Git.Trace.WriteLine("proxy cleared.");
            }
            ProxyUri = tmp;
        }

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();

            builder.Append("protocol=")
                   .Append(_queryProtocol ?? string.Empty)
                   .Append("\n");
            builder.Append("host=")
                   .Append(_queryHost ?? string.Empty)
                   .Append("\n");
            builder.Append("path=")
                   .Append(_queryPath ?? string.Empty)
                   .Append("\n");

            // Only write out username if we know it.
            if (_username != null)
            {
                builder.Append("username=")
                       .Append(_username)
                       .Append("\n");
            }

            // Only write out password if we know it.
            if (_password != null)
            {
                builder.Append("password=")
                       .Append(_password)
                       .Append("\n");
            }

            return builder.ToString();
        }

        /// <summary>
        /// Writes the UTF-8 encoded value of <see cref="ToString"/> directly to a <see cref="Stream"/>.
        /// </summary>
        /// <param name="writableStream">The <see cref="Stream"/> to write to.</param>
        public virtual void WriteToStream(Stream writableStream)
        {
            if (writableStream is null)
                throw new ArgumentNullException(nameof(writableStream));
            if (!writableStream.CanWrite)
                throw new ArgumentException("CanWrite property returned false.", nameof(writableStream));

            // Git reads/writes UTF-8, we'll explicitly encode to Utf-8 to avoid NetFx or the
            // operating system making the wrong encoding decisions.
            string output = ToString();
            byte[] bytes = Encoding.UTF8.GetBytes(output);

            // write the bytes.
            writableStream.Write(bytes, 0, bytes.Length);
        }

        internal virtual void CreateTargetUri()
        {
            string actualUrl = null;
            string queryUrl = null;
            string proxyUrl = _proxyUri?.OriginalString;

            // When the target requests a path...
            if (UseHttpPath)
            {
                // and lacks a protocol...
                if (string.IsNullOrWhiteSpace(_queryProtocol))
                {
                    // and the target lacks a path: use just the host.
                    if ((string.IsNullOrWhiteSpace(_queryPath)))
                    {
                        queryUrl = $"{_queryHost}/";

                        // If the username is known, include it in the actual URL.
                        if (!string.IsNullOrEmpty(_username))
                        {
                            var escapedUsername = Uri.EscapeDataString(_username);
                            actualUrl = $"{escapedUsername}@{_queryHost}/";
                        }
                    }
                    // Combine the host + path.
                    else
                    {
                        queryUrl = $"{_queryHost}/{_queryPath}";

                        // If the username is known, include it in the actual URL.
                        if (!string.IsNullOrEmpty(_username))
                        {
                            var escapedUsername = Uri.EscapeDataString(_username);
                            actualUrl = $"{escapedUsername}@{_queryHost}/{_queryPath}";
                        }
                    }
                }
                // and has a protocol...
                else
                {
                    // and the target lacks a path, combine protocol + host
                    if (string.IsNullOrWhiteSpace(_queryPath))
                    {
                        queryUrl = $"{_queryProtocol}://{_queryHost}/";

                        // If the username is known, include it in the actual URL.
                        if (!string.IsNullOrEmpty(_username))
                        {
                            var escapedUsername = Uri.EscapeDataString(_username);
                            actualUrl = $"{_queryProtocol}://{escapedUsername}@{_queryHost}/";
                        }
                    }
                    // Combine protocol + host + path.
                    else
                    {
                        queryUrl = $"{_queryProtocol}://{_queryHost}/{_queryPath}";

                        // If the username is known, include it in the actual URL.
                        if (!string.IsNullOrEmpty(_username))
                        {
                            var escapedUsername = Uri.EscapeDataString(_username);
                            actualUrl = $"{_queryProtocol}://{escapedUsername}@{_queryHost}/{_queryPath}";
                        }
                    }
                }
            }
            // When the target ignores paths...
            else
            {
                // and lacks a protocol...
                if (string.IsNullOrWhiteSpace(_queryProtocol))
                {
                    // and the host starts with "\\", strip the path...
                    if (_queryHost.StartsWith(@"\\", StringComparison.Ordinal))
                    {
                        int idx = _queryHost.IndexOfAny(SeperatorCharacters, 2);
                        queryUrl = (idx > 0)
                            ? _queryHost.Substring(0, idx)
                            : _queryHost;

                        queryUrl += '/';

                        // If the username is known, include it in the actual URL.
                        if (string.IsNullOrEmpty(_username))
                        {
                            actualUrl = queryUrl;
                        }
                        else
                        {
                            var escapedUsername = Uri.EscapeDataString(_username);
                            actualUrl = $"{_queryProtocol}://{escapedUsername}@{_queryHost}/";
                        }
                    }
                    // Use just the host.
                    else
                    {
                        queryUrl = $"{_queryHost}/";

                        // If the username is known, include it in the actual URL.
                        if (string.IsNullOrEmpty(_username))
                        {
                            actualUrl = queryUrl;
                        }
                        else
                        {
                            var escapedUsername = Uri.EscapeDataString(_username);
                            actualUrl = $"{_queryProtocol}://{escapedUsername}@{_queryHost}/";
                        }
                    }

                    // Append path information in the actual URL, but avoid empty paths.
                    if (!string.IsNullOrWhiteSpace(_queryPath) && _queryPath.Length > 1)
                    {
                        actualUrl += _queryPath;
                    }
                }
                // and the protocol is "file://" strip any path.
                else if (_queryProtocol.StartsWith(Uri.UriSchemeFile, StringComparison.OrdinalIgnoreCase))
                {
                    int idx = _queryHost.IndexOfAny(SeperatorCharacters);

                    // Strip the host as necessary.
                    queryUrl = (idx > 0)
                        ? _queryHost.Substring(0, idx)
                        : _queryHost;

                    // Combine with protocol.
                    queryUrl = $"{_queryProtocol}://{queryUrl}/";

                    // If the username is known, include it in the actual URL.
                    if (string.IsNullOrEmpty(_username))
                    {
                        actualUrl = queryUrl;
                    }
                    else
                    {
                        var escapedUsername = Uri.EscapeDataString(_username);
                        actualUrl = $"{_queryProtocol}://{escapedUsername}@{_queryHost}/";
                    }

                    // Append path information in the actual URL, but avoid empty paths.
                    if (!string.IsNullOrWhiteSpace(_queryPath) && _queryPath.Length > 1)
                    {
                        actualUrl += _queryPath;
                    }
                }
                // Combine the protocol + host.
                else
                {
                    queryUrl = $"{_queryProtocol}://{_queryHost}/";

                    // If the username is known, include it in the actual URL.
                    if (string.IsNullOrEmpty(_username))
                    {
                        actualUrl = queryUrl;
                    }
                    else
                    {
                        var escapedUsername = Uri.EscapeDataString(_username);
                        actualUrl = $"{_queryProtocol}://{escapedUsername}@{_queryHost}/";
                    }

                    // Append path information in the actual URL, but avoid empty paths.
                    if (!string.IsNullOrWhiteSpace(_queryPath) && _queryPath.Length > 1)
                    {
                        actualUrl += _queryPath;
                    }
                }
            }

            // Take the query URL if the actual URL is still unset.
            actualUrl = actualUrl ?? queryUrl;

            // Create the target URI object.
            _targetUri = new TargetUri(actualUrl, queryUrl, proxyUrl);
        }
    }
}
