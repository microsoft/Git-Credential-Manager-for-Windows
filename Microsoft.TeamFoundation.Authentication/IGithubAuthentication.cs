using System;

namespace Microsoft.TeamFoundation.Authentication
{
    public interface IGithubAuthentication : IAuthentication
    {
        bool InteractiveLogon(Uri targetUri, out Credential credentials);
        bool NoninteractiveLogonWithCredentials(Uri targetUri, string username, string password, string authenticationCode = null);
    }
}
