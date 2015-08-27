using System;

namespace Microsoft.TeamFoundation.Authentication
{
    /// <summary>
    /// Base authentication mechanisms for setting, retrieving, and deleting stored credentials.
    /// </summary>
    public abstract class BaseAuthentication : IAuthentication
    {
        /// <summary>
        /// Deletes a <see cref="Credential"/> from the storage used by the authentication object.
        /// </summary>
        /// <param name="targetUri">
        /// The uniform resource indicator used to uniquely identitfy the credentials.
        /// </param>
        public abstract void DeleteCredentials(Uri targetUri);

        /// <summary>
        /// Gets a <see cref="Credential"/> from the storage used by the authentication object.
        /// </summary>
        /// <param name="targetUri">
        /// The uniform resource indicator used to uniquely identitfy the credentials.
        /// </param>
        /// <param name="credentials">
        /// If successful a <see cref="Credential"/> object from the authentication object, 
        /// authority or storage; otherwise `null`.
        /// </param>
        /// <returns>True if successful; otherwise false.</returns>
        public abstract bool GetCredentials(Uri targetUri, out Credential credentials);

        /// <summary>
        /// Sets a <see cref="Credential"/> in the storage used by the authentication object.
        /// </summary>
        /// <param name="targetUri">
        /// The uniform resource indicator used to uniquely identitfy the credentials.
        /// </param>
        /// <param name="credentials">The value to be stored.</param>
        /// <returns>True if successful; otherwise false.</returns>
        public abstract bool SetCredentials(Uri targetUri, Credential credentials);
    }
}
