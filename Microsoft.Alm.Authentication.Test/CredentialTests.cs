using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;

namespace Microsoft.Alm.Authentication.Test
{
    [TestClass]
    public class CredentialTests
    {
        const string Namespace = "test";

        [TestMethod]
        public void UriToName_GitHubSimple()
        {
            const string Expected = Namespace + ":https://www.github.com";
            const string Original = "https://www.github.com";

            UriToNameTest(Namespace, Original, Expected);
        }

        [TestMethod]
        public void UriToName_VstsSimple()
        {
            const string Expected = Namespace + ":https://account.visualstudio.com";
            const string Original = "https://account.visualstudio.com";

            UriToNameTest(Namespace, Original, Expected);
        }

        [TestMethod]
        public void UriToName_HttpsWithPath()
        {
            const string Expected = Namespace + ":https://github.com/Microsoft/Git-Credential-Manager-for-Windows.git";
            const string Original = "https://github.com/Microsoft/Git-Credential-Manager-for-Windows.git";

            UriToNameTest(Namespace, Original, Expected);
        }

        [TestMethod]
        public void UriToName_HttpsWithTrailingSlash()
        {
            const string Expected = Namespace + ":https://www.github.com";
            const string Original = "https://www.github.com";

            UriToNameTest(Namespace, Original, Expected);
        }

        [TestMethod]
        public void UriToName_ComplexVsts()
        {
            const string Expected = Namespace + ":https://mytenant.visualstudio.com/MYTENANT/_git/App.MyApp";
            const string Original = "https://mytenant.visualstudio.com/MYTENANT/_git/App.MyApp";

            var uri = new Uri(Original);
            var actual = Secret.UriToName(uri, Namespace);

            Assert.AreEqual(Expected, actual);
        }

        [TestMethod]
        public void UriToName_Unc()
        {
            const string Expected = Namespace + ":file://unc/path";
            const string Original = @"\\unc\path";

            UriToNameTest(Namespace, Original, Expected);
        }

        [TestMethod]
        public void UriToName_UncWithPrefix()
        {
            const string Expected = Namespace + ":file://unc/path";
            const string Original = @"file://unc/path";

            UriToNameTest(Namespace, Original, Expected);
        }

        [TestMethod]
        public void UriToName_UncWithTrailingSlash()
        {
            const string Expected = Namespace + ":file://unc/path";
            const string Original = @"\\unc\path\";

            var uri = new Uri(Original);
            var actual = Secret.UriToName(uri, Namespace);

            Assert.AreEqual(Expected, actual);
        }

        [TestMethod]
        public void CredentialStoreUrl()
        {
            ICredentialStoreTest(new SecretStore("test", null, null, Secret.UriToName), "http://dummy.url/for/testing", "username", "password");
        }

        [TestMethod]
        public void CredentialStoreUrlWithParams()
        {
            ICredentialStoreTest(new SecretStore("test", null, null, Secret.UriToName), "http://dummy.url/for/testing?with=params", "username", "password");
        }

        [TestMethod]
        public void CredentialStoreUnc()
        {
            ICredentialStoreTest(new SecretStore("test", null, null, Secret.UriToName), @"\\unc\share\test", "username", "password");
        }

        [TestMethod]
        public void CredentialStoreUsernameNullReject()
        {
            try
            {
                ICredentialStoreTest(new SecretStore("test", null, null, Secret.UriToName), "http://dummy.url/for/testing", null, "null_usernames_are_illegal");
                Assert.Fail("Null username was accepted");
            }
            catch { }
        }

        [TestMethod]
        public void CredentialStoreUsernameBlank()
        {
            ICredentialStoreTest(new SecretStore("test", null, null, Secret.UriToName), "http://dummy.url/for/testing", "", "blank_usernames_are_legal");
        }

        [TestMethod]
        public void CredentialStorePasswordNull()
        {
            ICredentialStoreTest(new SecretStore("test", null, null, Secret.UriToName), "http://dummy.url/for/testing", "null_passwords_are_illegal", null);
        }

        [TestMethod]
        public void CredentialStorePassswordBlank()
        {
            ICredentialStoreTest(new SecretStore("test", null, null, Secret.UriToName), "http://dummy.url/for/testing", "blank_passwords_are_legal", "");
        }

        [TestMethod]
        public void SecretCacheUrl()
        {
            ICredentialStoreTest(new SecretCache("test-cache"), "http://dummy.url/for/testing", "username", "password");
            ICredentialStoreTest(new SecretCache("test-cache"), "http://dummy.url/for/testing", "username", "password");
        }

        [TestMethod]
        public void SecretCacheUrlWithParams()
        {
            ICredentialStoreTest(new SecretCache("test-cache", Secret.UriToName), "http://dummy.url/for/testing?with=params", "username", "password");
        }

        [TestMethod]
        public void SecretCacheUnc()
        {
            ICredentialStoreTest(new SecretCache("test-cache", Secret.UriToName), @"\\unc\share\test", "username", "password");
        }

        [TestMethod]
        public void SecretCacheUsernameNull()
        {
            ICredentialStoreTest(new SecretCache("test-cache", Secret.UriToName), "http://dummy.url/for/testing", null, "null_usernames_are_illegal");
        }

        [TestMethod]
        public void SecretCacheUsernameBlankReject()
        {
            ICredentialStoreTest(new SecretCache("test-cache", Secret.UriToName), "http://dummy.url/for/testing", "", "blank_usernames_are_illegal");
        }

        [TestMethod]
        public void SecretCachePasswordNull()
        {
            ICredentialStoreTest(new SecretCache("test-cache", Secret.UriToName), "http://dummy.url/for/testing", "null_passwords_are_illegal", null);
        }

        private void ICredentialStoreTest(ICredentialStore credentialStore, string url, string username, string password)
        {
            try
            {
                TargetUri uri = new TargetUri(url);
                Credential writeCreds = new Credential(username, password);
                Credential readCreds = null;

                credentialStore.WriteCredentials(uri, writeCreds);

                if ((readCreds = credentialStore.ReadCredentials(uri)) != null)
                {
                    Assert.AreEqual(writeCreds.Password, readCreds.Password, "Passwords did not match between written and read credentials");
                    Assert.AreEqual(writeCreds.Username, readCreds.Username, "Usernames did not match between written and read credentials");
                }
                else
                {
                    Assert.Fail("Failed to read credentials");
                }

                credentialStore.DeleteCredentials(uri);

                Assert.IsNull(readCreds = credentialStore.ReadCredentials(uri), "Deleted credentials were read back");
            }
            catch (Exception exception)
            {
                Assert.Fail(exception.Message);
            }
        }

        private void UriToNameTest(string @namespace, string original, string expected)
        {
            var uri = new Uri(original);
            var actual = Secret.UriToName(uri, Namespace);

            Assert.AreEqual(expected, actual);
        }
    }
}
