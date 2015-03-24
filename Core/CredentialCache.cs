using System;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace Microsoft.TeamFoundation.Git.Helpers.Authentication
{
    class CredentialCache : BaseSecureStore, ICredentialStore
    {
        internal CredentialCache()
        {
            _store = new ConcurrentDictionary<string, Credential>(StringComparer.OrdinalIgnoreCase);
        }

        private ConcurrentDictionary<string, Credential> _store;

        public void DeleteCredentials(Uri targetUri)
        {
            BaseSecureStore.ValidateTargetUri(targetUri);
            string targetName = this.GetTargetName(targetUri);

            Credential credentials = null;
            _store.TryRemove(targetName, out credentials);
        }
        public bool ReadCredentials(Uri targetUri, out Credential credentials)
        {
            BaseSecureStore.ValidateTargetUri(targetUri);
            string targetName = this.GetTargetName(targetUri);

            return _store.TryGetValue(targetName, out credentials);
        }

        public void WriteCredentials(Uri targetUri, Credential credentials)
        {
            BaseSecureStore.ValidateTargetUri(targetUri);
            string targetName = this.GetTargetName(targetUri);

            _store[targetName] = credentials;
        }

        protected override string GetTargetName(Uri targetUri)
        {
            const string PrimaryNameFormat = "{0}://{1}";

            Debug.Assert(targetUri != null, "The targetUri parameter is null");
            Debug.Assert(targetUri.IsAbsoluteUri, "The targetUri parameter is not absolute");

            // trim any trailing slashes and/or whitespace for compat with git-credential-winstore
            string trimmedHostUrl = targetUri.Host
                                             .TrimEnd('/', '\\')
                                             .TrimEnd();
            string targetName = String.Format(PrimaryNameFormat, targetUri.Scheme, trimmedHostUrl);
            return targetName;
        }
    }
}
