using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace Microsoft.Alm.Git.Test
{
    /// <summary>
    /// A class to test <see cref="Configuration"/>.
    /// </summary>
    public class ConfigurationTests
    {
        [Fact]
        public void GitConfig_ParseSimple()
        {
            const string input = @"
[core]
    autocrlf = false
";

            var values = TestParseGitConfig(input);

            Assert.Equal("false", values["core.autocrlf"], StringComparer.OrdinalIgnoreCase);
        }

        [Fact]
        public void GitConfig_ParseOverwritesValues()
        {
            const string input = @"
[core]
    autocrlf = true
    autocrlf = ThisShouldBeInvalidButIgnored
    autocrlf = false
";

            var values = TestParseGitConfig(input);

            Assert.Equal("false", values["core.autocrlf"], StringComparer.OrdinalIgnoreCase);
        }

        [Fact]
        public void GitConfig_ParsePartiallyQuoted()
        {
            const string input = @"
[core ""oneQuote]
    autocrlf = ""false
";

            var values = TestParseGitConfig(input);

            Assert.Equal("false", values["core.oneQuote.autocrlf"]);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times")]
        [Fact]
        public void GitConfig_ParseSampleFile()
        {
            var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var me = this.GetType();
            var us = me.Assembly;

            using (var rs = us.GetManifestResourceStream(me, "sample.gitconfig"))
            using (var sr = new StreamReader(rs))
            {
                Configuration.ParseGitConfig(sr, values);
            }

            Assert.Equal(36, values.Count);
            Assert.Equal("\\\"C:/Utils/Compare It!/wincmp3.exe\\\" \\\"$(cygpath -w \\\"$LOCAL\\\")\\\" \\\"$(cygpath -w \\\"$REMOTE\\\")\\\"", values["difftool.cygcompareit.cmd"], StringComparer.OrdinalIgnoreCase);
            Assert.Equal("!f() { git fetch origin && git checkout -b $1 origin/master --no-track; }; f", values["alias.cob"], StringComparer.OrdinalIgnoreCase);
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

        [Fact]
        public void GitConfig_ReadThrough()
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
                var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                Configuration.ParseGitConfig(reader, dict);

                var values = new Dictionary<ConfigurationLevel, Dictionary<string, string>>();

                foreach (var level in Configuration.Levels)
                {
                    values[level] = dict;
                }

                cut = new Configuration.Impl(values);
            }

            Assert.True(cut.ContainsKey("CoRe.AuToCrLf"));
            Assert.Equal("false", cut["CoRe.AuToCrLf"], StringComparer.OrdinalIgnoreCase);

            Configuration.Entry entry;
            Assert.True(cut.TryGetEntry("core", (string)null, "autocrlf", out entry));
            Assert.Equal("false", entry.Value, StringComparer.OrdinalIgnoreCase);

            Assert.True(cut.TryGetEntry("credential", new Uri("https://microsoft.visualstudio.com"), "authority", out entry));
            Assert.Equal("AAD", entry.Value, StringComparer.OrdinalIgnoreCase);

            Assert.True(cut.TryGetEntry("credential", new Uri("https://mseng.visualstudio.com"), "authority", out entry));
            Assert.Equal("MSA", entry.Value, StringComparer.OrdinalIgnoreCase);

            Assert.True(cut.TryGetEntry("credential", new Uri("https://ntlm.visualstudio.com"), "authority", out entry));
            Assert.Equal("NTLM", entry.Value, StringComparer.OrdinalIgnoreCase);
        }
    }
}
