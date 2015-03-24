using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.TeamFoundation.Git.Helpers.Authentication
{
    public class TokenCache : BaseCredentialStore, ITokenStore
    {
        static TokenCache()
        {
            _tokenCache = new ConcurrentDictionary<string, Token>(StringComparer.OrdinalIgnoreCase);
        }

        internal TokenCache(string prefix)
        {
            Debug.Assert(!String.IsNullOrWhiteSpace(prefix), "The prefix parameter value is invalid");

            _prefix = prefix;
        }

        static readonly ConcurrentDictionary<string, Token> _tokenCache;

        private readonly string _prefix;

        public void DeleteToken(Uri targetUri)
        {
            BaseCredentialStore.ValidateTargetUri(targetUri);
            string targetName = this.GetTargetName(targetUri);

            Token token = null;
            _tokenCache.TryRemove(targetName, out token);
        }

        public bool ReadToken(Uri targetUri, out Token token)
        {
            BaseCredentialStore.ValidateTargetUri(targetUri);
            string targetName = this.GetTargetName(targetUri);

            return _tokenCache.TryGetValue(targetName, out token);
        }

        public void WriteToken(Uri targetUri, Token token)
        {
            BaseCredentialStore.ValidateTargetUri(targetUri);
            string targetName = this.GetTargetName(targetUri);

            _tokenCache[targetName] = token;
        }

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
