using System;
using System.Diagnostics;

namespace Microsoft.TeamFoundation.Git.Helpers.Authentication
{
    public sealed class TokenStore : BaseSecureStore, ITokenStore
    {
        internal TokenStore(string prefix)
        {
            Debug.Assert(!String.IsNullOrWhiteSpace(prefix), "The prefix parameter value is invalid");

            _prefix = prefix;
        }

        private readonly string _prefix;

        /// <summary>
        /// Deleted credentials for target URI from the credential store
        /// </summary>
        /// <param name="targetUri">The URI of the target for which credentials are being deleted</param>
        public void DeleteToken(Uri targetUri)
        {
            BaseSecureStore.ValidateTargetUri(targetUri);

            string targetName = this.GetTargetName(targetUri);
            this.Delete(targetName);
        }
        /// <summary>
        /// Reads credentials for a target URI from the credential store
        /// </summary>
        /// <param name="targetUri">The URI of the target for which credentials are being read</param>
        /// <param name="token">The token from the store; null if failure</param>
        /// <returns>True if success; false if failure</returns>
        public bool ReadToken(Uri targetUri, out Token token)
        {
            BaseSecureStore.ValidateTargetUri(targetUri);

            token = null;

            string targetName = this.GetTargetName(targetUri);
            token = base.ReadToken(targetName);

            return token != null;
        }
        /// <summary>
        /// Writes credentials for a target URI to the credential store
        /// </summary>
        /// <param name="targetUri">The URI of the target for which credentials are being stored</param>
        /// <param name="token">The token to be stored</param>
        public void WriteToken(Uri targetUri, Token token)
        {
            BaseSecureStore.ValidateTargetUri(targetUri);
            if (token == null)
                throw new ArgumentNullException("token", "The token parameter is null");
            if (String.IsNullOrWhiteSpace(token.Value))
                throw new ArgumentException("The token parameter is invlaid", "token");

            string targetName = this.GetTargetName(targetUri);
            this.WriteToken(targetName, token, "Azure Directory Refresh Token");
        }
        /// <summary>
        /// Formats a TargetName string based on the TargetUri base on the format started by git-credential-winstore
        /// </summary>
        /// <param name="targetUri">Uri of the target</param>
        /// <returns>Properly formatted TargetName string</returns>
        protected override string GetTargetName(Uri targetUri)
        {
            const string TokenNameFormat = "{0}:{1}://{2}";

            System.Diagnostics.Debug.Assert(targetUri != null, "The targetUri parameter is null");

            // trim any trailing slashes and/or whitespace for compat with git-credential-winstore
            string trimmedHostUrl = targetUri.Host
                                             .TrimEnd('/', '\\')
                                             .TrimEnd();
            string targetName = String.Format(TokenNameFormat, _prefix, targetUri.Scheme, trimmedHostUrl);
            return targetName;
        }
    }
}
