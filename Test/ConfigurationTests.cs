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
