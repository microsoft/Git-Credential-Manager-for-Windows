using System;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace Microsoft.TeamFoundation.Git.Helpers.Authentication
{
    class CredentialCache : BaseSecureStore, ICredentialStore
    {
        static CredentialCache()
        {
            _cache = new ConcurrentDictionary<string, Credential>(StringComparer.OrdinalIgnoreCase);
        }

        internal CredentialCache(string prefix)
        {
            Debug.Assert(!String.IsNullOrWhiteSpace(prefix), "The prefix parameter value is invalid");

            _prefix = prefix;
        }

        private static readonly ConcurrentDictionary<string, Credential> _cache;

        private readonly string _prefix;

        public void DeleteCredentials(Uri targetUri)
        {
            BaseSecureStore.ValidateTargetUri(targetUri);
            string targetName = this.GetTargetName(targetUri);

            Credential credentials = null;
            _cache.TryRemove(targetName, out credentials);
        }
        public bool ReadCredentials(Uri targetUri, out Credential credentials)
        {
            BaseSecureStore.ValidateTargetUri(targetUri);
            string targetName = this.GetTargetName(targetUri);

            return _cache.TryGetValue(targetName, out credentials);
        }

        public void WriteCredentials(Uri targetUri, Credential credentials)
        {
            BaseSecureStore.ValidateTargetUri(targetUri);
            string targetName = this.GetTargetName(targetUri);

            _cache[targetName] = credentials;
        }

        protected override string GetTargetName(Uri targetUri)
        {
            const string PrimaryNameFormat = "{0}{1}://{2}";

            Debug.Assert(targetUri != null, "The targetUri parameter is null");
            Debug.Assert(targetUri.IsAbsoluteUri, "The targetUri parameter is not absolute");

            // trim any trailing slashes and/or whitespace for compat with git-credential-winstore
            string trimmedHostUrl = targetUri.Host
                                             .TrimEnd('/', '\\')
                                             .TrimEnd();
            string targetName = String.Format(PrimaryNameFormat, _prefix, targetUri.Scheme, trimmedHostUrl);
            return targetName;
        }
    }
}
