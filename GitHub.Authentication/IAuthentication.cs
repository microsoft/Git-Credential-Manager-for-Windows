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

using System.Threading.Tasks;
using Microsoft.Alm.Authentication;

namespace GitHub.Authentication
{
    public interface IAuthentication : Microsoft.Alm.Authentication.IAuthentication
    {
        /// <summary>
        /// Tokens acquired are stored in the secure secret store provided during initialization.
        /// <para/>
        /// Returns acquired `<see cref="Credential"/>` if successful; otherwise `<see langword="null"/>`.
        /// </summary>
        /// <param name="targetUri">
        /// The unique identifier for the resource for which access is to be acquired.
        /// </param>
        Task<Credential> InteractiveLogon(TargetUri targetUri);

        /// <summary>
        /// Tokens acquired are stored in the secure secret store provided during initialization.
        /// <para/>
        /// Returns acquired `<see cref="Credential"/>` if successful; otherwise `<see langword="null"/>`.
        /// </summary>
        /// <param name="targetUri">
        /// The unique identifier for the resource for which access is to be acquired.
        /// </param>
        /// <param name="credentials">
        /// The credentials of the account for which access is to be acquired.
        /// </param>
        /// <param name="authenticationCode">
        /// The two-factor authentication code for use in access acquisition.
        /// </param>
        Task<Credential> NoninteractiveLogonWithCredentials(TargetUri targetUri, Credential credentials, string authenticationCode);

        /// <summary>
        /// Tokens acquired are stored in the secure secret store provided during initialization.
        /// <para/>
        /// Returns acquired `<see cref="Credential"/>` if successful; otherwise `<see langword="null"/>`.
        /// </summary>
        /// <param name="targetUri">
        /// The unique identifier for the resource for which access is to be acquired.
        /// </param>
        /// <param name="credentials">
        /// The credentials of the account for which access is to be acquired.
        /// </param>
        Task<Credential> NoninteractiveLogonWithCredentials(TargetUri targetUri, Credential credentials);

        /// <summary>
        /// Tests the validity of a set of credentials.
        /// <para/>
        /// Returns `<see langword="true"/>` if the credentials are still valid; otherwise `<see langword="false"/>`.
        /// </summary>
        /// <param name="targetUri">
        /// The unique identifier for the resource for which access is to be validated against.
        /// </param>
        /// <param name="credentials">
        /// The credentials of the account for which access is to be validated.
        /// </param>
        Task<bool> ValidateCredentials(TargetUri targetUri, Credential credentials);
    }
}
