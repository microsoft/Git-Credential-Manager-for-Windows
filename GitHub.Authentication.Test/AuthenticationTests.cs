using GitHub.Authentication.Test.Fakes;
using Microsoft.Alm.Authentication;
using System;
using System.Threading.Tasks;
using Xunit;

namespace GitHub.Authentication.Test
{
    public class AuthenticationTests
    {
        [Theory]
        [InlineData("https://github.com/", "https://github.com/")]
        [InlineData("https://gist.github.com/", "https://gist.github.com/")]
        [InlineData("https://github.com/", "https://gist.github.com/")]
        public async Task GetSetCredentialsNormalizesGistUrls(string writeUriString, string retrieveUriString)
        {
            var retrieveUri = new Uri(retrieveUriString);
            var credentialStore = new InMemoryCredentialStore();
            var prompts = new AuthenticationPrompts(RuntimeContext.Default);
            
            var authentication = new Authentication(
                RuntimeContext.Default,
                new Uri(writeUriString),
                TokenScope.Gist,
                credentialStore,
                new Authentication.AcquireCredentialsDelegate(prompts.CredentialModalPrompt),
                new Authentication.AcquireAuthenticationCodeDelegate(prompts.AuthenticationCodeModalPrompt),
                null);

            await authentication.SetCredentials(new Uri(writeUriString), new Credential("haacked"));
            var credentials = await authentication.GetCredentials(retrieveUri);
            Assert.Equal("haacked", credentials.Username);
        }

        [Fact]
        public async Task GetSetCredentialsDoesNotReturnCredentialForRandomUrl()
        {
            var retrieveUri = new Uri("https://example.com/");
            var credentialStore = new InMemoryCredentialStore();
            var prompts = new AuthenticationPrompts(RuntimeContext.Default);

            var authentication = new Authentication(
                RuntimeContext.Default,
                new Uri("https://github.com/"),
                TokenScope.Gist,
                credentialStore,
                new Authentication.AcquireCredentialsDelegate(prompts.CredentialModalPrompt),
                new Authentication.AcquireAuthenticationCodeDelegate(prompts.AuthenticationCodeModalPrompt),
                null);

            await authentication.SetCredentials(new Uri("https://github.com/"), new Credential("haacked"));

            Assert.Null(await authentication.GetCredentials(retrieveUri));
        }
    }
}
