using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Alm.Authentication;
using Xunit;

namespace Atlassian.Bitbucket.Authentication.Test
{
    public class AuthenticationTest
    {
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
            catch (System.AggregateException exception)
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
                catch (System.AggregateException exception)
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
                catch (System.AggregateException exception)
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
                catch (System.AggregateException exception)
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
                catch (System.AggregateException exception)
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
                catch (System.AggregateException exception)
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
            credentialStore.Credentials.Add("https://example.com/", new Credential("john", "squire"));

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
            credentialStore.Credentials.Add("https://example.com/", new Credential("john", "a1b2c3"));
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
            credentialStore.Credentials.Add("https://example.com/", new Credential("john", "squire"));
            // per user credentials
            credentialStore.Credentials.Add("https://john@example.com/", new Credential("john", "squire"));

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
            credentialStore.Credentials.Add("https://john@example.com/", new Credential("john", "squire"));

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
            Assert.Equal(new WebProxy().Address, resultUri.WebProxy.Address);
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
            Assert.Equal(new WebProxy().Address, resultUri.WebProxy.Address);
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
            Assert.Equal(new WebProxy().Address, resultUri.WebProxy.Address);
            Assert.Equal("https://example.com/", resultUri.ToString(false, true, true));
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
