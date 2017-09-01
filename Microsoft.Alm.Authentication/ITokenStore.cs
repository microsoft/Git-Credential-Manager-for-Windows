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

namespace Microsoft.Alm.Authentication
{
    public interface ITokenStore
    {
        /// <summary>
        /// Deletes a <see cref="Token"/> from the underlying storage.
        /// </summary>
        /// <param name="targetUri">The key identifying which token is being deleted.</param>
        bool DeleteToken(TargetUri targetUri);

        /// <summary>
        /// Reads a <see cref="Token"/> from the underlying storage.
        /// </summary>
        /// <param name="targetUri">The key identifying which token to read.</param>
        /// <returns>A <see cref="Token"/> from the store is successful; otherwise <see langword="null"/>.</returns>
        Token ReadToken(TargetUri targetUri);

        /// <summary>
        /// Writes a <see cref="Token"/> to the underlying storage.
        /// </summary>
        /// <param name="targetUri">
        /// Unique identifier for the token, used when reading back from storage.
        /// </param>
        /// <param name="token">The <see cref="Token"/> to be written.</param>
        bool WriteToken(TargetUri targetUri, Token token);
    }
}
