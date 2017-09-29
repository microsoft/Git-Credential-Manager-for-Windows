using GitHub.Authentication.Test.Fakes;
using Microsoft.Alm.Authentication;
using System;
using Xunit;

namespace GitHub.Authentication.Test
{
    public class AuthenticationTests
    {
        [Theory]
        [InlineData("https://github.com/", "https://github.com/")]
        [InlineData("https://gist.github.com/", "https://gist.github.com/")]
        [InlineData("https://github.com/", "https://gist.github.com/")]
        [InlineData("https://gist.github.com/", "https://gist.github.com/")]
        public void GetSetCredentialsNormalizesGistUrls(string writeUriString, string retrieveUriString)
        {
            var retrieveUri = new Uri(retrieveUriString);
            var credentialStore = new InMemoryCredentialStore();
            
            var authentication = new Authentication(
                new Uri(writeUriString),
                TokenScope.Gist,
                credentialStore,
                new Authentication.AcquireCredentialsDelegate(AuthenticationPrompts.CredentialModalPrompt),
                new Authentication.AcquireAuthenticationCodeDelegate(AuthenticationPrompts.AuthenticationCodeModalPrompt),
                null);

            authentication.SetCredentials(new Uri(writeUriString), new Credential("haacked"));
            Assert.Equal("haacked", authentication.GetCredentials(retrieveUri).Username);
        }

        [Fact]
        public void GetSetCredentialsDoesNotReturnCredentialForRandomUrl()
        {
            var retrieveUri = new Uri("https://example.com/");
            var credentialStore = new InMemoryCredentialStore();

            var authentication = new Authentication(
                new Uri("https://github.com/"),
                TokenScope.Gist,
                credentialStore,
                new Authentication.AcquireCredentialsDelegate(AuthenticationPrompts.CredentialModalPrompt),
                new Authentication.AcquireAuthenticationCodeDelegate(AuthenticationPrompts.AuthenticationCodeModalPrompt),
                null);

            authentication.SetCredentials(new Uri("https://github.com/"), new Credential("haacked"));

            Assert.Null(authentication.GetCredentials(retrieveUri));
        }
    }
}
