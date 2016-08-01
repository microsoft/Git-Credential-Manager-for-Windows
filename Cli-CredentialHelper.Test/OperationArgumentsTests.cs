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
            const string input = "protocol=https\n"
                               + "host=example.visualstudio.com\n"
                               + "path=path\n"
                               + "username=userName\n"
                               + "password=incorrect\n";

            OperationArguments cut;
            using (var memory = new MemoryStream())
            using (var writer = new StreamWriter(memory))
            {
                writer.Write(input);
                writer.Flush();

                memory.Seek(0, SeekOrigin.Begin);

                cut = new OperationArguments(memory);
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

        [TestMethod]
        public void SpecialCharacters()
        {
            const string input = "protocol=https\n"
                               + "host=example.visualstudio.com\n"
                               + "path=path\n"
                               + "username=userNamể\n"
                               + "password=ḭncorrect\n";

            OperationArguments cut;
            using (var memory = new MemoryStream())
            using (var writer = new StreamWriter(memory))
            {
                writer.Write(input);
                writer.Flush();

                memory.Seek(0, SeekOrigin.Begin);

                cut = new OperationArguments(memory);
            }

            Assert.AreEqual("https", cut.QueryProtocol);
            Assert.AreEqual("example.visualstudio.com", cut.QueryHost);
            Assert.AreEqual("https://example.visualstudio.com/", cut.TargetUri.ToString());
            Assert.AreEqual("path", cut.QueryPath);
            Assert.AreEqual("userNamể", cut.CredUsername);
            Assert.AreEqual("ḭncorrect", cut.CredPassword);

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
