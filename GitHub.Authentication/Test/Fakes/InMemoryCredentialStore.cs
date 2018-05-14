using Microsoft.Alm.Authentication;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace GitHub.Authentication.Test.Fakes
{
    public class InMemoryCredentialStore : ICredentialStore
    {
        Dictionary<Uri, Credential> _credentials = new Dictionary<Uri, Credential>();

        public string Namespace => "??";

        public Secret.UriNameConversionDelegate UriNameConversion
        {
            get { return null; }
            set {; }
        }

        public Task<bool> DeleteCredentials(TargetUri targetUri)
        {
           bool result = _credentials.Remove(targetUri);
            return Task.FromResult(result);
        }

        public Task<Credential> ReadCredentials(TargetUri targetUri)
        {
            Credential result = (_credentials.TryGetValue(targetUri, out result))
                ? result
                : null;

            return Task.FromResult(result);
        }

        public Task<bool> WriteCredentials(TargetUri targetUri, Credential credentials)
        {
            _credentials[targetUri] = credentials;
            return Task.FromResult(true);
        }
    }
}
