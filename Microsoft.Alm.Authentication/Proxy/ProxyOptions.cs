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
using System.IO;
using static System.FormattableString;
using static System.StringComparer;

namespace Microsoft.Alm.Authentication.Test
{
    [System.Diagnostics.DebuggerDisplay("{DebuggerDisplay, nq}")]
    public class ProxyOptions
    {
        public ProxyOptions(ProxyMode mode, string solutionDirectory, string projectDirectory)
        {
            if (mode != ProxyMode.DataCapture && mode != ProxyMode.DataPassthrough && mode != ProxyMode.DataReplay)
                throw new ArgumentOutOfRangeException(nameof(mode));
            if (string.IsNullOrEmpty(solutionDirectory))
                throw new ArgumentNullException(nameof(solutionDirectory));
            if (string.IsNullOrEmpty(projectDirectory))
                throw new ArgumentNullException(nameof(projectDirectory));
            if (mode != ProxyMode.DataReplay && !Directory.Exists(solutionDirectory))
            {
                var inner = new DirectoryNotFoundException(solutionDirectory);
                throw new ArgumentException(inner.Message, nameof(solutionDirectory), inner);
            }
            if (mode != ProxyMode.DataReplay && !Directory.Exists(projectDirectory))
            {
                while (projectDirectory.Length > 0
                    && (projectDirectory[0] == '\\' || projectDirectory[0] == '/'))
                {
                    projectDirectory = projectDirectory.Remove(0, 1);
                }

                projectDirectory = Path.Combine(solutionDirectory, projectDirectory);

                if (!Directory.Exists(projectDirectory))
                {
                    var inner = new DirectoryNotFoundException(projectDirectory);
                    throw new ArgumentException(inner.Message, nameof(projectDirectory), inner);
                }
            }

            Mode = mode;
            ProjectDirectory = projectDirectory;
            SolutionDirectory = solutionDirectory;
        }

        public string FauxHomePath { get; set; } = UnitTestBase.FauxDataHomePath;

        public string FauxPrefixPath { get; set; } = UnitTestBase.FauxDataSolutionPath;

        public string FauxResultPath { get; set; } = UnitTestBase.FauxDataResultPath;

        public ProxyMode Mode { get; }

        public string ProjectDirectory { get; }

        public HashSet<string> ResultPaths { get; } = new HashSet<string>(OrdinalIgnoreCase);

        public string SolutionDirectory { get; }

        internal string DebuggerDisplay
        {
            get { return Invariant($"{nameof(ProxyOptions)}: {Mode}[{ResultPaths?.Count}]"); }
        }
    }
}
