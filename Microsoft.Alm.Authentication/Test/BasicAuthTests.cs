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
        public async Task BasicAuthUserUriDeleteCredentialsTest()
        {
            TargetUri targetUserUri = new TargetUri("http://username@localhost");
            TargetUri targetGenericUri = new TargetUri("http://localhost");
            BasicAuthentication basicAuth = GetBasicAuthentication(RuntimeContext.Default, "basic-delete-user");

            await basicAuth.CredentialStore.WriteCredentials(targetUserUri, new Credential("username", "password"));
            await basicAuth.CredentialStore.WriteCredentials(targetGenericUri, new Credential("username", "password"));

            /* User-included format is what comes out of "erase" action, so that's what we want to test */
            await basicAuth.DeleteCredentials(targetUserUri);

            Assert.Null(await basicAuth.CredentialStore.ReadCredentials(targetUserUri));
            Assert.Null(await basicAuth.CredentialStore.ReadCredentials(targetGenericUri));
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
        public async Task BasicAuthUserUriGetCredentialsTest()
        {
            TargetUri targetUserUri = new TargetUri("http://username@localhost");
            TargetUri targetGenericUri = new TargetUri("http://localhost");

            BasicAuthentication basicAuth = GetBasicAuthentication(RuntimeContext.Default, "basic-get-user");

            Credential credentials = null;

            Assert.Null(credentials = await basicAuth.GetCredentials(targetGenericUri));
            Assert.Null(credentials = await basicAuth.GetCredentials(targetUserUri));

            credentials = new Credential("username", "password");

            await basicAuth.CredentialStore.WriteCredentials(targetGenericUri, credentials);

            Assert.NotNull(credentials = await basicAuth.GetCredentials(targetUserUri));
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
