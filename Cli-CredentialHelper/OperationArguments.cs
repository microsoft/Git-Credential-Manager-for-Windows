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
using System.Diagnostics;
using System.IO;
using System.Text;
using Microsoft.Alm.Authentication;

namespace Microsoft.Alm.CredentialHelper
{
    internal sealed class OperationArguments
    {
        internal OperationArguments(Stream readableStream)
            : this()
        {
            if (ReferenceEquals(readableStream, null))
                throw new ArgumentNullException("stdin");

            if (readableStream == Stream.Null || !readableStream.CanRead)
            {
                Console.Error.WriteLine("Fatal: unable to read input.");
                Environment.Exit(-1);
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
                                    this.QueryProtocol = pair[1];
                                    break;

                                case "host":
                                    this.QueryHost = pair[1];
                                    break;

                                case "path":
                                    this.QueryPath = pair[1];
                                    break;

                                case "username":
                                    this.CredUsername = pair[1];
                                    break;

                                case "password":
                                    this.CredPassword = pair[1];
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

            this.QueryProtocol = targetUri.Scheme;
            this.QueryHost = targetUri.Host;
            this.QueryPath = targetUri.AbsolutePath;

            this.CreateTargetUri();
        }
        private OperationArguments()
        {
            this.Authority = AuthorityType.Auto;
            this.Interactivity = Interactivity.Auto;
            this.UseModalUi = true;
            this.ValidateCredentials = true;
            this.WriteLog = false;
        }

        public AuthorityType Authority { get; set; }
        public string CredPassword { get; private set; }
        public string CredUsername { get; private set; }
        public string CustomNamespace { get; set; }
        public Interactivity Interactivity { get; set; }
        public bool PreserveCredentials { get; set; }
        public Uri ProxyUri
        {
            get { return _proxyUri; }
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
            get { return _queryUri; }
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
                }
                CreateTargetUri();
            }
        }
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

        private string _queryHost;
        private string _queryPath;
        private string _queryProtocol;
        private Uri _queryUri;
        private Uri _proxyUri;
        private TargetUri _targetUri;
        private bool _useHttpPath;

        public void SetCredentials(Credential credentials)
        {
            this.CredUsername = credentials.Username;
            this.CredPassword = credentials.Password;
        }

        public void SetProxy(string url)
        {
            Uri tmp;
            if (Uri.TryCreate(url, UriKind.Absolute, out tmp))
            {
                this.ProxyUri = tmp;
            }
        }

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();

            builder.Append("protocol=")
                   .Append(this.QueryProtocol ?? String.Empty)
                   .Append("\n");
            builder.Append("host=")
                   .Append(this.QueryHost ?? String.Empty)
                   .Append("\n");
            builder.Append("path=")
                   .Append(this.QueryPath ?? String.Empty)
                   .Append("\n");
            // only write out username if we know it
            if (this.CredUsername != null)
            {
                builder.Append("username=")
                       .Append(this.CredUsername)
                       .Append("\n");
            }
            // only write out password if we know it
            if (this.CredPassword != null)
            {
                builder.Append("password=")
                       .Append(this.CredPassword)
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
                throw new ArgumentNullException("writableStream");
            if (!writableStream.CanWrite)
                throw new ArgumentException("writableStream");

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
            string actualUrl = _useHttpPath
                ? String.Format("{0}://{1}/{2}", this.QueryProtocol, this.QueryHost, this.QueryPath)
                : String.Format("{0}://{1}", this.QueryProtocol, this.QueryHost);

            if (Uri.TryCreate(actualUrl, UriKind.Absolute, out _queryUri))
            {
                _targetUri = new TargetUri(_queryUri, _proxyUri);
            }
        }
    }
}
