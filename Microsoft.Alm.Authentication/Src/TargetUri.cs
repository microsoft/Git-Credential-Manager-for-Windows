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
using System.Text;

namespace Microsoft.Alm.Authentication
{
    /// <summary>
    /// Represents a complex `<see cref="Uri"/>` with optional proxy data.
    /// </summary>
    public sealed class TargetUri
    {
        public TargetUri(Uri queryUri, Uri proxyUri, Uri actualUri)
        {
            if (queryUri is null)
                throw new ArgumentNullException(nameof(queryUri));
            if (!queryUri.IsAbsoluteUri)
            {
                var inner = new UriFormatException("Uri must be absolute.");
                throw new ArgumentException(inner.Message, nameof(queryUri), inner);
            }
            if (proxyUri != null && !proxyUri.IsAbsoluteUri)
            {
                var inner = new UriFormatException("Uri must be absolute.");
                throw new ArgumentException(inner.Message, nameof(proxyUri), inner);
            }
            if (actualUri != null && !queryUri.IsAbsoluteUri)
            {
                var inner = new UriFormatException("Uri must be absolute.");
                throw new ArgumentException(inner.Message, nameof(actualUri), inner);
            }

            _proxyUri = proxyUri;
            _queryUri = queryUri;
            _actualUri = actualUri;
        }

        public TargetUri(Uri queryUri, Uri proxyUri)
            : this(queryUri, proxyUri, null)
        { }

        public TargetUri(Uri target)
            : this(target, null, null)
        { }

        public TargetUri(string queryUrl, string proxyUrl, string actualUrl)
        {
            if (queryUrl is null)
                throw new ArgumentNullException(nameof(queryUrl));

            Uri proxyUri = null;
            Uri queryUri = null;
            Uri actualUri = null;

            if (!Uri.TryCreate(queryUrl, UriKind.Absolute, out queryUri))
                throw new UriFormatException(nameof(queryUrl));

            if (proxyUrl != null && !Uri.TryCreate(proxyUrl, UriKind.Absolute, out proxyUri))
                throw new UriFormatException(nameof(queryUrl));

            if (actualUrl != null && !Uri.TryCreate(actualUrl, UriKind.Absolute, out actualUri))
                throw new UriFormatException(nameof(actualUrl));

            _proxyUri = proxyUri;
            _queryUri = queryUri;
            _actualUri = actualUri;
        }

        public TargetUri(string targetUrl, string proxyUrl)
            : this(targetUrl, proxyUrl, null)
        { }

        public TargetUri(string targetUrl)
            : this(targetUrl, null, null)
        { }

        private readonly Uri _actualUri;
        private readonly Uri _proxyUri;
        private readonly Uri _queryUri;

        /// <summary>
        /// Gets the `<see cref="Uri.AbsolutePath"/>` of the `<see cref="QueryUri"/>`.
        /// </summary>
        public string AbsolutePath
        {
            get { return QueryUri.AbsolutePath; }
        }

        /// <summary>
        /// Gets or sets the actual URI credentials are being sought for.
        /// <para/>
        /// Informational only, and should not be used in-place of `<seealso cref="QueryUri"/>`.
        /// <para/>
        /// When fulfilling Git credential requests, represents the remote URL specified in the git-remote-http(s).exe invocation which initiated this GCM instance.
        /// <para/>
        /// Default value is `<see langword="null"/>`.
        /// </summary>
        public Uri ActualUri
        {
            get { return _actualUri; }
        }

        /// <summary>
        /// Returns `<see langword="true"/>` if `<see cref="ActualUri"/>` contains `<see cref="Uri.UserInfo"/>`; otherwise `<see langword="false"/>`.
        /// </summary>
        public bool ContainsUserInfo
        {
            get { return QueryUri.AbsoluteUri.IndexOf('@') >= 0; }
        }

        /// <summary>
        /// Gets the `<see cref="Uri.DnsSafeHost"/>` of the `<see cref="QueryUri"/>`.
        /// </summary>
        public string DnsSafeHost
        {
            get { return QueryUri.DnsSafeHost; }
        }

