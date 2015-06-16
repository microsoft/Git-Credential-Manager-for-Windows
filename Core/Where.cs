using System;
using System.IO;
using System.Linq;

namespace Microsoft.TeamFoundation.Git.Helpers
{
    public static class Where
    {
        static public bool App(string name, out string path)
        {
            string pathext = Environment.GetEnvironmentVariable("PATHEXT");
            string envpath = Environment.GetEnvironmentVariable("PATH");

            string[] exts = pathext.Split(';');
            string[] paths = envpath.Split(';');

            for (int i = 0; i < paths.Length; i++)
            {
                if (String.IsNullOrWhiteSpace(paths[i]))
                    continue;

                for (int j = 0; j < exts.Length; j++)
                {
                    if (String.IsNullOrWhiteSpace(exts[j]))
                        continue;

                    string value = String.Format("{0}\\{1}{2}", paths[i], name, exts[j]);
                    if (File.Exists(value))
                    {
                        path = value;
                        return true;
                    }
                }
            }

            path = null;
            return false;
        }

        public static bool GitSystemConfig(out string path)
        {
            if (App("git", out path))
            {
                FileInfo gitInfo = new FileInfo(path);
                DirectoryInfo dir = gitInfo.Directory;
                if (dir.Parent != null)
                {
                    dir = dir.Parent;
                }

                var file = dir.EnumerateFiles("gitconfig", SearchOption.AllDirectories).FirstOrDefault();
                if (file != null && file.Exists)
                {
                    path = file.FullName;
                    return true;
                }
            }

            path = null;
            return false;
        }
    }
}
