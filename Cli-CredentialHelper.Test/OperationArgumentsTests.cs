using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Alm.Cli.Test
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

        [TestMethod]
        public void CreateTargetUriGitHubSimple()
        {
            var input = new InputArg()
            {
                Protocol = "https",
                Host = "github.com",
            };

            CreateTargetUriTestDefault(input);
            CreateTargetUriTestSansPath(input);
            CreateTargetUriTestWithPath(input);
        }

        [TestMethod]
        public void CreateTargetUri_VstsSimple()
        {
            var input = new InputArg()
            {
                Protocol = "https",
                Host = "team.visualstudio.com",
            };

            CreateTargetUriTestDefault(input);
            CreateTargetUriTestSansPath(input);
            CreateTargetUriTestWithPath(input);
        }

        [TestMethod]
        public void CreateTargetUriGitHubComplex()
        {
            var input = new InputArg()
            {
                Protocol = "https",
                Host = "github.com",
                Path = "Microsoft/Git-Credential-Manager-for-Windows.git"
            };

            CreateTargetUriTestDefault(input);
            CreateTargetUriTestSansPath(input);
            CreateTargetUriTestWithPath(input);
        }

        [TestMethod]
        public void CreateTargetUriWithPortNumber()
        {
            var input = new InputArg()
            {
                Protocol = "https",
                Host = "onpremis:8080",
            };

            CreateTargetUriTestDefault(input);
            CreateTargetUriTestSansPath(input);
            CreateTargetUriTestWithPath(input);
        }

        [TestMethod]
        public void CreateTargetUriComplexAndMessy()
        {
            var input = new InputArg()
            {
                Protocol = "https",
                Host = "foo.bar.com:8181",
                Path = "this-is/a/path%20with%20spaces",
            };

            CreateTargetUriTestDefault(input);
            CreateTargetUriTestSansPath(input);
            CreateTargetUriTestWithPath(input);
        }

        [TestMethod]
        public void CreateTargetUriWithCredentials()
        {
            var input = new InputArg()
            {
                Protocol = "http",
                Host = "insecure.com",
                Username = "naive",
                Password = "password",
            };

            CreateTargetUriTestDefault(input);
            CreateTargetUriTestSansPath(input);
            CreateTargetUriTestWithPath(input);
        }

        [TestMethod]
        public void CreateTargetUriUnc()
        {
            var input = new InputArg()
            {
                Protocol = "file",
                Host = "unc",
                Path = "server/path",
            };

            CreateTargetUriTestDefault(input);
            CreateTargetUriTestSansPath(input);
            CreateTargetUriTestWithPath(input);
        }

        [TestMethod]
        public void CreateTargetUriUncColloquial()
        {
            var input = new InputArg()
            {
                Host = @"\\windows\has\weird\paths",
            };

            CreateTargetUriTestDefault(input);
            CreateTargetUriTestSansPath(input);
            CreateTargetUriTestWithPath(input);
        }

        private void CreateTargetUriTestDefault(InputArg input)
        {
            using (var memory = new MemoryStream())
            using (var writer = new StreamWriter(memory))
            {
                writer.Write(input.ToString());
                writer.Flush();

                memory.Seek(0, SeekOrigin.Begin);

                var oparg = new OperationArguments(memory);

                Assert.IsNotNull(oparg);
                Assert.AreEqual(input.Protocol ?? string.Empty, oparg.QueryProtocol);
                Assert.AreEqual(input.Host ?? string.Empty, oparg.QueryHost);
                Assert.AreEqual(input.Path, oparg.QueryPath);
                Assert.AreEqual(input.Username, oparg.CredUsername);
                Assert.AreEqual(input.Password, oparg.CredPassword);

                // file or unc paths are treated specially
                if (oparg.QueryUri.Scheme != "file")
                {
                    Assert.AreEqual("/", oparg.QueryUri.AbsolutePath);
                }
            }
        }

        private void CreateTargetUriTestSansPath(InputArg input)
        {
            using (var memory = new MemoryStream())
            using (var writer = new StreamWriter(memory))
            {
                writer.Write(input.ToString());
                writer.Flush();

                memory.Seek(0, SeekOrigin.Begin);

                var oparg = new OperationArguments(memory);
                oparg.UseHttpPath = false;

                Assert.IsNotNull(oparg);
                Assert.AreEqual(input.Protocol ?? string.Empty, oparg.QueryProtocol);
                Assert.AreEqual(input.Host ?? string.Empty, oparg.QueryHost);
                Assert.AreEqual(input.Path, oparg.QueryPath);
                Assert.AreEqual(input.Username, oparg.CredUsername);
                Assert.AreEqual(input.Password, oparg.CredPassword);

                // file or unc paths are treated specially
                if (oparg.QueryUri.Scheme != "file")
                {
                    Assert.AreEqual("/", oparg.QueryUri.AbsolutePath);
                }
            }
        }

        private void CreateTargetUriTestWithPath(InputArg input)
        {
            using (var memory = new MemoryStream())
            using (var writer = new StreamWriter(memory))
            {
                writer.Write(input.ToString());
                writer.Flush();

                memory.Seek(0, SeekOrigin.Begin);

                var oparg = new OperationArguments(memory);
                oparg.UseHttpPath = true;

                Assert.IsNotNull(oparg);
                Assert.AreEqual(input.Protocol ?? string.Empty, oparg.QueryProtocol);
                Assert.AreEqual(input.Host ?? string.Empty, oparg.QueryHost);
                Assert.AreEqual(input.Path, oparg.QueryPath);
                Assert.AreEqual(input.Username, oparg.CredUsername);
                Assert.AreEqual(input.Password, oparg.CredPassword);

                // file or unc paths are treated specially
                if (oparg.QueryUri.Scheme != "file")
                {
                    Assert.AreEqual("/" + input.Path, oparg.QueryUri.AbsolutePath);
                }
            }
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

        private struct InputArg
        {
            public string Protocol;
            public string Host;
            public string Path;
            public string Username;
            public string Password;

            public override string ToString()
            {
                StringBuilder sb = new StringBuilder();

                sb.Append("protocol=").Append(Protocol).Append("\n");
                sb.Append("host=").Append(Host).Append("\n");

                if (Path != null)
                {
                    sb.Append("path=").Append(Path).Append("\n");
                }
                if (Username != null)
                {
                    sb.Append("username=").Append(Username).Append("\n");
                }
                if (Password != null)
                {
                    sb.Append("password=").Append(Password).Append("\n");
                }

                sb.Append("\n");

                return sb.ToString();
            }
        }
    }
}
