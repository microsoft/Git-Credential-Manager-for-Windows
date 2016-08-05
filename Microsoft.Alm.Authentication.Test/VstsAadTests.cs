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

            Credential personalAccessToken;

            aadAuthentication.DeleteCredentials(targetUri);
            Assert.IsNull(personalAccessToken = aadAuthentication.PersonalAccessTokenStore.ReadCredentials(targetUri), "Personal Access Tokens were not deleted as expected");

            aadAuthentication.DeleteCredentials(targetUri);
            Assert.IsNull(personalAccessToken = aadAuthentication.PersonalAccessTokenStore.ReadCredentials(targetUri), "Personal Access Tokens were not deleted as expected");
        }

        [TestMethod]
        public void VstsAadGetCredentialsTest()
        {
            TargetUri targetUri = DefaultTargetUri;
            VstsAadAuthentication aadAuthentication = GetVstsAadAuthentication("aad-get");

            Credential credentials;

            Assert.IsNull(credentials = aadAuthentication.GetCredentials(targetUri), "Credentials were retrieved unexpectedly.");

            aadAuthentication.PersonalAccessTokenStore.WriteCredentials(targetUri, DefaultPersonalAccessToken);

            Assert.IsNotNull(credentials = aadAuthentication.GetCredentials(targetUri), "Credentials were not retrieved as expected.");
        }

        [TestMethod]
        public void VstsAadInteractiveLogonTest()
        {
            TargetUri targetUri = DefaultTargetUri;
            VstsAadAuthentication aadAuthentication = GetVstsAadAuthentication("aad-logon");

            Credential personalAccessToken;

            Assert.IsNull(personalAccessToken = aadAuthentication.PersonalAccessTokenStore.ReadCredentials(targetUri), "Personal Access Token found in store unexpectedly.");

            Assert.IsNotNull(personalAccessToken = aadAuthentication.InteractiveLogon(targetUri, false).Result, "Interactive logon failed unexpectedly.");

            Assert.IsNotNull(personalAccessToken = aadAuthentication.PersonalAccessTokenStore.ReadCredentials(targetUri), "Personal Access Token not found in store as expected.");
        }

        [TestMethod]
        public void VstsAadNoninteractiveLogonTest()
        {
            TargetUri targetUri = DefaultTargetUri;
            VstsAadAuthentication aadAuthentication = GetVstsAadAuthentication("aad-noninteractive");

            Credential personalAccessToken;

            Assert.IsNotNull(personalAccessToken = Task.Run(async () => { return await aadAuthentication.NoninteractiveLogon(targetUri, false); }).Result, "Non-interactive logon unexpectedly failed.");

            Assert.IsNotNull(personalAccessToken = aadAuthentication.PersonalAccessTokenStore.ReadCredentials(targetUri), "Personal Access Token not found in store as expected.");
        }


        [TestMethod]
        public void VstsAadSetCredentialsTest()
        {
            TargetUri targetUri = DefaultTargetUri;
            VstsAadAuthentication aadAuthentication = GetVstsAadAuthentication("aad-set");
            Credential credentials = DefaultCredentials;

            Credential personalAccessToken;

            aadAuthentication.SetCredentials(targetUri, credentials);

            Assert.IsNull(personalAccessToken = aadAuthentication.PersonalAccessTokenStore.ReadCredentials(targetUri), "Personal Access Token unexpectedly found in store.");
            Assert.IsNull(credentials = aadAuthentication.GetCredentials(targetUri), "Credentials were retrieved unexpectedly.");
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
            IVstsAuthority vstsAuthority = new AuthorityFake();
            return new VstsAadAuthentication(tokenStore1, tokenStore2, vstsAuthority);
        }
    }
}
