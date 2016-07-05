using System.Threading.Tasks;

namespace Microsoft.Alm.Authentication
{
    internal interface IGitHubAuthority
    {
        Task<GitHubAuthenticationResult> AcquireToken(
            TargetUri targetUri,
            string username,
            string password,
            string authenticationCode,
            GitHubTokenScope scope);

        Task<bool> ValidateCredentials(TargetUri targetUri, Credential credentials);
    }
}
