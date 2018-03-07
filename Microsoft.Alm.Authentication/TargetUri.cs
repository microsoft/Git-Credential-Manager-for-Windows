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
        public TargetUri(Uri queryUri, Uri proxyUri)
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

            _proxyUri = proxyUri;
            _queryUri = queryUri;
        }

        public TargetUri(Uri target)
            : this(target, null)
        { }

        public TargetUri(string queryUrl, string proxyUrl)
        {
            if (queryUrl is null)
                throw new ArgumentNullException(nameof(queryUrl));

            Uri proxyUri = null;
            Uri queryUri = null;

            if (!Uri.TryCreate(queryUrl, UriKind.Absolute, out queryUri))
                throw new UriFormatException(nameof(queryUrl));

            if (proxyUrl != null && !Uri.TryCreate(proxyUrl, UriKind.Absolute, out proxyUri))
                throw new UriFormatException(nameof(queryUrl));

            _proxyUri = proxyUri;
            _queryUri = queryUri;
        }

        public TargetUri(string targetUrl)
            : this(targetUrl,  null)
        { }

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
        /// Gets the `<see cref="Uri.DnsSafeHost"/>` of the `<see cref="QueryUri"/>`.
        /// </summary>
        public string DnsSafeHost
        {
            get { return QueryUri.DnsSafeHost; }
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
        public bool IsAbsoluteUri { get { return true; } }

        /// <summary>
        /// Gets whether the port value of the `<see cref="QueryUri"/>` is the default for this scheme.
        /// </summary>
        public bool IsDefaultPort { get { return QueryUri.IsDefaultPort; } }

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
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1056:UriPropertiesShouldNotBeStrings")]
        public string TargetUriUsername { get { return QueryUri.UserInfo; } }

        /// <summary>
        /// Returns a version of this `<see cref="TargetUri"/>` that contains the specified username.
        /// </summary>
        /// <remarks>
        /// If the `<see cref="TargetUri"/>` already contains a username, that one is kept NOT overwritten.
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
        /// Returns `<see langword="true"/>` if `<see cref="ActualUri"/>` contains `<see cref="Uri.UserInfo"/>`; otherwise `<see langword="false"/>`.
        /// </summary>
        public bool TargetUriContainsUsername { get { return QueryUri.AbsoluteUri.IndexOf('@') >= 0; } }

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
