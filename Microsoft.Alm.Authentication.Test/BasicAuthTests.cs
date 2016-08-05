using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Alm.Authentication.Test
{
    [TestClass]
    public class BasicAuthTests
    {
        public BasicAuthTests()
        {
            Trace.Listeners.AddRange(Debug.Listeners);
        }

        [TestMethod]
        public void BasicAuthDeleteCredentialsTest()
        {
            TargetUri targetUri = new TargetUri("http://localhost");
            BasicAuthentication basicAuth = GetBasicAuthentication("basic-delete");

            basicAuth.CredentialStore.WriteCredentials(targetUri, new Credential("username", "password"));

            Credential credentials;

            basicAuth.DeleteCredentials(targetUri);

            Assert.IsNull(credentials = basicAuth.CredentialStore.ReadCredentials(targetUri), "User credentials were not deleted as expected");
        }

        [TestMethod]
        public void BasicAuthGetCredentialsTest()
        {
            TargetUri targetUri = new TargetUri("http://localhost");
            BasicAuthentication basicAuth = GetBasicAuthentication("basic-get");

            Credential credentials = null;

            Assert.IsNull(credentials = basicAuth.GetCredentials(targetUri), "User credentials were unexpectedly retrieved.");

            credentials = new Credential("username", "password");

            basicAuth.CredentialStore.WriteCredentials(targetUri, credentials);

            Assert.IsNotNull(credentials = basicAuth.GetCredentials(targetUri), "User credentials were unexpectedly not retrieved.");
        }

        [TestMethod]
        public void BasicAuthSetCredentialsTest()
        {
            TargetUri targetUri = new TargetUri("http://localhost");
            BasicAuthentication basicAuth = GetBasicAuthentication("basic-set");

            Credential credentials = null;

            Assert.IsNull(credentials = basicAuth.GetCredentials(targetUri), "User credentials were unexpectedly retrieved.");
            try
            {
                basicAuth.SetCredentials(targetUri, credentials);
                Assert.Fail("User credentials were unexpectedly set.");
            }
            catch { }

            credentials = new Credential("username", "password");

            basicAuth.SetCredentials(targetUri, credentials);

            Assert.IsNotNull(credentials = basicAuth.GetCredentials(targetUri), "User credentials were unexpectedly not retrieved.");
        }

        private BasicAuthentication GetBasicAuthentication(string @namespace)
        {
            ICredentialStore credentialStore = new SecretCache(@namespace);

            return new BasicAuthentication(credentialStore);
        }
    }
}
