using System;
using System.Collections.Generic;
using Xunit;

namespace Microsoft.Alm.Authentication.Test
{
    public class CredentialTests
    {
        private const string Namespace = "test";

        public static object[] CredentialData
        {
            get
            {
                List<object[]> data = new List<object[]>()
                {
                    new object[] { false, "http://dummy.url/for/testing", "username", "password", false },
                    new object[] { false, "http://dummy.url/for/testing?with=params", "username", "password", false },
                    new object[] { false, "file://unc/share/test", "username", "password", false },
                    new object[] { false, "http://dummy.url/for/testing", null, "null_usernames_are_illegal", true },
                    new object[] { false, "http://dummy.url/for/testing", "", "blank_usernames_are_legal", false },
                    new object[] { false, "http://dummy.url/for/testing", "null_passwords_are_legal", null, false },
                    new object[] { false, "http://dummy.url/for/testing", "blank_passwords_are_legal", "", false },
                    new object[] { false, "http://dummy.url/for/testing", "username", "password", false },
                    new object[] { false, "http://dummy.url:999/for/testing", "username", "password", false },

                    new object[] { true, "http://dummy.url/for/testing", "username", "password", false },
                    new object[] { true, "http://dummy.url/for/testing?with=params", "username", "password", false },
                    new object[] { true, "file://unc/share/test", "username", "password", false },
                    new object[] { true, "http://dummy.url/for/testing", null, "null_usernames_are_illegal", true },
                    new object[] { true, "http://dummy.url/for/testing", "", "blank_usernames_are_legal", false },
                    new object[] { true, "http://dummy.url/for/testing", "null_passwords_are_legal", null, false },
                    new object[] { true, "http://dummy.url/for/testing", "blank_passwords_are_legal", "", false },
                    new object[] { true, "http://dummy.url/for/testing", "username", "password", false },
                    new object[] { true, "http://dummy.url:999/for/testing", "username", "password", false },
                };

                return data.ToArray();
            }
        }

        public static object[] UriToNameData
        {
            get
            {
                var data = new List<object[]>()
                {
                    new object[] { "https://microsoft.visualstudio.com", null },
                    new object[] { "https://www.github.com", null },
                    new object[] { "https://bitbucket.org", null },
                    new object[] { "https://github.com/Microsoft/Git-Credential-Manager-for-Windows.git", null },
                    new object[] { "https://microsoft.visualstudio.com/", "https://microsoft.visualstudio.com" },
                    new object[] { "https://mytenant.visualstudio.com/MYTENANT/_git/App.MyApp", null },
                    new object[] { "file://unc/path", null },
                    new object[] { "file://tfs01/vc/repos", null },
                    new object[] { "http://vsts-tfs:8080/tfs", null },
                };

                return data.ToArray();
            }
        }

        [Theory]
        [MemberData(nameof(CredentialData))]
        public void Credential_WriteDelete(bool useCache, string url, string username, string password, bool throws)
        {
            Action action = () =>
            {
                var uri = new TargetUri(url);
                var writeCreds = new Credential(username, password);
                var credentialStore = useCache
                ? new SecretCache("test", Secret.UriToName) as ICredentialStore
                : new SecretStore("test", null, null, Secret.UriToName) as ICredentialStore;
                Credential readCreds = null;

                credentialStore.WriteCredentials(uri, writeCreds);

                readCreds = credentialStore.ReadCredentials(uri);
                Assert.NotNull(readCreds);
                Assert.Equal(writeCreds.Password, readCreds.Password);
                Assert.Equal(writeCreds.Username, readCreds.Username);

                credentialStore.DeleteCredentials(uri);

                Assert.Null(readCreds = credentialStore.ReadCredentials(uri));
            };

            if (throws)
            {
                Assert.Throws<ArgumentNullException>(action);
            }
            else
            {
                action();
            }
        }

        [Theory]
        [MemberData(nameof(UriToNameData))]
        public void UriToName(string original, string expected)
        {
            var uri = new Uri(original);
            var actual = Secret.UriToName(uri, Namespace);

            expected = $"{Namespace}:{expected ?? original}";

            Assert.Equal(expected, actual, StringComparer.Ordinal);
        }
    }
}
