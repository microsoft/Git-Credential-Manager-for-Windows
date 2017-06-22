using System.Diagnostics;
using Xunit;

namespace Microsoft.Alm.Authentication.Test
{
    public class BasicAuthTests
    {
        public BasicAuthTests()
        {
            Trace.Listeners.AddRange(Debug.Listeners);
        }

        [Fact]
        public void BasicAuthDeleteCredentialsTest()
        {
            TargetUri targetUri = new TargetUri("http://localhost");
            BasicAuthentication basicAuth = GetBasicAuthentication("basic-delete");

            basicAuth.CredentialStore.WriteCredentials(targetUri, new Credential("username", "password"));

            basicAuth.DeleteCredentials(targetUri);

            Assert.Null(basicAuth.CredentialStore.ReadCredentials(targetUri));
        }

        [Fact]
        public void BasicAuthGetCredentialsTest()
        {
            TargetUri targetUri = new TargetUri("http://localhost");
            BasicAuthentication basicAuth = GetBasicAuthentication("basic-get");

            Credential credentials = null;

            Assert.Null(credentials = basicAuth.GetCredentials(targetUri));

            credentials = new Credential("username", "password");

            basicAuth.CredentialStore.WriteCredentials(targetUri, credentials);

            Assert.NotNull(credentials = basicAuth.GetCredentials(targetUri));
        }

        [Fact]
        public void BasicAuthSetCredentialsTest()
        {
            TargetUri targetUri = new TargetUri("http://localhost");
            BasicAuthentication basicAuth = GetBasicAuthentication("basic-set");

            Credential credentials = null;

            Assert.Null(credentials = basicAuth.GetCredentials(targetUri));
            Assert.Throws<System.ArgumentNullException>(() =>
            {
                basicAuth.SetCredentials(targetUri, credentials);
            });

            credentials = new Credential("username", "password");

            basicAuth.SetCredentials(targetUri, credentials);

            Assert.NotNull(credentials = basicAuth.GetCredentials(targetUri));
        }

        private BasicAuthentication GetBasicAuthentication(string @namespace)
        {
            ICredentialStore credentialStore = new SecretCache(@namespace);

            return new BasicAuthentication(credentialStore, NtlmSupport.Auto, null, null);
        }
    }
}
