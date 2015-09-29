using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Alm.Git
{
    internal struct GitPath : IEquatable<GitPath>
    {
        public const string Version1_32bit = @"libexec\git-core\";
        public const string Version2_32bit = @"mingw32\libexec\git-core\";
        public const string Version2_64bit = @"mingw64\libexec\git-core\";

        public GitPath(string path, string libexec)
        {
            if (String.IsNullOrEmpty(path))
                throw new ArgumentNullException("path", "The path parameter cannot be null or empty.");
            if (String.IsNullOrEmpty(libexec))
                throw new ArgumentNullException("libexec", "The libexec parameter cannot be null or empty.");

            Path = path;
            Libexec = System.IO.Path.Combine(path, libexec);
            Cmd = System.IO.Path.Combine(path, @"cmd", "git.exe");
        }

        public readonly string Path;
        public readonly string Libexec;
        public readonly string Cmd;

        public override bool Equals(object obj)
        {
            if (!(obj is GitPath))
                return false;

            return this == (GitPath)obj;
        }

        public bool Equals(GitPath other)
        {
            return this == other;
        }

        public override int GetHashCode()
        {
            return Path.GetHashCode();
        }

        public static bool operator ==(GitPath path1, GitPath path2)
        {
            return String.Equals(path1.Libexec, path2.Libexec, StringComparison.OrdinalIgnoreCase);
        }

        public static bool operator !=(GitPath path1, GitPath path2)
        {
            return !(path1 == path2);
        }
    }
}
