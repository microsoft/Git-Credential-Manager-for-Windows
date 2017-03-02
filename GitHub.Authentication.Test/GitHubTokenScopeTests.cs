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
            var val = GitHubTokenScope.Gist + GitHubTokenScope.Notifications;
            Assert.AreEqual(val.Value, GitHubTokenScope.Gist.Value + " " + GitHubTokenScope.Notifications.Value);

            val += GitHubTokenScope.OrgAdmin;
            Assert.AreEqual(val.Value, GitHubTokenScope.Gist.Value + " " + GitHubTokenScope.Notifications.Value + " " + GitHubTokenScope.OrgAdmin);
        }

        [TestMethod]
        public void AndOperator()
        {
            var val = (GitHubTokenScope.Gist & GitHubTokenScope.Gist);
            Assert.AreEqual(GitHubTokenScope.Gist, val);

            val = GitHubTokenScope.OrgAdmin + GitHubTokenScope.OrgHookAdmin + GitHubTokenScope.Gist;
            Assert.IsTrue((val & GitHubTokenScope.OrgAdmin) == GitHubTokenScope.OrgAdmin);
            Assert.IsTrue((val & GitHubTokenScope.OrgHookAdmin) == GitHubTokenScope.OrgHookAdmin);
            Assert.IsTrue((val & GitHubTokenScope.Gist) == GitHubTokenScope.Gist);
            Assert.IsFalse((val & GitHubTokenScope.OrgRead) == GitHubTokenScope.OrgRead);
            Assert.IsTrue((val & GitHubTokenScope.OrgRead) == GitHubTokenScope.None);
        }

        [TestMethod]
        public void Equality()
        {
            Assert.AreEqual(GitHubTokenScope.OrgWrite, GitHubTokenScope.OrgWrite);
            Assert.AreEqual(GitHubTokenScope.None, GitHubTokenScope.None);

            Assert.AreNotEqual(GitHubTokenScope.Gist, GitHubTokenScope.PublicKeyAdmin);
            Assert.AreNotEqual(GitHubTokenScope.Gist, GitHubTokenScope.None);

            Assert.AreEqual(GitHubTokenScope.OrgRead | GitHubTokenScope.PublicKeyRead | GitHubTokenScope.OrgHookAdmin, GitHubTokenScope.OrgRead | GitHubTokenScope.PublicKeyRead | GitHubTokenScope.OrgHookAdmin);
            Assert.AreEqual(GitHubTokenScope.OrgHookAdmin | GitHubTokenScope.OrgRead | GitHubTokenScope.PublicKeyRead, GitHubTokenScope.OrgRead | GitHubTokenScope.PublicKeyRead | GitHubTokenScope.OrgHookAdmin);

            Assert.AreNotEqual(GitHubTokenScope.OrgRead | GitHubTokenScope.PublicKeyWrite | GitHubTokenScope.OrgHookAdmin, GitHubTokenScope.OrgRead | GitHubTokenScope.PublicKeyRead | GitHubTokenScope.OrgHookAdmin);
            Assert.AreNotEqual(GitHubTokenScope.OrgRead | GitHubTokenScope.PublicKeyRead | GitHubTokenScope.OrgHookAdmin, GitHubTokenScope.OrgRead | GitHubTokenScope.PublicKeyRead);
        }

        [TestMethod]
        public void HashCode()
        {
            HashSet<int> hashCodes = new HashSet<int>();

            foreach (var item in GitHubTokenScope.EnumerateValues())
            {
                Assert.IsTrue(hashCodes.Add(item.GetHashCode()));
            }

            int loop1 = 0;
            foreach (var item1 in GitHubTokenScope.EnumerateValues())
            {
                int loop2 = 0;

                foreach (var item2 in GitHubTokenScope.EnumerateValues())
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
            var val1 = (GitHubTokenScope.Gist | GitHubTokenScope.Gist);
            Assert.AreEqual(GitHubTokenScope.Gist, val1);

            val1 = GitHubTokenScope.OrgAdmin + GitHubTokenScope.OrgHookAdmin + GitHubTokenScope.Gist;
            var val2 = val1 | GitHubTokenScope.OrgAdmin;
            Assert.AreEqual(val1, val2);

            val2 = GitHubTokenScope.OrgAdmin | GitHubTokenScope.OrgHookAdmin | GitHubTokenScope.Gist;
            Assert.AreEqual(val1, val2);
            Assert.IsTrue((val2 & GitHubTokenScope.OrgAdmin) == GitHubTokenScope.OrgAdmin);
            Assert.IsTrue((val2 & GitHubTokenScope.OrgHookAdmin) == GitHubTokenScope.OrgHookAdmin);
            Assert.IsTrue((val2 & GitHubTokenScope.Gist) == GitHubTokenScope.Gist);
            Assert.IsFalse((val2 & GitHubTokenScope.OrgRead) == GitHubTokenScope.OrgRead);
        }

        [TestMethod]
        public void MinusOperator()
        {
            var val1 = GitHubTokenScope.Gist | GitHubTokenScope.Repo | GitHubTokenScope.RepoDelete;
            var val2 = val1 - GitHubTokenScope.RepoDelete;
            Assert.AreEqual(val2, GitHubTokenScope.Gist | GitHubTokenScope.Repo);

            var val3 = val1 - val2;
            Assert.AreEqual(val3, GitHubTokenScope.RepoDelete);

            var val4 = val3 - GitHubTokenScope.RepoDeployment;
            Assert.AreEqual(val3, val4);

            var val5 = (GitHubTokenScope.Gist + GitHubTokenScope.Repo) - (GitHubTokenScope.Repo | GitHubTokenScope.RepoHookAdmin | GitHubTokenScope.OrgWrite);
            Assert.AreEqual(val5, GitHubTokenScope.Gist);
        }

        [TestMethod]
        public void XorOperator()
        {
            var val1 = GitHubTokenScope.RepoDelete + GitHubTokenScope.PublicKeyAdmin;
            var val2 = GitHubTokenScope.PublicKeyAdmin + GitHubTokenScope.PublicKeyRead;
            var val3 = val1 ^ val2;
            Assert.AreEqual(val3, GitHubTokenScope.RepoDelete | GitHubTokenScope.PublicKeyRead);
        }
    }
}
