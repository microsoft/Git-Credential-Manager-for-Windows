using System;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Alm.Authentication.Test
{
    internal class AuthorityFake : IVstsAuthority
    {
        public AuthorityFake(string expectedQueryParameters)
        {
            ExpectedQueryParameters = expectedQueryParameters;
        }

        internal readonly string ExpectedQueryParameters;

        public async Task<Token> GeneratePersonalAccessToken(TargetUri targetUri, Token accessToken, VstsTokenScope tokenScope, bool requireCompactToken, TimeSpan? tokenDuration)
        {
            return await Task.FromResult(new Token("personal-access-token", TokenType.Personal));
        }

        public async Task<Token> InteractiveAcquireToken(TargetUri targetUri, string clientId, string resource, Uri redirectUri, string queryParameters = null)
        {
            Assert.Equal(ExpectedQueryParameters, queryParameters);

            return await Task.FromResult(new Token("token-access", TokenType.Access));
        }

        public async Task<Token> NoninteractiveAcquireToken(TargetUri targetUri, string clientId, string resource, Uri redirectUri)
        {
            return await Task.FromResult(new Token("token-access", TokenType.Access));
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
