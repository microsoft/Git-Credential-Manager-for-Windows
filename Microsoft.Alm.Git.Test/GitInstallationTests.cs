using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace Microsoft.Alm.Git.Test
{
    /// <summary>
    /// A class to test <see cref="GitInstallation"/>.
    /// </summary>
    public class GitInstallationTests
    {
        [Fact]
        public void GitInstallation_CaseInsensitiveComparison()
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

            Assert.Equal(15, list.Count);
            Assert.Equal(5, set.Count);

            Assert.Equal(6, list.Where(x => x.Version == KnownGitDistribution.GitForWindows32v1).Count());
            Assert.Equal(6, list.Where(x => x.Version == KnownGitDistribution.GitForWindows32v2).Count());
            Assert.Equal(3, list.Where(x => x.Version == KnownGitDistribution.GitForWindows64v2).Count());

            Assert.Equal(2, set.Where(x => x.Version == KnownGitDistribution.GitForWindows32v1).Count());
            Assert.Equal(2, set.Where(x => x.Version == KnownGitDistribution.GitForWindows32v2).Count());
            Assert.Equal(1, set.Where(x => x.Version == KnownGitDistribution.GitForWindows64v2).Count());

            foreach (var v in Enum.GetValues(typeof(KnownGitDistribution)))
            {
                KnownGitDistribution kgd = (KnownGitDistribution)v;

                var a = list.Where(x => x.Version == kgd);
                Assert.True(a.All(x => x != a.First() || GitInstallation.PathComparer.Equals(x.Cmd, a.First().Cmd)));
                Assert.True(a.All(x => x != a.First() || GitInstallation.PathComparer.Equals(x.Config, a.First().Config)));
                Assert.True(a.All(x => x != a.First() || GitInstallation.PathComparer.Equals(x.Git, a.First().Git)));
                Assert.True(a.All(x => x != a.First() || GitInstallation.PathComparer.Equals(x.Libexec, a.First().Libexec)));
                Assert.True(a.All(x => x != a.First() || GitInstallation.PathComparer.Equals(x.Sh, a.First().Sh)));
            }
        }
    }
}
