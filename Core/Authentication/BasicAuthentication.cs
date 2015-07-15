using System;

namespace Microsoft.TeamFoundation.Git.Helpers.Authentication
{
    public sealed class BasicAuthentication : BaseAuthentication, IAuthentication
    {
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

        public override void DeleteCredentials(Uri targetUri)
        {
            BaseSecureStore.ValidateTargetUri(targetUri);

            this.CredentialStore.DeleteCredentials(targetUri);
            this.CredentialCache.DeleteCredentials(targetUri);
        }

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
