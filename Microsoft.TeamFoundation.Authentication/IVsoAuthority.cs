using System;
using System.Threading.Tasks;

namespace Microsoft.TeamFoundation.Authentication
{
    internal interface IVsoAuthority : IAzureAuthority
    {
        Task<Token> GeneratePersonalAccessToken(Uri targetUri, Token accessToken, VsoTokenScope tokenScope, bool requireCompactToken);
        Task<bool> ValidateCredentials(Uri targetUri, Credential credentials);
        Task<bool> ValidateToken(Uri targetUri, Token token);
    }
}
