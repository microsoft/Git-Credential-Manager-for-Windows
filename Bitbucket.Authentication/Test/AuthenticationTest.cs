using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Alm.Authentication;
using Moq;
using Xunit;

namespace Atlassian.Bitbucket.Authentication.Test
{
    public class AuthenticationTest
    {
        /// <summary>
        ///  used to populate delegates;
        /// </summary>
        private const string _validUsername = "john";
        private const string _validPassword = "squire";
        private Credential _validBasicAuthCredentials = new Credential(_validUsername, _validPassword);

        private const string _invalidUsername = "invalid_username";
        private const string _invalidPassword = "invalid_password";
        private Credential _invalidBasicAuthCredentials = new Credential(_invalidUsername, _invalidPassword);


        [Fact]
        public void VerifyBitbucketOrgIsIdentified()
        {
            var targetUri = new TargetUri("https://bitbucket.org");
            var bbAuth = Authentication.GetAuthentication(RuntimeContext.Default, targetUri, new MockCredentialStore(), null, null);

            Assert.NotNull(bbAuth);
        }

        [Fact]
        public void VerifyNonBitbucketOrgIsIgnored()
        {
            var targetUri = new TargetUri("https://example.com");
            var bbAuth = Authentication.GetAuthentication(RuntimeContext.Default, targetUri, new MockCredentialStore(), null, null);

            Assert.Null(bbAuth);
        }

        [Fact]
        public async Task VerifySetCredentialStoresValidCredentials()
        {
            var targetUri = new TargetUri("https://example.com");
            var credentialStore = new MockCredentialStore();
            var credentials = new Credential("a", "b");
            var bbAuth = new Authentication(RuntimeContext.Default, credentialStore, null, null);

            try
            {
                await bbAuth.SetCredentials(targetUri, credentials);

                var writeCalls = credentialStore.MethodCalls
                    .Where(mc => mc.Key.Equals("WriteCredentials"))
                        .SelectMany(mc => mc.Value)
                            .Where(wc => wc.Key.Contains(targetUri.ToString())
                                && wc.Key.Contains(credentials.Username)
                                && wc.Key.Contains(credentials.Password));

                Assert.Single(writeCalls);
            }
            catch (AggregateException exception)
            {
                exception = exception.Flatten();
                throw exception.InnerException;
            }
        }

