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
    internal interface IVstsAuthority : IAzureAuthority
    {
        /// <summary>
        /// Generates a personal access token for use with Visual Studio Team Services.
        /// <para/>
        /// Returns the acquired token if successful; otherwise <see langword="null"/>;
        /// </summary>
        /// <param name="targetUri">The uniform resource indicator of the resource access tokens are being requested for.</param>
        /// <param name="accessToken">Access token granted by the identity authority (Azure).</param>
        /// <param name="tokenScope">The requested access scopes to be granted to the token.</param>
        /// <param name="requireCompactToken">`<see langword="true"/>` if requesting a compact format token; otherwise `<see langword="false"/>`.</param>
        /// <param name="tokenDuration">
        /// The requested lifetime of the requested token.
        /// <para/>
        /// The authority granting the token decides the actual lifetime of any token granted, regardless of the duration requested.
        /// </param>
        Task<Token> GeneratePersonalAccessToken(TargetUri targetUri, Token accessToken, VstsTokenScope tokenScope, bool requireCompactToken, TimeSpan? tokenDuration = null);

        /// <summary>
        /// Validates that a `<see cref="Credential"/>` is valid to grant access to the VSTS resource referenced by `<paramref name="targetUri"/>`.
        /// <para/>
        /// Returns `<see langword="true"/>` if successful; otherwise `<see langword="false"/>`.
        /// </summary>
        /// <param name="targetUri">URI of the VSTS resource.</param>
        /// <param name="credentials">`<see cref="Credential"/>` expected to grant access to the VSTS service.</param>
        Task<bool> ValidateCredentials(TargetUri targetUri, Credential credentials);

        /// <summary>
        /// Validates that a `<see cref="Token"/>` is valid to grant access to the VSTS resource referenced by `<paramref name="targetUri"/>`.
        /// <para/>
        /// Returns `<see langword="true"/>` if successful; otherwise `<see langword="false"/>`.
        /// </summary>
        /// <param name="targetUri">URI of the VSTS resource.</param>
        /// <param name="token">`<see cref="Token"/>` expected to grant access to the VSTS resource.</param>
        Task<bool> ValidateToken(TargetUri targetUri, Token token);
    }
}
