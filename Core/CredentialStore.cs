using System;
using System.Diagnostics;

namespace Microsoft.TeamFoundation.Git.Helpers.Authentication
{
    public class CredentialStore : BaseCredentialStore, ICredentialStore
    {
        internal CredentialStore(string prefix)
        {
            Debug.Assert(!String.IsNullOrWhiteSpace(prefix), "The prefix parameter value is invalid");

            _prefix = prefix;
        }

        private readonly string _prefix;

        /// <summary>
        /// Deleted credentials for target URI from the credential store
        /// </summary>
        /// <param name="targetUri">The URI of the target for which credentials are being deleted</param>
        public void DeleteCredentials(Uri targetUri)
        {
            BaseCredentialStore.ValidateTargetUri(targetUri);

            string targetName = this.GetTargetName(targetUri);
            try
            {
                this.Delete(targetName);
            }
            catch (Exception exception)
            {
                Debug.WriteLine(exception);
            }
        }
        /// <summary>
        /// Reads credentials for a target URI from the credential store
        /// </summary>
        /// <param name="targetUri">The URI of the target for which credentials are being read</param>
        /// <param name="credentials">The credentials from the store; null if failure</param>
        /// <returns>True if success; false if failure</returns>
        public bool ReadCredentials(Uri targetUri, out Credentials credentials)
        {
            BaseCredentialStore.ValidateTargetUri(targetUri);

            string targetName = this.GetTargetName(targetUri);
            credentials = this.Read(targetName);

            return credentials != null;
        }
        /// <summary>
        /// Writes credentials for a target URI to the credential store
        /// </summary>
        /// <param name="targetUri">The URI of the target for which credentials are being stored</param>
        /// <param name="credentials">The credentials to be stored</param>
        public void WriteCredentials(Uri targetUri, Credentials credentials)
        {
            BaseCredentialStore.ValidateTargetUri(targetUri);
            BaseCredentialStore.ValidateCredentials(credentials);

            string targetName = this.GetTargetName(targetUri);
            this.Write(targetName, credentials);
        }
        /// <summary>
        /// Formats a TargetName string based on the TargetUri base on the format started by git-credential-winstore
        /// </summary>
        /// <param name="targetUri">Uri of the target</param>
        /// <returns>Properly formatted TargetName string</returns>
        protected override string GetTargetName(Uri targetUri)
        {
            // use the format started by git-credential-winstore for maximum compatibility
            // see https://gitcredentialstore.codeplex.com/
            const string PrimaryNameFormat = "{0}:{1}://{2}";

            System.Diagnostics.Debug.Assert(targetUri != null, "The targetUri parameter is null");

            // trim any trailing slashes and/or whitespace for compat with git-credential-winstore
            string trimmedHostUrl = targetUri.Host
                                             .TrimEnd('/', '\\')
                                             .TrimEnd();
            string targetName = String.Format(PrimaryNameFormat, _prefix, targetUri.Scheme, trimmedHostUrl);
            return targetName;
        }
    }
}
