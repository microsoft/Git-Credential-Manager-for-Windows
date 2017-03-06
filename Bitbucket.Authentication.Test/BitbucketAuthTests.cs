using System.Diagnostics;
using Microsoft.Alm.Authentication;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Atlassian.Bitbucket.Authentication.Test
{
    [TestClass]
    public class BitbucketAuthTests
    {
        public BitbucketAuthTests()
        {
            Trace.Listeners.AddRange(Debug.Listeners);
        }

        [TestMethod]
        public void BitbucketAuthDeleteCredentialsTest()
        {
            var targetUri = new TargetUri("http://localhost");
            var bitbucketAuth = GetBitbucketAuthentication("Bitbucket-delete");

            bitbucketAuth.PersonalAccessTokenStore.WriteCredentials(targetUri, new Credential("username", "password"));

            Credential credentials;

            bitbucketAuth.DeleteCredentials(targetUri);

            Assert.IsNull(credentials = bitbucketAuth.PersonalAccessTokenStore.ReadCredentials(targetUri),
                "User credentials were not deleted as expected");
        }

        [TestMethod]
        public void BitbucketAuthGetCredentialsTest()
        {
            var targetUri = new TargetUri("http://localhost");
            var bitbucketAuth = GetBitbucketAuthentication("Bitbucket-get");

            Credential credentials = null;

            Assert.IsNull(credentials = bitbucketAuth.GetCredentials(targetUri),
                "User credentials were unexpectedly retrieved.");

            credentials = new Credential("username", "password");

            bitbucketAuth.PersonalAccessTokenStore.WriteCredentials(targetUri, credentials);

            Assert.IsNotNull(credentials = bitbucketAuth.GetCredentials(targetUri),
                "User credentials were unexpectedly not retrieved.");
        }

        [TestMethod]
        public void BitbucketAuthSetCredentialsTest()
        {
            var targetUri = new TargetUri("http://localhost");
            var bitbucketAuth = GetBitbucketAuthentication("Bitbucket-set");

            Credential credentials = null;

            Assert.IsNull(credentials = bitbucketAuth.GetCredentials(targetUri),
                "User credentials were unexpectedly retrieved.");
            try
            {
                bitbucketAuth.SetCredentials(targetUri, credentials);
                Assert.Fail("User credentials were unexpectedly set.");
            }
            catch
            {
            }

            credentials = new Credential("username", "password");

            bitbucketAuth.SetCredentials(targetUri, credentials);

            Assert.IsNotNull(credentials = bitbucketAuth.GetCredentials(targetUri),
                "User credentials were unexpectedly not retrieved.");
        }

        private Authentication GetBitbucketAuthentication(string @namespace)
        {
            ICredentialStore credentialStore = new SecretCache(@namespace);

            return new Authentication(credentialStore, null, null);
        }
    }
}