using Microsoft.Alm.Authentication;
using System;
using System.Collections.Generic;

namespace GitHub.Authentication.Test.Fakes
{
    public class InMemoryCredentialStore : ICredentialStore
    {
        Dictionary<Uri, Credential> _credentials = new Dictionary<Uri, Credential>();

        public string Namespace => "??";

        public Secret.UriNameConversion UriNameConversion => null;

        public bool DeleteCredentials(TargetUri targetUri)
        {
            _credentials.Remove(targetUri);
            return true;
        }

        public Credential ReadCredentials(TargetUri targetUri)
        {
            Credential result;
            return _credentials.TryGetValue(targetUri, out result)
                ? result
                : null;
        }

        public bool WriteCredentials(TargetUri targetUri, Credential credentials)
        {
            _credentials[targetUri] = credentials;
            return true;
        }
    }
}
