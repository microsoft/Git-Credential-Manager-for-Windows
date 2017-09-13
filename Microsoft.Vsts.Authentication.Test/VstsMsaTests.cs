using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Alm.Authentication.Test
{
    public class VstsMsaTests : AuthenticationTests
    {
        public VstsMsaTests()
            : base()
        { }

        [Fact]
        public void VstsMsaDeleteCredentialsTest()
        {
            TargetUri targetUri = DefaultTargetUri;
            VstsMsaAuthentication msaAuthority = GetVstsMsaAuthentication("msa-delete");

            msaAuthority.PersonalAccessTokenStore.WriteCredentials(targetUri, DefaultPersonalAccessToken);

            msaAuthority.DeleteCredentials(targetUri);
            Assert.Null(msaAuthority.PersonalAccessTokenStore.ReadCredentials(targetUri));

            msaAuthority.DeleteCredentials(targetUri);
            Assert.Null(msaAuthority.PersonalAccessTokenStore.ReadCredentials(targetUri));
        }

        [Fact]
        public void VstsMsaGetCredentialsTest()
        {
            TargetUri targetUri = DefaultTargetUri;
            VstsMsaAuthentication msaAuthority = GetVstsMsaAuthentication("msa-get");

            Assert.Null(msaAuthority.GetCredentials(targetUri));

            msaAuthority.PersonalAccessTokenStore.WriteCredentials(targetUri, DefaultPersonalAccessToken);

            Assert.NotNull(msaAuthority.GetCredentials(targetUri));
        }

        [Fact]
        public void VstsMsaInteractiveLogonTest()
        {
            TargetUri targetUri = DefaultTargetUri;
            VstsMsaAuthentication msaAuthority = GetVstsMsaAuthentication("msa-logon");

            Assert.Null(msaAuthority.PersonalAccessTokenStore.ReadCredentials(targetUri));

            Assert.NotNull(msaAuthority.InteractiveLogon(targetUri, false).Result);

            Assert.NotNull(msaAuthority.PersonalAccessTokenStore.ReadCredentials(targetUri));
        }

        [Fact]
        public void VstsMsaSetCredentialsTest()
        {
            TargetUri targetUri = DefaultTargetUri;
            VstsMsaAuthentication msaAuthority = GetVstsMsaAuthentication("msa-set");

            msaAuthority.SetCredentials(targetUri, DefaultCredentials);

            Assert.Null(msaAuthority.PersonalAccessTokenStore.ReadCredentials(targetUri));
        }

        [Fact]
        public void VstsMsaValidateCredentialsTest()
        {
            VstsMsaAuthentication msaAuthority = GetVstsMsaAuthentication("msa-validate");
            Credential credentials = null;

            Assert.False(Task.Run(async () => { return await msaAuthority.ValidateCredentials(DefaultTargetUri, credentials); }).Result, "Credential validation unexpectedly failed.");

            credentials = DefaultCredentials;

            Assert.True(Task.Run(async () => { return await msaAuthority.ValidateCredentials(DefaultTargetUri, credentials); }).Result, "Credential validation unexpectedly failed.");
        }

        private static VstsMsaAuthentication GetVstsMsaAuthentication(string @namespace)
        {
            ICredentialStore tokenStore1 = new SecretCache(@namespace + 1);
            ITokenStore tokenStore2 = new SecretCache(@namespace + 2);
            IVstsAuthority liveAuthority = new AuthorityFake(VstsMsaAuthentication.QueryParameters);
            return new VstsMsaAuthentication(tokenStore1, tokenStore2, liveAuthority);
        }
    }
}
