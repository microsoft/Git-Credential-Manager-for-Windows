using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Alm.Authentication;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Atlassian.Bitbucket.Authentication.Test
{
    [TestClass]
    public class BitbucketAuthenticationTest
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
        }

    }

    public class MockCredentialStore : ICredentialStore
    {
        public Dictionary<string, Dictionary<List<string>, int>> MethodCalls =
            new Dictionary<string, Dictionary<List<string>, int>>();

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
            RecordMethodCall("DeleteCredentials", new List<string>() { targetUri.ToString() });
        }

        public Credential ReadCredentials(TargetUri targetUri)
        {
            // do nothing 
            RecordMethodCall("ReadCredentials", new List<string>() { targetUri.ToString() });
            return null;
        }

        public void WriteCredentials(TargetUri targetUri, Credential credentials)
        {
            // do nothing
            RecordMethodCall("WriteCredentials", new List<string>() { targetUri.ToString(), credentials.Username, credentials.Password });
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
