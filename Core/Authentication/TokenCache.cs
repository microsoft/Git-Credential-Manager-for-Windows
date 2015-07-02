using System;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace Microsoft.TeamFoundation.Git.Helpers.Authentication
{
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

        public void DeleteToken(Uri targetUri)
        {
            ValidateTargetUri(targetUri);
            string targetName = this.GetTargetName(targetUri);

            Token token = null;
            _cache.TryRemove(targetName, out token);
        }

        public bool ReadToken(Uri targetUri, out Token token)
        {
            ValidateTargetUri(targetUri);
            string targetName = this.GetTargetName(targetUri);

            return _cache.TryGetValue(targetName, out token);
        }

        public void WriteToken(Uri targetUri, Token token)
        {
            ValidateTargetUri(targetUri);
            Token.Validate(token);

            string targetName = this.GetTargetName(targetUri);

            _cache[targetName] = token;
        }

        protected override string GetTargetName(Uri targetUri)
        {
            const string TokenNameFormat = "{0}:{1}://{2}";

            Debug.Assert(targetUri != null, "The targetUri parameter is null");

            // trim any trailing slashes and/or whitespace for compat with git-credential-winstore
            string trimmedHostUrl = targetUri.Host
                                             .TrimEnd('/', '\\')
                                             .TrimEnd();
            string targetName = String.Format(TokenNameFormat, _prefix, targetUri.Scheme, trimmedHostUrl);
            return targetName;
        }
    }
}
