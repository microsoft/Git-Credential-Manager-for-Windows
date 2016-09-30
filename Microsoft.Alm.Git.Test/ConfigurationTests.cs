using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Alm.Git.Test
{
    /// <summary>
    /// A class to test <see cref="Configuration"/>.
    /// </summary>
    [TestClass]
    public class ConfigurationTests
    {
        [TestMethod]
        public void ParseGitConfigSimple()
        {
            const string input = @"
[core]
    autocrlf = false
";

            var values = TestParseGitConfig(input);

            Assert.AreEqual("false", values["core.autocrlf"]);
        }

        [TestMethod]
        public void ParseGitConfigOverwritesValues()
        {
            const string input = @"
[core]
    autocrlf = true
    autocrlf = ThisShouldBeInvalidButIgnored
    autocrlf = false
";

            var values = TestParseGitConfig(input);

            Assert.AreEqual("false", values["core.autocrlf"]);
        }

        [TestMethod]
        public void ParseGitConfigPartiallyQuoted()
        {
            const string input = @"
[core ""oneQuote]
    autocrlf = ""false
";

            var values = TestParseGitConfig(input);

            Assert.AreEqual("false", values["core.oneQuote.autocrlf"]);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times")]
        [TestMethod]
        public void ParseGitConfigSampleFile()
        {
            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var me = this.GetType();
            var us = me.Assembly;

            using (var rs = us.GetManifestResourceStream(me, "sample.gitconfig"))
            using (var sr = new StreamReader(rs))
            {
                Configuration.ParseGitConfig(sr, values);
            }

            Assert.AreEqual(36, values.Count);
            Assert.AreEqual("\\\"C:/Utils/Compare It!/wincmp3.exe\\\" \\\"$(cygpath -w \\\"$LOCAL\\\")\\\" \\\"$(cygpath -w \\\"$REMOTE\\\")\\\"", values["difftool.cygcompareit.cmd"], "The quotes remained.");
            Assert.AreEqual("!f() { git fetch origin && git checkout -b $1 origin/master --no-track; }; f", values["alias.cob"], "The quotes were stripped.");
        }

        private static Dictionary<string, string> TestParseGitConfig(string input)
        {
            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            using (var sr = new StringReader(input))
            {
                Configuration.ParseGitConfig(sr, values);
            }
            return values;
        }

        [TestMethod]
        public void ReadThroughPublicMethods()
        {
            const string input = "\n" +
                    "[core]\n" +
                    "    autocrlf = false\n" +
                    "[credential \"microsoft.visualstudio.com\"]\n" +
                    "    authority = AAD\n" +
                    "[credential \"visualstudio.com\"]\n" +
                    "    authority = MSA\n" +
                    "[credential \"https://ntlm.visualstudio.com\"]\n" +
                    "    authority = NTLM\n" +
                    "[credential]\n" +
                    "    helper = manager\n" +
                    "";
            Configuration cut;

            using (var reader = new StringReader(input))
            {
                cut = new Configuration(reader);
            }

            Assert.AreEqual(true, cut.ContainsKey("CoRe.AuToCrLf"));
            Assert.AreEqual("false", cut["CoRe.AuToCrLf"]);

            Configuration.Entry entry;
            Assert.AreEqual(true, cut.TryGetEntry("core", (string)null, "autocrlf", out entry));
            Assert.AreEqual("false", entry.Value);

            Assert.AreEqual(true, cut.TryGetEntry("credential", new Uri("https://microsoft.visualstudio.com"), "authority", out entry));
            Assert.AreEqual("AAD", entry.Value);

            Assert.AreEqual(true, cut.TryGetEntry("credential", new Uri("https://mseng.visualstudio.com"), "authority", out entry));
            Assert.AreEqual("MSA", entry.Value);

            Assert.AreEqual(true, cut.TryGetEntry("credential", new Uri("https://ntlm.visualstudio.com"), "authority", out entry));
            Assert.AreEqual("NTLM", entry.Value);
        }
    }
}
