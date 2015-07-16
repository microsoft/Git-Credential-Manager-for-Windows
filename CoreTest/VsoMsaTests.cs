using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.TeamFoundation.Git.Helpers.Authentication
{
    [TestClass]
    public class VsoMsaTests : AuthenticationTests
    {
        public VsoMsaTests()
            : base()
        { }

        [TestMethod]
        public void VsoMsaDeleteCredentialsTest()
        {
            Uri targetUri = DefaultTargetUri;
            VsoMsaAuthentication msaAuthority = GetVsoMsaAuthentication("msa-delete");

            msaAuthority.PersonalAccessTokenStore.WriteToken(targetUri, DefaultPersonalAccessToken);
            msaAuthority.AdaRefreshTokenStore.WriteToken(targetUri, DefaultAzureRefreshToken);

            Token personalAccessToken;
            Token azureToken;

            msaAuthority.DeleteCredentials(targetUri);
            Assert.IsFalse(msaAuthority.PersonalAccessTokenStore.ReadToken(targetUri, out personalAccessToken), "Personal Access Tokens were not deleted as expected"); ;
            Assert.IsTrue(msaAuthority.AdaRefreshTokenStore.ReadToken(targetUri, out azureToken), "Refresh Token wasn't read as expected.");

            msaAuthority.DeleteCredentials(targetUri);
            Assert.IsFalse(msaAuthority.PersonalAccessTokenStore.ReadToken(targetUri, out personalAccessToken), "Personal Access Tokens were not deleted as expected"); ;
            Assert.IsFalse(msaAuthority.AdaRefreshTokenStore.ReadToken(targetUri, out azureToken), "Refresh Token were not deleted as expected.");
        }

        [TestMethod]
        public void VsoMsaGetCredentialsTest()
        {
            Uri targetUri = DefaultTargetUri;
            VsoMsaAuthentication msaAuthority = GetVsoMsaAuthentication("msa-get");
            Credential credentials;

            Assert.IsFalse(msaAuthority.GetCredentials(targetUri, out credentials), "Credentials were retrieved unexpectedly.");

            msaAuthority.PersonalAccessTokenStore.WriteToken(targetUri, DefaultPersonalAccessToken);
            msaAuthority.AdaRefreshTokenStore.WriteToken(targetUri, DefaultAzureRefreshToken);

            Assert.IsTrue(msaAuthority.GetCredentials(targetUri, out credentials), "Credentials were not retrieved as expected.");
        }

        [TestMethod]
        public void VsoMsaInteractiveLogonTest()
        {
            Uri targetUri = DefaultTargetUri;
            VsoMsaAuthentication msaAuthority = GetVsoMsaAuthentication("msa-logon");

            Token personalAccessToken;
            Token azureToken;

            Assert.IsFalse(msaAuthority.PersonalAccessTokenCache.ReadToken(targetUri, out personalAccessToken), "Personal Access Token found in cache unexpectedly.");
            Assert.IsFalse(msaAuthority.PersonalAccessTokenStore.ReadToken(targetUri, out personalAccessToken), "Personal Access Token found in store unexpectedly.");
            Assert.IsFalse(msaAuthority.AdaRefreshTokenStore.ReadToken(targetUri, out azureToken), "ADA Refresh Token found in store unexpectedly.");

            Assert.IsTrue(msaAuthority.InteractiveLogon(targetUri, false), "Interactive logon failed unexpectedly.");

            Assert.IsTrue(msaAuthority.PersonalAccessTokenCache.ReadToken(targetUri, out personalAccessToken), "Personal Access Token not found in cache as expected.");
            Assert.IsTrue(msaAuthority.PersonalAccessTokenStore.ReadToken(targetUri, out personalAccessToken), "Personal Access Token not found in store as expected.");
            Assert.IsTrue(msaAuthority.AdaRefreshTokenStore.ReadToken(targetUri, out azureToken) && azureToken.Value == "token-refresh", "ADA Refresh Token not found in store as expected.");
        }

        [TestMethod]
        public void VsoMsaRefreshCredentialsTest()
        {
            Uri targetUri = DefaultTargetUri;
            Uri invlaidUri = InvalidTargetUri;
            VsoMsaAuthentication msaAuthority = GetVsoMsaAuthentication("msa-refresh");

            msaAuthority.AdaRefreshTokenStore.WriteToken(targetUri, DefaultAzureRefreshToken);

            Token personalAccessToken;

            Assert.IsFalse(msaAuthority.PersonalAccessTokenCache.ReadToken(targetUri, out personalAccessToken), "Personal Access Token unexpectedly found in cache.");
            Assert.IsFalse(msaAuthority.PersonalAccessTokenStore.ReadToken(targetUri, out personalAccessToken), "Personal Access Token unexpectedly found in store.");

            Assert.IsTrue(Task.Run(async () => { return await msaAuthority.RefreshCredentials(targetUri, false); }).Result, "Credentials refresh failed unexpectedly.");
            Assert.IsFalse(Task.Run(async () => { return await msaAuthority.RefreshCredentials(invlaidUri, false); }).Result, "Credentials refresh succeeded unexpectedly.");

            Assert.IsTrue(msaAuthority.PersonalAccessTokenCache.ReadToken(targetUri, out personalAccessToken), "Personal Access Token not found in cache as expected.");
            Assert.IsTrue(msaAuthority.PersonalAccessTokenStore.ReadToken(targetUri, out personalAccessToken), "Personal Access Token not found in store as expected.");
        }

        [TestMethod]
        public void VsoMsaSetCredentialsTest()
        {
            Uri targetUri = DefaultTargetUri;
            VsoMsaAuthentication msaAuthority = GetVsoMsaAuthentication("msa-set");
            Token personalAccessToken;
            Token azureToken;

            try
            {
                msaAuthority.SetCredentials(targetUri, DefaultCredentials);
                Assert.Fail("Credentials were unexpectedly set.");
            }
            catch { }

            Assert.IsFalse(msaAuthority.PersonalAccessTokenCache.ReadToken(targetUri, out personalAccessToken), "Personal Access Token unexpectedly found in cache.");
            Assert.IsFalse(msaAuthority.PersonalAccessTokenStore.ReadToken(targetUri, out personalAccessToken), "Personal Access Token unexpectedly found in store.");
            Assert.IsFalse(msaAuthority.AdaRefreshTokenStore.ReadToken(targetUri, out azureToken), "ADA Refresh Token unexpectedly found in store.");
        }

        [TestMethod]
        public void VsoMsaValidateCredentialsTest()
        {
            VsoMsaAuthentication msaAuthority = GetVsoMsaAuthentication("msa-validate");
            Credential credentials = null;

            Assert.IsFalse(Task.Run(async () => { return await msaAuthority.ValidateCredentials(DefaultTargetUri, credentials); }).Result, "Credential validation unexpectedly failed.");

            credentials = DefaultCredentials;

            Assert.IsTrue(Task.Run(async () => { return await msaAuthority.ValidateCredentials(DefaultTargetUri, credentials); }).Result, "Credential validation unexpectedly failed.");
        }

        private VsoMsaAuthentication GetVsoMsaAuthentication(string prefix)
        {
            ITokenStore patStore = new TokenCache(prefix);
            ITokenStore patCache = new TokenCache(prefix);
            ITokenStore tokenStore = new TokenCache(prefix);
            ITokenStore tokenCache = new TokenCache(prefix);
            ILiveAuthority liveAuthority = new AuthorityFake();
            return new VsoMsaAuthentication(patStore, patCache, tokenStore, tokenCache, liveAuthority);
        }
    }
}
