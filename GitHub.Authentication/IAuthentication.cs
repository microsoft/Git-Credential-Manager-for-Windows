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
        /// <para></para>
        /// <para>Tokens acquired are stored in the secure secret store provided during initialization.</para>
        /// </summary>
        /// <param name="targetUri">
        /// The unique identifier for the resource for which access is to be acquired.
        /// </param>
        /// ///
        /// <returns>Acquired <see cref="Credential"/> if successful; otherwise <see langword="null"/>.</returns>
        Task<Credential> InteractiveLogon(TargetUri targetUri);

        /// <summary>
        /// <para></para>
        /// <para>Tokens acquired are stored in the secure secret store provided during initialization.</para>
        /// </summary>
        /// <param name="targetUri">
        /// The unique identifier for the resource for which access is to be acquired.
        /// </param>
        /// <param name="username">The username of the account for which access is to be acquired.</param>
        /// <param name="password">The password of the account for which access is to be acquired.</param>
        /// <param name="authenticationCode">
        /// The two-factor authentication code for use in access acquisition.
        /// </param>
        /// <returns>Acquired <see cref="Credential"/> if successful; otherwise <see langword="null"/>.</returns>
        Task<Credential> NoninteractiveLogonWithCredentials(TargetUri targetUri, string username, string password, string authenticationCode);

        /// <summary>
        /// <para></para>
        /// <para>Tokens acquired are stored in the secure secret store provided during initialization.</para>
        /// </summary>
        /// <param name="targetUri">
        /// The unique identifier for the resource for which access is to be acquired.
        /// </param>
        /// <param name="username">The username of the account for which access is to be acquired.</param>
        /// <param name="password">The password of the account for which access is to be acquired.</param>
        /// <returns>Acquired <see cref="Credential"/> if successful; otherwise <see langword="null"/>.</returns>
        Task<Credential> NoninteractiveLogonWithCredentials(TargetUri targetUri, string username, string password);

        Task<bool> ValidateCredentials(TargetUri targetUri, Credential credentials);
    }
}
