using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using Microsoft.Alm.Authentication;

namespace Microsoft.Alm.CredentialHelper
{
    internal sealed class OperationArguments
    {
        internal OperationArguments(TextReader stdin)
        {
            Debug.Assert(stdin != null, "The stdin parameter is null");

            this.Authority = AuthorityType.Auto;
            this.Interactivity = Interactivity.Auto;
            this.UseModalUi = true;
            this.ValidateCredentials = true;
            this.WriteLog = false;

            if (stdin == TextReader.Null)
            {
                Console.Error.WriteLine("Unable to read from stdin");
                Environment.Exit(-1);
            }
            else
            {
                string line;
                while (!String.IsNullOrWhiteSpace((line = stdin.ReadLine())))
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

                this.CreateTargetUri();
            }
        }

        public AuthorityType Authority { get; set; }
        public string CredPassword { get; private set; }
        public string CredUsername { get; private set; }
        public Interactivity Interactivity { get; set; }
        public bool PreserveCredentials { get; set; }
        public string ProxyHost
        {
            get { return _proxyHost; }
            set
            {
                _proxyHost = value;
                CreateTargetUri();
            }
        }
        public string ProxyPath
        {
            get { return _proxyPath; }
            set
            {
                _proxyPath = value;
                CreateTargetUri();
            }
        }
        public string ProxyProtocol
        {
            get { return _proxyProtocol; }
            set
            {
                _proxyProtocol = value;
                CreateTargetUri();
            }
        }
        public Uri ProxyUri
        {
            get { return _proxyUri; }
            internal set
            {
                if (value == null)
                {
                    _proxyHost = null;
                    _proxyPath = null;
                    _proxyProtocol = null;
                }
                else
                {
                    _proxyHost = value.DnsSafeHost;
                    _proxyPath = value.AbsolutePath;
                    _proxyProtocol = value.Scheme;
                }
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
        private string _proxyHost;
        private string _proxyPath;
        private string _proxyProtocol;
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

        internal void CreateTargetUri()
        {
            string actualUrl = _useHttpPath
                ? String.Format("{0}://{1}/{2}", this.QueryProtocol, this.QueryHost, this.QueryPath)
                : String.Format("{0}://{1}", this.QueryProtocol, this.QueryHost);
            string proxyUrl = _useHttpPath
                ? String.Format("{0}://{1}/{2}", this.ProxyProtocol, this.ProxyHost, this.ProxyPath)
                : String.Format("{0}://{1}", this.ProxyProtocol, this.ProxyHost);

            if (Uri.TryCreate(actualUrl, UriKind.Absolute, out _queryUri)
                | Uri.TryCreate(proxyUrl, UriKind.Absolute, out _proxyUri))
            {
                _targetUri = new TargetUri(_queryUri, _proxyUri);
            }
        }
    }
}
