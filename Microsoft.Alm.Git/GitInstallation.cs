using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

namespace Microsoft.Alm.Git
{
    public struct GitInstallation : IEquatable<GitInstallation>
    {
        internal const string AllVersionCmdPath = @"cmd\git.exe";
        internal const string Version1Config32Path = @"etc\gitconfig";
        internal const string Version2Config32Path = @"mingw32\etc\gitconfig";
        internal const string Version2Config64Path = @"mingw64\etc\gitconfig";
        internal const string Version1Libexec32Path = @"libexec\git-core\";
        internal const string Version2Libexec32Path = @"mingw32\libexec\git-core";
        internal const string Version2Libexec64Path = @"mingw64\libexec\git-core";

        public static readonly IReadOnlyDictionary<KnownGitDistribution, string> CommonConfigPaths
            = new Dictionary<KnownGitDistribution, string>
            {
                { KnownGitDistribution.GitForWindows32v1, Version1Config32Path },
                { KnownGitDistribution.GitForWindows32v2, Version2Config32Path },
                { KnownGitDistribution.GitForWindows64v2, Version2Config64Path },
            };
        public static readonly IReadOnlyDictionary<KnownGitDistribution, string> CommonCmdPaths
            = new Dictionary<KnownGitDistribution, string>
            {
                { KnownGitDistribution.GitForWindows32v1, AllVersionCmdPath },
                { KnownGitDistribution.GitForWindows32v2, AllVersionCmdPath },
                { KnownGitDistribution.GitForWindows64v2, AllVersionCmdPath },
            };
        public static readonly IReadOnlyDictionary<KnownGitDistribution, string> CommonLibexecPaths
            = new Dictionary<KnownGitDistribution, string>
            {
                { KnownGitDistribution.GitForWindows32v1, Version1Libexec32Path },
                { KnownGitDistribution.GitForWindows32v2, Version2Libexec32Path },
                { KnownGitDistribution.GitForWindows64v2, Version2Libexec64Path },
            };

        internal GitInstallation(string path, KnownGitDistribution version)
        {
            Debug.Assert(!String.IsNullOrWhiteSpace(path), "The `path` parameter is null or invalid.");
            Debug.Assert(CommonConfigPaths.ContainsKey(version), "The `version` parameter not found in `CommonConfigPaths`.");
            Debug.Assert(CommonCmdPaths.ContainsKey(version), "The `version` parameter not found in `CommonCmdPaths`.");
            Debug.Assert(CommonLibexecPaths.ContainsKey(version), "The `version` parameter not found in `CommonLibExecPaths`.");

            // trim off trailing '\' characters to increase compatibility
            path = path.TrimEnd('\\');

            Config = System.IO.Path.Combine(path, CommonConfigPaths[version]);
            Cmd = System.IO.Path.Combine(path, CommonCmdPaths[version]);
            Libexec = System.IO.Path.Combine(path, CommonLibexecPaths[version]);
            Path = path;
            Version = version;
        }

        public readonly string Config;
        public readonly string Cmd;
        public readonly string Libexec;
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
            return Path.GetHashCode();
        }

        public override String ToString()
        {
            return Path;
        }

        internal static bool IsValid(GitInstallation value)
        {
            return Directory.Exists(value.Path)
                && Directory.Exists(value.Libexec)
                && File.Exists(value.Cmd);
        }

        public static bool operator ==(GitInstallation install1, GitInstallation install2)
        {
            return install1.Version == install2.Version
                && String.Equals(install1.Path, install2.Path, StringComparison.OrdinalIgnoreCase);
        }

        public static bool operator !=(GitInstallation install1, GitInstallation install2)
        {
            return !(install1 == install2);
        }
    }
}
