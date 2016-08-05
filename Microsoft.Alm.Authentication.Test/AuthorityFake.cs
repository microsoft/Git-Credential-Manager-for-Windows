using System;
using System.Threading.Tasks;

namespace Microsoft.Alm.Authentication.Test
{
    internal class AuthorityFake : IVstsAuthority
    {
        public async Task<Token> GeneratePersonalAccessToken(TargetUri targetUri, Token accessToken, VstsTokenScope tokenScope, bool requireCompactToken)
        {
            return await Task.Run(() => { return new Token("personal-access-token", TokenType.Personal); });
        }

        public async Task<Token> InteractiveAcquireToken(TargetUri targetUri, string clientId, string resource, Uri redirectUri, string queryParameters = null)
        {
            return await Task.Run(() => { return new Token("token-access", TokenType.Access); });
        }

        public async Task<Token> NoninteractiveAcquireToken(TargetUri targetUri, string clientId, string resource, Uri redirectUri, string queryParameters = null)
        {
            return await Task.Run(() => { return new Token("token-access", TokenType.Access); });
        }

        public async Task<bool> ValidateCredentials(TargetUri targetUri, Credential credentials)
        {
            return await Task.Run(() =>
            {
                try
                {
                    BaseSecureStore.ValidateCredential(credentials);
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
