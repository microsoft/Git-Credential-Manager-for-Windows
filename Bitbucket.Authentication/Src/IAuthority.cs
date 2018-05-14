/**** Git Credential Manager for Windows ****
 *
 * Copyright (c) Atlassian
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

using System.Threading.Tasks;
using Microsoft.Alm.Authentication;

namespace Atlassian.Bitbucket.Authentication
{
    /// <summary>
    /// Defines the interactions with the Authority capable of providing and validating Bitbucket credentials.
    /// </summary>
    internal interface IAuthority
    {
        /// <summary>
        /// Use the provided credentials, username and password, to request an access token from the Authority.
        /// <para>
        /// For Bitbucket an access token can be a password, if the user and their account is
        /// configured to allow Basic Auth requests.
        /// </para>
        /// </summary>
        /// <param name="targetUri">defines the Authority to call</param>
        /// <param name="credentials">the credentials to use when requesting the token</param>
        /// <param name="resultType">
        /// Optional parameter. Used to pass in the results of a previous token request, e.g. first
        /// attempt used Basic Auth now try OAuth
        /// </param>
        /// <param name="scope">the access scopes to request</param>
        /// <returns></returns>
        Task<AuthenticationResult> AcquireToken(TargetUri targetUri, Credential credentials, AuthenticationResultType resultType, TokenScope scope);

        /// <summary>
        /// Use an existing refresh token to request a new access token from the specified Authority
        /// </summary>
        /// <param name="targetUri">defines the Authority to call</param>
        /// <param name="refreshToken">the existing refresh_token to use</param>
        /// <returns></returns>
        Task<AuthenticationResult> RefreshToken(TargetUri targetUri, string refreshToken);

        /// <summary>
        /// Confirm that an existing set of credentials are still valid for accessing the Authority
        /// </summary>
        /// <param name="targetUri"></param>
        /// <param name="username"></param>
        /// <param name="credentials"></param>
        /// <returns></returns>
        Task<bool> ValidateCredentials(TargetUri targetUri, string username, Credential credentials);
    }
}
