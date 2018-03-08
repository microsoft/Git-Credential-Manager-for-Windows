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
    /// <summary>
    /// Base authentication mechanisms for setting, retrieving, and deleting stored credentials.
    /// </summary>
    public abstract class BaseAuthentication : Base, IAuthentication
    {
        protected BaseAuthentication(RuntimeContext context)
            : base(context)
        { }

        /// <summary>
        /// Deletes a `<see cref="Credential"/>` from the storage used by the authentication object.
        /// </summary>
        /// <param name="targetUri">The uniform resource indicator used to uniquely identify the credentials.</param>
        public abstract Task<bool> DeleteCredentials(TargetUri targetUri);

        /// <summary>
        /// Deletes a `<see cref="Credential"/>` from the storage used by the authentication object.
        /// </summary>
        /// <param name="targetUri">The uniform resource indicator used to uniquely identify the credentials.</param>
        /// <param name="username">The username of the credentials to be deleted.</param>
        public virtual Task<bool> DeleteCredentials(TargetUri targetUri, string username)
            => DeleteCredentials(targetUri, null);

        /// <summary>
        /// Gets a <see cref="Credential"/> from the storage used by the authentication object.
        /// <para/>
        /// Returns a `<see cref="Credential"/>` if successful; otherwise `<see langword="null"/>`.
        /// </summary>
        /// <param name="targetUri">The uniform resource indicator used to uniquely identify the credentials.</param>
        public abstract Task<Credential> GetCredentials(TargetUri targetUri);

        /// <summary>
        /// Sets a <see cref="Credential"/> in the storage used by the authentication object.
        /// </summary>
        /// <param name="targetUri">The uniform resource indicator used to uniquely identify the credentials.</param>
        /// <param name="credentials">The value to be stored.</param>
        public abstract Task<bool> SetCredentials(TargetUri targetUri, Credential credentials);
    }

    /// <summary>
    /// The result of a credential acquisition attempt.
    /// </summary>
    public enum AcquireCredentialResult
    {
        Unknown,

        /// <summary>
        /// The acquisition attempt failed.
        /// </summary>
        Failed,

        /// <summary>
        /// The acquisition attempt succeeded.
        /// </summary>
        Suceeded,
    }

    /// <summary>
    /// Delegate for interactively acquiring credentials.
    /// <para/>
    /// Returns a <see cref="Credential"/> object from the authentication object, authority or storage if successful; otherwise <see langword="null"/>.
    /// </summary>
    /// <param name="targetUri">The uniform resource indicator used to uniquely identify the credentials.</param>
    public delegate Credential AcquireCredentialsDelegate(TargetUri targetUri);

    /// <summary>
    /// Delegate for interactivity related to credential acquisition.
    /// </summary>
    /// <param name="targetUri">The uniform resource indicator used to uniquely identify the credentials.</param>
    /// <param name="result">Result of previous attempt to acquire credentials.</param>
    public delegate void AcquireResultDelegate(TargetUri targetUri, AcquireCredentialResult result);
}
