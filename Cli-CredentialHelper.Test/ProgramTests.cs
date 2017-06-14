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
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Microsoft.Alm.Cli.Test
{
    [TestClass]
    public class ProgramTests
    {
        [TestMethod]
        public void LoadOperationArgumentsTest()
        {
            Program._dieExceptionCallback = (Exception e) => Assert.Fail($"Error: {e.ToString()}");
            Program._dieMessageCallback = (string m) => Assert.Fail($"Error: {m}");
            Program._exitCallback = (int e, string m) => Assert.Fail($"Error: {e} {m}");

            var envvars = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "HOME", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) }
            };
            var gitconfig = new Git.Configuration.Impl();
            var targetUri = new Authentication.TargetUri("https://example.visualstudio.com/");

            Mock<OperationArguments> opargsMock = new Mock<OperationArguments>();
            opargsMock.Setup(r => r.EnvironmentVariables)
                      .Returns(envvars);
            opargsMock.Setup(r => r.GitConfiguration)
                      .Returns(gitconfig);
            opargsMock.Setup(r => r.LoadConfiguration());
            opargsMock.Setup(r => r.TargetUri)
                      .Returns(targetUri);
            opargsMock.Setup(r => r.QueryUri)
                      .Returns(targetUri);

            var opargs = opargsMock.Object;

            Program.LoadOperationArguments(opargs);

            Assert.IsNotNull(opargs);
        }

        [TestMethod]
        public void TryReadBooleanTest()
        {
            bool? yesno;

            var envvars = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "HOME", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) },
                { Program.EnvironPreserveCredentialsKey, "no" },
            };
            var gitconfig = new Git.Configuration.Impl();
            var targetUri = new Authentication.TargetUri("https://example.visualstudio.com/");

            Mock<OperationArguments> opargsMock = new Mock<OperationArguments>();
            opargsMock.Setup(r => r.EnvironmentVariables)
                      .Returns(envvars);
            opargsMock.Setup(r => r.GitConfiguration)
                      .Returns(gitconfig);
            opargsMock.Setup(r => r.LoadConfiguration());
            opargsMock.Setup(r => r.TargetUri)
                      .Returns(targetUri);
            opargsMock.Setup(r => r.QueryUri)
                      .Returns(targetUri);

            Assert.IsFalse(Program.TryReadBoolean(opargsMock.Object, "notFound", "notFound", out yesno));
            Assert.IsFalse(yesno.HasValue);

            Assert.IsTrue(Program.TryReadBoolean(opargsMock.Object, Program.ConfigPreserveCredentialsKey, Program.EnvironPreserveCredentialsKey, out yesno));
            Assert.IsTrue(yesno.HasValue);
            Assert.IsFalse(yesno.Value);

            envvars = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "HOME", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) },
                { Program.EnvironPreserveCredentialsKey, "yes" },
            };
            opargsMock.Setup(r => r.EnvironmentVariables)
                      .Returns(envvars);

            Assert.IsTrue(Program.TryReadBoolean(opargsMock.Object, Program.ConfigPreserveCredentialsKey, Program.EnvironPreserveCredentialsKey, out yesno));
            Assert.IsTrue(yesno.HasValue);
            Assert.IsTrue(yesno.Value);

            envvars = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "HOME", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) },
                { Program.EnvironPreserveCredentialsKey, string.Empty },
            };
            opargsMock.Setup(r => r.EnvironmentVariables)
                      .Returns(envvars);

            Assert.IsFalse(Program.TryReadBoolean(opargsMock.Object, Program.ConfigPreserveCredentialsKey, Program.EnvironPreserveCredentialsKey, out yesno));
            Assert.IsFalse(yesno.HasValue);
        }
    }
}
