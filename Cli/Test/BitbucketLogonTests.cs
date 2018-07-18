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
    public class BibucketLogonTests : Authentication.Test.UnitTestBase
    {
        public BibucketLogonTests(Xunit.Abstractions.ITestOutputHelper output)
            : base(XunitHelper.Convert(output))
        { }

        [Fact(Skip = "No modal proxy available")]
        public void Logon2fa_Success()
        {
            InitializeTest();

            var errorBuffer = new byte[4096];
            var outputBuffer = new byte[4096];
            var program = new Program(Context);

            using (var errorStream = new MemoryStream(errorBuffer))
            using (var inputStream = new MemoryStream())
            using (var outputStream = new MemoryStream(outputBuffer))
            using (var writer = new StreamWriter(inputStream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false)))
            {
                writer.Write("protocol=https");
                writer.Write("\n");
                writer.Write("host=bitbucket.org");
                writer.Write("\n");
                writer.Write("username=whoisj");
                writer.Write("\n\n");

                writer.Flush();

                inputStream.Seek(0, SeekOrigin.Begin);

                program._exit = (Program p, int exitcode, string message, string path, int line, string name) =>
                {
                    Assert.Same(program, p);
                };
                program._openStandardErrorStream = (Program p) =>
                {
                    Assert.Same(program, p);

                    return errorStream;
                };
                program._openStandardInputStream = (Program p) =>
                {
                    Assert.Same(program, p);

                    return inputStream;
                };
                program._openStandardOutputStream = (Program p) =>
                {
                    Assert.Same(program, p);

                    return outputStream;
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
                            Assert.Equal("https", value, Ordinal);
                        }
                        break;

                        case "host":
                        {
                            foundHost = true;
                            Assert.Equal("github.com", value, Ordinal);
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
    }
}
