using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Microsoft.Alm.Authentication.Test
{
    [TestClass]
    public class TokenTests
    {
        private const string TokenString = "The Azure AD Authentication Library (ADAL) for .NET enables client application developers to easily authenticate users to cloud or on-premises Active Directory (AD), and then obtain access tokens for securing API calls. ADAL for .NET has many features that make authentication easier for developers, such as asynchronous support, a configurable token cache that stores access tokens and refresh tokens, automatic token refresh when an access token expires and a refresh token is available, and more. By handling most of the complexity, ADAL can help a developer focus on business logic in their application and easily secure resources without being an expert on security.";

        [TestMethod]
        public void TokenStoreUrl()
        {
            ITokenStoreTest(new SecretStore("test-token"), "http://dummy.url/for/testing", TokenString);
        }
        [TestMethod]
        public void TokenStoreUrlWithParams()
        {
            ITokenStoreTest(new SecretStore("test-token"), "http://dummy.url/for/testing?with=params", TokenString);
        }
        [TestMethod]
        public void TokenStoreUnc()
        {
            ITokenStoreTest(new SecretStore("test-token"), @"\\unc\share\test", TokenString);
        }
        [TestMethod]
        public void TokenCacheUrl()
        {
            ITokenStoreTest(new SecretCache("test-token"), "http://dummy.url/for/testing", TokenString);
        }
        [TestMethod]
        public void TokenCacheUrlWithParams()
        {
            ITokenStoreTest(new SecretCache("test-token"), "http://dummy.url/for/testing?with=params", TokenString);
        }
        [TestMethod]
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

            if ((readToken = tokenStore.ReadToken(uri)) != null)
            {
                Assert.AreEqual(writeToken.Value, readToken.Value, "Token values did not match between written and read");
                Assert.AreEqual(writeToken.Type, readToken.Type, "Token types did not mathc between written and read");
            }
            else
            {
                Assert.Fail("Failed to read token");
            }

            tokenStore.DeleteToken(uri);

            Assert.IsNull(readToken = tokenStore.ReadToken(uri), "Deleted token was read back");
        }
    }
}
