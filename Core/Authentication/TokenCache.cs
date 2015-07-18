using System;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace Microsoft.TeamFoundation.Git.Helpers.Authentication
{
    /// <summary>
    /// In memory, thread-safe token cache which indexes values by target.
    /// </summary>
    public sealed class TokenCache : BaseSecureStore, ITokenStore
    {
        internal TokenCache(string prefix)
        {
            Debug.Assert(!String.IsNullOrWhiteSpace(prefix), "The prefix parameter value is invalid");

            _cache = new ConcurrentDictionary<string, Token>(StringComparer.OrdinalIgnoreCase);
            _prefix = prefix;
        }

        private readonly ConcurrentDictionary<string, Token> _cache;

        private readonly string _prefix;

        /// <summary>
        /// Deletes a token from the cache.
        /// </summary>
        /// <param name="targetUri">The key which to find and delete the token with.</param>
        public void DeleteToken(Uri targetUri)
        {
            ValidateTargetUri(targetUri);

            Trace.WriteLine("TokenCache::DeleteToken");

            string targetName = this.GetTargetName(targetUri);

            Token token = null;
            _cache.TryRemove(targetName, out token);
        }
        /// <summary>
        /// Gets a token from the cache.
        /// </summary>
        /// <param name="targetUri">The key which to find the token.</param>
        /// <param name="token">The token if successful; otherwise `null`.</param>
        /// <returns>True if successful; false otherwise.</returns>
        public bool ReadToken(Uri targetUri, out Token token)
        {
            ValidateTargetUri(targetUri);

            Trace.WriteLine("TokenCache::ReadToken");

            string targetName = this.GetTargetName(targetUri);

            return _cache.TryGetValue(targetName, out token);
        }
        /// <summary>
        /// Writes a token to the cache.
        /// </summary>
        /// <param name="targetUri">The key which to index the token by.</param>
        /// <param name="token">The token to write to the cache.</param>
        public void WriteToken(Uri targetUri, Token token)
        {
            ValidateTargetUri(targetUri);
            Token.Validate(token);

            Trace.WriteLine("TokenCache::WriteToken");

            string targetName = this.GetTargetName(targetUri);

            _cache[targetName] = token;
        }

        protected override string GetTargetName(Uri targetUri)
        {
            const string TokenNameFormat = "{0}:{1}://{2}";

            Debug.Assert(targetUri != null, "The targetUri parameter is null");

            Trace.WriteLine("TokenCache::GetTargetName");

            // trim any trailing slashes and/or whitespace for compat with git-credential-winstore
            string trimmedHostUrl = targetUri.Host
                                             .TrimEnd('/', '\\')
                                             .TrimEnd();
            string targetName = String.Format(TokenNameFormat, _prefix, targetUri.Scheme, trimmedHostUrl);
            return targetName;
        }
    }
}
