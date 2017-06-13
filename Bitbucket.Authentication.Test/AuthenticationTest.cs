using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Alm.Authentication;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net;

namespace Atlassian.Bitbucket.Authentication.Test
{
    [TestClass]
    public class AuthenticationTest
    {
        [TestMethod]
        public void VerifyBitbucketOrgIsIdentified()
        {
            var targetUri = new TargetUri("https://bitbucket.org");
            var bbAuth = Authentication.GetAuthentication(targetUri, new MockCredentialStore(), null, null);

            Assert.IsNotNull(bbAuth);
        }

        [TestMethod]
        public void VerifyNonBitbucketOrgIsIgnored()
        {
            var targetUri = new TargetUri("https://example.com");
            var bbAuth = Authentication.GetAuthentication(targetUri, new MockCredentialStore(), null, null);

            Assert.IsNull(bbAuth);
        }

        [TestMethod]
        public void VerifySetCredentialStoresValidCredentials()
        {
            var targetUri = new TargetUri("https://example.com");
            var credentialStore = new MockCredentialStore();
            var credentials = new Credential("a", "b");
            var bbAuth = new Authentication(credentialStore, null, null);

            bbAuth.SetCredentials(targetUri, credentials);

            var writeCalls = credentialStore.MethodCalls
                .Where(mc => mc.Key.Equals("WriteCredentials"))
                    .SelectMany(mc => mc.Value)
                        .Where(wc => wc.Key.Contains(targetUri.ToString())
                            && wc.Key.Contains(credentials.Username)
                            && wc.Key.Contains(credentials.Password));

            Assert.AreEqual(writeCalls.Count(), 1);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void VerifySetCredentialDoesNotStoreForNullTargetUri()
        {
            var credentialStore = new MockCredentialStore();
            var credentials = new Credential("a", "b");
            var bbAuth = new Authentication(credentialStore, null, null);

            bbAuth.SetCredentials(null, credentials);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void VerifySetCredentialDoesNotStoresForNullCredentials()
        {
            var targetUri = new TargetUri("https://example.com");
            var credentialStore = new MockCredentialStore();
            var bbAuth = new Authentication(credentialStore, null, null);

            bbAuth.SetCredentials(targetUri, null);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void VerifySetCredentialDoesNotStoreForTooLongPassword()
        {
            var targetUri = new TargetUri("https://example.com");
            var credentialStore = new MockCredentialStore();
            var credentials = new Credential("a", new string('x', 2047 + 1));
            var bbAuth = new Authentication(credentialStore, null, null);

            bbAuth.SetCredentials(targetUri, credentials);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void VerifySetCredentialDoesNotStoreForTooLongUsername()
        {
            var targetUri = new TargetUri("https://example.com");
            var credentialStore = new MockCredentialStore();
            var credentials = new Credential(new string('x', 2047 + 1), "b");
            var bbAuth = new Authentication(credentialStore, null, null);

            bbAuth.SetCredentials(targetUri, credentials);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void VerifyDeleteCredentialDoesNotDeleteForNullTargetUri()
        {
            var credentialStore = new MockCredentialStore();
            var bbAuth = new Authentication(credentialStore, null, null);

            bbAuth.DeleteCredentials(null);

            var deleteCalls = credentialStore.MethodCalls
                .Where(mc => mc.Key.Equals("DeleteCredentials"))
                    .SelectMany(mc => mc.Value);

            Assert.AreEqual(deleteCalls.Count(), 0);
        }

        [TestMethod]
        public void VerifyDeleteCredentialForBasicAuthReadsTwiceDeletesOnce()
        {
            var credentialStore = new MockCredentialStore();
            // add a stored basic auth credential to delete.
            credentialStore.Credentials.Add("https://example.com/", new Credential("john", "squire"));

            var bbAuth = new Authentication(credentialStore, null, null);

            var targetUri = new TargetUri("https://example.com/");
            bbAuth.DeleteCredentials(targetUri);

            var readCalls = credentialStore.MethodCalls
                .Where(mc => mc.Key.Equals("ReadCredentials"))
                    .SelectMany(mc => mc.Value);

            // 2 read calls, 1 for the basic uri and 1 for /refresh_token
            Assert.AreEqual(2, readCalls.Count());
            Assert.IsTrue(readCalls.Any(rc => rc.Key[0].Equals("https://example.com/")));
            Assert.IsTrue(readCalls.Any(rc => rc.Key[0].Equals("https://example.com/refresh_token")));

            var deleteCalls = credentialStore.MethodCalls
                .Where(mc => mc.Key.Equals("DeleteCredentials"))
                    .SelectMany(mc => mc.Value);

            // 1 delete call, 1 for the basic uri 0 for /refresh_token as there isn't one
            Assert.AreEqual(1, deleteCalls.Count());
            Assert.IsTrue(deleteCalls.Any(rc => rc.Key[0].Equals("https://example.com/")));
            Assert.IsFalse(deleteCalls.Any(rc => rc.Key[0].Equals("https://example.com/refresh_token")));
        }

        [TestMethod]
        public void VerifyDeleteCredentialForOAuthReadsTwiceDeletesTwice()
        {
            var credentialStore = new MockCredentialStore();
            // add a stored basic auth credential to delete.
            credentialStore.Credentials.Add("https://example.com/", new Credential("john", "a1b2c3"));
            credentialStore.Credentials.Add("https://example.com/refresh_token", new Credential("john", "d4e5f6"));

            var bbAuth = new Authentication(credentialStore, null, null);

            var targetUri = new TargetUri("https://example.com/");
            bbAuth.DeleteCredentials(targetUri);

            var readCalls = credentialStore.MethodCalls
                .Where(mc => mc.Key.Equals("ReadCredentials"))
                    .SelectMany(mc => mc.Value);

            // 2 read calls, 1 for the basic uri and 1 for /refresh_token
            Assert.AreEqual(2, readCalls.Count());
            Assert.IsTrue(readCalls.Any(rc => rc.Key[0].Equals("https://example.com/")));
            Assert.IsTrue(readCalls.Any(rc => rc.Key[0].Equals("https://example.com/refresh_token")));

            var deleteCalls = credentialStore.MethodCalls
                .Where(mc => mc.Key.Equals("DeleteCredentials"))
                    .SelectMany(mc => mc.Value);

            // 2 delete call, 1 for the basic uri, 1 for /refresh_token as there is one
            Assert.AreEqual(2, deleteCalls.Count());
            Assert.IsTrue(deleteCalls.Any(rc => rc.Key[0].Equals("https://example.com/")));
            Assert.IsTrue(deleteCalls.Any(rc => rc.Key[0].Equals("https://example.com/refresh_token")));
        }

        [TestMethod]
        public void VerifyDeleteCredentialForBasicAuthReadsQuinceDeletesTwiceIfHostCredentialsExistAndShareUsername()
        {
            var credentialStore = new MockCredentialStore();
            // add a stored basic auth credential to delete.
            // per host credentials
            credentialStore.Credentials.Add("https://example.com/", new Credential("john", "squire"));
            // per user credentials
            credentialStore.Credentials.Add("https://john@example.com/", new Credential("john", "squire"));

            var bbAuth = new Authentication(credentialStore, null, null);

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
            Assert.AreEqual(5, readCalls.Count());
            Assert.AreEqual(1, readCalls.Count(rc => rc.Key[0].Equals("https://john@example.com/")));
            Assert.AreEqual(1, readCalls.Count(rc => rc.Key[0].Equals("https://john@example.com/refresh_token")));
            Assert.AreEqual(2, readCalls.Count(rc => rc.Key[0].Equals("https://example.com/")));
            Assert.AreEqual(1, readCalls.Count(rc => rc.Key[0].Equals("https://example.com/refresh_token")));

            var deleteCalls = credentialStore.MethodCalls
                .Where(mc => mc.Key.Equals("DeleteCredentials"))
                    .SelectMany(mc => mc.Value);

            // 2 delete calls
            // 1 for the basic uri with username
            // 1 for the basic uri without username
            Assert.AreEqual(2, deleteCalls.Count());
            Assert.AreEqual(1, deleteCalls.Count(rc => rc.Key[0].Equals("https://john@example.com/")));
            Assert.IsFalse(deleteCalls.Any(rc => rc.Key[0].Equals("https://example.com/refresh_token")));
            Assert.AreEqual(1, deleteCalls.Count(rc => rc.Key[0].Equals("https://example.com/")));
        }

        [TestMethod]
        public void VerifyDeleteCredentialForBasicAuthReadsThriceDeletesOnceIfHostCredentialsExistAndDoNotShareUsername()
        {
            var credentialStore = new MockCredentialStore();
            // add a stored basic auth credential to delete.
            // per host credentials
            credentialStore.Credentials.Add("https://example.com/", new Credential("ian", "brown"));
            // per user credentials
            credentialStore.Credentials.Add("https://john@example.com/", new Credential("john", "squire"));

            var bbAuth = new Authentication(credentialStore, null, null);

            var targetUri = new TargetUri("https://john@example.com/");
            bbAuth.DeleteCredentials(targetUri);

            var readCalls = credentialStore.MethodCalls
                .Where(mc => mc.Key.Equals("ReadCredentials"))
                    .SelectMany(mc => mc.Value);

            // 5 read calls
            // 1 for the basic uri with username
            // 1 for /refresh_token with username
            // 1 for the basic uri to compare username
            Assert.AreEqual(3, readCalls.Count());
            Assert.AreEqual(1, readCalls.Count(rc => rc.Key[0].Equals("https://john@example.com/")));
            Assert.AreEqual(1, readCalls.Count(rc => rc.Key[0].Equals("https://john@example.com/refresh_token")));
            Assert.AreEqual(1, readCalls.Count(rc => rc.Key[0].Equals("https://example.com/")));
            
            var deleteCalls = credentialStore.MethodCalls
                .Where(mc => mc.Key.Equals("DeleteCredentials"))
                    .SelectMany(mc => mc.Value);

            // 1 delete calls
            // 1 for the basic uri with username
            // DOES NOT delete the Host credentials because they are for a different username.
            Assert.AreEqual(1, deleteCalls.Count());
            Assert.AreEqual(1, deleteCalls.Count(rc => rc.Key[0].Equals("https://john@example.com/")));
            Assert.IsFalse(deleteCalls.Any(rc => rc.Key[0].Equals("https://example.com/refresh_token")));
            Assert.IsFalse(deleteCalls.Any(rc => rc.Key[0].Equals("https://example.com/")));
        }

        [TestMethod]
        public void VerifyGetPerUserTargetUriInsertsMissingUsernameToActualUri()
        {
            var credentialStore = new MockCredentialStore();
            var bbAuth = new Authentication(credentialStore, null, null);

            var targetUri = new TargetUri("https://example.com");
            var username = "johnsquire";

            var resultUri = targetUri.GetPerUserTargetUri(username);

            Assert.AreEqual("/", resultUri.AbsolutePath);
            Assert.AreEqual("https://johnsquire@example.com/", resultUri.ActualUri.AbsoluteUri);
            Assert.AreEqual("example.com", resultUri.DnsSafeHost);
            Assert.AreEqual("example.com", resultUri.Host);
            Assert.AreEqual(true, resultUri.IsAbsoluteUri);
            Assert.AreEqual(true, resultUri.IsDefaultPort);
            Assert.AreEqual(443, resultUri.Port);
            Assert.AreEqual(null, resultUri.ProxyUri);
            Assert.AreEqual("https://johnsquire@example.com/", resultUri.QueryUri.AbsoluteUri);
            Assert.AreEqual("https", resultUri.Scheme);
            Assert.AreEqual(new WebProxy().Address, resultUri.WebProxy.Address);
            Assert.AreEqual("https://example.com/", resultUri.ToString());
        }

        [TestMethod]
        public void VerifyGetPerUserTargetUriDoesNotDuplicateUsernameOnActualUri()
        {
            var credentialStore = new MockCredentialStore();
            var bbAuth = new Authentication(credentialStore, null, null);

            var targetUri = new TargetUri("https://johnsquire@example.com");
            var username = "johnsquire";

            var resultUri = targetUri.GetPerUserTargetUri(username);

            Assert.AreEqual("/", resultUri.AbsolutePath);
            Assert.AreEqual("https://johnsquire@example.com/", resultUri.ActualUri.AbsoluteUri);
            Assert.AreEqual("example.com", resultUri.DnsSafeHost);
            Assert.AreEqual("example.com", resultUri.Host);
            Assert.AreEqual(true, resultUri.IsAbsoluteUri);
            Assert.AreEqual(true, resultUri.IsDefaultPort);
            Assert.AreEqual(443, resultUri.Port);
            Assert.AreEqual(null, resultUri.ProxyUri);
            Assert.AreEqual("https://johnsquire@example.com/", resultUri.QueryUri.AbsoluteUri);
            Assert.AreEqual("https", resultUri.Scheme);
            Assert.AreEqual(new WebProxy().Address, resultUri.WebProxy.Address);
            Assert.AreEqual("https://example.com/", resultUri.ToString());
        }
    }

    public class MockCredentialStore: ICredentialStore
    {
        public Dictionary<string, Dictionary<List<string>, int>> MethodCalls =
            new Dictionary<string, Dictionary<List<string>, int>>();

        public Dictionary<string, Credential> Credentials = new Dictionary<string, Credential>();

        public string Namespace
        {
            get { throw new NotImplementedException(); }
        }

        public Secret.UriNameConversion UriNameConversion
        {
            get { throw new NotImplementedException(); }
        }

        public void DeleteCredentials(TargetUri targetUri)
        {
            // do nothing
            RecordMethodCall("DeleteCredentials", new List<string>() {targetUri.ActualUri.AbsoluteUri });
        }

        public Credential ReadCredentials(TargetUri targetUri)
        {
            // do nothing
            RecordMethodCall("ReadCredentials", new List<string>() {targetUri.ActualUri.AbsoluteUri });
            return Credentials != null && Credentials.Keys.Contains(targetUri.ActualUri.AbsoluteUri) ? Credentials[targetUri.ActualUri.AbsoluteUri] : null;
        }

        public void WriteCredentials(TargetUri targetUri, Credential credentials)
        {
            // do nothing
            RecordMethodCall("WriteCredentials", new List<string>() {targetUri.ActualUri.AbsoluteUri, credentials.Username, credentials.Password });
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
