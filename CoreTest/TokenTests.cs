using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Microsoft.TeamFoundation.Git.Helpers.Authentication
{
    [TestClass]
    public class TokenTests
    {
        const string TokenString = "The Azure AD Authentication Library (ADAL) for .NET enables client application developers to easily authenticate users to cloud or on-premises Active Directory (AD), and then obtain access tokens for securing API calls. ADAL for .NET has many features that make authentication easier for developers, such as asynchronous support, a configurable token cache that stores access tokens and refresh tokens, automatic token refresh when an access token expires and a refresh token is available, and more. By handling most of the complexity, ADAL can help a developer focus on business logic in their application and easily secure resources without being an expert on security.";

        [TestMethod]
        public void TokenStoreUrl()
        {
            ITokenStoreTest(new SecretStore("test-token"), "http://dummy.url/for/testing", TokenString, DateTimeOffset.Now);
        }
        [TestMethod]
        public void TokenStoreUrlWithParams()
        {
            ITokenStoreTest(new SecretStore("test-token"), "http://dummy.url/for/testing?with=params", TokenString, DateTimeOffset.Now);
        }
        [TestMethod]
        public void TokenStoreUnc()
        {
            ITokenStoreTest(new SecretStore("test-token"), @"\\unc\share\test", TokenString, DateTimeOffset.Now);
        }
        [TestMethod]
        public void TokenCacheUrl()
        {
            ITokenStoreTest(new SecretCache("test-token"), "http://dummy.url/for/testing", TokenString, DateTimeOffset.Now);
        }
        [TestMethod]
        public void TokenCacheUrlWithParams()
        {
            ITokenStoreTest(new SecretCache("test-token"), "http://dummy.url/for/testing?with=params", TokenString, DateTimeOffset.Now);
        }
        [TestMethod]
        public void TokenCacheUnc()
        {
            ITokenStoreTest(new SecretCache("test-token"), @"\\unc\share\test", TokenString, DateTimeOffset.Now);
        }

        private void ITokenStoreTest(ITokenStore tokenStore, string url, string token, DateTimeOffset expires)
        {
            try
            {
                Uri uri = new Uri(url, UriKind.Absolute);

                Token writeToken = new Token(token, TokenType.Test);
                Token readToken = null;

                tokenStore.WriteToken(uri, writeToken);

                if (tokenStore.ReadToken(uri, out readToken))
                {
                    Assert.AreEqual(writeToken.Value, readToken.Value, "Token values did not match between written and read");
                    Assert.AreEqual(writeToken.Type, readToken.Type, "Token types did not mathc between written and read");
                }
                else
                {
                    Assert.Fail("Failed to read token");
                }

                tokenStore.DeleteToken(uri);

                Assert.IsFalse(tokenStore.ReadToken(uri, out readToken), "Deleted token was read back");
            }
            catch (Exception exception)
            {
                Assert.Fail(exception.Message);
            }
        }
    }
}
