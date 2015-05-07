using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.TeamFoundation.Git.Helpers.Authentication
{
    [TestClass]
    public class VsoAadTests
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

            aadAuthentication.PersonalAccessTokenStore.WriteCredentials(targetUri, new Credential("username", "password"));
            aadAuthentication.AdaRefreshTokenStore.WriteToken(targetUri, new Token("token-value", TokenType.Test));

            Credential credentials;
            Token token;

            aadAuthentication.DeleteCredentials(targetUri);
            Assert.IsFalse(aadAuthentication.PersonalAccessTokenStore.ReadCredentials(targetUri, out credentials), "Personal Access Tokens were not deleted as expected"); ;
            Assert.IsTrue(aadAuthentication.AdaRefreshTokenStore.ReadToken(targetUri, out token), "Refresh Token wasn't read as expected.");

            aadAuthentication.DeleteCredentials(targetUri);
            Assert.IsFalse(aadAuthentication.PersonalAccessTokenStore.ReadCredentials(targetUri, out credentials), "Personal Access Tokens were not deleted as expected"); ;
            Assert.IsFalse(aadAuthentication.AdaRefreshTokenStore.ReadToken(targetUri, out token), "Refresh Token were not deleted as expected.");
        }

        [TestMethod]
        public void VsoAadGetCredentialsTest()
        {
            Uri targetUri = new Uri("http://localhost");
            VsoAadAuthentication aadAuthentication = GetVsoAadAuthentication("aad-get");

            Credential credentials;

            Assert.IsFalse(aadAuthentication.GetCredentials(targetUri, out credentials), "Credentials were retrieved unexpectedly.");

            aadAuthentication.PersonalAccessTokenStore.WriteCredentials(targetUri, new Credential("username", "password"));
            aadAuthentication.AdaRefreshTokenStore.WriteToken(targetUri, new Token("token-value", TokenType.Test));

            Assert.IsTrue(aadAuthentication.GetCredentials(targetUri, out credentials), "Credentials were not retrieved as expected.");
        }

        [TestMethod]
        public void VsoAadInteractiveLogonTest()
        {
            Uri targetUri = new Uri("http://localhost");
            VsoAadAuthentication aadAuthentication = GetVsoAadAuthentication("aad-logon");

            Credential credentials;
            Token token;

            Assert.IsFalse(aadAuthentication.PersonalAccessTokenCache.ReadCredentials(targetUri, out credentials), "Personal Access Token found in cache unexpectedly.");
            Assert.IsFalse(aadAuthentication.PersonalAccessTokenStore.ReadCredentials(targetUri, out credentials), "Personal Access Token found in store unexpectedly.");
            Assert.IsFalse(aadAuthentication.AdaRefreshTokenStore.ReadToken(targetUri, out token), "ADA Refresh Token found in store unexpectedly.");

            Assert.IsTrue(aadAuthentication.InteractiveLogon(targetUri), "Interactive logon failed unexpectedly.");

            Assert.IsTrue(aadAuthentication.PersonalAccessTokenCache.ReadCredentials(targetUri, out credentials), "Personal Access Token not found in cache as expected.");
            Assert.IsTrue(aadAuthentication.PersonalAccessTokenStore.ReadCredentials(targetUri, out credentials), "Personal Access Token not found in store as expected.");
            Assert.IsTrue(aadAuthentication.AdaRefreshTokenStore.ReadToken(targetUri, out token) && token.Value == "token-refresh", "ADA Refresh Token not found in store as expected.");
        }

        [TestMethod]
        public void VsoAadNoninteractiveLogonTest()
        {
            Uri targetUri = new Uri("http://localhost");
            VsoAadAuthentication aadAuthentication = GetVsoAadAuthentication("aad-noninteractive");

            Credential credentials;
            Token token;

            Assert.IsTrue(Task.Run(async () => { return await aadAuthentication.NoninteractiveLogon(targetUri); }).Result, "Non-interactive logon unexpectedly failed.");

            Assert.IsTrue(aadAuthentication.PersonalAccessTokenCache.ReadCredentials(targetUri, out credentials), "Personal Access Token not found in cache as expected.");
            Assert.IsTrue(aadAuthentication.PersonalAccessTokenStore.ReadCredentials(targetUri, out credentials), "Personal Access Token not found in store as expected.");
            Assert.IsTrue(aadAuthentication.AdaRefreshTokenStore.ReadToken(targetUri, out token) && token.Value == "token-refresh", "ADA Refresh Token not found in store as expected.");
        }

        [TestMethod]
        public void VsoAadNoninteractiveLogonWithCredentialsTest()
        {
            Uri targetUri = new Uri("http://localhost");
            VsoAadAuthentication aadAuthentication = GetVsoAadAuthentication("aad-noninter-creds");

            Credential originCreds = new Credential("orignal-username", "original-password");
            Credential credentials;
            Token token;

            Assert.IsTrue(Task.Run(async () => { return await aadAuthentication.NoninteractiveLogonWithCredentials(targetUri, originCreds); }).Result, "Non-interactive logon unexpectedly failed.");

            Assert.IsTrue(aadAuthentication.PersonalAccessTokenCache.ReadCredentials(targetUri, out credentials), "Personal Access Token not found in cache as expected.");
            Assert.IsTrue(aadAuthentication.PersonalAccessTokenStore.ReadCredentials(targetUri, out credentials), "Personal Access Token not found in store as expected.");
            Assert.IsTrue(aadAuthentication.AdaRefreshTokenStore.ReadToken(targetUri, out token) && token.Value == "token-refresh", "ADA Refresh Token not found in store as expected.");

            Assert.IsFalse(String.Equals(originCreds.Password, credentials.Password, StringComparison.OrdinalIgnoreCase) || String.Equals(originCreds.Username, credentials.Username, StringComparison.OrdinalIgnoreCase), "Supplied credentials and Personal Access Token values unexpectedly matched.");
        }

        [TestMethod]
        public void VsoAadRefreshCredentialsTest()
        {
            Uri targetUri = new Uri("http://localhost");
            Uri errorUri = new Uri("http://incorrect");
            VsoAadAuthentication aadAuthentication = GetVsoAadAuthentication("aad-refresh");

            aadAuthentication.AdaRefreshTokenStore.WriteToken(targetUri, new Token("token-refesh", TokenType.Refresh));

            Credential credentials;

            Assert.IsFalse(aadAuthentication.PersonalAccessTokenCache.ReadCredentials(targetUri, out credentials), "Personal Access Token unexpectedly found in cache.");
            Assert.IsFalse(aadAuthentication.PersonalAccessTokenStore.ReadCredentials(targetUri, out credentials), "Personal Access Token unexpectedly found in store.");

            Assert.IsTrue(Task.Run(async () => { return await aadAuthentication.RefreshCredentials(targetUri); }).Result, "Credentials refresh failed unexpectedly.");
            Assert.IsFalse(Task.Run(async () => { return await aadAuthentication.RefreshCredentials(errorUri); }).Result, "Credentials refresh succeeded unexpectedly.");

            Assert.IsTrue(aadAuthentication.PersonalAccessTokenCache.ReadCredentials(targetUri, out credentials), "Personal Access Token not found in cache as expected.");
            Assert.IsTrue(aadAuthentication.PersonalAccessTokenStore.ReadCredentials(targetUri, out credentials), "Personal Access Token not found in store as expected.");
        }

        [TestMethod]
        public void VsoAadSetCredentialsTest()
        {
            Uri targetUri = new Uri("http://localhost");
            VsoAadAuthentication aadAuthentication = GetVsoAadAuthentication("aad-set");

            Credential credentials;
            Token token;

            Assert.IsTrue(aadAuthentication.SetCredentials(targetUri, new Credential("username", "password")), "Setting credentials unexpectedly failed.");
            Assert.IsTrue(aadAuthentication.PersonalAccessTokenCache.ReadCredentials(targetUri, out credentials), "Personal Access Token unexpectedly not found in cache.");
            Assert.IsTrue(aadAuthentication.PersonalAccessTokenStore.ReadCredentials(targetUri, out credentials), "Personal Access Token unexpectedly not found in store.");
            Assert.IsTrue(aadAuthentication.AdaRefreshTokenStore.ReadToken(targetUri, out token), "ADA Refresh Token unexpectedly not found in store.");
        }

        public void VsoAadValidateCredentialsTest()
        {
            VsoAadAuthentication aadAuthentication = GetVsoAadAuthentication("aad-validate");
            Credential credentials = null;

            Assert.IsFalse(Task.Run(async () => { return await aadAuthentication.ValidateCredentials(credentials); }).Result, "Credential validation unexpectedly failed.");

            credentials = new Credential("username", "password");

            Assert.IsTrue(Task.Run(async () => { return await aadAuthentication.ValidateCredentials(credentials); }).Result, "Credential validation unexpectedly failed.");
        }

        private VsoAadAuthentication GetVsoAadAuthentication(string prefix)
        {
            ICredentialStore patStore = new CredentialCache(prefix);
            ICredentialStore patCache = new CredentialCache(prefix);
            ITokenStore tokenStore = new TokenCache(prefix);
            IAzureAuthority azureAuthority = new AuthorityFake();
            IVsoAuthority vsoAuthority = new AuthorityFake();
            return new VsoAadAuthentication(patStore, patCache, tokenStore, azureAuthority, vsoAuthority);
        }
    }
}
