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
    /// Defines the main entry points for managing Bitbucket credentials.
    /// </summary>
    public interface IAuthentication
    {
        /// <summary>
        /// <para>
        /// Ask the user for credentials to use to acquire tokens for use with the specified resource
        /// </para>
        /// <para>Tokens acquired are stored in the secure secret store provided during initialization.</para>
        /// </summary>
        /// <param name="targetUri">
        /// The unique identifier for the resource for which access is to be acquired.
        /// </param>
        /// ///
        /// <returns>Acquired <see cref="Credential"/> if successful; otherwise <see langword="null"/>.</returns>
        Task<Credential> InteractiveLogon(TargetUri targetUri);

        /// <summary>
        /// Validate the supplied credentials
        /// </summary>
        /// <param name="targetUri"></param>
        /// <param name="username"></param>
        /// <param name="credentials"></param>
        /// <returns></returns>
        Task<Credential> ValidateCredentials(TargetUri targetUri, string username, Credential credentials);
    }
}
