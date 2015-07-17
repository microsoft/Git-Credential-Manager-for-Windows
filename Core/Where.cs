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

        public static bool GitGlobalConfig(out string path)
        {
            const string GlobalConfigFileName = ".gitconfig";

            path = null;

            var globalPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), GlobalConfigFileName);

            if (File.Exists(globalPath))
            {
                path = globalPath;
            }

            return path != null;
        }

        public static bool GitLocalConfig(string startingDirectory, out string path)
        {
            const string GitOdbFolderName = ".git";
            const string LocalConfigFileName = "config";

            path = null;

            var dir = new DirectoryInfo(startingDirectory);
            Func<DirectoryInfo, FileSystemInfo> hasOdb = (DirectoryInfo info) =>
            {
                return info.EnumerateFileSystemInfos()
                           .Where((FileSystemInfo sub) =>
                           {
                               return String.Equals(sub.Name, GitOdbFolderName, StringComparison.OrdinalIgnoreCase);
                           })
                           .FirstOrDefault();
            };

            FileSystemInfo result = null;
            while (dir.Exists && dir.Parent.Exists)
            {
                if ((result = hasOdb(dir)) != null)
                    break;

                dir = dir.Parent;
            }

            if (result !=null && result.Exists)
            {
                if (result is DirectoryInfo)
                {
                    var localPath = Path.Combine(result.FullName, LocalConfigFileName);
                    if (File.Exists(localPath))
                    {
                        path = localPath;
                    }
                }
                else
                {
                    var content = File.ReadAllText(result.FullName);

                    // TODO: handle .git file redirect
                }
            }

            return path != null;
        }
        public static bool GitLocalConfig(out string path)
        {
            return GitLocalConfig(Environment.CurrentDirectory, out path);
        }

        public static bool GitSystemConfig(out string path)
        {
            const string SystemConfigFileName = "gitconfig";

            if (App("git", out path))
            {
                FileInfo gitInfo = new FileInfo(path);
                DirectoryInfo dir = gitInfo.Directory;
                if (dir.Parent != null)
                {
                    dir = dir.Parent;
                }

                var file = dir.EnumerateFiles(SystemConfigFileName, SearchOption.AllDirectories).FirstOrDefault();
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
