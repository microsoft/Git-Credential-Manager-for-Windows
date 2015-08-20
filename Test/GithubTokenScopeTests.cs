using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.TeamFoundation.Authentication.Test
{
    [TestClass]
    public class GithubTokenScopeTests
    {
        [TestMethod]
        public void AddOperator()
        {
            var val = GithubTokenScope.Gist + GithubTokenScope.Notifications;
            Assert.AreEqual(val.Value, GithubTokenScope.Gist.Value + " " + GithubTokenScope.Notifications.Value);

            val += GithubTokenScope.OrgAdmin;
            Assert.AreEqual(val.Value, GithubTokenScope.Gist.Value + " " + GithubTokenScope.Notifications.Value + " " + GithubTokenScope.OrgAdmin);
        }

        [TestMethod]
        public void AndOperator()
        {
            var val = (GithubTokenScope.Gist & GithubTokenScope.Gist);
            Assert.AreEqual(GithubTokenScope.Gist, val);

            val = GithubTokenScope.OrgAdmin + GithubTokenScope.OrgHookAdmin + GithubTokenScope.Gist;
            Assert.IsTrue((val & GithubTokenScope.OrgAdmin) == GithubTokenScope.OrgAdmin);
            Assert.IsTrue((val & GithubTokenScope.OrgHookAdmin) == GithubTokenScope.OrgHookAdmin);
            Assert.IsTrue((val & GithubTokenScope.Gist) == GithubTokenScope.Gist);
            Assert.IsFalse((val & GithubTokenScope.OrgRead) == GithubTokenScope.OrgRead);
            Assert.IsTrue((val & GithubTokenScope.OrgRead) == GithubTokenScope.None);
        }

        [TestMethod]
        public void Equality()
        {
            Assert.AreEqual(GithubTokenScope.OrgWrite, GithubTokenScope.OrgWrite);
            Assert.AreEqual(GithubTokenScope.None, GithubTokenScope.None);

            Assert.AreNotEqual(GithubTokenScope.Gist, GithubTokenScope.PublicKeyAdmin);
            Assert.AreNotEqual(GithubTokenScope.Gist, GithubTokenScope.None);

            Assert.AreEqual(GithubTokenScope.OrgRead | GithubTokenScope.PublicKeyRead | GithubTokenScope.OrgHookAdmin, GithubTokenScope.OrgRead | GithubTokenScope.PublicKeyRead | GithubTokenScope.OrgHookAdmin);
            Assert.AreEqual(GithubTokenScope.OrgHookAdmin | GithubTokenScope.OrgRead | GithubTokenScope.PublicKeyRead, GithubTokenScope.OrgRead | GithubTokenScope.PublicKeyRead | GithubTokenScope.OrgHookAdmin);

            Assert.AreNotEqual(GithubTokenScope.OrgRead | GithubTokenScope.PublicKeyWrite | GithubTokenScope.OrgHookAdmin, GithubTokenScope.OrgRead | GithubTokenScope.PublicKeyRead | GithubTokenScope.OrgHookAdmin);
            Assert.AreNotEqual(GithubTokenScope.OrgRead | GithubTokenScope.PublicKeyRead | GithubTokenScope.OrgHookAdmin, GithubTokenScope.OrgRead | GithubTokenScope.PublicKeyRead);
        }

        [TestMethod]
        public void HashCode()
        {
            HashSet<int> hashCodes = new HashSet<int>();

            foreach (var item in GithubTokenScope.EnumerateValues())
            {
                Assert.IsTrue(hashCodes.Add(item.GetHashCode()));
            }

            int loop1 = 0;
            foreach (var item1 in GithubTokenScope.EnumerateValues())
            {
                int loop2 = 0;

                foreach (var item2 in GithubTokenScope.EnumerateValues())
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
            var val1 = (GithubTokenScope.Gist | GithubTokenScope.Gist);
            Assert.AreEqual(GithubTokenScope.Gist, val1);

            val1 = GithubTokenScope.OrgAdmin + GithubTokenScope.OrgHookAdmin + GithubTokenScope.Gist;
            var val2 = val1 | GithubTokenScope.OrgAdmin;
            Assert.AreEqual(val1, val2);

            val2 = GithubTokenScope.OrgAdmin | GithubTokenScope.OrgHookAdmin | GithubTokenScope.Gist;
            Assert.AreEqual(val1, val2);
            Assert.IsTrue((val2 & GithubTokenScope.OrgAdmin) == GithubTokenScope.OrgAdmin);
            Assert.IsTrue((val2 & GithubTokenScope.OrgHookAdmin) == GithubTokenScope.OrgHookAdmin);
            Assert.IsTrue((val2 & GithubTokenScope.Gist) == GithubTokenScope.Gist);
            Assert.IsFalse((val2 & GithubTokenScope.OrgRead) == GithubTokenScope.OrgRead);
        }

        [TestMethod]
        public void MinusOpertor()
        {
            var val1 = GithubTokenScope.Gist | GithubTokenScope.Repo | GithubTokenScope.RepoDelete;
            var val2 = val1 - GithubTokenScope.RepoDelete;
            Assert.AreEqual(val2, GithubTokenScope.Gist | GithubTokenScope.Repo);

            var val3 = val1 - val2;
            Assert.AreEqual(val3, GithubTokenScope.RepoDelete);

            var val4 = val3 - GithubTokenScope.RepoDeployment;
            Assert.AreEqual(val3, val4);

            var val5 = (GithubTokenScope.Gist + GithubTokenScope.Repo) - (GithubTokenScope.Repo | GithubTokenScope.RepoHookAdmin | GithubTokenScope.OrgWrite);
            Assert.AreEqual(val5, GithubTokenScope.Gist);
        }

        [TestMethod]
        public void XorOperator()
        {
            var val1 = GithubTokenScope.RepoDelete + GithubTokenScope.PublicKeyAdmin;
            var val2 = GithubTokenScope.PublicKeyAdmin + GithubTokenScope.PublicKeyRead;
            var val3 = val1 ^ val2;
            Assert.AreEqual(val3, GithubTokenScope.RepoDelete | GithubTokenScope.PublicKeyRead);
        }
    }
}
