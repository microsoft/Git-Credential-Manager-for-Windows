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
using System.IO;

namespace Microsoft.Alm.Git
{
    public struct GitInstallation : IEquatable<GitInstallation>
    {
        public static readonly StringComparer PathComparer = StringComparer.InvariantCultureIgnoreCase;

        internal const string GitExeName = @"git.exe";
        internal const string AllVersionCmdPath = @"cmd";
        internal const string AllVersionGitPath = @"cmd\" + GitExeName;
        internal const string AllVersionShPath = @"bin\sh.exe";
        internal const string AllVersionBinGitPath = @"bin\" + GitExeName;
        internal const string Version1Config32Path = @"etc\gitconfig";
        internal const string Version2Config32Path = @"mingw32\etc\gitconfig";
        internal const string Version2Config64Path = @"mingw64\etc\gitconfig";
        internal const string Version1Libexec32Path = @"libexec\git-core\";
        internal const string Version2Libexec32Path = @"mingw32\libexec\git-core";
        internal const string Version2Libexec64Path = @"mingw64\libexec\git-core";

        public static readonly IReadOnlyDictionary<KnownGitDistribution, string> CommonCmdPaths
           = new Dictionary<KnownGitDistribution, string>
           {
                { KnownGitDistribution.GitForWindows32v1, AllVersionCmdPath },
                { KnownGitDistribution.GitForWindows32v2, AllVersionCmdPath },
                { KnownGitDistribution.GitForWindows64v2, AllVersionCmdPath },
           };
        public static readonly IReadOnlyDictionary<KnownGitDistribution, string> CommonConfigPaths
            = new Dictionary<KnownGitDistribution, string>
            {
                { KnownGitDistribution.GitForWindows32v1, Version1Config32Path },
                { KnownGitDistribution.GitForWindows32v2, Version2Config32Path },
                { KnownGitDistribution.GitForWindows64v2, Version2Config64Path },
            };
        public static readonly IReadOnlyDictionary<KnownGitDistribution, string> CommonGitPaths
            = new Dictionary<KnownGitDistribution, string>
            {
                { KnownGitDistribution.GitForWindows32v1, AllVersionGitPath },
                { KnownGitDistribution.GitForWindows32v2, AllVersionGitPath },
                { KnownGitDistribution.GitForWindows64v2, AllVersionGitPath },
            };
        public static readonly IReadOnlyDictionary<KnownGitDistribution, string> CommonLibexecPaths
            = new Dictionary<KnownGitDistribution, string>
            {
                { KnownGitDistribution.GitForWindows32v1, Version1Libexec32Path },
                { KnownGitDistribution.GitForWindows32v2, Version2Libexec32Path },
                { KnownGitDistribution.GitForWindows64v2, Version2Libexec64Path },
            };
        public static readonly IReadOnlyDictionary<KnownGitDistribution, string> CommonShPaths
            = new Dictionary<KnownGitDistribution, string>
            {
                { KnownGitDistribution.GitForWindows32v1, AllVersionShPath },
                { KnownGitDistribution.GitForWindows32v2, AllVersionShPath },
                { KnownGitDistribution.GitForWindows64v2, AllVersionShPath },
            };

        internal GitInstallation(string path, KnownGitDistribution version)
        {
            Debug.Assert(!String.IsNullOrWhiteSpace(path), "The `path` parameter is null or invalid.");
            Debug.Assert(CommonConfigPaths.ContainsKey(version), "The `version` parameter not found in `CommonConfigPaths`.");
            Debug.Assert(CommonCmdPaths.ContainsKey(version), "The `version` parameter not found in `CommonCmdPaths`.");
            Debug.Assert(CommonGitPaths.ContainsKey(version), "The `version` parameter not found in `CommonGitPaths`.");
            Debug.Assert(CommonLibexecPaths.ContainsKey(version), "The `version` parameter not found in `CommonLibExecPaths`.");
            Debug.Assert(CommonShPaths.ContainsKey(version), "The `version` parameter not found in `CommonShPaths`.");

            path = path.TrimEnd('\\');

            // Make sure the GitExeName isn't included as a part of the path.
            if (path.EndsWith(AllVersionGitPath, StringComparison.InvariantCultureIgnoreCase))
            {
                path = path.Substring(0, path.Length - AllVersionGitPath.Length);
            }

            // Versions of git installation could have 2 binaries. One in the `bin` directory
            // and the other in the `cmd` directory. Handle both scenarios.

            if (path.EndsWith(AllVersionBinGitPath, StringComparison.InvariantCultureIgnoreCase))
            {
                path = path.Substring(0, path.Length - AllVersionBinGitPath.Length);
            }

            if (path.EndsWith(GitExeName, StringComparison.InvariantCultureIgnoreCase))
            {
                path = path.Substring(0, path.Length - GitExeName.Length);
            }

            // trim off trailing '\' characters to increase compatibility
            path = path.TrimEnd('\\');

            Path = path;
            Version = version;
            _cmd = null;
            _config = null;
            _git = null;
            _libexec = null;
            _sh = null;
        }

        public string Config
        {
            get
            {
                if (_config == null)
                {
                    _config = System.IO.Path.Combine(Path, CommonConfigPaths[Version]);
                }
                return _config;
            }
        }
        private string _config;
        public string Cmd
        {
            get
            {
                if (_cmd == null)
                {
                    _cmd = System.IO.Path.Combine(Path, CommonCmdPaths[Version]);
                }
                return _cmd;
            }
        }
        private string _cmd;
        public string Git
        {
            get
            {
                if (_git == null)
                {
                    _git = System.IO.Path.Combine(Path, CommonGitPaths[Version]);
                }
                return _git;
            }
        }
        private string _git;
        public string Libexec
        {
            get
            {
                if (_libexec == null)
                {
                    _libexec = System.IO.Path.Combine(Path, CommonLibexecPaths[Version]);
                }
                return _libexec;
            }
        }
        private string _libexec;
        public string Sh
        {
            get
            {
                if (_sh == null)
                {
                    _sh = System.IO.Path.Combine(Path, CommonShPaths[Version]);
                }
                return _sh;
            }
        }
        private string _sh;
        public readonly string Path;
        public readonly KnownGitDistribution Version;

        public override bool Equals(object obj)
        {
            if (obj is GitInstallation)
                return this == (GitInstallation)obj;

            return false;
        }

        public bool Equals(GitInstallation other)
        {
            return this == other;
        }

        public override int GetHashCode()
        {
            return Path.ToLowerInvariant()
                       .GetHashCode();
        }

        public override String ToString()
        {
            return Path;
        }

        internal static bool IsValid(GitInstallation value)
        {
            return Directory.Exists(value.Path)
                && Directory.Exists(value.Libexec)
                && File.Exists(value.Git);
        }

        public static bool operator ==(GitInstallation install1, GitInstallation install2)
        {
            return install1.Version == install2.Version
                && PathComparer.Equals(install1.Path, install2.Path);
        }

        public static bool operator !=(GitInstallation install1, GitInstallation install2)
        {
            return !(install1 == install2);
        }
    }
}
