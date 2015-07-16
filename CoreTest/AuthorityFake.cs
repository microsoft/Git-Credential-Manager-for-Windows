using System;
using System.Threading.Tasks;

namespace Microsoft.TeamFoundation.Git.Helpers.Authentication
{
    internal class AuthorityFake : IAzureAuthority, ILiveAuthority, IVsoAuthority
    {
        public Tokens AcquireToken(Uri targetUri, string clientId, string resource, Uri redirectUri, string queryParameters = null)
        {
            return new Tokens("token-access", "token-refresh");
        }

        public async Task<Tokens> AcquireTokenAsync(Uri targetUri, string clientId, string resource, Credential credentials = null)
        {
            return await Task.Run(() => { return new Tokens("token-access", "token-refresh"); });
        }

        public async Task<Tokens> AcquireTokenByRefreshTokenAsync(Uri targetUri, string clientId, string resource, Token refreshToken)
        {
            return await Task.Run(() => { return new Tokens("token-access", "token-refresh"); });
        }

        public async Task<Token> GeneratePersonalAccessToken(Uri targetUri, Token accessToken, VsoTokenScope tokenScope, bool requireCompactToken)
        {
            return await Task.Run(() => { return new Token("personal-access-token", TokenType.VsoPat); });
        }

        public async Task<bool> ValidateCredentials(Uri targetUri, Credential credentials)
        {
            return await Task.Run(() =>
            {
                try
                {
                    Credential.Validate(credentials);
                    return true;
                }
                catch { }
                return false;
            });
        }
    }
}
