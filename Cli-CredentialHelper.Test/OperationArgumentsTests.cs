using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.Alm.Authentication;
using Xunit;

namespace Microsoft.Alm.Cli.Test
{
    public class OperationArgumentsTests
    {
        [Fact]
        public void Typical()
        {
            var input = new InputArg
            {
                Host = "example.visualstudio.com",
                Password = "incorrect",
                Path = "path",
                Protocol = "https",
                Username = "userName",
            };

            OperationArguments cut;
            using (var memory = new MemoryStream())
            using (var writer = new StreamWriter(memory))
            {
                writer.Write(input.ToString());
                writer.Flush();

                memory.Seek(0, SeekOrigin.Begin);

                cut = new OperationArguments(RuntimeContext.Default, memory);
            }

            Assert.Equal(input.Protocol, cut.QueryProtocol);
            Assert.Equal(input.Host, cut.QueryHost);
            Assert.Equal(input.Path, cut.QueryPath);
            Assert.Equal(input.Username, cut.Username);
            Assert.Equal(input.Password, cut.Password);

            Assert.Equal("https://userName@example.visualstudio.com/path", cut.TargetUri.ToString());

            var expected = ReadLines(input.ToString());
            var actual = ReadLines(cut.ToString());

            Assert.Equal(expected, actual);
        }

        [Fact]
        public void SpecialCharacters()
        {
            var input = new InputArg
            {
                Host = "example.visualstudio.com",
                Password = "ḭncorrect",
                Path = "path",
                Protocol = Uri.UriSchemeHttps,
                Username = "userNamể"
            };

            OperationArguments cut;
            using (var memory = new MemoryStream())
            using (var writer = new StreamWriter(memory))
            {
                writer.Write(input.ToString());
                writer.Flush();

                memory.Seek(0, SeekOrigin.Begin);

                cut = new OperationArguments(RuntimeContext.Default, memory);
            }

            Assert.Equal(input.Protocol, cut.QueryProtocol, StringComparer.Ordinal);
            Assert.Equal(input.Host, cut.QueryHost, StringComparer.Ordinal);
            Assert.Equal(input.Path, cut.QueryPath, StringComparer.Ordinal);
            Assert.Equal(input.Username, cut.Username, StringComparer.Ordinal);
            Assert.Equal(input.Password, cut.Password, StringComparer.Ordinal);

            Assert.Equal("https://userNamể@example.visualstudio.com/path", cut.TargetUri.ToString(), StringComparer.Ordinal);

            var expected = input.ToString();
            var actual = cut.ToString();
            Assert.Equal(expected, actual, StringComparer.Ordinal);
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
        }

        [Fact]
        public void CreateTargetUriUncColloquial()
        {
            var input = new InputArg()
            {
                Host = @"\\windows\has\weird\paths",
            };

            CreateTargetUriTestDefault(input);
        }

        private void CreateTargetUriTestDefault(InputArg input)
        {
            using (var memory = new MemoryStream())
            using (var writer = new StreamWriter(memory))
            {
                writer.Write(input.ToString());
                writer.Flush();

                memory.Seek(0, SeekOrigin.Begin);

                var oparg = new OperationArguments(RuntimeContext.Default, memory);

                Assert.NotNull(oparg);
                Assert.Equal(input.Protocol ?? string.Empty, oparg.QueryProtocol, StringComparer.Ordinal);
                Assert.Equal(input.Host ?? string.Empty, oparg.QueryHost, StringComparer.Ordinal);
                Assert.Equal(input.Path, oparg.QueryPath, StringComparer.Ordinal);
                Assert.Equal(input.Username, oparg.Username, StringComparer.Ordinal);
                Assert.Equal(input.Password, oparg.Password, StringComparer.Ordinal);
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
