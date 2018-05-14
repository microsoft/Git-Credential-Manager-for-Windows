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

namespace Microsoft.Alm.Authentication
{
    public interface IAuthentication
    {
        /// <summary>
        /// Deletes the `<see cref="Credential"/>`, associated with `<paramref name="targetUri"/>`, from the storage used by the authentication object.
        /// <para/>
        /// Returns `<see langword="true"/>` if successful; otherwise `<see langword="false"/>`;
        /// </summary>
        /// <param name="targetUri">The uniform resource indicator used to uniquely identify the credentials.</param>
        Task<bool> DeleteCredentials(TargetUri targetUri);

        /// <summary>
        /// Returns the `<see cref="Credential"/>`, associated with `<paramref name="targetUri"/>`, from the authentication object, authority or storage if successful; otherwise `<see langword="null"/>`.
        /// </summary>
        /// <param name="targetUri">he uniform resource indicator used to uniquely identify the credentials.</param>
        Task<Credential> GetCredentials(TargetUri targetUri);

        /// <summary>
        /// Sets the `<see cref="Credential"/>`, associated with `<paramref name="targetUri"/>`, in the storage used by the authentication object.
        /// <para/>
        /// Returns `<see langword="true"/>` if successful; otherwise `<see langword="false"/>`;
        /// </summary>
        /// <param name="targetUri">he uniform resource indicator used to uniquely identify the credentials.</param>
        /// <param name="credentials">The value to be stored.</param>
        Task<bool> SetCredentials(TargetUri targetUri, Credential credentials);
    }
}
