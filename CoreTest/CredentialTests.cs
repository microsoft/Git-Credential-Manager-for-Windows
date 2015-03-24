using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.TeamFoundation.Git.Helpers.Authentication.Test
{
    [TestClass]
    public class CredentialTests
    {
        [TestMethod]
        public void CredentialStoreUrl()
        {
            ICredentialStoreTest(new CredentialStore("test"), "http://dummy.url/for/testing", "username", "password");
        }
        [TestMethod]
        public void CredentialStoreUrlWithParams()
        {
            ICredentialStoreTest(new CredentialStore("test"), "http://dummy.url/for/testing?with=params", "username", "password");
        }
        [TestMethod]
        public void CredentialStoreUnc()
        {
            ICredentialStoreTest(new CredentialStore("test"), @"\\unc\share\test", "username", "password");
        }
        [TestMethod]
        public void CredentialStoreUsernameNullReject()
        {
            try
            {
                ICredentialStoreTest(new CredentialStore("test"), "http://dummy.url/for/testing", null, "null_usernames_are_illegal");
                Assert.Fail("Null username was accepted");
            }
            catch { }
        }
        [TestMethod]
        public void CredentialStoreUsernameBlankReject()
        {
            try
            {
                ICredentialStoreTest(new CredentialStore("test"), "http://dummy.url/for/testing", "", "blank_usernames_are_illegal");
                Assert.Fail("Empty username was accepted");
            }
            catch { }
        }
        [TestMethod]
        public void CredentialStorePasswordNullReject()
        {
            try
            {
                ICredentialStoreTest(new CredentialStore("test"), "http://dummy.url/for/testing", "null_passwords_are_illegal", null);
                Assert.Fail("Null password was accepted");
            }
            catch { }
        }

        [TestMethod]
        public void CredentialCacheUrl()
        {
            ICredentialStoreTest(new CredentialCache(), "http://dummy.url/for/testing", "username", "password");
        }
        [TestMethod]
        public void CredentialCacheUrlWithParams()
        {
            ICredentialStoreTest(new CredentialCache(), "http://dummy.url/for/testing?with=params", "username", "password");
        }
        [TestMethod]
        public void CredentialCacheUnc()
        {
            ICredentialStoreTest(new CredentialCache(), @"\\unc\share\test", "username", "password");
        }
        [TestMethod]
        public void CredentialCacheUsernameNullReject()
        {
            try
            {
                ICredentialStoreTest(new CredentialCache(), "http://dummy.url/for/testing", null, "null_usernames_are_illegal");
                Assert.Fail("Null username was accepted");
            }
            catch { }
        }
        [TestMethod]
        public void CredentialCacheUsernameBlankReject()
        {
            try
            {
                ICredentialStoreTest(new CredentialCache(), "http://dummy.url/for/testing", "", "blank_usernames_are_illegal");
                Assert.Fail("Empty username was accepted");
            }
            catch { }
        }
        [TestMethod]
        public void CredentialCachePasswordNullReject()
        {
            try
            {
                ICredentialStoreTest(new CredentialCache(), "http://dummy.url/for/testing", "null_passwords_are_illegal", null);
                Assert.Fail("Null password was accepted");
            }
            catch { }
        }        

        private void ICredentialStoreTest(ICredentialStore credentialStore, string url, string username, string password)
        {
            try
            {
                Uri uri = new Uri(url, UriKind.Absolute);
                Credential writeCreds = new Credential(username, password);
                Credential readCreds = null;

                credentialStore.WriteCredentials(uri, writeCreds);

                if (credentialStore.ReadCredentials(uri, out readCreds))
                {
                    Assert.AreEqual(writeCreds.Password, readCreds.Password, "Passwords did not match between written and read credentials");
                    Assert.AreEqual(writeCreds.Username, readCreds.Username, "Usernames did not match between written and read credentials");
                }
                else
                {
                    Assert.Fail("Failed to read credentials");
                }

                credentialStore.DeleteCredentials(uri);

                Assert.IsFalse(credentialStore.ReadCredentials(uri, out readCreds), "Deleted credentials were read back");
            }
            catch (Exception exception)
            {
                Assert.Fail(exception.Message);
            }
        }
    }
}