        /// <summary>
        /// Gets `<see langword="true"/>` if `<seealso cref="QueryUri"/>` contains path information; otherwise `<see langword="false"/>`.
        /// </summary>
        public bool HasPath
        {
            get
            {
                return _queryUri != null
                    && _queryUri.LocalPath != null
                    && _queryUri.LocalPath.Length > 1;
            }
        }

        /// <summary>
        /// Gets the `<see cref="Uri.Host"/>` of the `<see cref="QueryUri"/>`.
        /// </summary>
        public string Host
        {
            get { return QueryUri.Host; }
        }

        /// <summary>
        /// Gets whether the `<see cref="QueryUri"/>` is absolute.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1822:MarkMembersAsStatic")]
        public bool IsAbsoluteUri
        {
            get { return true; }
        }

        /// <summary>
        /// Gets whether the port value of the `<see cref="QueryUri"/>` is the default for this scheme.
        /// </summary>
        public bool IsDefaultPort
        {
            get { return QueryUri.IsDefaultPort; }
        }

        /// <summary>
        /// Gets the `<see cref="Uri.Port"/>` of the `<see cref="QueryUri"/>`.
        /// </summary>
        public int Port
        {
            get { return QueryUri.Port; }
        }

        /// <summary>
        /// Gets the proxy `<see cref="Uri"/>` of the target if it exists; otherwise `<see langword="null"/>`.
        /// </summary>
        public Uri ProxyUri
        {
            get { return _proxyUri; }
        }

        /// <summary>
        /// Gets the `<see cref="Uri"/>` that should be used for all queries.
        /// </summary>
        public Uri QueryUri
        {
            get { return _queryUri; }
        }

        /// <summary>
        /// Gets the `<see cref="Uri.Scheme"/>` name of the `<see cref="QueryUri"/>`.
        /// </summary>
        public string Scheme
        {
            get { return QueryUri.Scheme; }
        }

        /// <summary>
        /// Gets the `<see cref="Uri.UserInfo"/>` from `<see cref="ActualUri"/>`.
        /// </summary>
        public string UserInfo
        {
            get { return QueryUri.UserInfo; }
        }

        /// <summary>
        /// Returns a new `<seealso cref="TargetUri"/>` based on this instance combined with `<paramref name="queryUrl"/>`, `<paramref name="proxyUrl"/>`, and/or `<paramref name="actualUrl"/>`.
        /// </summary>
        /// <param name="queryUrl">
        /// The new URL used for all queries if not `<see langword="null"/>`; otherwise is unchanged from `<seealso cref="QueryUri"/>`.
        /// </param>
        /// <param name="proxyUrl">
        /// The new URL used for all queries if not `<see langword="null"/>`; otherwise is unchanged from `<seealso cref="ProxyUri"/>`.
        /// </param>
        /// <param name="actualUrl">
        /// The new URL used for all queries if not `<see langword="null"/>`; otherwise is unchanged from `<seealso cref="ActualUri"/>`.
        /// </param>
        /// <exception cref="ArgumentException">When all arguments are `<see langword="null"/>`.</exception>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed")]
        public TargetUri CreateWith(string queryUrl = null, string proxyUrl = null, string actualUrl = null)
        {
            if (queryUrl is null && proxyUrl is null && actualUrl is null)
                throw new ArgumentException("At least one argument must be not `null`.");

            queryUrl = queryUrl ?? _queryUri?.ToString();
            proxyUrl = proxyUrl ?? _proxyUri?.ToString();
            actualUrl = actualUrl ?? _actualUri?.ToString();

            return new TargetUri(queryUrl, proxyUrl, actualUrl);
        }

