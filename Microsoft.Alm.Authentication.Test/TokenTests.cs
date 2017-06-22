using Xunit;

namespace Microsoft.Alm.Authentication.Test
{
    public class TokenTests
    {
        private const string TokenString = "The Azure AD Authentication Library (ADAL) for .NET enables client application developers to easily authenticate users to cloud or on-premises Active Directory (AD), and then obtain access tokens for securing API calls. ADAL for .NET has many features that make authentication easier for developers, such as asynchronous support, a configurable token cache that stores access tokens and refresh tokens, automatic token refresh when an access token expires and a refresh token is available, and more. By handling most of the complexity, ADAL can help a developer focus on business logic in their application and easily secure resources without being an expert on security.";

        [Fact]
        public void TokenStoreUrl()
        {
            ITokenStoreTest(new SecretStore("test-token"), "http://dummy.url/for/testing", TokenString);
        }

        [Fact]
        public void TokenStoreUrlWithParams()
        {
            ITokenStoreTest(new SecretStore("test-token"), "http://dummy.url/for/testing?with=params", TokenString);
        }

        [Fact]
        public void TokenStoreUnc()
        {
            ITokenStoreTest(new SecretStore("test-token"), @"\\unc\share\test", TokenString);
        }

        [Fact]
        public void TokenCacheUrl()
        {
            ITokenStoreTest(new SecretCache("test-token"), "http://dummy.url/for/testing", TokenString);
        }

        [Fact]
        public void TokenCacheUrlWithParams()
        {
            ITokenStoreTest(new SecretCache("test-token"), "http://dummy.url/for/testing?with=params", TokenString);
        }

        [Fact]
        public void TokenCacheUnc()
        {
            ITokenStoreTest(new SecretCache("test-token"), @"\\unc\share\test", TokenString);
        }

        private static void ITokenStoreTest(ITokenStore tokenStore, string url, string token)
        {
            TargetUri uri = new TargetUri(url);

            Token writeToken = new Token(token, TokenType.Test);
            Token readToken = null;

            tokenStore.WriteToken(uri, writeToken);

            readToken = tokenStore.ReadToken(uri);
            Assert.NotNull(readToken);
            Assert.Equal(writeToken.Value, readToken.Value);
            Assert.Equal(writeToken.Type, readToken.Type);

            tokenStore.DeleteToken(uri);

            Assert.Null(readToken = tokenStore.ReadToken(uri));
        }
    }
}
