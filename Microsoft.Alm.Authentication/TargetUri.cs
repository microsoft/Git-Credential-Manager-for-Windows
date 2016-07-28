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
            if (actualUrl == null)
                throw new ArgumentNullException("actualUrl");
            if (!Uri.TryCreate(actualUrl, UriKind.Absolute, out ActualUri))
                throw new ArgumentException("URL is invalid.", "actualUrl");
            if (proxyUrl != null && !Uri.TryCreate(proxyUrl, UriKind.Absolute, out ProxyUri))
                throw new ArgumentException("URL is invalid.", "proxyUrl");
        }
        public TargetUri(string targetUrl)
            : this(targetUrl, null)
        { }

        /// <summary>
        /// Gets the <see cref="Uri.AbsolutePath"/> of the <see cref="ResolvedUri"/>.
        /// </summary>
        public string AbsolutePath
        {
            get { return ResolvedUri.AbsolutePath; }
        }
        /// <summary>
        /// The actual <see cref="Uri"/> of the target.
        /// </summary>
        public readonly Uri ActualUri;
        /// <summary>
        /// Gets the <see cref="Uri.DnsSafeHost"/> of the <see cref="ResolvedUri"/>.
        /// </summary>
        public string DnsSafeHost
        {
            get { return ResolvedUri.DnsSafeHost; }
        }
        /// <summary>
        /// Gets the <see cref="Uri.Host"/> of the <see cref="ResolvedUri"/>.
        /// </summary>
        public string Host
        {
            get { return ResolvedUri.Host; }
        }
        /// <summary>
        /// Gets whether the <see cref="ResolvedUri"/> is absolute.
        /// </summary>
        public bool IsAbsoluteUri { get { return true; } }
        /// <summary>
        /// Gets whether the port value of the <see cref="ResolvedUri"/> is the default for this scheme.
        /// </summary>
        public bool IsDefaultPort { get { return ResolvedUri.IsDefaultPort; } }
        /// <summary>
        /// Gets the <see cref="Uri.Port"/> of the <see cref="ResolvedUri"/>.
        /// </summary>
        public int Port
        {
            get { return ResolvedUri.Port; }
        }
        /// <summary>
        /// The proxy <see cref="Uri"/> of the target if it exists; otherwise <see langword="null"/>.
        /// </summary>
        public readonly Uri ProxyUri;
        /// <summary>
        /// Gets <see cref="ProxyUri"/> if it exists; otherwise <see cref="ActualUri"/>.
        /// </summary>
        public Uri ResolvedUri
        {
            get { return (ProxyUri ?? ActualUri); }
        }
        /// <summary>
        /// Gets the <see cref="Uri.Scheme"/> name of the <see cref="ResolvedUri"/>.
        /// </summary>
        public string Scheme
        {
            get { return ResolvedUri.Scheme; }
        }
        /// <summary>
        /// Determines whether the <see cref="ResolvedUri"/> is a base of the specified <see cref="Uri"/>.
        /// </summary>
        /// <param name="uri">The <see cref="Uri"/> to test.</param>
        /// <returns><see langword="True"/> if is a base of <param name="uri"/>; otherwise, <see langword="false"/>.</returns>
        public bool IsBaseOf(Uri uri)
        {
            return ResolvedUri.IsBaseOf(uri);
        }
        /// <summary>
        /// Determines whether the <see cref="ResolvedUri"/> is a base of the specified <see cref="TargetUri.ResolvedUri"/>.
        /// </summary>
        /// <param name="targetUri">The <see cref="TargetUri"/> to test.</param>
        /// <returns><see langword="True"/> if is a base of <param name="targetUri"/>; otherwise, <see langword="false"/>.</returns>
        public bool IsBaseOf(TargetUri targetUri)
        {
            if (targetUri == null)
                return false;

            return ResolvedUri.IsBaseOf(targetUri.ResolvedUri);
        }
        /// <summary>
        /// Gets a canonical string representation for the <see cref="ResolvedUri"/>.
        /// </summary>
        /// <returns></returns>
        public override String ToString()
        {
            return ResolvedUri.ToString();
        }

        public static implicit operator Uri(TargetUri targetUri)
        {
            return ReferenceEquals(targetUri, null)
                ? null
                : targetUri.ResolvedUri;
        }

        public static implicit operator TargetUri(Uri uri)
        {
            return ReferenceEquals(uri, null)
                ? null
                : new TargetUri(uri);
        }
    }
}
