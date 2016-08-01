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
using System.Net;
using System.Net.Http;

namespace Microsoft.Alm.Authentication
{
    /// <summary>
    /// Represents a potentially proxied <see cref="Uri"/>.
    /// </summary>
    public sealed class TargetUri
    {
        public TargetUri(Uri actualUri, Uri proxyUri)
        {
            if (actualUri == null)
                throw new ArgumentNullException("actualUri");
            if (!actualUri.IsAbsoluteUri)
                throw new ArgumentException("Uri is not absolute.", "actualUri");
            if (proxyUri != null && !proxyUri.IsAbsoluteUri)
                throw new ArgumentException("Uri is not absolute.", "proxyUri");

            ProxyUri = proxyUri;
            ActualUri = actualUri;
        }

        public TargetUri(Uri target)
            : this(target, null)
        { }

        public TargetUri(string actualUrl, string proxyUrl)
        {
            if (ReferenceEquals(actualUrl, null))
                throw new ArgumentNullException("actualUrl");
            if (!Uri.TryCreate(actualUrl, UriKind.Absolute, out ActualUri))
                throw new UriFormatException("actualUrl");
            if (!(ReferenceEquals(proxyUrl, null) || Uri.TryCreate(proxyUrl, UriKind.Absolute, out ProxyUri)))
                throw new UriFormatException("proxyUrl");
        }

        public TargetUri(string targetUrl)
            : this(targetUrl, null)
        { }

        /// <summary>
        /// Gets the <see cref="Uri.AbsolutePath"/> of the <see cref="ActualUri"/>.
        /// </summary>
        public string AbsolutePath
        {
            get { return ActualUri.AbsolutePath; }
        }
        /// <summary>
        /// The actual <see cref="Uri"/> of the target.
        /// </summary>
        public readonly Uri ActualUri;

        /// <summary>
        /// Gets the <see cref="Uri.DnsSafeHost"/> of the <see cref="ActualUri"/>.
        /// </summary>
        public string DnsSafeHost
        {
            get { return ActualUri.DnsSafeHost; }
        }

        /// <summary>
        /// Gets the <see cref="Uri.Host"/> of the <see cref="ActualUri"/>.
        /// </summary>
        public string Host
        {
            get { return ActualUri.Host; }
        }

        /// <summary>
        /// Gets whether the <see cref="ActualUri"/> is absolute.
        /// </summary>
        public bool IsAbsoluteUri { get { return true; } }

        /// <summary>
        /// Gets whether the port value of the <see cref="ActualUri"/> is the default for this scheme.
        /// </summary>
        public bool IsDefaultPort { get { return ActualUri.IsDefaultPort; } }

        /// <summary>
        /// Gets the <see cref="Uri.Port"/> of the <see cref="ActualUri"/>.
        /// </summary>
        public int Port
        {
            get { return ActualUri.Port; }
        }

        /// <summary>
        /// The proxy <see cref="Uri"/> of the target if it exists; otherwise <see langword="null"/>.
        /// </summary>
        public readonly Uri ProxyUri;

        /// <summary>
        /// Gets the <see cref="Uri.Scheme"/> name of the <see cref="ActualUri"/>.
        /// </summary>
        public string Scheme
        {
            get { return ActualUri.Scheme; }
        }

        /// <summary>
        /// Determines whether the <see cref="ActualUri"/> is a base of the specified <see cref="Uri"/>.
        /// </summary>
        /// <param name="uri">The <see cref="Uri"/> to test.</param>
        /// <returns><see langword="True"/> if is a base of <param name="uri"/>; otherwise, <see langword="false"/>.</returns>
        public bool IsBaseOf(Uri uri)
        {
            return ActualUri.IsBaseOf(uri);
        }

        /// <summary>
        /// Determines whether the <see cref="ActualUri"/> is a base of the specified <see cref="TargetUri.ActualUri"/>.
        /// </summary>
        /// <param name="targetUri">The <see cref="TargetUri"/> to test.</param>
        /// <returns><see langword="True"/> if is a base of <param name="targetUri"/>; otherwise, <see langword="false"/>.</returns>
        public bool IsBaseOf(TargetUri targetUri)
        {
            if (targetUri == null)
                return false;

            return ActualUri.IsBaseOf(targetUri.ActualUri);
        }

        /// <summary>
        /// Gets a canonical string representation for the <see cref="ActualUri"/>.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return ActualUri.ToString();
        }

        /// <summary>
        /// Gets the client header enabled to work with proxies as necissary.
        /// </summary>
        public HttpClientHandler HttpClientHandler
        {
            get
            {
                bool useProxy = ProxyUri != null;
                return new HttpClientHandler()
                {
                    Proxy = WebProxy,
                    UseProxy = useProxy,
                    MaxAutomaticRedirections = 2,
                    UseDefaultCredentials = true
                };
            }
        }

        /// <summary>
        /// Gets the web proxy abstraction for working with proxies as necissary.
        /// </summary>
        public WebProxy WebProxy
        {
            get
            {
                if (ProxyUri != null)
                {
                    WebProxy proxy = new WebProxy(ProxyUri);

                    int tokenIndex = ProxyUri.UserInfo.IndexOf(':');
                    bool hasUserNameAndPassword = tokenIndex != -1;
                    bool hasAuthenticationSpecified = !string.IsNullOrWhiteSpace(ProxyUri.UserInfo);

                    // check if the user has specified authentications (comes as UserInfo)
                    if (hasAuthenticationSpecified && hasUserNameAndPassword)
                    {
                        string userName = ProxyUri.UserInfo.Substring(0, tokenIndex);
                        string password = ProxyUri.UserInfo.Substring(tokenIndex + 1);

                        NetworkCredential proxyCreds = new NetworkCredential(
                            userName,
                            password
                        );

                        proxy.UseDefaultCredentials = false;
                        proxy.Credentials = proxyCreds;
                    }
                    else
                    {
                        // if no explicit proxy authentication, set to use default (Credentials will be set to DefaultCredentials automatically)
                        proxy.UseDefaultCredentials = true;
                    }

                    return proxy;
                }
                else
                {
                    return new WebProxy();
                }
            }
        }

        public static implicit operator Uri(TargetUri targetUri)
        {
            return ReferenceEquals(targetUri, null)
                ? null
                : targetUri.ActualUri;
        }

        public static implicit operator TargetUri(Uri uri)
        {
            return ReferenceEquals(uri, null)
                ? null
                : new TargetUri(uri);
        }
    }
}
