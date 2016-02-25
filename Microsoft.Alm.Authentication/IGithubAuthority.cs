using System;
using System.Threading.Tasks;

namespace Microsoft.Alm.Authentication
{
    internal interface IGithubAuthority
    {
        Task<GithubAuthenticationResult> AcquireToken(
            TargetUri targetUri,
            string username,
            string password,
            string authenticationCode,
            GithubTokenScope scope);

        Task<bool> ValidateCredentials(TargetUri targetUri, Credential credentials);
    }
}