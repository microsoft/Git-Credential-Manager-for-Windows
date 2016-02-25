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

            string line;
            while (!String.IsNullOrWhiteSpace((line = stdin.ReadLine())))
            {
                string[] pair = line.Split(new[] { '=' }, 2);

                if (pair.Length == 2)
                {
                    switch (pair[0])
                    {
                        case "protocol":
                            this.Protocol = pair[1];
                            break;

                        case "host":
                            this.Host = pair[1];
                            break;

                        case "path":
                            this.Path = pair[1];
                            break;

                        case "username":
                            this.Username = pair[1];
                            break;

                        case "password":
                            this.Password = pair[1];
                            break;
                    }
                }
            }

            this.CreateTargetUri();
        }

        public AuthorityType Authority { get; set; }
        public readonly string Host;
        public Uri HttpProxy
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
        public Uri HostUri { get { return _actualUri; } }
        public Interactivity Interactivity { get; set; }
        public readonly string Path;
        public string Password { get; private set; }
        public bool PreserveCredentials { get; set; }
        public readonly string Protocol;
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
        public TargetUri TargetUri
        {
            get { return _targetUri; }
        }
        public string Username { get; private set; }
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

        private Uri _actualUri;
        private string _proxyHost;
        private string _proxyPath;
        private string _proxyProtocol;
        private Uri _proxyUri;
        private TargetUri _targetUri;
        private bool _useHttpPath;

        public void SetCredentials(Credential credentials)
        {
            this.Username = credentials.Username;
            this.Password = credentials.Password;
        }

        public void SetProxy(string url)
        {
            Uri tmp;
            if (Uri.TryCreate(url, UriKind.Absolute, out tmp))
            {
                this.HttpProxy = tmp;
            }
        }

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder();

            builder.Append("protocol=")
                   .Append(this.Protocol ?? String.Empty)
                   .Append("\n");
            builder.Append("host=")
                   .Append(this.Host ?? String.Empty)
                   .Append("\n");
            builder.Append("path=")
                   .Append(this.Path ?? String.Empty)
                   .Append("\n");
            // only write out username if we know it
            if (this.Username != null)
            {
                builder.Append("username=")
                       .Append(this.Username)
                       .Append("\n");
            }
            // only write out password if we know it
            if (this.Password != null)
            {
                builder.Append("password=")
                       .Append(this.Password)
                       .Append("\n");
            }

            return builder.ToString();
        }

        internal void CreateTargetUri()
        {
            string actualUrl = _useHttpPath
                ? String.Format("{0}://{1}/{2}", this.Protocol, this.Host, this.Path)
                : String.Format("{0}://{1}", this.Protocol, this.Host);
            string proxyUrl = _useHttpPath
                ? String.Format("{0}://{1}/{2}", this.ProxyProtocol, this.ProxyHost, this.ProxyPath)
                : String.Format("{0}://{1}", this.ProxyProtocol, this.ProxyHost);

            if (Uri.TryCreate(actualUrl, UriKind.Absolute, out _actualUri)
                || Uri.TryCreate(proxyUrl, UriKind.Absolute, out _proxyUri))
            {
                _targetUri = new TargetUri(_actualUri, _proxyUri);
            }
        }
    }
}