        [Fact]
        public void VerifySetCredentialDoesNotStoreForNullTargetUri()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                try
                {
                    Task.Run(async () =>
                    {
                        var credentialStore = new MockCredentialStore();
                        var credentials = new Credential("a", "b");
                        var bbAuth = new Authentication(RuntimeContext.Default, credentialStore, null, null);

                        await bbAuth.SetCredentials(null, credentials);
                    }).Wait();
                }
                catch (AggregateException exception)
                {
                    exception = exception.Flatten();
                    throw exception.InnerException;
                }
            });
        }

        [Fact]
        public void VerifySetCredentialDoesNotStoresForNullCredentials()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                try
                {
                    Task.Run(async () =>
                    {
                        var targetUri = new TargetUri("https://example.com");
                        var credentialStore = new MockCredentialStore();
                        var bbAuth = new Authentication(RuntimeContext.Default, credentialStore, null, null);

                        await bbAuth.SetCredentials(targetUri, null);
                    }).Wait();
                }
                catch (AggregateException exception)
                {
                    exception = exception.Flatten();
                    throw exception.InnerException;
                }
            });
        }

        [Fact]
        public void VerifySetCredentialDoesNotStoreForTooLongPassword()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                try
                {
                    Task.Run(async () =>
                    {
                        var targetUri = new TargetUri("https://example.com");
                        var credentialStore = new MockCredentialStore();
                        var credentials = new Credential("a", new string('x', 2047 + 1));
                        var bbAuth = new Authentication(RuntimeContext.Default, credentialStore, null, null);

                        await bbAuth.SetCredentials(targetUri, credentials);
                    }).Wait();
                }
                catch (AggregateException exception)
                {
                    exception = exception.Flatten();
                    throw exception.InnerException;
                }
            });
        }

        [Fact]
        public void VerifySetCredentialDoesNotStoreForTooLongUsername()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                try
                {
                    Task.Run(async () =>
                    {
                        var targetUri = new TargetUri("https://example.com");
                        var credentialStore = new MockCredentialStore();
                        var credentials = new Credential(new string('x', 2047 + 1), "b");
                        var bbAuth = new Authentication(RuntimeContext.Default, credentialStore, null, null);

                        await bbAuth.SetCredentials(targetUri, credentials);
                    }).Wait();
                }
                catch (AggregateException exception)
                {
                    exception = exception.Flatten();
                    throw exception.InnerException;
                }
            });
        }

        [Fact]
        public void VerifyDeleteCredentialDoesNotDeleteForNullTargetUri()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                try
                {
                    Task.Run(async () =>
                    {
                        var credentialStore = new MockCredentialStore();
                        var bbAuth = new Authentication(RuntimeContext.Default, credentialStore, null, null);

                        await bbAuth.DeleteCredentials(null);

                        var deleteCalls = credentialStore.MethodCalls
                            .Where(mc => mc.Key.Equals("DeleteCredentials"))
                                .SelectMany(mc => mc.Value);

                        Assert.Empty(deleteCalls);
                    }).Wait();
                }
                catch (AggregateException exception)
                {
                    exception = exception.Flatten();
                    throw exception.InnerException;
                }
            });
        }

        [Fact]
        public void VerifyDeleteCredentialForBasicAuthReadsTwiceDeletesOnce()
        {
            var credentialStore = new MockCredentialStore();
            // Add a stored basic authentication credential to delete.
            credentialStore.Credentials.Add("https://example.com/", _validBasicAuthCredentials);

            var bbAuth = new Authentication(RuntimeContext.Default, credentialStore, null, null);

            var targetUri = new TargetUri("https://example.com/");
            bbAuth.DeleteCredentials(targetUri);

            var readCalls = credentialStore.MethodCalls
                .Where(mc => mc.Key.Equals("ReadCredentials"))
                    .SelectMany(mc => mc.Value);

            // 2 read calls, 1 for the basic uri and 1 for /refresh_token
            Assert.Equal(2, readCalls.Count());
            Assert.Contains(readCalls, rc => rc.Key[0].Equals("https://example.com/"));
            Assert.Contains(readCalls, rc => rc.Key[0].Equals("https://example.com/refresh_token"));

            var deleteCalls = credentialStore.MethodCalls
                .Where(mc => mc.Key.Equals("DeleteCredentials"))
                    .SelectMany(mc => mc.Value);

            // 1 delete call, 1 for the basic uri 0 for /refresh_token as there isn't one
            Assert.Single(deleteCalls);
            Assert.Contains(deleteCalls, rc => rc.Key[0].Equals("https://example.com/"));
            Assert.DoesNotContain(deleteCalls, rc => rc.Key[0].Equals("https://example.com/refresh_token"));
        }

        [Fact]
        public void VerifyDeleteCredentialForOAuthReadsTwiceDeletesTwice()
        {
            var credentialStore = new MockCredentialStore();
            // add a stored basic auth credential to delete.
            credentialStore.Credentials.Add("https://example.com/", _validBasicAuthCredentials);
            credentialStore.Credentials.Add("https://example.com/refresh_token", new Credential("john", "d4e5f6"));

            var bbAuth = new Authentication(RuntimeContext.Default, credentialStore, null, null);

            var targetUri = new TargetUri("https://example.com/");
            bbAuth.DeleteCredentials(targetUri);

            var readCalls = credentialStore.MethodCalls
                .Where(mc => mc.Key.Equals("ReadCredentials"))
                    .SelectMany(mc => mc.Value);

            // 2 read calls, 1 for the basic uri and 1 for /refresh_token
            Assert.Equal(2, readCalls.Count());
            Assert.Contains(readCalls, rc => rc.Key[0].Equals("https://example.com/"));
            Assert.Contains(readCalls, rc => rc.Key[0].Equals("https://example.com/refresh_token"));

            var deleteCalls = credentialStore.MethodCalls
                .Where(mc => mc.Key.Equals("DeleteCredentials"))
                    .SelectMany(mc => mc.Value);

            // 2 delete call, 1 for the basic uri, 1 for /refresh_token as there is one
            Assert.Equal(2, deleteCalls.Count());
            Assert.Contains(deleteCalls, rc => rc.Key[0].Equals("https://example.com/"));
            Assert.Contains(deleteCalls, rc => rc.Key[0].Equals("https://example.com/refresh_token"));
        }

        [Fact]
        public void VerifyDeleteCredentialForBasicAuthReadsQuinceDeletesTwiceIfHostCredentialsExistAndShareUsername()
        {
            var credentialStore = new MockCredentialStore();
            // add a stored basic auth credential to delete.
            // per host credentials
            credentialStore.Credentials.Add("https://example.com/", _validBasicAuthCredentials);
            // per user credentials
            credentialStore.Credentials.Add("https://john@example.com/", _validBasicAuthCredentials);

            var bbAuth = new Authentication(RuntimeContext.Default, credentialStore, null, null);

            var targetUri = new TargetUri("https://john@example.com/");
            bbAuth.DeleteCredentials(targetUri);

            var readCalls = credentialStore.MethodCalls
                .Where(mc => mc.Key.Equals("ReadCredentials"))
                    .SelectMany(mc => mc.Value);

            // 5 read calls
            // 1 for the basic uri with username
            // 1 for /refresh_token with username
            // 1 for the basic uri to compare username
            // 1 for the basic uri without username
            // 1 for /refresh_token without username
            Assert.Equal(5, readCalls.Count());
            Assert.Equal(1, readCalls.Count(rc => rc.Key[0].Equals("https://john@example.com/")));
            Assert.Equal(1, readCalls.Count(rc => rc.Key[0].Equals("https://john@example.com/refresh_token")));
            Assert.Equal(2, readCalls.Count(rc => rc.Key[0].Equals("https://example.com/")));
            Assert.Equal(1, readCalls.Count(rc => rc.Key[0].Equals("https://example.com/refresh_token")));

            var deleteCalls = credentialStore.MethodCalls
                .Where(mc => mc.Key.Equals("DeleteCredentials"))
                    .SelectMany(mc => mc.Value);

            // 2 delete calls
            // 1 for the basic uri with username
            // 1 for the basic uri without username
            Assert.Equal(2, deleteCalls.Count());
            Assert.Equal(1, deleteCalls.Count(rc => rc.Key[0].Equals("https://john@example.com/")));
            Assert.DoesNotContain(deleteCalls, rc => rc.Key[0].Equals("https://example.com/refresh_token"));
            Assert.Equal(1, deleteCalls.Count(rc => rc.Key[0].Equals("https://example.com/")));
        }

        [Fact]
        public void VerifyDeleteCredentialForBasicAuthReadsThriceDeletesOnceIfHostCredentialsExistAndDoNotShareUsername()
        {
            var credentialStore = new MockCredentialStore();
            // add a stored basic auth credential to delete.
            // per host credentials
            credentialStore.Credentials.Add("https://example.com/", new Credential("ian", "brown"));
            // per user credentials
            credentialStore.Credentials.Add("https://john@example.com/", _validBasicAuthCredentials);

            var bbAuth = new Authentication(RuntimeContext.Default, credentialStore, null, null);

            var targetUri = new TargetUri("https://john@example.com/");
            bbAuth.DeleteCredentials(targetUri);

            var readCalls = credentialStore.MethodCalls
                .Where(mc => mc.Key.Equals("ReadCredentials"))
                    .SelectMany(mc => mc.Value);

            // 5 read calls
            // 1 for the basic uri with username
            // 1 for /refresh_token with username
            // 1 for the basic uri to compare username
            Assert.Equal(3, readCalls.Count());
            Assert.Equal(1, readCalls.Count(rc => rc.Key[0].Equals("https://john@example.com/")));
            Assert.Equal(1, readCalls.Count(rc => rc.Key[0].Equals("https://john@example.com/refresh_token")));
            Assert.Equal(1, readCalls.Count(rc => rc.Key[0].Equals("https://example.com/")));

            var deleteCalls = credentialStore.MethodCalls
                .Where(mc => mc.Key.Equals("DeleteCredentials"))
                    .SelectMany(mc => mc.Value);

            // 1 delete calls
            // 1 for the basic uri with username
            // DOES NOT delete the Host credentials because they are for a different username.
            Assert.Single(deleteCalls);
            Assert.Equal(1, deleteCalls.Count(rc => rc.Key[0].Equals("https://john@example.com/")));
            Assert.DoesNotContain(deleteCalls, rc => rc.Key[0].Equals("https://example.com/refresh_token"));
            Assert.DoesNotContain(deleteCalls, rc => rc.Key[0].Equals("https://example.com/"));
        }

        [Fact]
        public void VerifyGetPerUserTargetUriInsertsMissingUsernameToActualUri()
        {
            var credentialStore = new MockCredentialStore();
            var bbAuth = new Authentication(RuntimeContext.Default, credentialStore, null, null);

            var targetUri = new TargetUri("https://example.com");
            var username = "johnsquire";

            var resultUri = targetUri.GetPerUserTargetUri(username);

            Assert.Equal("/", resultUri.AbsolutePath);
            Assert.Equal("https://johnsquire@example.com/", resultUri.QueryUri.AbsoluteUri);
            Assert.Equal("example.com", resultUri.DnsSafeHost);
            Assert.Equal("example.com", resultUri.Host);
            Assert.True(resultUri.IsAbsoluteUri);
            Assert.True(resultUri.IsDefaultPort);
            Assert.Equal(443, resultUri.Port);
            Assert.Null(resultUri.ProxyUri);
            Assert.Equal("https://johnsquire@example.com/", resultUri.QueryUri.AbsoluteUri);
            Assert.Equal("https", resultUri.Scheme);
            Assert.Equal("https://example.com/", resultUri.ToString(false, true, true));
        }

        [Fact]
        public void VerifyGetPerUserTargetUriFormatsEmailUsernames()
        {
            var credentialStore = new MockCredentialStore();
            var bbAuth = new Authentication(RuntimeContext.Default, credentialStore, null, null);

            var targetUri = new TargetUri("https://example.com");
            var username = "johnsquire@stoneroses.com";

            var resultUri = targetUri.GetPerUserTargetUri(username);

            Assert.Equal("/", resultUri.AbsolutePath);
            Assert.Equal("https://johnsquire%40stoneroses.com@example.com/", resultUri.QueryUri.AbsoluteUri);
            Assert.Equal("example.com", resultUri.DnsSafeHost);
            Assert.Equal("example.com", resultUri.Host);
            Assert.True(resultUri.IsAbsoluteUri);
            Assert.True(resultUri.IsDefaultPort);
            Assert.Equal(443, resultUri.Port);
            Assert.Null(resultUri.ProxyUri);
            Assert.Equal("https://johnsquire%40stoneroses.com@example.com/", resultUri.QueryUri.AbsoluteUri);
            Assert.Equal("https", resultUri.Scheme);
            Assert.Equal("https://example.com/", resultUri.ToString(false, true, true));
        }
        [Fact]
        public void VerifyGetPerUserTargetUriDoesNotDuplicateUsernameOnActualUri()
        {
            var credentialStore = new MockCredentialStore();
            var bbAuth = new Authentication(RuntimeContext.Default, credentialStore, null, null);

            var targetUri = new TargetUri("https://johnsquire@example.com");
            var username = "johnsquire";

            var resultUri = targetUri.GetPerUserTargetUri(username);

            Assert.Equal("/", resultUri.AbsolutePath);
            Assert.Equal("https://johnsquire@example.com/", resultUri.QueryUri.AbsoluteUri);
            Assert.Equal("example.com", resultUri.DnsSafeHost);
            Assert.Equal("example.com", resultUri.Host);
            Assert.True(resultUri.IsAbsoluteUri);
            Assert.True(resultUri.IsDefaultPort);
            Assert.Equal(443, resultUri.Port);
            Assert.Null(resultUri.ProxyUri);
            Assert.Equal("https://johnsquire@example.com/", resultUri.QueryUri.AbsoluteUri);
            Assert.Equal("https", resultUri.Scheme);
            Assert.Equal("https://example.com/", resultUri.ToString(false, true, true));
        }

        [Fact]
        public async void VerifyInteractiveLoginAquiresAndStoresValidBasicAuthCredentials()
        {
            var bitbucketUrl = "https://bitbucket.org";
            var credentialStore = new Mock<ICredentialStore>();

            var targetUri = new TargetUri(bitbucketUrl);

            // Mock the behaviour of IAuthority.AcquireToken() to basically mimic BasicAuthAuthenticator.GetAuthAsync() validating the useername/password
            var authority = new Mock<IAuthority>();
            authority
                .Setup(a => a.AcquireToken(It.IsAny<TargetUri>(), It.IsAny<Credential>(), It.IsAny<AuthenticationResultType>(), It.IsAny<TokenScope>()))
                // return 'success' with the validated credentials
                .Returns(Task.FromResult(new AuthenticationResult(AuthenticationResultType.Success, new Token(_validBasicAuthCredentials.Password, TokenType.Personal))));

            var bbAuth = new Authentication(RuntimeContext.Default, credentialStore.Object, MockValidBasicAuthCredentialsAquireCredentialsCallback, MockValidAquireAuthenticationOAuthCallback, authority.Object);

            var credentials = await bbAuth.InteractiveLogon(targetUri);

            Assert.NotNull(credentials);

            // attempted to validate credentials
            authority.Verify(a => a.AcquireToken(targetUri, _validBasicAuthCredentials, AuthenticationResultType.None, TokenScope.SnippetWrite | TokenScope.RepositoryWrite), Times.Once);
            // valid credentials stored
            credentialStore.Verify(c => c.WriteCredentials(targetUri, _validBasicAuthCredentials), Times.Once);

        }

        [Fact]
        public async void VerifyInteractiveLoginDoesNotAquireInvalidBasicAuthCredentials()
        {
            var bitbucketUrl = "https://bitbucket.org";
            var credentialStore = new Mock<ICredentialStore>();

            var targetUri = new TargetUri(bitbucketUrl);

            // Mock the behaviour of IAuthority.AcquireToken() to basically mimic BasicAuthAuthenticator.GetAuthAsync() validating the useername/password
            var authority = new Mock<IAuthority>();
            authority
                .Setup(a => a.AcquireToken(It.IsAny<TargetUri>(), It.IsAny<Credential>(), It.IsAny<AuthenticationResultType>(), It.IsAny<TokenScope>()))
                // return 'failure' with the validated credentials
                .Returns(Task.FromResult(new AuthenticationResult(AuthenticationResultType.Failure)));

            var bbAuth = new Authentication(RuntimeContext.Default, credentialStore.Object, MockInvalidBasicAuthCredentialsAquireCredentialsCallback, MockValidAquireAuthenticationOAuthCallback, authority.Object);

            var credentials = await bbAuth.InteractiveLogon(targetUri);

            Assert.NotNull(credentials);
            Assert.Equal(_invalidUsername, credentials.Username);
            Assert.Equal(_invalidPassword, credentials.Password);

            // attempted to validate credentials
            authority.Verify(a => a.AcquireToken(It.IsAny<TargetUri>(), It.IsAny<Credential>(), It.IsAny<AuthenticationResultType>(), It.IsAny<TokenScope>()), Times.Once);
            // no attempt to store invalid credentials
            credentialStore.Verify(c => c.WriteCredentials(It.IsAny<TargetUri>(), It.IsAny<Credential>()), Times.Never);
        }

        [Fact]
        public async void VerifyInteractiveLoginDoesNotAquireInvalidBasicAuthCredentialsWithUsername()
        {
            var bitbucketUrl = "https://bitbucket.org";
            var credentialStore = new Mock<ICredentialStore>();

            // mock the result that normally causes issues
            var validAuthenticationResult = new AuthenticationResult(AuthenticationResultType.Success)
            {
                Token = new Token(_validPassword, TokenType.Personal),
                RemoteUsername = _validUsername
            };

            var targetUri = new TargetUri(bitbucketUrl);

            // Mock the behaviour of IAuthority.AcquireToken() to basically mimic BasicAuthAuthenticator.GetAuthAsync() validating the useername/password
            var authority = new Mock<IAuthority>();
            authority
                .Setup(a => a.AcquireToken(It.IsAny<TargetUri>(), It.IsAny<Credential>(), It.IsAny<AuthenticationResultType>(), It.IsAny<TokenScope>()))
                // return 'success' with the validated credentials
                .Returns(Task.FromResult(validAuthenticationResult));

            var bbAuth = new Authentication(RuntimeContext.Default, credentialStore.Object, 
                MockInvalidBasicAuthCredentialsAquireCredentialsCallback, MockValidAquireAuthenticationOAuthCallback, authority.Object);

            // perform login with username
            var credentials = await bbAuth.InteractiveLogon(targetUri, _validUsername);

            Assert.NotNull(credentials);
            Assert.Equal(_validUsername, credentials.Username);
            Assert.Equal(_validPassword, credentials.Password);

            // attempted to validate credentials
            authority.Verify(a => a.AcquireToken(It.IsAny<TargetUri>(), It.IsAny<Credential>(), It.IsAny<AuthenticationResultType>(),
                It.IsAny<TokenScope>()), Times.Once);
           
            // must have a valid attempt to store the valid credentials
            credentialStore.Verify(c => c.WriteCredentials(It.IsAny<TargetUri>(), credentials), Times.Once);
        }

        [Fact]
        public async void VerifyInteractiveLoginDoesNothingIfUserDoesNotEnterCredentials()
        {
            var bitbucketUrl = "https://bitbucket.org";
            var credentialStore = new Mock<ICredentialStore>();

            var targetUri = new TargetUri(bitbucketUrl);

            // Mock the behaviour of IAuthority.AcquireToken() to basically mimic BasicAuthAuthenticator.GetAuthAsync() validating the useername/password
            var authority = new Mock<IAuthority>();
            authority
                .Setup(a => a.AcquireToken(It.IsAny<TargetUri>(), It.IsAny<Credential>(), It.IsAny<AuthenticationResultType>(), It.IsAny<TokenScope>()))
                // return 'failure' with the validated credentials
                .Returns(Task.FromResult(new AuthenticationResult(AuthenticationResultType.Failure)));

            var bbAuth = new Authentication(RuntimeContext.Default, credentialStore.Object, MockNoCredentialsAquireCredentialsCallback, MockValidAquireAuthenticationOAuthCallback, authority.Object);

            var credentials = await bbAuth.InteractiveLogon(targetUri);

            Assert.Null(credentials);

            // no attempted to validate credentials
            authority.Verify(a => a.AcquireToken(It.IsAny<TargetUri>(), It.IsAny<Credential>(), It.IsAny<AuthenticationResultType>(), It.IsAny<TokenScope>()), Times.Never);
            // no attempt to store invalid credentials
            credentialStore.Verify(c => c.WriteCredentials(It.IsAny<TargetUri>(), It.IsAny<Credential>()), Times.Never);
        }

        [Fact]
        public async void VerifyInteractiveLoginAquiresAndStoresValidOAuthCredentials()
        {
            var bitbucketUrl = "https://bitbucket.org";
            var credentialStore = new Mock<ICredentialStore>();

            var targetUri = new TargetUri(bitbucketUrl);

            // Mock the behaviour of IAuthority.AcquireToken() to basically mimic BasicAuthAuthenticator.GetAuthAsync() validating the useername/password
            var authority = new Mock<IAuthority>();
            authority
                .Setup(a => a.AcquireToken(It.IsAny<TargetUri>(), It.IsAny<Credential>(), AuthenticationResultType.None, It.IsAny<TokenScope>()))
                // return 'twofactor' with the validated credentials to indicate 2FAOAuth
                .Returns(Task.FromResult(new AuthenticationResult(AuthenticationResultType.TwoFactor)));
            authority
                .Setup(a => a.AcquireToken(It.IsAny<TargetUri>(), It.IsAny<Credential>(), AuthenticationResultType.TwoFactor, It.IsAny<TokenScope>()))
                // return 'twofactor' with the validated credentials to indicate 2FA/OAuth
                .Returns(Task.FromResult(new AuthenticationResult(AuthenticationResultType.Success, new Token("access_token", TokenType.Personal), new Token("refresh_token", TokenType.BitbucketRefresh))));

            var bbAuth = new Authentication(RuntimeContext.Default, credentialStore.Object, MockValidBasicAuthCredentialsAquireCredentialsCallback, MockValidAquireAuthenticationOAuthCallback, authority.Object);

            var credentials = await bbAuth.InteractiveLogon(targetUri);

            Assert.NotNull(credentials);

            // attempted to validate credentials
            authority.Verify(a => a.AcquireToken(targetUri, _validBasicAuthCredentials, AuthenticationResultType.None, TokenScope.SnippetWrite | TokenScope.RepositoryWrite), Times.Once);
            authority.Verify(a => a.AcquireToken(targetUri, _validBasicAuthCredentials, AuthenticationResultType.TwoFactor, TokenScope.SnippetWrite | TokenScope.RepositoryWrite), Times.Once);
            // valid access token + refresh token stored for the per user and per host urls so 2 x 2 calls
            credentialStore.Verify(c => c.WriteCredentials(It.IsAny<TargetUri>(), It.IsAny<Credential>()), Times.Exactly(4));

        }

        private bool MockValidAquireAuthenticationOAuthCallback(string title, TargetUri targetUri, AuthenticationResultType resultType, string username)
        {
            return true;
        }

        private bool MockValidBasicAuthCredentialsAquireCredentialsCallback(string titleMessage, TargetUri targetUri, out string username, out string password)
        {
            username = _validBasicAuthCredentials.Username;
            password = _validBasicAuthCredentials.Password;
            return true;
        }

        private bool MockInvalidBasicAuthCredentialsAquireCredentialsCallback(string titleMessage, TargetUri targetUri, out string username, out string password)
        {
            username = _invalidUsername;
            password = _invalidPassword;
            return true;
        }

        private bool MockNoCredentialsAquireCredentialsCallback(string titleMessage, TargetUri targetUri, out string username, out string password)
        {
            username = null;
            password = null;
            return false;
        }
    }

    public class MockCredentialStore : ICredentialStore 
    {
        public Dictionary<string, Dictionary<List<string>, int>> MethodCalls =
            new Dictionary<string, Dictionary<List<string>, int>>();

        public Dictionary<string, Credential> Credentials = new Dictionary<string, Credential>();

        public string Namespace
        {
            get { throw new NotImplementedException(); }
        }

        public Secret.UriNameConversionDelegate UriNameConversion
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public Task<bool> DeleteCredentials(TargetUri targetUri)
        {
            // do nothing
            RecordMethodCall("DeleteCredentials", new List<string>() { targetUri.QueryUri.AbsoluteUri });
            return Task.FromResult(true);
        }

        public Task<Credential> ReadCredentials(TargetUri targetUri)
        {
            // do nothing
            RecordMethodCall("ReadCredentials", new List<string>() { targetUri.QueryUri.AbsoluteUri });
            var credentials = (Credentials != null && Credentials.Keys.Contains(targetUri.QueryUri.AbsoluteUri))
                ? Credentials[targetUri.QueryUri.AbsoluteUri]
                : null;

            return Task.FromResult(credentials);
        }

        public Task<bool> WriteCredentials(TargetUri targetUri, Credential credentials)
        {
            // do nothing
            RecordMethodCall("WriteCredentials", new List<string>() { targetUri.QueryUri.AbsoluteUri, credentials.Username, credentials.Password });
            return Task.FromResult(true);
        }

        private void RecordMethodCall(string methodName, List<string> args)
        {
            if (!MethodCalls.ContainsKey(methodName))
            {
                MethodCalls[methodName] = new Dictionary<List<string>, int>()
                {
                    {
                        args,
                        1
                    }
                };
            }
            else if (!MethodCalls[methodName].ContainsKey(args))
            {
                MethodCalls[methodName][args] = 1;
            }
            else
            {
                MethodCalls[methodName][args] = MethodCalls[methodName][args] + 1;
            }
        }
    }
}
