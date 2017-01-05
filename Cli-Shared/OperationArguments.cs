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

using Microsoft.Alm.Authentication;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Microsoft.Alm.Cli
{
    internal sealed class OperationArguments
    {
        private static readonly char[] SeperatorCharacters = { '/', '\\' };

        internal OperationArguments(Stream readableStream)
            : this()
        {
            if (ReferenceEquals(readableStream, null))
                throw new ArgumentNullException(nameof(readableStream));

            if (readableStream == Stream.Null || !readableStream.CanRead)
            {
                Program.Die("Unable to read input.");
            }
            else
            {
                //
                byte[] buffer = new byte[4096];
                int read = 0;

                int r;
                while ((r = readableStream.Read(buffer, read, buffer.Length - read)) > 0)
                {
                    read += r;

                    // if we've filled the buffer, make it larger
                    // this could hit an out of memory condition, but that'd require
                    // the called to be attempting to do so, since that's not a secyity
                    // threat we can safely ignore that and allow NetFx to handle it
                    if (read == buffer.Length)
                    {
                        Array.Resize(ref buffer, buffer.Length * 2);
                    }

                    if ((read > 0 && read < 3 && buffer[read - 1] == '\n'))
                    {
                        Program.Die("Invalid input, please see 'https://www.kernel.org/pub/software/scm/git/docs/git-credential.html'.");
                    }

                    // the input ends with LFLF, check for that and break the read loop
                    // unless input is coming from CLRF system, in which case it'll be CLRFCLRF
                    if ((buffer[read - 2] == '\n'
                            && buffer[read - 1] == '\n')
                        || (buffer[read - 4] == '\r'
                            && buffer[read - 3] == '\n'
                            && buffer[read - 2] == '\r'
                            && buffer[read - 1] == '\n'))
                        break;
                }

                // Git uses UTF-8 for string, don't let the OS decide how to decode it
                // instead we'll actively decode the UTF-8 block ourselves
                string input = Encoding.UTF8.GetString(buffer, 0, read);

                // the `StringReader` is just useful
                using (StringReader reader = new StringReader(input))
                {
                    string line;
                    while (!String.IsNullOrWhiteSpace((line = reader.ReadLine())))
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

                this.CreateTargetUri();
            }
        }
        internal OperationArguments(Uri targetUri)
            : this()
        {
            if (ReferenceEquals(targetUri, null))
                throw new ArgumentNullException("targetUri");

            _queryProtocol = targetUri.Scheme;
            _queryHost = targetUri.Host;
            _queryPath = targetUri.AbsolutePath;

            this.CreateTargetUri();
        }
        private OperationArguments()
        {
            this.Authority = AuthorityType.Auto;
            this.Interactivity = Interactivity.Auto;
            this.UseModalUi = true;
            this.ValidateCredentials = true;
            this.WriteLog = false;
            _useHttpPath = false;
        }

        public AuthorityType Authority { get; set; }
        public string CredPassword
        {
            get { return _password; }
        }
        public string CredUsername
        {
            get { return _username; }
        }
        public string CustomNamespace { get; set; }
        /// <summary>
        /// Gets a map of the process's environmental variables keyed on case-insensitive names.
        /// </summary>
        public IReadOnlyDictionary<string, string> EnvironmentVariables
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
        /// Gets the process's Git configuration based on current working directory, user's folder, and Git's system directory.
        /// </summary>
        public Git.Configuration GitConfiguration
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
        public Interactivity Interactivity { get; set; }
        public string LoginHint { get; set; }
        public bool PreserveCredentials { get; set; }
        public Uri ProxyUri
        {
            get { return _targetUri.ProxyUri; }
            internal set
            {
                _proxyUri = value;
                CreateTargetUri();
            }
        }
        public string QueryHost
        {
            get { return _queryHost; }
            set
            {
                _queryHost = value;
                CreateTargetUri();
            }
        }
        public string QueryPath
        {
            get { return _queryPath; }
            set
            {
                _queryPath = value;
                CreateTargetUri();
            }
        }
        public string QueryProtocol
        {
            get { return _queryProtocol; }
            set
            {
                _queryProtocol = value;
                CreateTargetUri();
            }
        }
        public Uri QueryUri
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

                    // if there's a non-default port value, retain it
                    if (!value.IsDefaultPort)
                    {
                        _queryHost = $"{_queryHost}:{value.Port}";
                    }

                    // if there's a query segment, retain it
                    if (!string.IsNullOrWhiteSpace(value.Query))
                    {
                        _queryHost = $"{_queryHost}?{value.Query}";
                    }
                }
                CreateTargetUri();
            }
        }
        public bool UseConfigLocal { get; set; }
        public bool UseConfigSystem { get; set; }
        public TargetUri TargetUri
        {
            get { return _targetUri; }
        }
        public bool UseHttpPath
        {
            get { return _useHttpPath; }
            set
            {
                _useHttpPath = value;
                CreateTargetUri();
            }
        }
        public bool UseModalUi { get; set; }
        public bool ValidateCredentials { get; set; }
        public bool WriteLog { get; set; }

        private Git.Configuration _configuration;
        private Dictionary<string, string> _environmentVariables;
        private string _password;
        private Uri _proxyUri;
        private string _queryHost;
        private string _queryPath;
        private string _queryProtocol;
        private TargetUri _targetUri;
        private bool _useHttpPath;
        private string _username;

        public void LoadConfiguration()
        {
            _configuration = new Git.Configuration(Environment.CurrentDirectory, UseConfigLocal, UseConfigSystem);
        }

        public void SetCredentials(Credential credentials)
        {
            _username = credentials.Username;
            _password = credentials.Password;
        }

        public void SetProxy(string url)
        {
            Uri tmp = null;
            if (Uri.TryCreate(url, UriKind.Absolute, out tmp))
            {
                Git.Trace.WriteLine($"successfully set proxy to '{tmp.AbsoluteUri}'.");
            }
            else
            {
                Git.Trace.WriteLine("proxy cleared.");
            }
            this.ProxyUri = tmp;
        }

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();

            builder.Append("protocol=")
                   .Append(_queryProtocol ?? String.Empty)
                   .Append("\n");
            builder.Append("host=")
                   .Append(_queryHost ?? String.Empty)
                   .Append("\n");
            builder.Append("path=")
                   .Append(_queryPath ?? String.Empty)
                   .Append("\n");
            // only write out username if we know it
            if (_username != null)
            {
                builder.Append("username=")
                       .Append(_username)
                       .Append("\n");
            }
            // only write out password if we know it
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
        public void WriteToStream(Stream writableStream)
        {
            if (ReferenceEquals(writableStream, null))
                throw new ArgumentNullException(nameof(writableStream));
            if (!writableStream.CanWrite)
                throw new ArgumentException("CanWrite property returned false.", nameof(writableStream));

            // Git reads/writes UTF-8, we'll explicitly encode to Utf-8 to
            // avoid NetFx or the operating system making the wrong encoding
            // decisions.
            string output = ToString();
            byte[] bytes = Encoding.UTF8.GetBytes(output);

            // write the bytes.
            writableStream.Write(bytes, 0, bytes.Length);
        }

        internal void CreateTargetUri()
        {
            string actualUrl = null;
            string queryUrl = null;
            string proxyUrl = _proxyUri?.ToString();

            // when the target requests a path...
            if (UseHttpPath)
            {
                // and lacks a protocol...
                if (String.IsNullOrWhiteSpace(_queryProtocol))
                {
                    // and the target lacks a path: use just the host
                    if ((String.IsNullOrWhiteSpace(_queryPath)))
                    {
                        queryUrl = _queryHost;
                    }
                    // combine the host + path
                    else
                    {
                        queryUrl = $"{_queryHost}/{_queryPath}";
                    }
                }
                // and has a protocol...
                else
                {
                    // and the target lacks a path, combine protocol + host
                    if (String.IsNullOrWhiteSpace(_queryPath))
                    {
                        queryUrl = $"{_queryProtocol}://{_queryHost}";
                    }
                    // combine protocol + host + path
                    else
                    {
                        queryUrl = $"{_queryProtocol}://{_queryHost}/{_queryPath}";
                    }
                }

                actualUrl = queryUrl;
            }
            // when the target ignores paths...
            else
            {
                // and lacks a protocol...
                if (String.IsNullOrWhiteSpace(_queryProtocol))
                {
                    // and the host starts with "\\", strip the path...
                    if (_queryHost.StartsWith(@"\\", StringComparison.Ordinal))
                    {
                        int idx = _queryHost.IndexOfAny(SeperatorCharacters, 2);
                        queryUrl = (idx > 0)
                            ? _queryHost.Substring(0, idx)
                            : _queryHost;
                    }
                    // use just the host
                    else
                    {
                        queryUrl = _queryHost;
                    }

                    if (String.IsNullOrWhiteSpace(_queryPath))
                    {
                        actualUrl = queryUrl;
                    }
                    else
                    {
                        actualUrl = $"{queryUrl}/{_queryPath}";
                    }
                }
                // and the protocol is "file://" strip any path
                else if (_queryProtocol.StartsWith(Uri.UriSchemeFile, StringComparison.OrdinalIgnoreCase))
                {
                    int idx = _queryHost.IndexOfAny(SeperatorCharacters);

                    // strip the host as neccisary
                    queryUrl = (idx > 0)
                        ? _queryHost.Substring(0, idx)
                        : _queryHost;

                    // combine with protocol
                    queryUrl = $"{_queryProtocol}://{queryUrl}";

                    actualUrl = queryUrl;
                }
                // combine the protocol + host
                else
                {
                    queryUrl = $"{_queryProtocol}://{_queryHost}";

                    if (String.IsNullOrWhiteSpace(_queryPath))
                    {
                        actualUrl = queryUrl;
                    }
                    else
                    {
                        actualUrl = $"{queryUrl}/{_queryPath}";
                    }
                }
            }

            _targetUri = new TargetUri(actualUrl, queryUrl, proxyUrl);
        }
    }
}
