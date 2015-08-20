using System;

namespace Microsoft.TeamFoundation.Authentication
{
    internal interface IGithubAuthority
    {
        GithubAuthenticationResult AcquireToken(
            Uri targetUri,
            string username,
            string password,
            string authenticationCode,
            GithubTokenScope scope,
            out Token token);
    }
}