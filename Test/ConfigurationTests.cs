using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.TeamFoundation.Authentication.Test
{
    /// <summary>
    /// A class to test <see cref="Configuration"/>.
    /// </summary>
    [TestClass]
    public class ConfigurationTests
    {
        [TestMethod]
        public void ParseGitConfig_Simple()
        {
            const string input = @"
[core]
    autocrlf = false
";

            var values = TestParseGitConfig(input);

            Assert.AreEqual("false", values["core.autocrlf"]);
        }

        [TestMethod]
        public void ParseGitConfig_OverwritesValues()
        {
            // http://thedailywtf.com/articles/What_Is_Truth_0x3f_
            const string input = @"
[core]
    autocrlf = true
    autocrlf = FileNotFound
    autocrlf = false
";

            var values = TestParseGitConfig(input);

            Assert.AreEqual("false", values["core.autocrlf"]);
        }

        [TestMethod]
        public void ParseGitConfig_PartiallyQuoted()
        {
            const string input = @"
[core ""oneQuote]
    autocrlf = ""false
";

            var values = TestParseGitConfig(input);

            Assert.AreEqual("false", values["core.oneQuote.autocrlf"]);
        }

        [TestMethod]
        public void ParseGitConfig_SampleFile()
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
    }
}
