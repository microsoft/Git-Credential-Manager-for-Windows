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
        public TargetUri(Uri actualUri, Uri queryUri, Uri proxyUri)
        {
            if (actualUri == null)
                throw new ArgumentNullException(nameof(actualUri));
            if (!actualUri.IsAbsoluteUri)
                throw new ArgumentException("Uri is not absolute.", nameof(actualUri));
            if (queryUri != null && !queryUri.IsAbsoluteUri)
                throw new ArgumentException("Uri is not absolute.", nameof(queryUri));
            if (proxyUri != null && !proxyUri.IsAbsoluteUri)
                throw new ArgumentException("Uri is not absolute.", nameof(proxyUri));

            ActualUri = actualUri;
            ProxyUri = proxyUri;
            QueryUri = queryUri ?? actualUri;
        }

        public TargetUri(Uri target)
            : this(target, null, null)
        { }

        public TargetUri(string actualUrl, string queryUrl, string proxyUrl)
        {
            if (actualUrl == null)
                throw new ArgumentNullException(nameof(actualUrl));

            Uri actualUri = null;
            Uri proxyUri = null;
            Uri queryUri = null;

            if (!Uri.TryCreate(actualUrl, UriKind.Absolute, out actualUri))
                throw new UriFormatException(nameof(actualUrl));

            if (queryUrl != null && !Uri.TryCreate(queryUrl, UriKind.Absolute, out queryUri))
                throw new UriFormatException(nameof(queryUrl));

            if (queryUrl != null && !queryUri.Scheme.Equals(actualUri.Scheme, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException($"The `{actualUrl}` and `{queryUrl}` parameters must use the same protocol.", nameof(queryUri));

            if (proxyUrl != null && !Uri.TryCreate(proxyUrl, UriKind.Absolute, out proxyUri))
                throw new UriFormatException(nameof(queryUrl));

            ActualUri = actualUri;
            ProxyUri = proxyUri;
            QueryUri = queryUri ?? actualUri;
        }

        public TargetUri(string targetUrl)
            : this(targetUrl, null, null)
        { }

        /// <summary>
        /// Gets the <see cref="Uri.AbsolutePath"/> of the <see cref="QueryUri"/>.
        /// </summary>
        public string AbsolutePath
        {
            get { return QueryUri.AbsolutePath; }
        }

        /// <summary>
        /// The actual <see cref="Uri"/> of the target.
        /// </summary>
        public readonly Uri ActualUri;

        /// <summary>
        /// Gets the <see cref="Uri.DnsSafeHost"/> of the <see cref="QueryUri"/>.
        /// </summary>
        public string DnsSafeHost
        {
            get { return QueryUri.DnsSafeHost; }
        }

        /// <summary>
        /// Gets the <see cref="Uri.Host"/> of the <see cref="QueryUri"/>.
        /// </summary>
        public string Host
        {
            get { return QueryUri.Host; }
        }

        /// <summary>
        /// Gets whether the <see cref="QueryUri"/> is absolute.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
        public bool IsAbsoluteUri { get { return true; } }

        /// <summary>
        /// Gets whether the port value of the <see cref="QueryUri"/> is the default for this scheme.
        /// </summary>
        public bool IsDefaultPort { get { return QueryUri.IsDefaultPort; } }

        /// <summary>
        /// Gets the <see cref="Uri.Port"/> of the <see cref="QueryUri"/>.
        /// </summary>
        public int Port
        {
            get { return QueryUri.Port; }
        }

        /// <summary>
        /// The proxy <see cref="Uri"/> of the target if it exists; otherwise <see langword="null"/>.
        /// </summary>
        public readonly Uri ProxyUri;

        /// <summary>
        /// Gets the <see cref="Uri"/> that should be used for all queries.
        /// </summary>
        public readonly Uri QueryUri;

        /// <summary>
        /// Gets the <see cref="Uri.Scheme"/> name of the <see cref="QueryUri"/>.
        /// </summary>
        public string Scheme
        {
            get { return QueryUri.Scheme; }
        }

        /// <summary>
        /// Determines whether the <see cref="QueryUri"/> is a base of the specified <see cref="Uri"/>.
        /// </summary>
        /// <param name="uri">The <see cref="Uri"/> to test.</param>
        /// <returns><see langword="True"/> if is a base of <param name="uri"/>; otherwise, <see langword="false"/>.</returns>
        public bool IsBaseOf(Uri uri)
        {
            return QueryUri.IsBaseOf(uri);
        }

        /// <summary>
        /// Determines whether the <see cref="QueryUri"/> is a base of the specified <paramref name="targetUri"/>.
        /// </summary>
        /// <param name="targetUri">The <see cref="TargetUri"/> to test.</param>
        /// <returns>
        /// <see langword="True"/> if is a base of <param name="targetUri"/>; otherwise, <see langword="false"/>.
        /// </returns>
        public bool IsBaseOf(TargetUri targetUri)
        {
            if (targetUri == null)
                return false;

            return QueryUri.IsBaseOf(targetUri.ActualUri);
        }

        /// <summary>
        /// Gets the client header enabled to work with proxies as necessary.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
        public HttpClientHandler HttpClientHandler
        {
            get
            {
                bool useProxy = ProxyUri != null;

                var client = new HttpClientHandler()
                {
                    AllowAutoRedirect = true,
                    UseCookies = true,
                    UseProxy = useProxy,
                    MaxAutomaticRedirections = Global.MaxAutomaticRedirections,
                    UseDefaultCredentials = true,
                };

                if (useProxy)
                {
                    client.Proxy = WebProxy;
                }

                return client;
            }
        }

        /// <summary>
        /// Gets a canonical string representation for the <see cref="QueryUri"/>.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return ToString(false, true, true);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed")]
        public string ToString(bool username = false, bool port = false, bool path = false)
        {
            // Start building up a url with the scheme
            var url = QueryUri.Scheme + "://";

            // Append the username if asked for an it exiusts.
            if (username && QueryUri.UserInfo != null)
            {
                url += (QueryUri.UserEscaped)
                        ? QueryUri.UserInfo
                        : Uri.EscapeDataString(QueryUri.UserInfo);
                url += '@';
            }

            // Append the host name
            url += QueryUri.Host;

            // Append the port information if asked for and relevant
            if ((port && !QueryUri.IsDefaultPort))
            {
                url += ':';
                url += QueryUri.Port;
            }

            // Append some amount of path to the url
            if (path)
            {
                url += QueryUri.AbsolutePath;
            }
            else
            {
                url += '/';
            }

            return url;
        }

        /// <summary>
        /// Gets the web proxy abstraction for working with proxies as necessary.
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
                        // if no explicit proxy authentication, set to use default (Credentials will
                        // be set to DefaultCredentials automatically)
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
                : targetUri.QueryUri;
        }

        public static implicit operator TargetUri(Uri uri)
        {
            return ReferenceEquals(uri, null)
                ? null
                : new TargetUri(uri);
        }

        /// <summary>
        /// Get a version of this <see cref="TargetUri"/> that contains the specified username.
        /// </summary>
        /// <remarks>
        /// If the <see cref="TargetUri"/> already contains a username, that one is kept NOT overwritten.
        /// </remarks>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2234:PassSystemUriObjectsInsteadOfStrings")]
        public TargetUri GetPerUserTargetUri(string username)
        {
            // belt and braces, don't add a username if the URI already contains one.
            if (string.IsNullOrWhiteSpace(username) || TargetUriContainsUsername)
            {
                return this;
            }

            var encodedUsername = Uri.EscapeDataString(username);
            return new TargetUri(ActualUri.AbsoluteUri.Replace(Host, encodedUsername + "@" + Host));
        }

        /// <summary>
        /// Get a version of this <see cref="TargetUri"/> that does NOT contain any username.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1024:UsePropertiesWhereAppropriate")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2234:PassSystemUriObjectsInsteadOfStrings")]
        public TargetUri GetHostTargetUri()
        {
            // belt and braces, don't add a username if the URI already contains one.
            if (!TargetUriContainsUsername)
            {
                return this;
            }

            // default ToString() does not include any UserInfo
            return new TargetUri(ToString());
        }

        /// <summary>
        /// Determine if the ActualUri of this <see cref="TargetUri"/> contains UserInfo
        /// </summary>
        public bool TargetUriContainsUsername { get { return ActualUri.AbsoluteUri.Contains("@"); } }

        /// <summary>
        /// Get username contained in the ActualUri of this <see cref="TargetUri"/>
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1056:UriPropertiesShouldNotBeStrings")]
        public string TargetUriUsername { get { return ActualUri.UserInfo; } }
    }
}
