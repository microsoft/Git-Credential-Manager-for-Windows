using System;
using System.Threading.Tasks;

namespace Microsoft.Alm.Authentication
{
    internal interface IGithubAuthority
    {
        Task<GithubAuthenticationResult> AcquireToken(
            Uri targetUri,
            string username,
            string password,
            string authenticationCode,
            GithubTokenScope scope);

        Task<bool> ValidateCredentials(Uri targetUri, Credential credentials);
    }
}