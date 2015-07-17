using System;

namespace Microsoft.TeamFoundation.Git.Helpers.Authentication
{
    /// <summary>
    /// Facilitates basic authentication using simple username and password schemes.
    /// </summary>
    public sealed class BasicAuthentication : BaseAuthentication, IAuthentication
    {
        /// <summary>
        /// Creats a new <see cref="BasicAuthentication"/> object with a prefix used to interact
        /// with the underlying credential storage.
        /// </summary>
        /// <param name="credentialPrefix">
        /// Value use to parition values stored in the underlying credential storage.
        /// </param>
        public BasicAuthentication(string credentialPrefix)
        {
            this.CredentialStore = new CredentialStore(credentialPrefix);
            this.CredentialCache = new CredentialCache(credentialPrefix);
        }
        internal BasicAuthentication(ICredentialStore credentialStore, ICredentialStore credentialCache)
        {
            this.CredentialStore = credentialStore;
            this.CredentialCache = credentialCache;
        }

        internal ICredentialStore CredentialStore { get; set; }
        internal ICredentialStore CredentialCache { get; set; }

        /// <summary>
        /// Deletes a <see cref="Credential"/> from the storage used by the authentication object.
        /// </summary>
        /// <param name="targetUri">
        /// The uniform resource indicator used to uniquely identitfy the credentials.
        /// </param>
        public override void DeleteCredentials(Uri targetUri)
        {
            BaseSecureStore.ValidateTargetUri(targetUri);

            this.CredentialStore.DeleteCredentials(targetUri);
            this.CredentialCache.DeleteCredentials(targetUri);
        }
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
        public override bool GetCredentials(Uri targetUri, out Credential credentials)
        {
            BaseSecureStore.ValidateTargetUri(targetUri);

            // check the in-memory cache first
            if (!this.CredentialCache.ReadCredentials(targetUri, out credentials))
            {
                // fall-back to the on disk cache
                if (this.CredentialStore.ReadCredentials(targetUri, out credentials))
                {
                    // update the in-memory cache for faster future look-ups
                    this.CredentialCache.WriteCredentials(targetUri, credentials);
                }
            }

            return credentials != null;
        }
        /// <summary>
        /// Sets a <see cref="Credential"/> in the storage used by the authentication object.otr
        /// </summary>
        /// <param name="targetUri">
        /// The uniform resource indicator used to uniquely identitfy the credentials.
        /// </param>
        /// <param name="credentials">The value to be stored.</param>
        /// <returns>True if successful; otherwise false.</returns>
        public override bool SetCredentials(Uri targetUri, Credential credentials)
        {
            BaseSecureStore.ValidateTargetUri(targetUri);
            Credential.Validate(credentials);

            this.CredentialStore.WriteCredentials(targetUri, credentials);
            this.CredentialCache.WriteCredentials(targetUri, credentials);
            return true;
        }
    }
}
