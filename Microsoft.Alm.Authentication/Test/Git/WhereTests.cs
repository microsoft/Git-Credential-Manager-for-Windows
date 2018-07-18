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
using System.Diagnostics;
using Microsoft.Alm.Authentication.Test;
using Xunit;
using static System.StringComparer;

namespace Microsoft.Alm.Authentication.Git.Test
{
    public class WhereTests : UnitTestBase
    {
        private static readonly StringComparer PathComparer = OrdinalIgnoreCase;

        public WhereTests(Xunit.Abstractions.ITestOutputHelper output)
             : base(XunitHelper.Convert(output))
        { }

        public static object[ ][ ] FindAppData
        {
            get
            {
                int iteration = 0;

                return new object[ ][ ]
                {
                    new object[] { ++iteration, "cmd" },
                    new object[] { ++iteration, "calc" },
                    new object[] { ++iteration, "powershell" },
                    new object[] { ++iteration, "git" },
                };
            }
        }

        [Theory]
        [MemberData(nameof(FindAppData), DisableDiscoveryEnumeration = false)]
        public void Where_FindApp(int iteration, string app)
        {
            InitializeTest(iteration);

            bool expected = CmdWhere(app, out string path1);
            bool actual = Where.FindApp(app, out string path2);

            Assert.Equal(expected, actual);

            Assert.Equal(path1, path2, PathComparer);
        }

        [Fact]
        public void Where_FindGit()
        {
            InitializeTest();

            if (!Where.FindApp("git", out string gitPath))
                throw new Exception("Git not found on system");

            Assert.True(Where.FindGitInstallations(out List<Installation> installations));
            Assert.True(installations.Count > 0);
            Assert.Equal(installations[0].Git, gitPath, PathComparer);

            Installation installation;
            Assert.True(Where.FindGitInstallation(installations[0].Path, installations[0].Version, out installation));
            Assert.Equal(installation, installations[0], Installation.Comparer);
        }

        private bool CmdWhere(string app, out string path)
        {
            const string ExtendedDataSetName = nameof(WhereTests) + "_" + nameof(CmdWhere);

            if (TestMode == UnitTestMode.Replay)
            {
                if (!Proxy.Data.ExtendedData.TryGetValue(ExtendedDataSetName, out var dataset)
                    || !dataset.TryGetValue(app, out path))
                {
                    path = null;
                }
            }
            else
            {
                path = null;

                var startInfo = new ProcessStartInfo
                {
                    Arguments = "/c where " + app,
                    CreateNoWindow = true,
                    FileName = "cmd",
                    RedirectStandardError = true,
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                };

                using (var process = Process.Start(startInfo))
                {
                    if (process.WaitForExit(3000))
                    {
                        path = process.StandardOutput.ReadLine();
                        path = path.Trim();
                    }
                }

                if (TestMode == UnitTestMode.Capture)
                {
                    if (!Proxy.Data.ExtendedData.TryGetValue(ExtendedDataSetName, out var dataset))
                    {
                        dataset = new Dictionary<string, string>(Ordinal);

                        Proxy.Data.ExtendedData.Add(ExtendedDataSetName, dataset);
                    }

                    dataset[app] = path;
                }
            }

            return path != null;
        }
    }
}
