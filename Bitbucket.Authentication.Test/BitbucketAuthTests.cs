using System.Diagnostics;
using System.Threading.Tasks;
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
        public async Task BitbucketAuthDeleteCredentialsTest()
        {
            var targetUri = new TargetUri("http://localhost");
            var bitbucketAuth = GetBitbucketAuthentication("Bitbucket-delete");

            await bitbucketAuth.PersonalAccessTokenStore.WriteCredentials(targetUri, new Credential("username", "password"));

            Credential credentials;

            await bitbucketAuth.DeleteCredentials(targetUri);

            // "User credentials were not deleted as expected"
            Assert.Null(credentials = await bitbucketAuth.PersonalAccessTokenStore.ReadCredentials(targetUri));
        }

        [Fact]
        public async Task BitbucketAuthGetCredentialsTest()
        {
            var targetUri = new TargetUri("http://localhost");
            var bitbucketAuth = GetBitbucketAuthentication("Bitbucket-get");

            Credential credentials = null;

            // "User credentials were unexpectedly retrieved."
            Assert.Null(credentials = await bitbucketAuth.GetCredentials(targetUri));

            credentials = new Credential("username", "password");

            await bitbucketAuth.PersonalAccessTokenStore.WriteCredentials(targetUri, credentials);

            // "User credentials were unexpectedly not retrieved."
            Assert.NotNull(credentials = await bitbucketAuth.GetCredentials(targetUri));
        }

        [Fact]
        public async Task BitbucketAuthSetCredentialsTest()
        {
            var targetUri = new TargetUri("http://localhost");
            var bitbucketAuth = GetBitbucketAuthentication("Bitbucket-set");

            Credential credentials = null;

            // "User credentials were unexpectedly retrieved."
            Assert.Null(credentials = await bitbucketAuth.GetCredentials(targetUri));

            Assert.Throws<System.ArgumentNullException>(() =>
            {
                try
                {
                    var b = bitbucketAuth.SetCredentials(targetUri, credentials).Result;
                }
                catch (System.AggregateException exception)
                {
                    throw exception.Flatten().InnerException;
                }
            });

            credentials = new Credential("username", "password");

            await bitbucketAuth.SetCredentials(targetUri, credentials);

            // "User credentials were unexpectedly not retrieved."
            Assert.NotNull(credentials = await bitbucketAuth.GetCredentials(targetUri));
        }

        private Authentication GetBitbucketAuthentication(string @namespace)
        {
            ICredentialStore credentialStore = new SecretCache(@namespace);

            return new Authentication(credentialStore, null, null);
        }
    }
}
