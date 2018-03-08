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
            BasicAuthentication basicAuth = GetBasicAuthentication(RuntimeContext.Default, "basic-delete");

            await basicAuth.CredentialStore.WriteCredentials(targetUri, new Credential("username", "password"));

            await basicAuth.DeleteCredentials(targetUri);

            Assert.Null(await basicAuth.CredentialStore.ReadCredentials(targetUri));
        }

        [Fact]
        public async Task BasicAuthGetCredentialsTest()
        {
            TargetUri targetUri = new TargetUri("http://localhost");
            BasicAuthentication basicAuth = GetBasicAuthentication(RuntimeContext.Default, "basic-get");

            Credential credentials = null;

            Assert.Null(credentials = await basicAuth.GetCredentials(targetUri));

            credentials = new Credential("username", "password");

            await basicAuth.CredentialStore.WriteCredentials(targetUri, credentials);

            Assert.NotNull(credentials = await basicAuth.GetCredentials(targetUri));
        }

        [Fact]
        public async Task BasicAuthSetCredentialsTest()
        {
            TargetUri targetUri = new TargetUri("http://localhost");
            BasicAuthentication basicAuth = GetBasicAuthentication(RuntimeContext.Default, "basic-set");

            Credential credentials = null;

            Assert.Null(credentials = await basicAuth.GetCredentials(targetUri));
            Assert.Throws<System.ArgumentNullException>(() =>
            {
                try
                {
                    Task.Run(async () => { await basicAuth.SetCredentials(targetUri, credentials); }).Wait();
                }
                catch (System.AggregateException exception)
                {
                    exception = exception.Flatten();
                    throw exception.InnerException;
                }
            });

            credentials = new Credential("username", "password");

            await basicAuth.SetCredentials(targetUri, credentials);

            Assert.NotNull(credentials = await basicAuth.GetCredentials(targetUri));
        }

        private BasicAuthentication GetBasicAuthentication(RuntimeContext context, string @namespace)
        {
            ICredentialStore credentialStore = new SecretCache(context, @namespace);

            return new BasicAuthentication(context, credentialStore, NtlmSupport.Auto, null, null);
        }
    }
}
