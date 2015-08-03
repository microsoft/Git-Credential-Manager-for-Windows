using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.TeamFoundation.Authentication.Test
{
    [TestClass]
    public class VsoAadTests : AuthenticationTests
    {
        public VsoAadTests()
            : base()
        { }

        [TestMethod]
        public void VsoAadDeleteCredentialsTest()
        {
            Uri targetUri = DefaultTargetUri;
            VsoAadAuthentication aadAuthentication = GetVsoAadAuthentication("aad-delete");

            aadAuthentication.PersonalAccessTokenStore.WriteCredentials(targetUri, DefaultPersonalAccessToken);
            aadAuthentication.AdaRefreshTokenStore.WriteToken(targetUri, DefaultAzureRefreshToken);

            Credential personalAccessToken;
            Token azureToken;

            aadAuthentication.DeleteCredentials(targetUri);
            Assert.IsFalse(aadAuthentication.PersonalAccessTokenStore.ReadCredentials(targetUri, out personalAccessToken), "Personal Access Tokens were not deleted as expected");
            Assert.IsTrue(aadAuthentication.AdaRefreshTokenStore.ReadToken(targetUri, out azureToken), "Refresh Token wasn't read as expected.");

            aadAuthentication.DeleteCredentials(targetUri);
            Assert.IsFalse(aadAuthentication.PersonalAccessTokenStore.ReadCredentials(targetUri, out personalAccessToken), "Personal Access Tokens were not deleted as expected");
            Assert.IsFalse(aadAuthentication.AdaRefreshTokenStore.ReadToken(targetUri, out azureToken), "Refresh Token were not deleted as expected.");
        }

        [TestMethod]
        public void VsoAadGetCredentialsTest()
        {
            Uri targetUri = DefaultTargetUri;
            VsoAadAuthentication aadAuthentication = GetVsoAadAuthentication("aad-get");

            Credential credentials;

            Assert.IsFalse(aadAuthentication.GetCredentials(targetUri, out credentials), "Credentials were retrieved unexpectedly.");

            aadAuthentication.PersonalAccessTokenStore.WriteCredentials(targetUri, DefaultPersonalAccessToken);
            aadAuthentication.AdaRefreshTokenStore.WriteToken(targetUri, DefaultAzureRefreshToken);

            Assert.IsTrue(aadAuthentication.GetCredentials(targetUri, out credentials), "Credentials were not retrieved as expected.");
        }

        [TestMethod]
        public void VsoAadInteractiveLogonTest()
        {
            Uri targetUri = DefaultTargetUri;
            VsoAadAuthentication aadAuthentication = GetVsoAadAuthentication("aad-logon");

            Credential personalAccessToken;
            Token azureToken;

            Assert.IsFalse(aadAuthentication.PersonalAccessTokenStore.ReadCredentials(targetUri, out personalAccessToken), "Personal Access Token found in store unexpectedly.");
            Assert.IsFalse(aadAuthentication.AdaRefreshTokenStore.ReadToken(targetUri, out azureToken), "ADA Refresh Token found in store unexpectedly.");

            Assert.IsTrue(aadAuthentication.InteractiveLogon(targetUri, false), "Interactive logon failed unexpectedly.");

            Assert.IsTrue(aadAuthentication.PersonalAccessTokenStore.ReadCredentials(targetUri, out personalAccessToken), "Personal Access Token not found in store as expected.");
            Assert.IsTrue(aadAuthentication.AdaRefreshTokenStore.ReadToken(targetUri, out azureToken) && azureToken.Value == "token-refresh", "ADA Refresh Token not found in store as expected.");
        }

        [TestMethod]
        public void VsoAadNoninteractiveLogonTest()
        {
            Uri targetUri = DefaultTargetUri;
            VsoAadAuthentication aadAuthentication = GetVsoAadAuthentication("aad-noninteractive");

            Credential personalAccessToken;
            Token azureToken;

            Assert.IsTrue(Task.Run(async () => { return await aadAuthentication.NoninteractiveLogon(targetUri, false); }).Result, "Non-interactive logon unexpectedly failed.");

            Assert.IsTrue(aadAuthentication.PersonalAccessTokenStore.ReadCredentials(targetUri, out personalAccessToken), "Personal Access Token not found in store as expected.");
            Assert.IsTrue(aadAuthentication.AdaRefreshTokenStore.ReadToken(targetUri, out azureToken) && azureToken.Value == "token-refresh", "ADA Refresh Token not found in store as expected.");
        }

        [TestMethod]
        public void VsoAadNoninteractiveLogonWithCredentialsTest()
        {
            Uri targetUri = DefaultTargetUri;
            VsoAadAuthentication aadAuthentication = GetVsoAadAuthentication("aad-noninter-creds");

            Credential originCreds = DefaultCredentials;
            Credential personalAccessToken;
            Token azureToken;

            Assert.IsTrue(Task.Run(async () => { return await aadAuthentication.NoninteractiveLogonWithCredentials(targetUri, originCreds, false); }).Result, "Non-interactive logon unexpectedly failed.");

            Assert.IsTrue(aadAuthentication.PersonalAccessTokenStore.ReadCredentials(targetUri, out personalAccessToken), "Personal Access Token not found in store as expected.");
            Assert.IsTrue(aadAuthentication.AdaRefreshTokenStore.ReadToken(targetUri, out azureToken) && azureToken.Value == "token-refresh", "ADA Refresh Token not found in store as expected.");

            Assert.IsFalse(String.Equals(originCreds.Password, personalAccessToken.Password, StringComparison.OrdinalIgnoreCase) || String.Equals(originCreds.Username, personalAccessToken.Password, StringComparison.OrdinalIgnoreCase), "Supplied credentials and Personal Access Token values unexpectedly matched.");
        }

        [TestMethod]
        public void VsoAadRefreshCredentialsTest()
        {
            Uri targetUri = DefaultTargetUri;
            Uri invalidUri = InvalidTargetUri;
            VsoAadAuthentication aadAuthentication = GetVsoAadAuthentication("aad-refresh");

            aadAuthentication.AdaRefreshTokenStore.WriteToken(targetUri, DefaultAzureRefreshToken);

            Credential personalAccessToken;

            Assert.IsFalse(aadAuthentication.PersonalAccessTokenStore.ReadCredentials(targetUri, out personalAccessToken), "Personal Access Token unexpectedly found in store.");

            Assert.IsTrue(Task.Run(async () => { return await aadAuthentication.RefreshCredentials(targetUri, false); }).Result, "Credentials refresh failed unexpectedly.");
            Assert.IsFalse(Task.Run(async () => { return await aadAuthentication.RefreshCredentials(invalidUri, false); }).Result, "Credentials refresh succeeded unexpectedly.");

            Assert.IsTrue(aadAuthentication.PersonalAccessTokenStore.ReadCredentials(targetUri, out personalAccessToken), "Personal Access Token not found in store as expected.");
        }

        [TestMethod]
        public void VsoAadSetCredentialsTest()
        {
            Uri targetUri = DefaultTargetUri;
            VsoAadAuthentication aadAuthentication = GetVsoAadAuthentication("aad-set");
            Credential credentials = DefaultCredentials;

            Credential personalAccessToken;
            Token azureToken;

            Assert.IsFalse(aadAuthentication.SetCredentials(targetUri, credentials), "Credentials were unexpectedly set.");

            Assert.IsFalse(aadAuthentication.PersonalAccessTokenStore.ReadCredentials(targetUri, out personalAccessToken), "Personal Access Token unexpectedly found in store.");
            Assert.IsFalse(aadAuthentication.AdaRefreshTokenStore.ReadToken(targetUri, out azureToken), "ADA Refresh Token unexpectedly found in store.");
            Assert.IsFalse(aadAuthentication.GetCredentials(targetUri, out credentials), "Credentials were retrieved unexpectedly.");
        }

        public void VsoAadValidateCredentialsTest()
        {
            VsoAadAuthentication aadAuthentication = GetVsoAadAuthentication("aad-validate");
            Credential credentials = null;

            Assert.IsFalse(Task.Run(async () => { return await aadAuthentication.ValidateCredentials(DefaultTargetUri, credentials); }).Result, "Credential validation unexpectedly failed.");

            credentials = DefaultCredentials;

            Assert.IsTrue(Task.Run(async () => { return await aadAuthentication.ValidateCredentials(DefaultTargetUri, credentials); }).Result, "Credential validation unexpectedly failed.");
        }

        private VsoAadAuthentication GetVsoAadAuthentication(string @namespace)
        {
            ICredentialStore tokenStore1 = new SecretCache(@namespace + 1);
            ITokenStore tokenStore2 = new SecretCache(@namespace + 2);
            ITokenStore tokenStore3 = new SecretCache(@namespace + 3);
            IVsoAuthority vsoAuthority = new AuthorityFake();
            return new VsoAadAuthentication(tokenStore1, tokenStore2, tokenStore3, vsoAuthority);
        }
    }
}
