using System;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace Microsoft.TeamFoundation.Git.Helpers.Authentication
{
    /// <summary>
    /// Stores credentials relative to target URIs. In-memory, thread-safe.
    /// </summary>
    public sealed class CredentialCache : BaseSecureStore, ICredentialStore
    {
        static CredentialCache()
        {
            _cache = new ConcurrentDictionary<string, Credential>(StringComparer.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Creates a new CredentialCache
        /// </summary>
        /// <param name="prefix">The namespace of the credential set accessed by this instance</param>
        internal CredentialCache(string prefix)
        {
            Debug.Assert(!String.IsNullOrWhiteSpace(prefix), "The prefix parameter value is invalid");

            _prefix = prefix;
        }

        private static readonly ConcurrentDictionary<string, Credential> _cache;

        private readonly string _prefix;

        /// <summary>
        /// Deleted credentials for target URI from the credential store
        /// </summary>
        /// <param name="targetUri">The URI of the target for which credentials are being deleted</param>
        public void DeleteCredentials(Uri targetUri)
        {
            ValidateTargetUri(targetUri);

            Trace.WriteLine("CredentialCache::DeleteCredentials");

            string targetName = this.GetTargetName(targetUri);

            Credential credentials = null;
            _cache.TryRemove(targetName, out credentials);
        }
        /// <summary>
        /// Reads credentials for a target URI from the credential store
        /// </summary>
        /// <param name="targetUri">The URI of the target for which credentials are being read</param>
        /// <param name="credentials">The credentials from the store; null if failure</param>
        /// <returns>True if success; false if failure</returns>
        public bool ReadCredentials(Uri targetUri, out Credential credentials)
        {
            ValidateTargetUri(targetUri);

            Trace.WriteLine("CredentialCache::ReadCredentials");

            string targetName = this.GetTargetName(targetUri);

            return _cache.TryGetValue(targetName, out credentials);
        }
        /// <summary>
        /// Writes credentials for a target URI to the credential store
        /// </summary>
        /// <param name="targetUri">The URI of the target for which credentials are being stored</param>
        /// <param name="credentials">The credentials to be stored</param>
        public void WriteCredentials(Uri targetUri, Credential credentials)
        {
            ValidateTargetUri(targetUri);

            Trace.WriteLine("CredentialCache::WriteCredentials");

            string targetName = this.GetTargetName(targetUri);

            _cache[targetName] = credentials;
        }
        /// <summary>
        /// Formats a TargetName string based on the TargetUri base on the format started by git-credential-winstore
        /// </summary>
        /// <param name="targetUri">Uri of the target</param>
        /// <returns>Properly formatted TargetName string</returns>
        protected override string GetTargetName(Uri targetUri)
        {
            const string PrimaryNameFormat = "{0}{1}://{2}";

            Debug.Assert(targetUri != null && targetUri.IsAbsoluteUri, "The targetUri parameter is null or invalid");

            Trace.WriteLine("CredentialCache::GetTargetName");

            // trim any trailing slashes and/or whitespace for compat with git-credential-winstore
            string trimmedHostUrl = targetUri.Host
                                             .TrimEnd('/', '\\')
                                             .TrimEnd();
            string targetName = String.Format(PrimaryNameFormat, _prefix, targetUri.Scheme, trimmedHostUrl);
            return targetName;
        }
    }
}