        /// <summary>
        /// Returns a new `<seealso cref="TargetUri"/>` based on this instance combined with `<paramref name="queryUri"/>`, `<paramref name="proxyUri"/>`, and/or `<paramref name="commandUri"/>`.
        /// </summary>
        /// <param name="queryUri">
        /// The new `<seealso cref="Uri"/>` used for all queries if not `<see langword="null"/>`; otherwise is unchanged from `<seealso cref="QueryUri"/>`.
        /// </param>
        /// <param name="proxyUri">
        /// The new `<seealso cref="Uri"/>` used for all queries if not `<see langword="null"/>`; otherwise is unchanged from `<seealso cref="ProxyUri"/>`.
        /// </param>
        /// <param name="actualUri">
        /// The new `<seealso cref="Uri"/>` used for all queries if not `<see langword="null"/>`; otherwise is unchanged from `<seealso cref="ActualUri"/>`.
        /// </param>
        /// <exception cref="ArgumentException">When all arguments are `<see langword="null"/>`.</exception>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1026:DefaultParametersShouldNotBeUsed")]
        public TargetUri CreateWith(Uri queryUri = null, Uri proxyUri = null, Uri actualUri = null)
        {
            if (queryUri is null && proxyUri is null && actualUri is null)
                throw new ArgumentException("At least one argument must be not `null`.");

            queryUri = queryUri ?? _queryUri;
            proxyUri = proxyUri ?? _proxyUri;
            actualUri = actualUri ?? _actualUri;

            return new TargetUri(queryUri, proxyUri, actualUri);
        }

        /// <summary>
        /// Returns a version of this `<see cref="TargetUri"/>` that contains the specified username.
        /// </summary>
        /// <remarks>
        /// If the `<see cref="TargetUri"/>` already contains a username, that one is kept NOT overwritten.
        /// </remarks>
        public TargetUri GetPerUserTargetUri(string username)
        {
            // belt and braces, don't add a username if the URI already contains one.
            if (string.IsNullOrWhiteSpace(username) || ContainsUserInfo)
            {
                return this;
            }

            var encodedUsername = Uri.EscapeDataString(username);
            return new TargetUri(QueryUri.AbsoluteUri.Replace(Host, encodedUsername + "@" + Host));
        }

        /// <summary>
        /// Determines whether the `<see cref="QueryUri"/>` is a base of the specified `<see cref="Uri"/>`.
        /// <para/>
        /// Returns `<see langword="true"/>` if is a base of `<param name="uri"/>`; otherwise, `<see langword="false"/>`.
        /// </summary>
        /// <param name="uri">The `<see cref="Uri"/>` to test.</param>
        public bool IsBaseOf(Uri uri)
        {
            return QueryUri.IsBaseOf(uri);
        }

        /// <summary>
        /// Determines whether the `<see cref="QueryUri"/>` is a base of the specified `<paramref name="targetUri"/>`.
        /// <para/>
        /// Returns `<see langword="true"/>` if is a base of `<param name="uri"/>`; otherwise, `<see langword="false"/>`.
        /// </summary>
        /// <param name="targetUri">The `<see cref="TargetUri"/>` to test.</param>
        public bool IsBaseOf(TargetUri targetUri)
        {
            if (targetUri == null)
                return false;

            return QueryUri.IsBaseOf(targetUri);
        }

        /// <summary>
        /// Gets a canonical string representation for the `<see cref="QueryUri"/>`.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return ToString(true, true, true);
        }

        public string ToString(bool username, bool port, bool path)
        {
            // Start building up a URL with the scheme.
            StringBuilder url = new StringBuilder();
            url.Append(QueryUri.Scheme)
               .Append("://");

            // Append the username if asked for an it exists.
            if (username && !string.IsNullOrWhiteSpace(QueryUri.UserInfo))
            {
                url.Append(Uri.UnescapeDataString(QueryUri.UserInfo))
                   .Append('@');
            }

            // Append the host name.
            url.Append(QueryUri.Host);

            // Append the port information if asked for and relevant.
            if ((port && !QueryUri.IsDefaultPort))
            {
                url.Append(':')
                   .Append(QueryUri.Port);
            }

            // Append some amount of path to the URL.
            if (path)
            {
                url.Append(QueryUri.AbsolutePath);
            }
            else
            {
                url.Append('/');
            }

            return url.ToString();
        }

        public static implicit operator Uri(TargetUri targetUri)
        {
            return targetUri?.QueryUri;
        }

        public static implicit operator TargetUri(Uri uri)
        {
            return uri is null
                ? null
                : new TargetUri(uri);
        }
    }
}
