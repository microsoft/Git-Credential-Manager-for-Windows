using System;
using System.Threading.Tasks;

namespace Microsoft.Alm.Authentication.Test
{
    internal class AuthorityFake : IVstsAuthority
    {
        public TokenPair AcquireToken(TargetUri targetUri, string clientId, string resource, Uri redirectUri, string queryParameters = null)
        {
            return new TokenPair("token-access", "token-refresh");
        }

        public async Task<TokenPair> AcquireTokenAsync(TargetUri targetUri, string clientId, string resource, Credential credentials = null)
        {
            return await Task.Run(() => { return new TokenPair("token-access", "token-refresh"); });
        }

        public async Task<TokenPair> AcquireTokenByRefreshTokenAsync(TargetUri targetUri, string clientId, string resource, Token refreshToken)
        {
            return await Task.Run(() => { return new TokenPair("token-access", "token-refresh"); });
        }

        public async Task<Token> GeneratePersonalAccessToken(TargetUri targetUri, Token accessToken, VstsTokenScope tokenScope, bool requireCompactToken)
        {
            return await Task.Run(() => { return new Token("personal-access-token", TokenType.Personal); });
        }

        public async Task<bool> ValidateCredentials(TargetUri targetUri, Credential credentials)
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

        public async Task<bool> ValidateToken(TargetUri targetUri, Token token)
        {
            return await Task.Run(() =>
            {
                try
                {
                    Token.Validate(token);
                    return true;
                }
                catch { }
                return false;
            });
        }
    }
}
