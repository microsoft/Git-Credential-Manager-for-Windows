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
            VstsAadAuthentication aadAuthentication = GetVstsAadAuthentication("aad-delete", null);

            aadAuthentication.PersonalAccessTokenStore.WriteCredentials(targetUri, DefaultPersonalAccessToken);

            aadAuthentication.DeleteCredentials(targetUri);
            Assert.IsNull(aadAuthentication.PersonalAccessTokenStore.ReadCredentials(targetUri), "Personal Access Tokens were not deleted as expected");

            aadAuthentication.DeleteCredentials(targetUri);
            Assert.IsNull(aadAuthentication.PersonalAccessTokenStore.ReadCredentials(targetUri), "Personal Access Tokens were not deleted as expected");
        }

        [TestMethod]
        public void VstsAadGetCredentialsTest()
        {
            TargetUri targetUri = DefaultTargetUri;
            VstsAadAuthentication aadAuthentication = GetVstsAadAuthentication("aad-get", null);

            Assert.IsNull(aadAuthentication.GetCredentials(targetUri), "Credentials were retrieved unexpectedly.");

            aadAuthentication.PersonalAccessTokenStore.WriteCredentials(targetUri, DefaultPersonalAccessToken);

            Assert.IsNotNull(aadAuthentication.GetCredentials(targetUri), "Credentials were not retrieved as expected.");
        }

        [TestMethod]
        public void VstsAadInteractiveLogonTest()
        {
            TargetUri targetUri = DefaultTargetUri;
            VstsAadAuthentication aadAuthentication = GetVstsAadAuthentication("aad-logon", null);

            Assert.IsNull(aadAuthentication.PersonalAccessTokenStore.ReadCredentials(targetUri), "Personal Access Token found in store unexpectedly.");

            Assert.IsNotNull(aadAuthentication.InteractiveLogon(targetUri, false).Result, "Interactive logon failed unexpectedly.");

            Assert.IsNotNull(aadAuthentication.PersonalAccessTokenStore.ReadCredentials(targetUri), "Personal Access Token not found in store as expected.");
        }

        [TestMethod]
        public void VstsAadNoninteractiveLogonTest()
        {
            TargetUri targetUri = DefaultTargetUri;
            VstsAadAuthentication aadAuthentication = GetVstsAadAuthentication("aad-noninteractive", null);

            Assert.IsNotNull(Task.Run(async () => { return await aadAuthentication.NoninteractiveLogon(targetUri, false); }).Result, "Non-interactive logon unexpectedly failed.");

            Assert.IsNotNull(aadAuthentication.PersonalAccessTokenStore.ReadCredentials(targetUri), "Personal Access Token not found in store as expected.");
        }

        [TestMethod]
        public void VstsAadSetCredentialsTest()
        {
            TargetUri targetUri = DefaultTargetUri;
            VstsAadAuthentication aadAuthentication = GetVstsAadAuthentication("aad-set", null);
            Credential credentials = DefaultCredentials;

            aadAuthentication.SetCredentials(targetUri, credentials);

            Assert.IsNull(aadAuthentication.PersonalAccessTokenStore.ReadCredentials(targetUri), "Personal Access Token unexpectedly found in store.");
            Assert.IsNull(credentials = aadAuthentication.GetCredentials(targetUri), "Credentials were retrieved unexpectedly.");
        }

        [TestMethod]
        public void VstsAadValidateCredentialsTest()
        {
            VstsAadAuthentication aadAuthentication = GetVstsAadAuthentication("aad-validate", null);
            Credential credentials = null;

            Assert.IsFalse(Task.Run(async () => { return await aadAuthentication.ValidateCredentials(DefaultTargetUri, credentials); }).Result, "Credential validation unexpectedly failed.");

            credentials = DefaultCredentials;

            Assert.IsTrue(Task.Run(async () => { return await aadAuthentication.ValidateCredentials(DefaultTargetUri, credentials); }).Result, "Credential validation unexpectedly failed.");
        }

        [TestMethod]
        public void VstsAadValidateLoginHintTest()
        {
            TargetUri targetUri = DefaultTargetUri;
            VstsAadAuthentication aadAuthentication = GetVstsAadAuthentication("aad-loginhint", "username@domain");

            Assert.IsNotNull(Task.Run(async () => { return await aadAuthentication.NoninteractiveLogon(targetUri, false); }).Result, "Non-interactive logon unexpectedly failed.");

            Assert.IsNotNull(aadAuthentication.PersonalAccessTokenStore.ReadCredentials(targetUri), "Personal Access Token not found in store as expected.");
        }

        private static VstsAadAuthentication GetVstsAadAuthentication(string @namespace, string loginHint)
        {
            string expectedQueryParamters = null;
            if (loginHint != null)
            {
                expectedQueryParamters = "login_hint=" + loginHint;
            }

            ICredentialStore tokenStore1 = new SecretCache(@namespace + 1);
            ITokenStore tokenStore2 = new SecretCache(@namespace + 2);
            IVstsAuthority vstsAuthority = new AuthorityFake(expectedQueryParamters);
            return new VstsAadAuthentication(tokenStore1, tokenStore2, vstsAuthority, loginHint);
        }
    }
}
