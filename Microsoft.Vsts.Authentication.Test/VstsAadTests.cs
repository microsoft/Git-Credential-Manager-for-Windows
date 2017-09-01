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
        public void VstsAadDeleteCredentialsTest()
        {
            TargetUri targetUri = DefaultTargetUri;
            VstsAadAuthentication aadAuthentication = GetVstsAadAuthentication("aad-delete");

            aadAuthentication.PersonalAccessTokenStore.WriteCredentials(targetUri, DefaultPersonalAccessToken);

            aadAuthentication.DeleteCredentials(targetUri);
            Assert.Null(aadAuthentication.PersonalAccessTokenStore.ReadCredentials(targetUri));

            aadAuthentication.DeleteCredentials(targetUri);
            Assert.Null(aadAuthentication.PersonalAccessTokenStore.ReadCredentials(targetUri));
        }

        [Fact]
        public void VstsAadGetCredentialsTest()
        {
            TargetUri targetUri = DefaultTargetUri;
            VstsAadAuthentication aadAuthentication = GetVstsAadAuthentication("aad-get");

            Assert.Null(aadAuthentication.GetCredentials(targetUri));

            aadAuthentication.PersonalAccessTokenStore.WriteCredentials(targetUri, DefaultPersonalAccessToken);

            Assert.NotNull(aadAuthentication.GetCredentials(targetUri));
        }

        [Fact]
        public void VstsAadInteractiveLogonTest()
        {
            TargetUri targetUri = DefaultTargetUri;
            VstsAadAuthentication aadAuthentication = GetVstsAadAuthentication("aad-logon");

            Assert.Null(aadAuthentication.PersonalAccessTokenStore.ReadCredentials(targetUri));

            Assert.NotNull(aadAuthentication.InteractiveLogon(targetUri, false).Result);

            Assert.NotNull(aadAuthentication.PersonalAccessTokenStore.ReadCredentials(targetUri));
        }

        [Fact]
        public void VstsAadNoninteractiveLogonTest()
        {
            TargetUri targetUri = DefaultTargetUri;
            VstsAadAuthentication aadAuthentication = GetVstsAadAuthentication("aad-noninteractive");

            Assert.NotNull(Task.Run(async () => { return await aadAuthentication.NoninteractiveLogon(targetUri, false); }).Result);

            Assert.NotNull(aadAuthentication.PersonalAccessTokenStore.ReadCredentials(targetUri));
        }

        [Fact]
        public void VstsAadSetCredentialsTest()
        {
            TargetUri targetUri = DefaultTargetUri;
            VstsAadAuthentication aadAuthentication = GetVstsAadAuthentication("aad-set");
            Credential credentials = DefaultCredentials;

            aadAuthentication.SetCredentials(targetUri, credentials);

            Assert.Null(aadAuthentication.PersonalAccessTokenStore.ReadCredentials(targetUri));
            Assert.Null(credentials = aadAuthentication.GetCredentials(targetUri));
        }

        [Fact]
        public void VstsAadValidateCredentialsTest()
        {
            VstsAadAuthentication aadAuthentication = GetVstsAadAuthentication("aad-validate");
            Credential credentials = null;

            Assert.False(Task.Run(async () => { return await aadAuthentication.ValidateCredentials(DefaultTargetUri, credentials); }).Result, "Credential validation unexpectedly failed.");

            credentials = DefaultCredentials;

            Assert.True(Task.Run(async () => { return await aadAuthentication.ValidateCredentials(DefaultTargetUri, credentials); }).Result, "Credential validation unexpectedly failed.");
        }

        [Fact]
        public void VstsAadValidateLoginHintTest()
        {
            TargetUri targetUri = DefaultTargetUri;
            VstsAadAuthentication aadAuthentication = GetVstsAadAuthentication("aad-loginhint");

            Assert.NotNull(Task.Run(async () => { return await aadAuthentication.NoninteractiveLogon(targetUri, false); }).Result);

            Assert.NotNull(aadAuthentication.PersonalAccessTokenStore.ReadCredentials(targetUri));
        }

        private static VstsAadAuthentication GetVstsAadAuthentication(string @namespace)
        {
            string expectedQueryParameters = null;

            ICredentialStore tokenStore1 = new SecretCache(@namespace + 1);
            ITokenStore tokenStore2 = new SecretCache(@namespace + 2);
            IVstsAuthority vstsAuthority = new AuthorityFake(expectedQueryParameters);
            return new VstsAadAuthentication(tokenStore1, tokenStore2, vstsAuthority);
        }
    }
}
