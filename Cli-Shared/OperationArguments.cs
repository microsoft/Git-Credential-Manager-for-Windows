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

using Git = Microsoft.Alm.Authentication.Git;

namespace Microsoft.Alm.Cli
{
    internal class OperationArguments : Base
    {
        public OperationArguments(RuntimeContext context, Stream readableStream)
                : this(context)
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

            string username = null;
            string password = null;

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
                                {
                                    username = pair[1];
                                }
                                break;

                            case "password":
                                {
                                    password = pair[1];
                                }
                                break;
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

        public OperationArguments(RuntimeContext context, Uri targetUri)
            : this(context)
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

        public OperationArguments(RuntimeContext context)
            : base(context)
        {
            _authorityType = AuthorityType.Auto;
            _interactivity = Interactivity.Auto;
            _useLocalConfig = true;
            _useModalUi = true;
            _useSystemConfig = true;
            _validateCredentials = true;
            _vstsTokenScope = Program.VstsCredentialScope;
        }

        // Test-only constructor
        internal OperationArguments()
            : this(RuntimeContext.Default)
        { }

        private AuthorityType _authorityType;
        private Git.Configuration _configuration;
        private Credential _credentials;
        private string _customNamespace;
        private Dictionary<string, string> _environmentVariables;
        private Interactivity _interactivity;
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

        public virtual Credential Credentials
        {
            get { return _credentials; }
            set { _credentials = value; }
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
            get { return _configuration; }
        }

        public virtual Interactivity Interactivity
        {
            get { return _interactivity; }
            set { _interactivity = value; }
        }

        public virtual string Password
        {
            get { return _credentials?.Password; }
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

                // Re-create the target Uri.
                CreateTargetUri();
            }
        }

        public virtual string QueryHost
        {
            get { return _queryHost; }
            set
            {
                _queryHost = value;

                // Re-create the target Uri.
                CreateTargetUri();
            }
        }

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

        public virtual string QueryProtocol
        {
            get { return _queryProtocol; }
            set
            {
                _queryProtocol = value;

                // Re-create the target Uri.
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

                // Re-create the target Uri.
                CreateTargetUri();
            }
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

        public virtual bool UseHttpPath
        {
            get { return _useHttpPath; }
            set { _useHttpPath = value; }
        }

        public virtual bool UseModalUi
        {
            get { return _useModalUi; }
            set { _useModalUi = value; }
        }

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

        public virtual async Task LoadConfiguration()
        {
            _configuration = await Git.Configuration.ReadConfiuration(Context, Environment.CurrentDirectory, UseConfigLocal, UseConfigSystem);
        }

        public virtual void SetCredentials(string username, string password)
        {
            Credentials = new Credential(username, password);
        }

        public virtual void SetProxy(string url)
        {
            Uri tmp = null;

            if (Uri.TryCreate(url, UriKind.Absolute, out tmp))
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

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();

            builder.Append("protocol=")
                   .Append(_queryProtocol ?? string.Empty)
                   .Append('\n');
            builder.Append("host=")
                   .Append(_queryHost ?? string.Empty)
                   .Append('\n');
            builder.Append("path=")
                   .Append(_queryPath ?? string.Empty)
                   .Append('\n');

            // Only write out username if we know it.
            if (_credentials?.Username != null)
            {
                builder.Append("username=")
                       .Append(_credentials.Username)
                       .Append('\n');
            }

            // Only write out password if we know it.
            if (_credentials?.Password != null)
            {
                builder.Append("password=")
                       .Append(_credentials.Password)
                       .Append('\n');
            }

            builder.Append('\n');

            return builder.ToString();
        }

        /// <summary>
        /// Writes the UTF-8 encoded value of `<see cref="ToString"/>` directly to a `<see cref="Stream"/>`.
        /// </summary>
        /// <param name="writableStream">The `<see cref="Stream"/>` to write to.</param>
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
            string queryUrl = null;
            string proxyUrl = _proxyUri?.OriginalString;

            StringBuilder buffer = new StringBuilder();

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
                buffer.Append(_username)
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

            // Create the target URI object.
            _targetUri = new TargetUri(queryUrl, proxyUrl);
        }
    }
}
