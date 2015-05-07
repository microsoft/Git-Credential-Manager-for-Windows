using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.TeamFoundation.Git.Helpers.Authentication
{
    [TestClass]
    public class VsoMsaTests
    {
        public VsoMsaTests()
        {
            Trace.Listeners.AddRange(Debug.Listeners);
        }

        [TestMethod]
        public void VsoMsaDeleteCredentialsTest()
        {
            Uri targetUri = new Uri("http://localhost");
            VsoMsaAuthentication msaAuthority = GetVsoMsaAuthentication("msa-delete");

            msaAuthority.PersonalAccessTokenStore.WriteCredentials(targetUri, new Credential("username", "password"));
            msaAuthority.AdaRefreshTokenStore.WriteToken(targetUri, new Token("token-value", TokenType.Test));

            Credential credentials;
            Token token;

            msaAuthority.DeleteCredentials(targetUri);
            Assert.IsFalse(msaAuthority.PersonalAccessTokenStore.ReadCredentials(targetUri, out credentials), "Personal Access Tokens were not deleted as expected"); ;
            Assert.IsTrue(msaAuthority.AdaRefreshTokenStore.ReadToken(targetUri, out token), "Refresh Token wasn't read as expected.");

            msaAuthority.DeleteCredentials(targetUri);
            Assert.IsFalse(msaAuthority.PersonalAccessTokenStore.ReadCredentials(targetUri, out credentials), "Personal Access Tokens were not deleted as expected"); ;
            Assert.IsFalse(msaAuthority.AdaRefreshTokenStore.ReadToken(targetUri, out token), "Refresh Token were not deleted as expected.");
        }

        [TestMethod]
        public void VsoMsaGetCredentialsTest()
        {
            Uri targetUri = new Uri("http://localhost");
            VsoMsaAuthentication msaAuthority = GetVsoMsaAuthentication("msa-get");
            Credential credentials;

            Assert.IsFalse(msaAuthority.GetCredentials(targetUri, out credentials), "Credentials were retrieved unexpectedly.");

            msaAuthority.PersonalAccessTokenStore.WriteCredentials(targetUri, new Credential("username", "password"));
            msaAuthority.AdaRefreshTokenStore.WriteToken(targetUri, new Token("token-value", TokenType.Test));

            Assert.IsTrue(msaAuthority.GetCredentials(targetUri, out credentials), "Credentials were not retrieved as expected.");
        }

        [TestMethod]
        public void VsoMsaInteractiveLogonTest()
        {
            Uri targetUri = new Uri("http://localhost");
            VsoMsaAuthentication msaAuthority = GetVsoMsaAuthentication("msa-logon");

            Credential credentials;
            Token token;

            Assert.IsFalse(msaAuthority.PersonalAccessTokenCache.ReadCredentials(targetUri, out credentials), "Personal Access Token found in cache unexpectedly.");
            Assert.IsFalse(msaAuthority.PersonalAccessTokenStore.ReadCredentials(targetUri, out credentials), "Personal Access Token found in store unexpectedly.");
            Assert.IsFalse(msaAuthority.AdaRefreshTokenStore.ReadToken(targetUri, out token), "ADA Refresh Token found in store unexpectedly.");

            Assert.IsTrue(msaAuthority.InteractiveLogon(targetUri), "Interactive logon failed unexpectedly.");

            Assert.IsTrue(msaAuthority.PersonalAccessTokenCache.ReadCredentials(targetUri, out credentials), "Personal Access Token not found in cache as expected.");
            Assert.IsTrue(msaAuthority.PersonalAccessTokenStore.ReadCredentials(targetUri, out credentials), "Personal Access Token not found in store as expected.");
            Assert.IsTrue(msaAuthority.AdaRefreshTokenStore.ReadToken(targetUri, out token) && token.Value == "token-refresh", "ADA Refresh Token not found in store as expected.");
        }

        [TestMethod]
        public void VsoMsaRefreshCredentialsTest()
        {
            Uri targetUri = new Uri("http://localhost");
            Uri errorUri = new Uri("http://incorrect");
            VsoMsaAuthentication msaAuthority = GetVsoMsaAuthentication("msa-refresh");

            msaAuthority.AdaRefreshTokenStore.WriteToken(targetUri, new Token("token-refesh", TokenType.Refresh));

            Credential credentials;

            Assert.IsFalse(msaAuthority.PersonalAccessTokenCache.ReadCredentials(targetUri, out credentials), "Personal Access Token unexpectedly found in cache.");
            Assert.IsFalse(msaAuthority.PersonalAccessTokenStore.ReadCredentials(targetUri, out credentials), "Personal Access Token unexpectedly found in store.");

            Assert.IsTrue(Task.Run(async () => { return await msaAuthority.RefreshCredentials(targetUri); }).Result, "Credentials refresh failed unexpectedly.");
            Assert.IsFalse(Task.Run(async () => { return await msaAuthority.RefreshCredentials(errorUri); }).Result, "Credentials refresh succeeded unexpectedly.");

            Assert.IsTrue(msaAuthority.PersonalAccessTokenCache.ReadCredentials(targetUri, out credentials), "Personal Access Token not found in cache as expected.");
            Assert.IsTrue(msaAuthority.PersonalAccessTokenStore.ReadCredentials(targetUri, out credentials), "Personal Access Token not found in store as expected.");
        }

        [TestMethod]
        public void VsoMsaSetCredentialsTest()
        {
            Uri targetUri = new Uri("http://localhost");
            VsoMsaAuthentication msaAuthority = GetVsoMsaAuthentication("msa-set");
            Credential credentials;
            Token token;

            try
            {
                msaAuthority.SetCredentials(targetUri, new Credential("username", "password"));
                Assert.Fail("Credentials were unexpectedly set.");
            }
            catch { }

            Assert.IsFalse(msaAuthority.PersonalAccessTokenCache.ReadCredentials(targetUri, out credentials), "Personal Access Token unexpectedly found in cache.");
            Assert.IsFalse(msaAuthority.PersonalAccessTokenStore.ReadCredentials(targetUri, out credentials), "Personal Access Token unexpectedly found in store.");
            Assert.IsFalse(msaAuthority.AdaRefreshTokenStore.ReadToken(targetUri, out token), "ADA Refresh Token unexpectedly found in store.");
        }

        [TestMethod]
        public void VsoMsaValidateCredentialsTest()
        {
            VsoMsaAuthentication msaAuthority = GetVsoMsaAuthentication("msa-validate");
            Credential credentials = null;

            Assert.IsFalse(Task.Run(async () => { return await msaAuthority.ValidateCredentials(credentials); }).Result, "Credential validation unexpectedly failed.");

            credentials = new Credential("username", "password");

            Assert.IsTrue(Task.Run(async () => { return await msaAuthority.ValidateCredentials(credentials); }).Result, "Credential validation unexpectedly failed.");
        }

        private VsoMsaAuthentication GetVsoMsaAuthentication(string prefix)
        {
            ICredentialStore patStore = new CredentialCache(prefix);
            ICredentialStore patCache = new CredentialCache(prefix);
            ITokenStore tokenStore = new TokenCache(prefix);
            ILiveAuthority liveAuthority = new AuthorityFake();
            IVsoAuthority vsoAuthority = new AuthorityFake();
            return new VsoMsaAuthentication(patStore, patCache, tokenStore, liveAuthority, vsoAuthority);
        }
    }
}
