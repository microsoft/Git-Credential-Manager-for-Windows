using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace Microsoft.Alm.Git
{
    public static class Where
    {
        /// <summary>
        /// Finds the "best" path to an app of a given name.
        /// </summary>
        /// <param name="name">The name of the application, without extension, to find.</param>
        /// <param name="path">Path to the first match file which the operating system considers 
        /// executable.</param>
        /// <returns><see langword="True"/> if succeeds; <see langword="false"/> otherwise.</returns>
        static public bool App(string name, out string path)
        {
            Trace.WriteLine("Where::App");

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
        /// Finds and returns paths to Git installtions in common locations.
        /// </summary>
        /// <param name="gitCmdPath">The best path to git.exe for CMD invocation.</param>
        /// <param name="paths">All discoverd paths to the root of Git installations.</param>
        /// <returns><see langword="True"/> if Git was detected; <see langword="false"/> otherwise.</returns>
        public static bool Git(out string gitCmdPath, out List<string> paths)
        {
            const string GitAppName = @"Git";
            const string GitSubkeyName = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Git_is1";
            const string GitValueName = "InstallLocation";

            Trace.WriteLine("Where::Git");

            gitCmdPath = null;
            paths = new List<string>();

            var pf32path = String.Empty;
            var pf64path = String.Empty;
            var reg32path = String.Empty;
            var reg64path = String.Empty;
            var envpath = String.Empty;

            RegistryKey reg32key = null;
            RegistryKey reg64key = null;

            if ((reg32key = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32)) != null)
            {
                if ((reg32key = reg32key.OpenSubKey(GitSubkeyName)) != null)
                {
                    reg32path = reg32key.GetValue(GitValueName, reg32path) as String;
                }
            }

            if ((pf32path = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)) != null)
            {
                pf32path = Path.Combine(pf32path, GitAppName);
            }

            if (Environment.Is64BitOperatingSystem)
            {
                if ((reg64key = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64)) != null)
                {
                    if ((reg64key = reg64key.OpenSubKey(GitSubkeyName)) != null)
                    {
                        reg64path = reg64key.GetValue(GitValueName, reg64path) as String;
                    }
                }

                // since .NET returns %ProgramFiles% as %ProgramFilesX86% when the app is 32-bit
                // manual manipulation of the path is required

                if ((pf64path = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)) != null)
                {
                    pf64path = pf64path.Substring(0, pf64path.Length - 6);
                    pf64path = Path.Combine(pf64path, GitAppName);
                }
            }

            List<GitPath> candidates = new List<GitPath>();
            // add candidate locations in order of preference
            if (Where.App(GitAppName, out envpath))
            {
                // prefer the version of Git on %PATH%
                gitCmdPath = envpath;

                // `Where.App` returns the path to the executable, truncate to the installation root
                envpath = Path.GetDirectoryName(envpath);
                envpath = Path.GetDirectoryName(envpath);

                candidates.Add(new GitPath(envpath, GitPath.Version2_64bit));
                candidates.Add(new GitPath(envpath, GitPath.Version2_32bit));
                candidates.Add(new GitPath(envpath, GitPath.Version1_32bit));
            }
            if (!String.IsNullOrEmpty(reg64path))
            {
                candidates.Add(new GitPath(reg64path, GitPath.Version2_64bit));
            }
            if (!String.IsNullOrEmpty(pf32path))
            {
                candidates.Add(new GitPath(pf64path, GitPath.Version2_64bit));
            }
            if (!String.IsNullOrEmpty(reg32path))
            {
                candidates.Add(new GitPath(reg32path, GitPath.Version2_32bit));
                candidates.Add(new GitPath(reg32path, GitPath.Version1_32bit));
            }
            if (!String.IsNullOrEmpty(pf32path))
            {
                candidates.Add(new GitPath(pf32path, GitPath.Version2_32bit));
                candidates.Add(new GitPath(pf32path, GitPath.Version1_32bit));
            }

            HashSet<string> pathSet = new HashSet<string>();
            foreach (var candidate in candidates)
            {
                if (Directory.Exists(candidate.Libexec) && File.Exists(candidate.Cmd))
                {
                    // trap the first (preferred) path to Git
                    if (pathSet.Add(candidate.Path.TrimEnd('\\')) && String.IsNullOrEmpty(gitCmdPath))
                    {
                        gitCmdPath = candidate.Cmd;
                    }
                }
            }

            paths = pathSet.ToList();

            return gitCmdPath != null;
        }

        /// <summary>
        /// Gets the path to the Git global configuration file.
        /// </summary>
        /// <param name="path">Path to the Git global configuration</param>
        /// <returns><see langword="True"/> if succeeds; <see langword="false"/> otherwise.</returns>
        public static bool GitGlobalConfig(out string path)
        {
            const string GlobalConfigFileName = ".gitconfig";

            Trace.WriteLine("Where::GitGlobalConfig");

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
        /// <param name="startingDirectory">A directory of the repository where the configuration file is contained.</param>
        /// <param name="path">Path to the Git local configuration</param>
        /// <returns><see langword="True"/> if succeeds; <see langword="false"/> otherwise.</returns>
        public static bool GitLocalConfig(string startingDirectory, out string path)
        {
            const string GitOdbFolderName = ".git";
            const string LocalConfigFileName = "config";

            Trace.WriteLine("Where::GitLocalConfig");

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
                            // parse the file like gitdir: ../.git/modules/libgit2sharp
                            string content = null;

                            using (FileStream stream = (result as FileInfo).OpenRead())
                            using (StreamReader reader = new StreamReader(stream))
                            {
                                content = reader.ReadToEnd();
                            }

                            Match match;
                            if ((match = Regex.Match(content, @"gitdir\s*:\s*([^\r\n]+)", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)).Success
                                && match.Groups.Count > 1)
                            {
                                content = match.Groups[1].Value;
                                content = content.Replace('/', '\\');

                                string localPath = null;

                                if (Path.IsPathRooted(content))
                                {
                                    localPath = content;
                                }
                                else
                                {
                                    localPath = Path.GetDirectoryName(result.FullName);
                                    localPath = Path.Combine(localPath, content);
                                }

                                if (Directory.Exists(localPath))
                                {
                                    localPath = Path.Combine(localPath, LocalConfigFileName);
                                    if (File.Exists(localPath))
                                    {
                                        path = localPath;
                                    }
                                }
                            }
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
        /// <returns><see langword="True"/> if succeeds; <see langword="false"/> otherwise.</returns>
        public static bool GitLocalConfig(out string path)
        {
            return GitLocalConfig(Environment.CurrentDirectory, out path);
        }

        /// <summary>
        /// Gets the path to the Git system configuration file.
        /// </summary>
        /// <param name="path">Path to the Git system configuration.</param>
        /// <returns><see langword="True"/> if succeeds; <see langword="false"/> otherwise.</returns>
        public static bool GitSystemConfig(out string path)
        {
            const string SystemConfigFileName = "gitconfig";

            Trace.WriteLine("Where::GitSystemConfig");

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
