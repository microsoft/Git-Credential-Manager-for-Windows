using System.Diagnostics;
using System.Threading.Tasks;
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
        public async Task BasicAuthDeleteCredentialsTest()
        {
            TargetUri targetUri = new TargetUri("http://localhost");
            BasicAuthentication basicAuth = GetBasicAuthentication("basic-delete");

            Assert.True(await basicAuth.CredentialStore.WriteCredentials(targetUri, new Credential("username", "password")));

            Assert.True(await basicAuth.DeleteCredentials(targetUri));

            Assert.Null(await basicAuth.CredentialStore.ReadCredentials(targetUri));
        }

        [Fact]
        public async Task BasicAuthGetCredentialsTest()
        {
            TargetUri targetUri = new TargetUri("http://localhost");
            BasicAuthentication basicAuth = GetBasicAuthentication("basic-get");

            Credential credentials = null;

            Assert.Null(credentials = await basicAuth.GetCredentials(targetUri));

            credentials = new Credential("username", "password");

            Assert.True(await basicAuth.CredentialStore.WriteCredentials(targetUri, credentials));

            Assert.NotNull(credentials = await basicAuth.GetCredentials(targetUri));
        }

        [Fact]
        public async Task BasicAuthSetCredentialsTest()
        {
            TargetUri targetUri = new TargetUri("http://localhost");
            BasicAuthentication basicAuth = GetBasicAuthentication("basic-set");

            Credential credentials = null;

            Assert.Null(credentials = await basicAuth.GetCredentials(targetUri));
            Assert.Throws<System.ArgumentNullException>(() =>
            {
                try
                {
                    basicAuth.SetCredentials(targetUri, credentials).Wait();
                }
                catch (System.AggregateException exception)
                {
                    throw exception.Flatten().InnerException;
                }
            });

            credentials = new Credential("username", "password");

            Assert.True(await basicAuth.SetCredentials(targetUri, credentials));

            Assert.NotNull(credentials = await basicAuth.GetCredentials(targetUri));
        }

        private BasicAuthentication GetBasicAuthentication(string @namespace)
        {
            ICredentialStore credentialStore = new SecretCache(@namespace);

            return new BasicAuthentication(credentialStore, NtlmSupport.Auto, null, null);
        }
    }
}
