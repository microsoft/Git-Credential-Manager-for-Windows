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
    public interface ICredentialStore
    {
        /// <summary>
        /// Gets the namespace use by this store when reading / writing tokens.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1716:IdentifiersShouldNotMatchKeywords", MessageId = "Namespace")]
        string Namespace { get; }

        /// <summary>
        /// Gets or sets the name conversion delegate used when reading / writing tokens.
        /// </summary>
        Secret.UriNameConversion UriNameConversion { get; set; }

        /// <summary>
        /// Deletes a `<see cref="Credential"/>` from the underlying storage.
        /// <para/>
        /// Returns `<see langword="true"/>` if successful; otherwise `<see langword="false"/>`.
        /// </summary>
        /// <param name="targetUri">The key identifying which credential is being deleted.</param>
        Task<bool> DeleteCredentials(TargetUri targetUri);

        /// <summary>
        /// Reads a `<see cref="Credential"/>` from the underlying storage.
        /// <para/>
        /// Returns a `<see cref="Credential"/>` from the store is successful; otherwise `<see langword="null"/>`.
        /// </summary>
        /// <param name="targetUri">The key identifying which credential to read.</param>
        Task<Credential> ReadCredentials(TargetUri targetUri);

        /// <summary>
        /// Writes a `<see cref="Credential"/>` to the underlying storage.
        /// </summary>
        /// <param name="targetUri">
        /// Unique identifier for the credential, used when reading back from storage.
        /// </param>
        /// <param name="token">The `<see cref="Credential"/>` to be written.</param>
        Task<bool> WriteCredentials(TargetUri targetUri, Credential credentials);
    }
}
