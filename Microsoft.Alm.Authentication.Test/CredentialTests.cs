using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Microsoft.Alm.Authentication.Test
{
    [TestClass]
    public class CredentialTests
    {
        [TestMethod]
        public void CredentialStoreUrl()
        {
            ICredentialStoreTest(new SecretStore("test"), "http://dummy.url/for/testing", "username", "password");
        }

        [TestMethod]
        public void CredentialStoreUrlWithParams()
        {
            ICredentialStoreTest(new SecretStore("test"), "http://dummy.url/for/testing?with=params", "username", "password");
        }

        [TestMethod]
        public void CredentialStoreUnc()
        {
            ICredentialStoreTest(new SecretStore("test"), @"\\unc\share\test", "username", "password");
        }

        [TestMethod]
        public void CredentialStoreUsernameNullReject()
        {
            try
            {
                ICredentialStoreTest(new SecretStore("test"), "http://dummy.url/for/testing", null, "null_usernames_are_illegal");
                Assert.Fail("Null username was accepted");
            }
            catch { }
        }

        [TestMethod]
        public void CredentialStoreUsernameBlank()
        {
            ICredentialStoreTest(new SecretStore("test"), "http://dummy.url/for/testing", "", "blank_usernames_are_legal");
        }

        [TestMethod]
        public void CredentialStorePasswordNull()
        {
            ICredentialStoreTest(new SecretStore("test"), "http://dummy.url/for/testing", "null_passwords_are_illegal", null);
        }

        [TestMethod]
        public void CredentialStorePassswordBlank()
        {
            ICredentialStoreTest(new SecretStore("test"), "http://dummy.url/for/testing", "blank_passwords_are_legal", "");
        }

        [TestMethod]
        public void SecretCacheUrl()
        {
            ICredentialStoreTest(new SecretCache("test-cache"), "http://dummy.url/for/testing", "username", "password");
        }

        [TestMethod]
        public void SecretCacheUrlWithParams()
        {
            ICredentialStoreTest(new SecretCache("test-cache"), "http://dummy.url/for/testing?with=params", "username", "password");
        }

        [TestMethod]
        public void SecretCacheUnc()
        {
            ICredentialStoreTest(new SecretCache("test-cache"), @"\\unc\share\test", "username", "password");
        }

        [TestMethod]
        public void SecretCacheUsernameNull()
        {
            ICredentialStoreTest(new SecretCache("test-cache"), "http://dummy.url/for/testing", null, "null_usernames_are_illegal");
        }

        [TestMethod]
        public void SecretCacheUsernameBlankReject()
        {
            ICredentialStoreTest(new SecretCache("test-cache"), "http://dummy.url/for/testing", "", "blank_usernames_are_illegal");
        }
        
        [TestMethod]
        public void SecretCachePasswordNull()
        {
            ICredentialStoreTest(new SecretCache("test-cache"), "http://dummy.url/for/testing", "null_passwords_are_illegal", null);
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
