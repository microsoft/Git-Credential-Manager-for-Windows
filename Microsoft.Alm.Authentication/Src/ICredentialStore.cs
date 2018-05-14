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
    public interface ICredentialStore : ISecretStore
    {
        /// <summary>
        /// Deletes a `<seealso cref="Credential"/>` from the underlying storage.
        /// <para/>
        /// Returns `<see langword="true"/>` if successful; otherwise `<see langword="false"/>`.
        /// </summary>
        /// <param name="targetUri">Unique identifier used as the key for the `<seealso cref="Credential"/>`.</param>
        Task<bool> DeleteCredentials(TargetUri targetUri);

        /// <summary>
        /// Returns a `<seealso cref="Credential"/>` from the underlying storage if successful; otherwise `<see langword="null"/>`.
        /// </summary>
        /// <param name="targetUri">Unique identifier used as the key for the `<seealso cref="Credential"/>`.</param>
        Task<Credential> ReadCredentials(TargetUri targetUri);

        /// <summary>
        /// Writes a `<seealso cref="Credential"/>` to the underlying storage.
        /// <para/>
        /// Returns `<see langword="true"/>` if successful; otherwise `<see langword="false"/>`.
        /// </summary>
        /// <param name="targetUri">Unique identifier used as the key for the `<seealso cref="Credential"/>`.</param>
        /// <param name="credentials">The `<see cref="Credential"/>` to be written.</param>
        Task<bool> WriteCredentials(TargetUri targetUri, Credential credentials);
    }
}
