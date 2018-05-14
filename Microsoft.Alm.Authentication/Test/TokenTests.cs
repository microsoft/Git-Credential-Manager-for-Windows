using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Alm.Authentication.Test
{
    public class TokenTests
    {
        private const string TokenString = "The Azure AD Authentication Library (ADAL) for .NET enables client application developers to easily authenticate users to cloud or on-premises Active Directory (AD), and then obtain access tokens for securing API calls. ADAL for .NET has many features that make authentication easier for developers, such as asynchronous support, a configurable token cache that stores access tokens and refresh tokens, automatic token refresh when an access token expires and a refresh token is available, and more. By handling most of the complexity, ADAL can help a developer focus on business logic in their application and easily secure resources without being an expert on security.";

        public static object[][] TokenStoreData
        {
            get
            {
                var data = new List<object[]>()
                {
                    new object[] { true, "test-token", "http://dummy.url/for/testing", TokenString },
                    new object[] { true, "test-token", "http://dummy.url/for/testing?with=params", TokenString },
                    new object[] { true, "test-token", @"\\unc\share\test", TokenString },
                    new object[] { true, "test-token", "file://dummy.url/for/testing", TokenString },
                    new object[] { true, "test-token", "http://dummy.url:9090/for/testing", TokenString },
                    new object[] { false, "test-token", "http://dummy.url/for/testing", TokenString },
                    new object[] { false, "test-token", "http://dummy.url/for/testing?with=params", TokenString },
                    new object[] { false, "test-token", @"\\unc\share\test", TokenString },
                    new object[] { false, "test-token", "file://dummy.url/for/testing", TokenString },
                    new object[] { false, "test-token", "http://dummy.url:9090/for/testing", TokenString },
                };

                return data.ToArray();
            }
        }

        [Theory]
        [MemberData(nameof(TokenStoreData), DisableDiscoveryEnumeration = true)]
        public async Task Token_WriteDelete(bool useCache, string secretName, string url, string token)
        {
            var tokenStore = useCache
                ? new SecretCache(RuntimeContext.Default, secretName) as ITokenStore
                : new SecretStore(RuntimeContext.Default, secretName) as ITokenStore;
            var uri = new TargetUri(url);

            var writeToken = new Token(token, TokenType.Test);
            Token readToken = null;

            await tokenStore.WriteToken(uri, writeToken);

            readToken = await tokenStore.ReadToken(uri);
            Assert.NotNull(readToken);
            Assert.Equal(writeToken.Value, readToken.Value);
            Assert.Equal(writeToken.Type, readToken.Type);

            await tokenStore.DeleteToken(uri);

            Assert.Null(readToken = await tokenStore.ReadToken(uri));
        }
    }
}
