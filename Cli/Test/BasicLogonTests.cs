using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Alm.Authentication;
using Xunit;
using static System.StringComparer;

namespace Microsoft.Alm.Cli.Test
{
    public class BasicLogonTests : Authentication.Test.UnitTestBase
    {
        private static readonly Encoding Utf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);

        public BasicLogonTests(Xunit.Abstractions.ITestOutputHelper output)
            : base(XunitHelper.Convert(output))
        { }

        [Fact]
        public void EnvironmentUrlOverride()
        {
            const string protocol = "https";
            const string host = "microsoft-git-tools.visualstudio.com";
            const string urlOverride = protocol + "://github.com";

            Environment.SetEnvironmentVariable("GCM_URL_OVERRIDE", urlOverride, EnvironmentVariableTarget.Process);

            InitializeTest();

            var errorBuffer = new byte[4096];
            var outputBuffer = new byte[4096];
            var program = new Program(Context);

            using (var inputStream = new MemoryStream())
            using (var outputStream = new MemoryStream(outputBuffer))
            using (var errorStream = new MemoryStream(errorBuffer))
            using (var writer = new StreamWriter(inputStream, Utf8))
            {
                SetupProgramStandardPipes(program, inputStream, outputStream, errorStream);

                MimicGitCredential(writer, protocol, host);

                inputStream.Seek(0, SeekOrigin.Begin);

                program._exit = (Program p, int exitcode, string message, string path, int line, string name) =>
                    {
                        Assert.Same(program, p);
                        Assert.Equal(-1, exitcode);
                        Assert.Equal(Program.LogonFailedMessage, message, Ordinal);
                    };
                program._queryCredentials = (Program p, OperationArguments opArgs) =>
                    {
                        Assert.Same(program, p);
                        Assert.NotNull(opArgs);
                        Assert.NotNull(opArgs.UrlOverride);
                        Assert.NotNull(opArgs.TargetUri);

                        Assert.Equal(urlOverride, opArgs.UrlOverride, OrdinalIgnoreCase);

                        var actualUrl = opArgs.TargetUri.ActualUri?.ToString()?.TrimEnd('/');
                        var queryUrl = opArgs.TargetUri.QueryUri.ToString().TrimEnd('/');

                        Assert.Equal(urlOverride, actualUrl, OrdinalIgnoreCase);
                        Assert.Equal(protocol + "://" + host, queryUrl, OrdinalIgnoreCase);

                        return Task.FromResult<Credential>(null);
                    };

                program.Get();
            }
        }

        private static void MimicGitCredential(TextWriter writer, string protocol, string host)
        {
            writer.Write("protocol=");
            writer.Write(protocol);
            writer.Write("\n");
            writer.Write("host=");
            writer.Write(host);
            writer.Write("\n");
            writer.Write("\n");

            writer.Flush();
        }

        private static void SetupProgramStandardPipes(Program program, Stream standardInput, Stream standardOutput, Stream standardError)
        {
            program._openStandardErrorStream = (Program p) =>
            {
                Assert.Same(program, p);

                return standardError;
            };
            program._openStandardInputStream = (Program p) =>
            {
                Assert.Same(program, p);

                return standardInput;
            };
            program._openStandardOutputStream = (Program p) =>
            {
                Assert.Same(program, p);

                return standardOutput;
            };
            program._write = (Program p, string message) =>
            {
                Assert.Same(program, p);

                var buffer = Encoding.Unicode.GetBytes(message);
                standardError.Write(buffer, 0, buffer.Length);
            };
            program._writeLine = (Program p, string message) =>
            {
                Assert.Same(program, p);

                var buffer = Encoding.Unicode.GetBytes(message + Environment.NewLine);
                standardError.Write(buffer, 0, buffer.Length);
            };
        }
    }
}
