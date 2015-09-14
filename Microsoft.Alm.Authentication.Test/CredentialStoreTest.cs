using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.TeamFoundation.Git.Helpers.Authentication.Test
{
    [TestClass]
    public class CredentialTests
    {
        [TestMethod]
        public void CredentialStoreTests()
        {
            CredentialStoreTest("http://dummy.url/for/testing", "username", "password");
            CredentialStoreTest(@"\\unc\share\test", "username", "password");
            CredentialStoreTest("https://dummy.url/for/testing", "username", "password");
            CredentialStoreTest("http://dummy.url/for/testing?with=params", "username", "password");
            CredentialStoreTest("http://dummy.url/for/testing", "u", "password_that_is_kind_of_long");
            CredentialStoreTest("http://dummy.url/for/testing", "username", "");
            try
            {
                CredentialStoreTest("http://dummy.url/for/testing", null, "null_usernames_are_illegal");
                Assert.Fail("Null username was accepted");
            }
            catch { }
            try
            {
                CredentialStoreTest("http://dummy.url/for/testing", "", "blank_usernames_are_illegal");
                Assert.Fail("Empty username was accepted");
            }
            catch { }
            try
            {
                CredentialStoreTest("http://dummy.url/for/testing", "null_passwords_are_illegal", null);
                Assert.Fail("Null password was accepted");
            }
            catch { }
        }

        [TestMethod]
        public void TokenStoreTests()
        {
            const string tokenString = "The Azure AD Authentication Library (ADAL) for .NET enables client application developers to easily authenticate users to cloud or on-premises Active Directory (AD), and then obtain access tokens for securing API calls. ADAL for .NET has many features that make authentication easier for developers, such as asynchronous support, a configurable token cache that stores access tokens and refresh tokens, automatic token refresh when an access token expires and a refresh token is available, and more. By handling most of the complexity, ADAL can help a developer focus on business logic in their application and easily secure resources without being an expert on security.";

            TokenStoreTest("http://dummy.url/for/testing", tokenString);
            TokenStoreTest(@"\\unc\share\test", tokenString);
            TokenStoreTest("https://dummy.url/for/testing", tokenString);
            TokenStoreTest("http://dummy.url/for/testing?with=params", tokenString);
            try
            {
                TokenStoreTest("http://dummy.url/for/testing", null);
                Assert.Fail("Null token was accepted");
            }
            catch { }
            try
            {
                TokenStoreTest("http://dummy.url/for/testing", "");
                Assert.Fail("Empty token was accepted");
            }
            catch { }
        }

        private void CredentialStoreTest(string url, string username, string password)
        {
            try
            {
                Uri uri = new Uri(url, UriKind.Absolute);
                Credentials writeCreds = new Credentials(username, password);
                Credentials readCreds = null;

                ICredentialStore priamryStore = new CredentialStore("prime-test");

                priamryStore.WriteCredentials(uri, writeCreds);

                if (priamryStore.ReadCredentials(uri, out readCreds))
                {
                    Assert.AreEqual(writeCreds.Password, readCreds.Password, "Passwords did not match between written and read credentials");
                    Assert.AreEqual(writeCreds.Username, readCreds.Username, "Usernames did not match between written and read credentials");
                }
                else
                {
                    Assert.Fail("Failed to read credentials");
                }

                priamryStore.DeleteCredentials(uri);

                Assert.IsFalse(priamryStore.ReadCredentials(uri, out readCreds), "Deleted credentials were read back");
            }
            catch (Exception exception)
            {
                Assert.Fail(exception.Message);
            }
        }

        private void TokenStoreTest(string url, string token)
        {
            try
            {
                Uri uri = new Uri(url, UriKind.Absolute);

                ITokenStore tokenStore = new TokenStore("token-test");
                Token writeToken = new Token(token);
                Token readToken = null;

                tokenStore.WriteToken(uri, writeToken);

                if (tokenStore.ReadToken(uri, out readToken))
                {
                    Assert.AreEqual(writeToken.Value, readToken.Value, "Tokens did not match between written and read");
                }
                else
                {
                    Assert.Fail("Failed to read credentials");
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
