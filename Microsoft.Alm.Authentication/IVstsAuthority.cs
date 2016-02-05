using System;
using System.Threading.Tasks;

namespace Microsoft.Alm.Authentication
{
    internal interface IVstsAuthority : IAzureAuthority
    {
        Task<Token> GeneratePersonalAccessToken(Uri targetUri, Token accessToken, VstsTokenScope tokenScope, bool requireCompactToken);
        Task<bool> ValidateCredentials(Uri targetUri, Credential credentials);
        Task<bool> ValidateToken(Uri targetUri, Token token);
    }
}
