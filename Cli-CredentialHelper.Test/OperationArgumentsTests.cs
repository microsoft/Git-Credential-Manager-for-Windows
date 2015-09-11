using System.Collections;
using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.TeamFoundation.CredentialHelper.Test
{
    [TestClass]
    public class OperationArgumentsTests
    {
        [TestMethod]
        public void Typical()
        {
            const string input = @"protocol=https
host=example.visualstudio.com
path=path
username=userName
password=incorrect
";
            OperationArguments cut;
            using (var sr = new StringReader(input))
            {
                cut = new OperationArguments(sr);
            }

            Assert.AreEqual("https", cut.Protocol);
            Assert.AreEqual("example.visualstudio.com", cut.Host);
            Assert.AreEqual("https://example.visualstudio.com/", cut.TargetUri.ToString());
            Assert.AreEqual("path", cut.Path);
            Assert.AreEqual("userName", cut.Username);
            Assert.AreEqual("incorrect", cut.Password);

            var expected = ReadLines(input);
            var actual = ReadLines(cut.ToString());
            CollectionAssert.AreEqual(expected, actual);
        }

        private static ICollection ReadLines(string input)
        {
            var result = new List<string>();
            using (var sr = new StringReader(input))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    result.Add(line);
                }
            }
            return result;
        }
    }
}