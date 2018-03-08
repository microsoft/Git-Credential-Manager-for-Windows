using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Alm.Authentication.Test
{
    public class VstsAadTests : AuthenticationTests
    {
        public VstsAadTests()
            : base()
        { }

        [Fact]
        public async Task VstsAadDeleteCredentialsTest()
        {
            TargetUri targetUri = DefaultTargetUri;
            VstsAadAuthentication aadAuthentication = GetVstsAadAuthentication("aad-delete");

            if (aadAuthentication.VstsAuthority is AuthorityFake fake)
            {
                fake.CredentialsAreValid = false;
            }

            await aadAuthentication.PersonalAccessTokenStore.WriteCredentials(targetUri, DefaultPersonalAccessToken);

            await aadAuthentication.DeleteCredentials(targetUri);
            Assert.Null(await aadAuthentication.PersonalAccessTokenStore.ReadCredentials(targetUri));

            await aadAuthentication.DeleteCredentials(targetUri);
            Assert.Null(await aadAuthentication.PersonalAccessTokenStore.ReadCredentials(targetUri));
        }

        [Fact]
        public async Task VstsAadGetCredentialsTest()
        {
            TargetUri targetUri = DefaultTargetUri;
            VstsAadAuthentication aadAuthentication = GetVstsAadAuthentication("aad-get");

            Assert.Null(await aadAuthentication.GetCredentials(targetUri));

            await aadAuthentication.PersonalAccessTokenStore.WriteCredentials(targetUri, DefaultPersonalAccessToken);

            Assert.NotNull(await aadAuthentication.GetCredentials(targetUri));
        }

        [Fact]
        public async Task VstsAadInteractiveLogonTest()
        {
            TargetUri targetUri = DefaultTargetUri;
            VstsAadAuthentication aadAuthentication = GetVstsAadAuthentication("aad-logon");

            Assert.Null(await aadAuthentication.PersonalAccessTokenStore.ReadCredentials(targetUri));

            Assert.NotNull(await aadAuthentication.InteractiveLogon(targetUri, new PersonalAccessTokenOptions { RequireCompactToken = false }));

            Assert.NotNull(await aadAuthentication.PersonalAccessTokenStore.ReadCredentials(targetUri));
        }

        [Fact]
        public async Task VstsAadNoninteractiveLogonTest()
        {
            TargetUri targetUri = DefaultTargetUri;
            VstsAadAuthentication aadAuthentication = GetVstsAadAuthentication("aad-noninteractive");

            Assert.NotNull(await aadAuthentication.NoninteractiveLogon(targetUri, new PersonalAccessTokenOptions { RequireCompactToken = false }));

            Assert.NotNull(await aadAuthentication.PersonalAccessTokenStore.ReadCredentials(targetUri));
        }

        [Fact]
        public async Task VstsAadSetCredentialsTest()
        {
            TargetUri targetUri = DefaultTargetUri;
            VstsAadAuthentication aadAuthentication = GetVstsAadAuthentication("aad-set");
            Credential credentials = DefaultCredentials;

            await aadAuthentication.SetCredentials(targetUri, credentials);

            Assert.Null(await aadAuthentication.PersonalAccessTokenStore.ReadCredentials(targetUri));
            Assert.Null(credentials = await aadAuthentication.GetCredentials(targetUri));
        }

        [Fact]
        public async Task VstsAadValidateCredentialsTest()
        {
            VstsAadAuthentication aadAuthentication = GetVstsAadAuthentication("aad-validate");
            Credential credentials = null;

            Assert.False(await aadAuthentication.ValidateCredentials(DefaultTargetUri, credentials), "Credential validation unexpectedly failed.");

            credentials = DefaultCredentials;

            Assert.True(await aadAuthentication.ValidateCredentials(DefaultTargetUri, credentials), "Credential validation unexpectedly failed.");
        }

        private static VstsAadAuthentication GetVstsAadAuthentication(string @namespace)
        {
            string expectedQueryParameters = null;

            ICredentialStore tokenStore1 = new SecretCache(@namespace + 1, Secret.UriToIdentityUrl);
            ITokenStore tokenStore2 = new SecretCache(@namespace + 2, Secret.UriToIdentityUrl);
            IVstsAuthority vstsAuthority = new AuthorityFake(expectedQueryParameters);
            return new VstsAadAuthentication(tokenStore1, tokenStore2, vstsAuthority);
        }
    }
}
