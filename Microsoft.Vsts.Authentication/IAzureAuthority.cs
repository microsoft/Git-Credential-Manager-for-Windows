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
using System.Threading.Tasks;

namespace Microsoft.Alm.Authentication
{
    internal interface IAzureAuthority
    {
        /// <summary>
        /// acquires a <see cref="Token"/> from the authority via an interactive user logon prompt.
        /// </summary>
        /// <param name="targetUri">
        /// The uniform resource indicator of the resource access tokens are being requested for.
        /// </param>
        /// <param name="clientId">Identifier of the client requesting the token.</param>
        /// <param name="resource">
        /// Identifier of the target resource that is the recipient of the requested token.
        /// </param>
        /// <param name="redirectUri">Address to return to upon receiving a response from the authority.</param>
        /// <param name="queryParameters">
        /// Optional: appended as-is to the query string in the HTTP authentication request to the authority.
        /// </param>
        /// <returns>If successful a <see cref="TokenPair"/>; otherwise <see langword="null"/>.</returns>
        Task<Token> InteractiveAcquireToken(TargetUri targetUri, string clientId, string resource, Uri redirectUri, string queryParameters = null);

        /// <summary>
        /// Acquires a <see cref="Token"/> from the authority via an non-interactive user logon.
        /// </summary>
        /// <param name="targetUri">
        /// The uniform resource indicator of the resource access tokens are being requested for.
        /// </param>
        /// <param name="clientId">Identifier of the client requesting the token.</param>
        /// <param name="resource">
        /// Identifier of the target resource that is the recipient of the requested token.
        /// </param>
        /// <param name="redirectUri">Address to return to upon receiving a response from the authority.</param>
        /// <returns>If successful a <see cref="TokenPair"/>; otherwise <see langword="null"/>.</returns>
        Task<Token> NoninteractiveAcquireToken(TargetUri targetUri, string clientId, string resource, Uri redirectUri);
    }
}
