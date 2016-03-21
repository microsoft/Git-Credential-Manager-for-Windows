using System.Collections;
using System.Collections.Generic;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Alm.CredentialHelper.Test
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

            Assert.AreEqual("https", cut.QueryProtocol);
            Assert.AreEqual("example.visualstudio.com", cut.QueryHost);
            Assert.AreEqual("https://example.visualstudio.com/", cut.TargetUri.ToString());
            Assert.AreEqual("path", cut.QueryPath);
            Assert.AreEqual("userName", cut.CredUsername);
            Assert.AreEqual("incorrect", cut.CredPassword);

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
