using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Alm.Git.Test
{
    /// <summary>
    /// A class to test <see cref="GitInstallation"/>.
    /// </summary>
    [TestClass]
    public class GitInstallationTests
    {
        [TestMethod]
        public void CaseInsensitiveComparison()
        {
            List<GitInstallation> list = new List<GitInstallation>
            {
                new GitInstallation(@"C:\Program Files (x86)\Git", KnownGitDistribution.GitForWindows32v1),
                new GitInstallation(@"C:\Program Files (x86)\Git", KnownGitDistribution.GitForWindows32v2),
                new GitInstallation(@"C:\Program Files\Git", KnownGitDistribution.GitForWindows32v1),
                new GitInstallation(@"C:\Program Files\Git", KnownGitDistribution.GitForWindows32v2),
                new GitInstallation(@"C:\Program Files\Git", KnownGitDistribution.GitForWindows64v2),
                // ToLower versions
                new GitInstallation(@"C:\Program Files (x86)\Git".ToLower(), KnownGitDistribution.GitForWindows32v1),
                new GitInstallation(@"C:\Program Files (x86)\Git".ToLower(), KnownGitDistribution.GitForWindows32v2),
                new GitInstallation(@"C:\Program Files\Git".ToLower(), KnownGitDistribution.GitForWindows32v1),
                new GitInstallation(@"C:\Program Files\Git".ToLower(), KnownGitDistribution.GitForWindows32v2),
                new GitInstallation(@"C:\Program Files\Git".ToLower(), KnownGitDistribution.GitForWindows64v2),
                // ToUpper versions
                new GitInstallation(@"C:\Program Files (x86)\Git".ToUpper(), KnownGitDistribution.GitForWindows32v1),
                new GitInstallation(@"C:\Program Files (x86)\Git".ToUpper(), KnownGitDistribution.GitForWindows32v2),
                new GitInstallation(@"C:\Program Files\Git".ToUpper(), KnownGitDistribution.GitForWindows32v1),
                new GitInstallation(@"C:\Program Files\Git".ToUpper(), KnownGitDistribution.GitForWindows32v2),
                new GitInstallation(@"C:\Program Files\Git".ToUpper(), KnownGitDistribution.GitForWindows64v2),
            };

            HashSet<GitInstallation> set = new HashSet<GitInstallation>(list);

            Assert.AreEqual(15, list.Count);
            Assert.AreEqual(5, set.Count);

            Assert.AreEqual(6, list.Where(x => x.Version == KnownGitDistribution.GitForWindows32v1).Count());
            Assert.AreEqual(6, list.Where(x => x.Version == KnownGitDistribution.GitForWindows32v2).Count());
            Assert.AreEqual(3, list.Where(x => x.Version == KnownGitDistribution.GitForWindows64v2).Count());

            Assert.AreEqual(2, set.Where(x => x.Version == KnownGitDistribution.GitForWindows32v1).Count());
            Assert.AreEqual(2, set.Where(x => x.Version == KnownGitDistribution.GitForWindows32v2).Count());
            Assert.AreEqual(1, set.Where(x => x.Version == KnownGitDistribution.GitForWindows64v2).Count());

            foreach (var v in Enum.GetValues(typeof(KnownGitDistribution)))
            {
                KnownGitDistribution kgd = (KnownGitDistribution)v;

                var a = list.Where(x => x.Version == kgd);
                Assert.IsTrue(a.All(x => x != a.First() || string.Equals(x.Git, a.First().Git, System.StringComparison.OrdinalIgnoreCase)));
                Assert.IsTrue(a.All(x => x != a.First() || string.Equals(x.Config, a.First().Config, System.StringComparison.OrdinalIgnoreCase)));
                Assert.IsTrue(a.All(x => x != a.First() || string.Equals(x.Libexec, a.First().Libexec, System.StringComparison.OrdinalIgnoreCase)));
            }
        }
    }
}
