using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Xunit;

namespace Microsoft.Alm.Cli.Test
{
    public class OperationArgumentsTests
    {
        [Fact]
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

                cut = new OperationArguments.Impl(memory);
            }

            Assert.Equal("https", cut.QueryProtocol);
            Assert.Equal("example.visualstudio.com", cut.QueryHost);
            Assert.Equal("https://example.visualstudio.com/", cut.TargetUri.ToString());
            Assert.Equal("path", cut.QueryPath);
            Assert.Equal("userName", cut.CredUsername);
            Assert.Equal("incorrect", cut.CredPassword);

            var expected = ReadLines(input);
            var actual = ReadLines(cut.ToString());

            Assert.Equal(expected, actual);
        }

        [Fact]
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

                cut = new OperationArguments.Impl(memory);
            }

            Assert.Equal("https", cut.QueryProtocol);
            Assert.Equal("example.visualstudio.com", cut.QueryHost);
            Assert.Equal("https://example.visualstudio.com/", cut.TargetUri.ToString());
            Assert.Equal("path", cut.QueryPath);
            Assert.Equal("userNamể", cut.CredUsername);
            Assert.Equal("ḭncorrect", cut.CredPassword);

            var expected = ReadLines(input);
            var actual = ReadLines(cut.ToString());
            Assert.Equal(expected, actual);
        }

        [Fact]
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

        [Fact]
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

        [Fact]
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

        [Fact]
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

        [Fact]
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

        [Fact]
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

        [Fact]
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

        [Fact]
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

                var oparg = new OperationArguments.Impl(memory);

                Assert.NotNull(oparg);
                Assert.Equal(input.Protocol ?? string.Empty, oparg.QueryProtocol);
                Assert.Equal(input.Host ?? string.Empty, oparg.QueryHost);
                Assert.Equal(input.Path, oparg.QueryPath);
                Assert.Equal(input.Username, oparg.CredUsername);
                Assert.Equal(input.Password, oparg.CredPassword);

                // file or unc paths are treated specially
                if (oparg.QueryUri.Scheme != System.Uri.UriSchemeFile)
                {
                    Assert.Equal("/", oparg.QueryUri.AbsolutePath);
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

                var oparg = new OperationArguments.Impl(memory);
                oparg.UseHttpPath = false;

                Assert.NotNull(oparg);
                Assert.Equal(input.Protocol ?? string.Empty, oparg.QueryProtocol);
                Assert.Equal(input.Host ?? string.Empty, oparg.QueryHost);
                Assert.Equal(input.Path, oparg.QueryPath);
                Assert.Equal(input.Username, oparg.CredUsername);
                Assert.Equal(input.Password, oparg.CredPassword);

                // file or unc paths are treated specially
                if (oparg.QueryUri.Scheme != System.Uri.UriSchemeFile)
                {
                    Assert.Equal("/", oparg.QueryUri.AbsolutePath);
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

                var oparg = new OperationArguments.Impl(memory);
                oparg.UseHttpPath = true;

                Assert.NotNull(oparg);
                Assert.Equal(input.Protocol ?? string.Empty, oparg.QueryProtocol);
                Assert.Equal(input.Host ?? string.Empty, oparg.QueryHost);
                Assert.Equal(input.Path, oparg.QueryPath);
                Assert.Equal(input.Username, oparg.CredUsername);
                Assert.Equal(input.Password, oparg.CredPassword);

                // file or unc paths are treated specially
                if (oparg.QueryUri.Scheme != System.Uri.UriSchemeFile)
                {
                    Assert.Equal("/" + input.Path, oparg.QueryUri.AbsolutePath);
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
