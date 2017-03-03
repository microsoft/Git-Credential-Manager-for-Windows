using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GitHub.Authentication.Test
{
    [TestClass]
    public class GitHubTokenScopeTests
    {
        [TestMethod]
        public void AddOperator()
        {
            var val = TokenScope.Gist + TokenScope.Notifications;
            Assert.AreEqual(val.Value, TokenScope.Gist.Value + " " + TokenScope.Notifications.Value);

            val += TokenScope.OrgAdmin;
            Assert.AreEqual(val.Value, TokenScope.Gist.Value + " " + TokenScope.Notifications.Value + " " + TokenScope.OrgAdmin);
        }

        [TestMethod]
        public void AndOperator()
        {
            var val = (TokenScope.Gist & TokenScope.Gist);
            Assert.AreEqual(TokenScope.Gist, val);

            val = TokenScope.OrgAdmin + TokenScope.OrgHookAdmin + TokenScope.Gist;
            Assert.IsTrue((val & TokenScope.OrgAdmin) == TokenScope.OrgAdmin);
            Assert.IsTrue((val & TokenScope.OrgHookAdmin) == TokenScope.OrgHookAdmin);
            Assert.IsTrue((val & TokenScope.Gist) == TokenScope.Gist);
            Assert.IsFalse((val & TokenScope.OrgRead) == TokenScope.OrgRead);
            Assert.IsTrue((val & TokenScope.OrgRead) == TokenScope.None);
        }

        [TestMethod]
        public void Equality()
        {
            Assert.AreEqual(TokenScope.OrgWrite, TokenScope.OrgWrite);
            Assert.AreEqual(TokenScope.None, TokenScope.None);

            Assert.AreNotEqual(TokenScope.Gist, TokenScope.PublicKeyAdmin);
            Assert.AreNotEqual(TokenScope.Gist, TokenScope.None);

            Assert.AreEqual(TokenScope.OrgRead | TokenScope.PublicKeyRead | TokenScope.OrgHookAdmin, TokenScope.OrgRead | TokenScope.PublicKeyRead | TokenScope.OrgHookAdmin);
            Assert.AreEqual(TokenScope.OrgHookAdmin | TokenScope.OrgRead | TokenScope.PublicKeyRead, TokenScope.OrgRead | TokenScope.PublicKeyRead | TokenScope.OrgHookAdmin);

            Assert.AreNotEqual(TokenScope.OrgRead | TokenScope.PublicKeyWrite | TokenScope.OrgHookAdmin, TokenScope.OrgRead | TokenScope.PublicKeyRead | TokenScope.OrgHookAdmin);
            Assert.AreNotEqual(TokenScope.OrgRead | TokenScope.PublicKeyRead | TokenScope.OrgHookAdmin, TokenScope.OrgRead | TokenScope.PublicKeyRead);
        }

        [TestMethod]
        public void HashCode()
        {
            HashSet<int> hashCodes = new HashSet<int>();

            foreach (var item in TokenScope.EnumerateValues())
            {
                Assert.IsTrue(hashCodes.Add(item.GetHashCode()));
            }

            int loop1 = 0;
            foreach (var item1 in TokenScope.EnumerateValues())
            {
                int loop2 = 0;

                foreach (var item2 in TokenScope.EnumerateValues())
                {
                    if (loop1 < loop2)
                    {
                        Assert.IsTrue(hashCodes.Add((item1 | item2).GetHashCode()));
                    }
                    else
                    {
                        Assert.IsFalse(hashCodes.Add((item1 | item2).GetHashCode()));
                    }

                    loop2++;
                }

                loop1++;
            }
        }

        [TestMethod]
        public void OrOperator()
        {
            var val1 = (TokenScope.Gist | TokenScope.Gist);
            Assert.AreEqual(TokenScope.Gist, val1);

            val1 = TokenScope.OrgAdmin + TokenScope.OrgHookAdmin + TokenScope.Gist;
            var val2 = val1 | TokenScope.OrgAdmin;
            Assert.AreEqual(val1, val2);

            val2 = TokenScope.OrgAdmin | TokenScope.OrgHookAdmin | TokenScope.Gist;
            Assert.AreEqual(val1, val2);
            Assert.IsTrue((val2 & TokenScope.OrgAdmin) == TokenScope.OrgAdmin);
            Assert.IsTrue((val2 & TokenScope.OrgHookAdmin) == TokenScope.OrgHookAdmin);
            Assert.IsTrue((val2 & TokenScope.Gist) == TokenScope.Gist);
            Assert.IsFalse((val2 & TokenScope.OrgRead) == TokenScope.OrgRead);
        }

        [TestMethod]
        public void MinusOperator()
        {
            var val1 = TokenScope.Gist | TokenScope.Repo | TokenScope.RepoDelete;
            var val2 = val1 - TokenScope.RepoDelete;
            Assert.AreEqual(val2, TokenScope.Gist | TokenScope.Repo);

            var val3 = val1 - val2;
            Assert.AreEqual(val3, TokenScope.RepoDelete);

            var val4 = val3 - TokenScope.RepoDeployment;
            Assert.AreEqual(val3, val4);

            var val5 = (TokenScope.Gist + TokenScope.Repo) - (TokenScope.Repo | TokenScope.RepoHookAdmin | TokenScope.OrgWrite);
            Assert.AreEqual(val5, TokenScope.Gist);
        }

        [TestMethod]
        public void XorOperator()
        {
            var val1 = TokenScope.RepoDelete + TokenScope.PublicKeyAdmin;
            var val2 = TokenScope.PublicKeyAdmin + TokenScope.PublicKeyRead;
            var val3 = val1 ^ val2;
            Assert.AreEqual(val3, TokenScope.RepoDelete | TokenScope.PublicKeyRead);
        }
    }
}
