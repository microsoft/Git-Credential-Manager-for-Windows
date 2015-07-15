using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.TeamFoundation.Git.Helpers.Authentication
{
    [TestClass]
    public class VsoAadTests : AuthenticationTests
    {
        public VsoAadTests()
        {
            Trace.Listeners.AddRange(Debug.Listeners);
        }

        [TestMethod]
        public void VsoAadDeleteCredentialsTest()
        {
            Uri targetUri = new Uri("http://localhost");
            VsoAadAuthentication aadAuthentication = GetVsoAadAuthentication("aad-delete");

            aadAuthentication.PersonalAccessTokenStore.WriteToken(targetUri, DefaultPersonalAccessToken);
            aadAuthentication.AdaRefreshTokenStore.WriteToken(targetUri, DefaultAzureRefreshToken);

            Token personalAccessToken;
            Token azureToken;

            aadAuthentication.DeleteCredentials(targetUri);
            Assert.IsFalse(aadAuthentication.PersonalAccessTokenStore.ReadToken(targetUri, out personalAccessToken), "Personal Access Tokens were not deleted as expected");
            Assert.IsTrue(aadAuthentication.AdaRefreshTokenStore.ReadToken(targetUri, out azureToken), "Refresh Token wasn't read as expected.");

            aadAuthentication.DeleteCredentials(targetUri);
            Assert.IsFalse(aadAuthentication.PersonalAccessTokenStore.ReadToken(targetUri, out personalAccessToken), "Personal Access Tokens were not deleted as expected");
            Assert.IsFalse(aadAuthentication.AdaRefreshTokenStore.ReadToken(targetUri, out azureToken), "Refresh Token were not deleted as expected.");
        }

        [TestMethod]
        public void VsoAadGetCredentialsTest()
        {
            Uri targetUri = new Uri("http://localhost");
            VsoAadAuthentication aadAuthentication = GetVsoAadAuthentication("aad-get");

            Credential credentials;

            Assert.IsFalse(aadAuthentication.GetCredentials(targetUri, out credentials), "Credentials were retrieved unexpectedly.");

            aadAuthentication.PersonalAccessTokenStore.WriteToken(targetUri, DefaultPersonalAccessToken);
            aadAuthentication.AdaRefreshTokenStore.WriteToken(targetUri, DefaultAzureRefreshToken);

            Assert.IsTrue(aadAuthentication.GetCredentials(targetUri, out credentials), "Credentials were not retrieved as expected.");
        }

        [TestMethod]
        public void VsoAadInteractiveLogonTest()
        {
            Uri targetUri = new Uri("http://localhost");
            VsoAadAuthentication aadAuthentication = GetVsoAadAuthentication("aad-logon");

            Token personalAccessToken;
            Token azureToken;

            Assert.IsFalse(aadAuthentication.PersonalAccessTokenCache.ReadToken(targetUri, out personalAccessToken), "Personal Access Token found in cache unexpectedly.");
            Assert.IsFalse(aadAuthentication.PersonalAccessTokenStore.ReadToken(targetUri, out personalAccessToken), "Personal Access Token found in store unexpectedly.");
            Assert.IsFalse(aadAuthentication.AdaRefreshTokenStore.ReadToken(targetUri, out azureToken), "ADA Refresh Token found in store unexpectedly.");

            Assert.IsTrue(aadAuthentication.InteractiveLogon(targetUri, false), "Interactive logon failed unexpectedly.");

            Assert.IsTrue(aadAuthentication.PersonalAccessTokenCache.ReadToken(targetUri, out personalAccessToken), "Personal Access Token not found in cache as expected.");
            Assert.IsTrue(aadAuthentication.PersonalAccessTokenStore.ReadToken(targetUri, out personalAccessToken), "Personal Access Token not found in store as expected.");
            Assert.IsTrue(aadAuthentication.AdaRefreshTokenStore.ReadToken(targetUri, out azureToken) && azureToken.Value == "token-refresh", "ADA Refresh Token not found in store as expected.");
        }

        [TestMethod]
        public void VsoAadNoninteractiveLogonTest()
        {
            Uri targetUri = new Uri("http://localhost");
            VsoAadAuthentication aadAuthentication = GetVsoAadAuthentication("aad-noninteractive");

            Token personalAccessToken;
            Token azureToken;

            Assert.IsTrue(Task.Run(async () => { return await aadAuthentication.NoninteractiveLogon(targetUri, false); }).Result, "Non-interactive logon unexpectedly failed.");

            Assert.IsTrue(aadAuthentication.PersonalAccessTokenCache.ReadToken(targetUri, out personalAccessToken), "Personal Access Token not found in cache as expected.");
            Assert.IsTrue(aadAuthentication.PersonalAccessTokenStore.ReadToken(targetUri, out personalAccessToken), "Personal Access Token not found in store as expected.");
            Assert.IsTrue(aadAuthentication.AdaRefreshTokenStore.ReadToken(targetUri, out azureToken) && azureToken.Value == "token-refresh", "ADA Refresh Token not found in store as expected.");
        }

        [TestMethod]
        public void VsoAadNoninteractiveLogonWithCredentialsTest()
        {
            Uri targetUri = new Uri("http://localhost");
            VsoAadAuthentication aadAuthentication = GetVsoAadAuthentication("aad-noninter-creds");

            Credential originCreds = DefaultCredentials;
            Token personalAccessToken;
            Token azureToken;

            Assert.IsTrue(Task.Run(async () => { return await aadAuthentication.NoninteractiveLogonWithCredentials(targetUri, originCreds, false); }).Result, "Non-interactive logon unexpectedly failed.");

            Assert.IsTrue(aadAuthentication.PersonalAccessTokenCache.ReadToken(targetUri, out personalAccessToken), "Personal Access Token not found in cache as expected.");
            Assert.IsTrue(aadAuthentication.PersonalAccessTokenStore.ReadToken(targetUri, out personalAccessToken), "Personal Access Token not found in store as expected.");
            Assert.IsTrue(aadAuthentication.AdaRefreshTokenStore.ReadToken(targetUri, out azureToken) && azureToken.Value == "token-refresh", "ADA Refresh Token not found in store as expected.");

            Assert.IsFalse(String.Equals(originCreds.Password, personalAccessToken.Value, StringComparison.OrdinalIgnoreCase) || String.Equals(originCreds.Username, personalAccessToken.Value, StringComparison.OrdinalIgnoreCase), "Supplied credentials and Personal Access Token values unexpectedly matched.");
        }

        [TestMethod]
        public void VsoAadRefreshCredentialsTest()
        {
            Uri targetUri = new Uri("http://microsoft.visualstudio.com/foo/bar.baz?bin=raz");
            Uri errorUri = new Uri("http://incorrect");
            VsoAadAuthentication aadAuthentication = GetVsoAadAuthentication("aad-refresh");

            aadAuthentication.AdaRefreshTokenStore.WriteToken(targetUri, DefaultAzureRefreshToken);

            Token personalAccessToken;

            Assert.IsFalse(aadAuthentication.PersonalAccessTokenCache.ReadToken(targetUri, out personalAccessToken), "Personal Access Token unexpectedly found in cache.");
            Assert.IsFalse(aadAuthentication.PersonalAccessTokenStore.ReadToken(targetUri, out personalAccessToken), "Personal Access Token unexpectedly found in store.");

            Assert.IsTrue(Task.Run(async () => { return await aadAuthentication.RefreshCredentials(targetUri, false); }).Result, "Credentials refresh failed unexpectedly.");
            Assert.IsFalse(Task.Run(async () => { return await aadAuthentication.RefreshCredentials(errorUri, false); }).Result, "Credentials refresh succeeded unexpectedly.");

            Assert.IsTrue(aadAuthentication.PersonalAccessTokenCache.ReadToken(targetUri, out personalAccessToken), "Personal Access Token not found in cache as expected.");
            Assert.IsTrue(aadAuthentication.PersonalAccessTokenStore.ReadToken(targetUri, out personalAccessToken), "Personal Access Token not found in store as expected.");
        }

        [TestMethod]
        public void VsoAadSetCredentialsTest()
        {
            Uri targetUri = new Uri("http://localhost");
            VsoAadAuthentication aadAuthentication = GetVsoAadAuthentication("aad-set");
            Credential credentials = DefaultCredentials;

            Token personalAccessToken;
            Token azureToken;

            Assert.IsFalse(aadAuthentication.SetCredentials(targetUri, credentials), "Credentials were unexpectedly set.");

            Assert.IsFalse(aadAuthentication.PersonalAccessTokenCache.ReadToken(targetUri, out personalAccessToken), "Personal Access Token unexpectedly found in cache.");
            Assert.IsFalse(aadAuthentication.PersonalAccessTokenStore.ReadToken(targetUri, out personalAccessToken), "Personal Access Token unexpectedly found in store.");
            Assert.IsFalse(aadAuthentication.AdaRefreshTokenStore.ReadToken(targetUri, out azureToken), "ADA Refresh Token unexpectedly found in store.");
            Assert.IsFalse(aadAuthentication.GetCredentials(targetUri, out credentials), "Credentials were retrieved unexpectedly.");
        }

        public void VsoAadValidateCredentialsTest()
        {
            VsoAadAuthentication aadAuthentication = GetVsoAadAuthentication("aad-validate");
            Credential credentials = null;

            Assert.IsFalse(Task.Run(async () => { return await aadAuthentication.ValidateCredentials(credentials); }).Result, "Credential validation unexpectedly failed.");

            credentials = DefaultCredentials;

            Assert.IsTrue(Task.Run(async () => { return await aadAuthentication.ValidateCredentials(credentials); }).Result, "Credential validation unexpectedly failed.");
        }

        private VsoAadAuthentication GetVsoAadAuthentication(string prefix)
        {
            ITokenStore patStore = new TokenCache(prefix);
            ITokenStore patCache = new TokenCache(prefix);
            ITokenStore tokenStore = new TokenCache(prefix);
            IAzureAuthority azureAuthority = new AuthorityFake();
            IVsoAuthority vsoAuthority = new AuthorityFake();
            return new VsoAadAuthentication(patStore, patCache, tokenStore, azureAuthority, vsoAuthority);
        }
    }
}
