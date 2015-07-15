using System;
using System.Threading.Tasks;

namespace Microsoft.TeamFoundation.Git.Helpers.Authentication
{
    internal interface IVsoAuthority
    {
        Tokens AcquireToken(string clientId, string resource, Uri redirectUri, string queryParameters = null);
        Task<Tokens> AcquireTokenByRefreshTokenAsync(string clientId, string resource, Token refreshToken);
        Task<Token> GeneratePersonalAccessToken(Uri targetUri, Token accessToken, VsoTokenScope tokenScope, bool requireCompactToken);
        Task<bool> ValidateCredentials(Credential credentials);
    }
}
