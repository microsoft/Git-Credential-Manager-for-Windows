using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Alm.Authentication.Test
{
    [TestClass]
    public class VstsMsaTests : AuthenticationTests
    {
        public VstsMsaTests()
            : base()
        { }

        [TestMethod]
        public void VstsMsaDeleteCredentialsTest()
        {
            TargetUri targetUri = DefaultTargetUri;
            VstsMsaAuthentication msaAuthority = GetVstsMsaAuthentication("msa-delete");

            msaAuthority.PersonalAccessTokenStore.WriteCredentials(targetUri, DefaultPersonalAccessToken);

            Credential personalAccessToken;

            msaAuthority.DeleteCredentials(targetUri);
            Assert.IsFalse(msaAuthority.PersonalAccessTokenStore.ReadCredentials(targetUri, out personalAccessToken), "Personal Access Tokens were not deleted as expected"); ;

            msaAuthority.DeleteCredentials(targetUri);
            Assert.IsFalse(msaAuthority.PersonalAccessTokenStore.ReadCredentials(targetUri, out personalAccessToken), "Personal Access Tokens were not deleted as expected"); ;
        }

        [TestMethod]
        public void VstsMsaGetCredentialsTest()
        {
            TargetUri targetUri = DefaultTargetUri;
            VstsMsaAuthentication msaAuthority = GetVstsMsaAuthentication("msa-get");
            Credential credentials;

            Assert.IsFalse(msaAuthority.GetCredentials(targetUri, out credentials), "Credentials were retrieved unexpectedly.");

            msaAuthority.PersonalAccessTokenStore.WriteCredentials(targetUri, DefaultPersonalAccessToken);

            Assert.IsTrue(msaAuthority.GetCredentials(targetUri, out credentials), "Credentials were not retrieved as expected.");
        }

        [TestMethod]
        public void VstsMsaInteractiveLogonTest()
        {
            TargetUri targetUri = DefaultTargetUri;
            VstsMsaAuthentication msaAuthority = GetVstsMsaAuthentication("msa-logon");
            Credential personalAccessToken;

            Assert.IsFalse(msaAuthority.PersonalAccessTokenStore.ReadCredentials(targetUri, out personalAccessToken), "Personal Access Token found in store unexpectedly.");

            Assert.IsNotNull(personalAccessToken = msaAuthority.InteractiveLogon(targetUri, false).Result, "Interactive logon failed unexpectedly.");

            Assert.IsTrue(msaAuthority.PersonalAccessTokenStore.ReadCredentials(targetUri, out personalAccessToken), "Personal Access Token not found in store as expected.");
        }

        [TestMethod]
        public void VstsMsaSetCredentialsTest()
        {
            TargetUri targetUri = DefaultTargetUri;
            VstsMsaAuthentication msaAuthority = GetVstsMsaAuthentication("msa-set");
            Credential personalAccessToken;

            try
            {
                msaAuthority.SetCredentials(targetUri, DefaultCredentials);
                Assert.Fail("Credentials were unexpectedly set.");
            }
            catch { }

            Assert.IsFalse(msaAuthority.PersonalAccessTokenStore.ReadCredentials(targetUri, out personalAccessToken), "Personal Access Token unexpectedly found in store.");
        }

        [TestMethod]
        public void VstsMsaValidateCredentialsTest()
        {
            VstsMsaAuthentication msaAuthority = GetVstsMsaAuthentication("msa-validate");
            Credential credentials = null;

            Assert.IsFalse(Task.Run(async () => { return await msaAuthority.ValidateCredentials(DefaultTargetUri, credentials); }).Result, "Credential validation unexpectedly failed.");

            credentials = DefaultCredentials;

            Assert.IsTrue(Task.Run(async () => { return await msaAuthority.ValidateCredentials(DefaultTargetUri, credentials); }).Result, "Credential validation unexpectedly failed.");
        }

        private VstsMsaAuthentication GetVstsMsaAuthentication(string @namespace)
        {
            ICredentialStore tokenStore1 = new SecretCache(@namespace + 1);
            ITokenStore tokenStore2 = new SecretCache(@namespace + 2);
            IVstsAuthority liveAuthority = new AuthorityFake();
            return new VstsMsaAuthentication(tokenStore1, tokenStore2, liveAuthority);
        }
    }
}
