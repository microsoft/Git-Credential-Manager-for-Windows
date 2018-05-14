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
        public async Task VstsMsaDeleteCredentialsTest()
        {
            TargetUri targetUri = DefaultTargetUri;
            VstsMsaAuthentication msaAuthority = GetVstsMsaAuthentication(RuntimeContext.Default, "msa-delete");

            if (msaAuthority.VstsAuthority is AuthorityFake fake)
            {
                fake.CredentialsAreValid = false;
            }

            await msaAuthority.PersonalAccessTokenStore.WriteCredentials(targetUri, DefaultPersonalAccessToken);

            await msaAuthority.DeleteCredentials(targetUri);
            Assert.Null(await msaAuthority.PersonalAccessTokenStore.ReadCredentials(targetUri));

            await msaAuthority.DeleteCredentials(targetUri);
            Assert.Null(await msaAuthority.PersonalAccessTokenStore.ReadCredentials(targetUri));
        }

        [Fact]
        public async Task VstsMsaGetCredentialsTest()
        {
            TargetUri targetUri = DefaultTargetUri;
            VstsMsaAuthentication msaAuthority = GetVstsMsaAuthentication(RuntimeContext.Default, "msa-get");

            Assert.Null(await msaAuthority.GetCredentials(targetUri));

            await msaAuthority.PersonalAccessTokenStore.WriteCredentials(targetUri, DefaultPersonalAccessToken);

            Assert.NotNull(await msaAuthority.GetCredentials(targetUri));
        }

        [Fact]
        public async Task VstsMsaInteractiveLogonTest()
        {
            TargetUri targetUri = DefaultTargetUri;
            VstsMsaAuthentication msaAuthority = GetVstsMsaAuthentication(RuntimeContext.Default, "msa-logon");

            Assert.Null(await msaAuthority.PersonalAccessTokenStore.ReadCredentials(targetUri));

            Assert.NotNull(await msaAuthority.InteractiveLogon(targetUri, false));

            Assert.NotNull(await msaAuthority.PersonalAccessTokenStore.ReadCredentials(targetUri));
        }

        [Fact]
        public async Task VstsMsaSetCredentialsTest()
        {
            TargetUri targetUri = DefaultTargetUri;
            VstsMsaAuthentication msaAuthority = GetVstsMsaAuthentication(RuntimeContext.Default, "msa-set");

            await msaAuthority.SetCredentials(targetUri, DefaultCredentials);

            Assert.Null(await msaAuthority.PersonalAccessTokenStore.ReadCredentials(targetUri));
        }

        [Fact]
        public async Task VstsMsaValidateCredentialsTest()
        {
            VstsMsaAuthentication msaAuthority = GetVstsMsaAuthentication(RuntimeContext.Default, "msa-validate");
            Credential credentials = null;

            Assert.False( await msaAuthority.ValidateCredentials(DefaultTargetUri, credentials), "Credential validation unexpectedly failed.");

            credentials = DefaultCredentials;

            Assert.True(await msaAuthority.ValidateCredentials(DefaultTargetUri, credentials), "Credential validation unexpectedly failed.");
        }

        private static VstsMsaAuthentication GetVstsMsaAuthentication(RuntimeContext context, string @namespace)
        {
            ICredentialStore tokenStore1 = new SecretCache(context, @namespace + 1, Secret.UriToIdentityUrl);
            ITokenStore tokenStore2 = new SecretCache(context, @namespace + 2, Secret.UriToIdentityUrl);
            IVstsAuthority liveAuthority = new AuthorityFake(VstsMsaAuthentication.QueryParameters);
            return new VstsMsaAuthentication(context, tokenStore1, tokenStore2, liveAuthority);
        }
    }
}
