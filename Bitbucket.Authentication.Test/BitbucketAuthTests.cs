using System.Diagnostics;
using Microsoft.Alm.Authentication;
using Xunit;

namespace Atlassian.Bitbucket.Authentication.Test
{
    public class BitbucketAuthTests
    {
        public BitbucketAuthTests()
        {
            Trace.Listeners.AddRange(Debug.Listeners);
        }

        [Fact]
        public void BitbucketAuthDeleteCredentialsTest()
        {
            var targetUri = new TargetUri("http://localhost");
            var bitbucketAuth = GetBitbucketAuthentication("Bitbucket-delete");

            bitbucketAuth.PersonalAccessTokenStore.WriteCredentials(targetUri, new Credential("username", "password"));

            Credential credentials;

            bitbucketAuth.DeleteCredentials(targetUri);

            // "User credentials were not deleted as expected"
            Assert.Null(credentials = bitbucketAuth.PersonalAccessTokenStore.ReadCredentials(targetUri));
        }

        [Fact]
        public void BitbucketAuthGetCredentialsTest()
        {
            var targetUri = new TargetUri("http://localhost");
            var bitbucketAuth = GetBitbucketAuthentication("Bitbucket-get");

            Credential credentials = null;

            // "User credentials were unexpectedly retrieved."
            Assert.Null(credentials = bitbucketAuth.GetCredentials(targetUri));

            credentials = new Credential("username", "password");

            bitbucketAuth.PersonalAccessTokenStore.WriteCredentials(targetUri, credentials);

            // "User credentials were unexpectedly not retrieved."
            Assert.NotNull(credentials = bitbucketAuth.GetCredentials(targetUri));
        }

        [Fact]
        public void BitbucketAuthSetCredentialsTest()
        {
            var targetUri = new TargetUri("http://localhost");
            var bitbucketAuth = GetBitbucketAuthentication("Bitbucket-set");

            Credential credentials = null;

            // "User credentials were unexpectedly retrieved."
            Assert.Null(credentials = bitbucketAuth.GetCredentials(targetUri));

            Assert.Throws<System.ArgumentNullException>(() =>
            {
                bitbucketAuth.SetCredentials(targetUri, credentials);
            });

            credentials = new Credential("username", "password");

            bitbucketAuth.SetCredentials(targetUri, credentials);

            // "User credentials were unexpectedly not retrieved."
            Assert.NotNull(credentials = bitbucketAuth.GetCredentials(targetUri));
        }

        private Authentication GetBitbucketAuthentication(string @namespace)
        {
            ICredentialStore credentialStore = new SecretCache(@namespace);

            return new Authentication(credentialStore, null, null);
        }
    }
}
