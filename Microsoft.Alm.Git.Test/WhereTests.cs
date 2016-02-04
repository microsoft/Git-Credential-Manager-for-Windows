using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Alm.Git.Test
{
    [TestClass]
    public class WhereTests
    {
        static StringComparer PathComparer = StringComparer.InvariantCultureIgnoreCase;

        [TestMethod]
        public void FindApp()
        {
            string[] apps = new[] { "cmd", "notepad", "calc", "powershell", "git" };
            foreach (string app in apps)
            {
                string path1;
                Assert.IsTrue(CmdWhere(app, out path1));
                string path2;
                Assert.IsTrue(Where.FindApp(app, out path2));

                Assert.IsTrue(PathComparer.Equals(path1, path2));
            }
        }

        [TestMethod]
        public void FindGit()
        {
            string gitPath;
            if (!Where.FindApp("git", out gitPath))
                Assert.Inconclusive();

            List<GitInstallation> installations;
            Assert.IsTrue(Where.FindGitInstallations(out installations));
            Assert.IsTrue(installations.Count > 0);
            Assert.IsTrue(PathComparer.Equals(installations[0].Git, gitPath));

            GitInstallation installation;
            Assert.IsTrue(Where.FindGitInstallation(installations[0].Path, installations[0].Version, out installation));
            Assert.IsTrue(installations[0] == installation);
        }

        private static bool CmdWhere(string app, out string path)
        {
            path = null;

            var startInfo = new ProcessStartInfo
            {
                Arguments = "/c where " + app,
                CreateNoWindow = true,
                FileName = "cmd",
                RedirectStandardError = true,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
            };
            var process = Process.Start(startInfo);
            if (process.WaitForExit(3000))
            {
                path = process.StandardOutput.ReadLine();
                path = path.Trim();
            }

            return path != null;
        }
    }
}
