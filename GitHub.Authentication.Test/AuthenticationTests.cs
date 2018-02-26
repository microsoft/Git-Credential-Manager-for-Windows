using GitHub.Authentication.Test.Fakes;
using Microsoft.Alm.Authentication;
using System;
using System.Threading.Tasks;
using Xunit;

using Git = Microsoft.Alm.Authentication.Git;

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
            var retrieveUri = new TargetUri(retrieveUriString);
            var credentialStore = new InMemoryCredentialStore();
            var authenticationPrompts = new AuthenticationPrompts(RuntimeContext.Default);
            
            var authentication = new Authentication(
                RuntimeContext.Default,
                new TargetUri(writeUriString),
                TokenScope.Gist,
                credentialStore,
                new Authentication.AcquireCredentialsDelegate(authenticationPrompts.CredentialModalPrompt),
                new Authentication.AcquireAuthenticationCodeDelegate(authenticationPrompts.AuthenticationCodeModalPrompt),
                null);

            Assert.True(await authentication.SetCredentials(new TargetUri(writeUriString), new Credential("haacked")));

            var credentials = await authentication.GetCredentials(retrieveUri);
            Assert.NotNull(credentials);

            Assert.Equal("haacked", credentials.Username, StringComparer.Ordinal);
        }

        [Fact]
        public async Task GetSetCredentialsDoesNotReturnCredentialForRandomUrl()
        {
            var retrieveUri = new TargetUri("https://example.com/");
            var credentialStore = new InMemoryCredentialStore();
            var authenticationPrompts = new AuthenticationPrompts(RuntimeContext.Default);

            var authentication = new Authentication(
                RuntimeContext.Default,
                new TargetUri("https://github.com/"),
                TokenScope.Gist,
                credentialStore,
                new Authentication.AcquireCredentialsDelegate(authenticationPrompts.CredentialModalPrompt),
                new Authentication.AcquireAuthenticationCodeDelegate(authenticationPrompts.AuthenticationCodeModalPrompt),
                null);

            Assert.True(await authentication.SetCredentials(new TargetUri("https://github.com/"), new Credential("haacked")));

            Assert.Null(await authentication.GetCredentials(retrieveUri));
        }
    }
}
