using System.Threading.Tasks;

namespace Microsoft.Alm.Authentication
{
    internal interface IVstsAuthority : IAzureAuthority
    {
        Task<Token> GeneratePersonalAccessToken(TargetUri targetUri, Token accessToken, VstsTokenScope tokenScope, bool requireCompactToken);
        Task<bool> ValidateCredentials(TargetUri targetUri, Credential credentials);
        Task<bool> ValidateToken(TargetUri targetUri, Token token);
    }
}
