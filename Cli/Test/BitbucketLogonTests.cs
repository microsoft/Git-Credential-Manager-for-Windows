/**** Git Credential Manager for Windows ****
 *
 * Copyright (c) Microsoft Corporation
 * All rights reserved.
 *
 * MIT License
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the """"Software""""), to deal
 * in the Software without restriction, including without limitation the rights to
 * use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
 * the Software, and to permit persons to whom the Software is furnished to do so,
 * subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED *AS IS*, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
 * FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
 * COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN
 * AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
 * WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE."
**/

using System;
using System.IO;
using System.Text;
using Xunit;
using static System.StringComparer;

namespace Microsoft.Alm.Cli.Test
{
    public class BibucketLogonTests : Atlassian.Bitbucket.Authentication.Test.UnitTestBase
    {
        public BibucketLogonTests(Xunit.Abstractions.ITestOutputHelper output)
            : base(XunitHelper.Convert(output))
        { }

        [Fact]
        public void Logon2fa_Cancel()
        {
            const string Protocol = "https";
            const string Host = "bitbucket.org";

            InitializeTest();

            var errorBuffer = new byte[4096];
            var outputBuffer = new byte[4096];
            var program = new Program(Context);

            using (var inputStream = new MemoryStream())
            using (var outputStream = new MemoryStream(outputBuffer))
            using (var errorStream = new MemoryStream(errorBuffer))
            using (var writer = new StreamWriter(inputStream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)))
            {
                SetupProgramStandardPipes(program, inputStream, outputStream, errorStream);

                MimicGitCredential(writer, Protocol, Host);

                inputStream.Seek(0, SeekOrigin.Begin);

                program._exit = (Program p, int exitcode, string message, string path, int line, string name) =>
                {
                    Assert.Same(program, p);
                    Assert.Equal(-1, exitcode);
                    Assert.Equal(Program.LogonFailedMessage, message, Ordinal);
                    Assert.Equal(nameof(Program.Get), name, Ordinal);

                    ConsoleFunctions.Exit(program, exitcode, message, path, line, name);

                    throw new ApplicationException(message);
                };

                // We know this will throw.
                Assert.Throws<AggregateException>(() =>
                {
                    try
                    {
                        program.Get();
                    }
                    catch (AggregateException exception)
                    {
                        Assert.NotNull(exception.InnerException);

                        Trace.WriteException(exception);

                        Assert.IsType<ApplicationException>(exception.InnerException);
                        Assert.Equal(Program.LogonFailedMessage, exception.InnerException.Message, Ordinal);

                        throw exception;
                    }
                });
            }

            // Assert nothing gets written to the output stream.
            using (var stream = new MemoryStream(outputBuffer))
            using (var reader = new StreamReader(stream, Encoding.Unicode))
            {
                string content = reader.ReadToEnd();

                Assert.NotNull(content);
                Assert.NotEmpty(content);
                Assert.Equal('\0', content[0]);
            }

            // Assert the correct error message gets written to the error stream.
            using (var stream = new MemoryStream(errorBuffer))
            using (var reader = new StreamReader(stream, Encoding.Unicode))
            {
                string content = reader.ReadToEnd();

                Assert.StartsWith(Program.LogonFailedMessage, content, StringComparison.Ordinal);
            }
        }

        [Fact]
        public void Logon2fa_Success()
        {
            const string Protocol = "https";
            const string Host = "bitbucket.org";

            InitializeTest();

            var errorBuffer = new byte[4096];
            var outputBuffer = new byte[4096];
            var program = new Program(Context);

            using (var inputStream = new MemoryStream())
            using (var outputStream = new MemoryStream(outputBuffer))
            using (var errorStream = new MemoryStream(errorBuffer))
            using (var writer = new StreamWriter(inputStream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)))
            {
                SetupProgramStandardPipes(program, inputStream, outputStream, errorStream);

                MimicGitCredential(writer, Protocol, Host);

                inputStream.Seek(0, SeekOrigin.Begin);

                program._exit = (Program p, int exitcode, string message, string path, int line, string name) =>
                {
                    Assert.Same(program, p);
                };

                program.Get();
            }

            using (var stream = new MemoryStream(outputBuffer))
            using (var reader = new StreamReader(stream))
            {
                bool foundHost = false;
                bool foundPassword = false;
                bool foundProtocol = false;
                bool foundUsername = false;

                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (string.IsNullOrWhiteSpace(line))
                        break;

                    int index = line.IndexOf('=');
                    Assert.NotEqual(-1, index);

                    string name = line.Substring(0, index);
                    string value = line.Substring(index + 1);

                    switch (name)
                    {
                        case "protocol":
                        {
                            foundProtocol = true;
                            Assert.Equal(Protocol, value, Ordinal);
                        }
                        break;

                        case "host":
                        {
                            foundHost = true;
                            Assert.Equal(Host, value, Ordinal);
                        }
                        break;

                        case "path":
                        break;

                        case "username":
                        {
                            foundUsername = true;
                        }
                        break;

                        case "password":
                        {
                            foundPassword = true;
                        }
                        break;
                    }
                }

                Assert.True(foundProtocol);
                Assert.True(foundHost);
                Assert.True(foundUsername);
                Assert.True(foundPassword);
            }

            using (var stream = new MemoryStream(errorBuffer))
            using (var reader = new StreamReader(stream))
            {
                string line = reader.ReadLine();

                Assert.True(line is null || line.Length == 0 || line[0] == '\0', $"Unexpected standard error content: \"{line}\"");
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
