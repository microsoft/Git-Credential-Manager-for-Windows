using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Alm.Authentication.Test
{
    [TestClass]
    public class VstsAadTests : AuthenticationTests
    {
        public VstsAadTests()
            : base()
        { }

        [TestMethod]
        public void VstsAadDeleteCredentialsTest()
        {
            TargetUri targetUri = DefaultTargetUri;
            VstsAadAuthentication aadAuthentication = GetVstsAadAuthentication("aad-delete");

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
        public void VstsAadGetCredentialsTest()
        {
            TargetUri targetUri = DefaultTargetUri;
            VstsAadAuthentication aadAuthentication = GetVstsAadAuthentication("aad-get");

            Credential credentials;

            Assert.IsFalse(aadAuthentication.GetCredentials(targetUri, out credentials), "Credentials were retrieved unexpectedly.");

            aadAuthentication.PersonalAccessTokenStore.WriteCredentials(targetUri, DefaultPersonalAccessToken);
            aadAuthentication.AdaRefreshTokenStore.WriteToken(targetUri, DefaultAzureRefreshToken);

            Assert.IsTrue(aadAuthentication.GetCredentials(targetUri, out credentials), "Credentials were not retrieved as expected.");
        }

        [TestMethod]
        public void VstsAadInteractiveLogonTest()
        {
            TargetUri targetUri = DefaultTargetUri;
            VstsAadAuthentication aadAuthentication = GetVstsAadAuthentication("aad-logon");

            Credential personalAccessToken;
            Token azureToken;

            Assert.IsFalse(aadAuthentication.PersonalAccessTokenStore.ReadCredentials(targetUri, out personalAccessToken), "Personal Access Token found in store unexpectedly.");
            Assert.IsFalse(aadAuthentication.AdaRefreshTokenStore.ReadToken(targetUri, out azureToken), "ADA Refresh Token found in store unexpectedly.");

            Assert.IsTrue(aadAuthentication.InteractiveLogon(targetUri, false), "Interactive logon failed unexpectedly.");

            Assert.IsTrue(aadAuthentication.PersonalAccessTokenStore.ReadCredentials(targetUri, out personalAccessToken), "Personal Access Token not found in store as expected.");
            Assert.IsTrue(aadAuthentication.AdaRefreshTokenStore.ReadToken(targetUri, out azureToken) && azureToken.Value == "token-refresh", "ADA Refresh Token not found in store as expected.");
        }

        [TestMethod]
        public void VstsAadNoninteractiveLogonTest()
        {
            TargetUri targetUri = DefaultTargetUri;
            VstsAadAuthentication aadAuthentication = GetVstsAadAuthentication("aad-noninteractive");

            Credential personalAccessToken;
            Token azureToken;

            Assert.IsTrue(Task.Run(async () => { return await aadAuthentication.NoninteractiveLogon(targetUri, false); }).Result, "Non-interactive logon unexpectedly failed.");

            Assert.IsTrue(aadAuthentication.PersonalAccessTokenStore.ReadCredentials(targetUri, out personalAccessToken), "Personal Access Token not found in store as expected.");
            Assert.IsTrue(aadAuthentication.AdaRefreshTokenStore.ReadToken(targetUri, out azureToken) && azureToken.Value == "token-refresh", "ADA Refresh Token not found in store as expected.");
        }

        [TestMethod]
        public void VstsAadNoninteractiveLogonWithCredentialsTest()
        {
            TargetUri targetUri = DefaultTargetUri;
            VstsAadAuthentication aadAuthentication = GetVstsAadAuthentication("aad-noninter-creds");

            Credential originCreds = DefaultCredentials;
            Credential personalAccessToken;
            Token azureToken;

            Assert.IsTrue(Task.Run(async () => { return await aadAuthentication.NoninteractiveLogonWithCredentials(targetUri, originCreds, false); }).Result, "Non-interactive logon unexpectedly failed.");

            Assert.IsTrue(aadAuthentication.PersonalAccessTokenStore.ReadCredentials(targetUri, out personalAccessToken), "Personal Access Token not found in store as expected.");
            Assert.IsTrue(aadAuthentication.AdaRefreshTokenStore.ReadToken(targetUri, out azureToken) && azureToken.Value == "token-refresh", "ADA Refresh Token not found in store as expected.");

            Assert.IsFalse(String.Equals(originCreds.Password, personalAccessToken.Password, StringComparison.OrdinalIgnoreCase) || String.Equals(originCreds.Username, personalAccessToken.Password, StringComparison.OrdinalIgnoreCase), "Supplied credentials and Personal Access Token values unexpectedly matched.");
        }

        [TestMethod]
        public void VstsAadRefreshCredentialsTest()
        {
            TargetUri targetUri = DefaultTargetUri;
            TargetUri invalidUri = InvalidTargetUri;
            VstsAadAuthentication aadAuthentication = GetVstsAadAuthentication("aad-refresh");

            aadAuthentication.AdaRefreshTokenStore.WriteToken(targetUri, DefaultAzureRefreshToken);

            Credential personalAccessToken;

            Assert.IsFalse(aadAuthentication.PersonalAccessTokenStore.ReadCredentials(targetUri, out personalAccessToken), "Personal Access Token unexpectedly found in store.");

            Assert.IsTrue(Task.Run(async () => { return await aadAuthentication.RefreshCredentials(targetUri, false); }).Result, "Credentials refresh failed unexpectedly.");
            Assert.IsFalse(Task.Run(async () => { return await aadAuthentication.RefreshCredentials(invalidUri, false); }).Result, "Credentials refresh succeeded unexpectedly.");

            Assert.IsTrue(aadAuthentication.PersonalAccessTokenStore.ReadCredentials(targetUri, out personalAccessToken), "Personal Access Token not found in store as expected.");
        }

        [TestMethod]
        public void VstsAadSetCredentialsTest()
        {
            TargetUri targetUri = DefaultTargetUri;
            VstsAadAuthentication aadAuthentication = GetVstsAadAuthentication("aad-set");
            Credential credentials = DefaultCredentials;

            Credential personalAccessToken;
            Token azureToken;

            Assert.IsFalse(aadAuthentication.SetCredentials(targetUri, credentials), "Credentials were unexpectedly set.");

            Assert.IsFalse(aadAuthentication.PersonalAccessTokenStore.ReadCredentials(targetUri, out personalAccessToken), "Personal Access Token unexpectedly found in store.");
            Assert.IsFalse(aadAuthentication.AdaRefreshTokenStore.ReadToken(targetUri, out azureToken), "ADA Refresh Token unexpectedly found in store.");
            Assert.IsFalse(aadAuthentication.GetCredentials(targetUri, out credentials), "Credentials were retrieved unexpectedly.");
        }

        public void VstsAadValidateCredentialsTest()
        {
            VstsAadAuthentication aadAuthentication = GetVstsAadAuthentication("aad-validate");
            Credential credentials = null;

            Assert.IsFalse(Task.Run(async () => { return await aadAuthentication.ValidateCredentials(DefaultTargetUri, credentials); }).Result, "Credential validation unexpectedly failed.");

            credentials = DefaultCredentials;

            Assert.IsTrue(Task.Run(async () => { return await aadAuthentication.ValidateCredentials(DefaultTargetUri, credentials); }).Result, "Credential validation unexpectedly failed.");
        }

        private VstsAadAuthentication GetVstsAadAuthentication(string @namespace)
        {
            ICredentialStore tokenStore1 = new SecretCache(@namespace + 1);
            ITokenStore tokenStore2 = new SecretCache(@namespace + 2);
            ITokenStore tokenStore3 = new SecretCache(@namespace + 3);
            IVstsAuthority vstsAuthority = new AuthorityFake();
            return new VstsAadAuthentication(tokenStore1, tokenStore2, tokenStore3, vstsAuthority);
        }
    }
}
