using System;
using System.IO;
using System.Linq;

namespace Microsoft.TeamFoundation.Authentication
{
    public static class Where
    {
        /// <summary>
        /// Finds the "best" path to an app of a given name.
        /// </summary>
        /// <param name="name">The name of the application, without extension, to find.</param>
        /// <param name="path">Path to the first match file which the operating system considers 
        /// executable.</param>
        /// <returns>True if succeeds; false otherwise.</returns>
        static public bool App(string name, out string path)
        {
            if (!String.IsNullOrWhiteSpace(name))
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
            }

            path = null;
            return false;
        }

        /// <summary>
        /// Gets the path to the Git global configuration file.
        /// </summary>
        /// <param name="path">Path to the Git global configuration</param>
        /// <returns>True if succeeds; false otherwise.</returns>
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

        /// <summary>
        /// Gets the path to the Git local configuration file based on the <paramref name="startingDirectory"/>.
        /// </summary>
        /// <param name="startingDirectory">A directory of the repository where the configuration file is contained..</param>
        /// <param name="path">Path to the Git local configuration</param>
        /// <returns>True if succeeds; false otherwise.</returns>
        public static bool GitLocalConfig(string startingDirectory, out string path)
        {
            const string GitOdbFolderName = ".git";
            const string LocalConfigFileName = "config";

            path = null;

            if (!String.IsNullOrWhiteSpace(startingDirectory))
            {
                var dir = new DirectoryInfo(startingDirectory);
                if (dir.Exists)
                {
                    Func<DirectoryInfo, FileSystemInfo> hasOdb = (DirectoryInfo info) =>
                    {
                        if (info == null || !info.Exists)
                            return null;

                        return info.EnumerateFileSystemInfos()
                                   .Where((FileSystemInfo sub) =>
                                   {
                                       return sub != null
                                           && sub.Exists
                                           && String.Equals(sub.Name, GitOdbFolderName, StringComparison.OrdinalIgnoreCase);
                                   })
                                   .FirstOrDefault();
                    };

                    FileSystemInfo result = null;
                    while (dir != null && dir.Exists && dir.Parent != null && dir.Parent.Exists)
                    {
                        if ((result = hasOdb(dir)) != null)
                            break;

                        dir = dir.Parent;
                    }

                    if (result != null && result.Exists)
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
                            // var content = File.ReadAllText(result.FullName);
                            // TODO: handle .git files
                        }
                    }
                }
            }

            return path != null;
        }
        /// <summary>
        /// Gets the path to the Git local configuration file based on the current working directory.
        /// </summary>
        /// <param name="path">Path to the Git local configuration.</param>
        /// <returns>True if succeeds; false otherwise.</returns>
        public static bool GitLocalConfig(out string path)
        {
            return GitLocalConfig(Environment.CurrentDirectory, out path);
        }

        /// <summary>
        /// Gets the path to the Git system configuration file.
        /// </summary>
        /// <param name="path">Path to the Git system configuration.</param>
        /// <returns>True if succeeds; false otherwise.</returns>
        public static bool GitSystemConfig(out string path)
        {
            const string SystemConfigFileName = "gitconfig";

            // find Git on the local disk - the system config is stored relative to it
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
