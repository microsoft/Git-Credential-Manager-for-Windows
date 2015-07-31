using System;
using System.Diagnostics;

namespace Microsoft.TeamFoundation.Authentication
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
        public BasicAuthentication(ICredentialStore credentialStore)
        {
            if (credentialStore == null)
                throw new ArgumentNullException("credentialStore", "The `credentialStore` parameter is null or invalid.");

            this.CredentialStore = credentialStore;
        }

        internal ICredentialStore CredentialStore { get; set; }

        /// <summary>
        /// Deletes a <see cref="Credential"/> from the storage used by the authentication object.
        /// </summary>
        /// <param name="targetUri">
        /// The uniform resource indicator used to uniquely identitfy the credentials.
        /// </param>
        public override void DeleteCredentials(Uri targetUri)
        {
            BaseSecureStore.ValidateTargetUri(targetUri);

            Trace.WriteLine("BasicAuthentication::DeleteCredentials");

            this.CredentialStore.DeleteCredentials(targetUri);
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

            Trace.WriteLine("BasicAuthentication::GetCredentials");

            this.CredentialStore.ReadCredentials(targetUri, out credentials);

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

            Trace.WriteLine("BasicAuthentication::SetCredentials");

            this.CredentialStore.WriteCredentials(targetUri, credentials);
            return true;
        }
    }
}
