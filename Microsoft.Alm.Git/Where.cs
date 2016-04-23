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
        static public bool FindApp(string name, out string path)
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
                            value = value.Replace("\\\\", "\\");
                            path = value;
                            return true;
                        }
                    }
                }
            }

            path = null;
            return false;
        }

        public static bool FindGitInstallation(string path, KnownGitDistribution distro, out GitInstallation installation)
        {
            installation = new GitInstallation(path, distro);
            return GitInstallation.IsValid(installation);
        }

        /// <summary>
        /// Finds and returns paths to Git installtions in common locations.
        /// </summary>
        /// <param name="hints">(optional) List of paths the caller believes Git can be found.</param>
        /// <param name="paths">
        /// All discoverd paths to the root of Git installations, ordered by 'priority' with first
        /// being the best installation to use when shelling out to Git.exe.
        /// </param>
        /// <returns><see langword="True"/> if Git was detected; <see langword="false"/> otherwise.</returns>
        public static bool FindGitInstallations(out List<GitInstallation> installations)
        {
            const string GitAppName = @"Git";
            const string GitSubkeyName = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Git_is1";
            const string GitValueName = "InstallLocation";

            Trace.WriteLine("Where::Git");

            installations = null;

            var programFiles32Path = String.Empty;
            var programFiles64Path = String.Empty;
            var appDataRoamingPath = String.Empty;
            var appDataLocalPath = String.Empty;
            var programDataPath = String.Empty;
            var reg32HklmPath = String.Empty;
            var reg64HklmPath = String.Empty;
            var reg32HkcuPath = String.Empty;
            var reg64HkcuPath = String.Empty;
            var shellPathValue = String.Empty;

            RegistryKey reg32HklmKey = null;
            RegistryKey reg64HklmKey = null;
            RegistryKey reg32HkcuKey = null;
            RegistryKey reg64HkcuKey = null;

            if ((reg32HklmKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32)) != null)
            {
                if ((reg32HklmKey = reg32HklmKey.OpenSubKey(GitSubkeyName)) != null)
                {
                    reg32HklmPath = reg32HklmKey.GetValue(GitValueName, reg32HklmPath) as String;
                }
            }

            if ((reg32HkcuKey = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry32)) != null)
            {
                if ((reg32HkcuKey = reg32HkcuKey.OpenSubKey(GitSubkeyName)) != null)
                {
                    reg32HkcuPath = reg32HkcuKey.GetValue(GitValueName, reg32HkcuPath) as String;
                }
            }

            if ((programFiles32Path = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)) != null)
            {
                programFiles32Path = Path.Combine(programFiles32Path, GitAppName);
            }

            if (Environment.Is64BitOperatingSystem)
            {
                if ((reg64HklmKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64)) != null)
                {
                    if ((reg64HklmKey = reg64HklmKey.OpenSubKey(GitSubkeyName)) != null)
                    {
                        reg64HklmPath = reg64HklmKey.GetValue(GitValueName, reg64HklmPath) as String;
                    }
                }

                if ((reg64HkcuKey = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64)) != null)
                {
                    if ((reg64HkcuKey = reg64HkcuKey.OpenSubKey(GitSubkeyName)) != null)
                    {
                        reg64HkcuPath = reg64HkcuKey.GetValue(GitValueName, reg64HkcuPath) as String;
                    }
                }

                // since .NET returns %ProgramFiles% as %ProgramFilesX86% when the app is 32-bit
                // manual manipulation of the path is required

                if ((programFiles64Path = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)) != null)
                {
                    programFiles64Path = programFiles64Path.Substring(0, programFiles64Path.Length - 6);
                    programFiles64Path = Path.Combine(programFiles64Path, GitAppName);
                }
            }

            if ((appDataRoamingPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)) != null)
            {
                appDataRoamingPath = Path.Combine(appDataRoamingPath, GitAppName);
            }

            if ((appDataLocalPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)) != null)
            {
                appDataLocalPath = Path.Combine(appDataLocalPath, GitAppName);
            }

            if ((programDataPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData)) != null)
            {
                programDataPath = Path.Combine(programDataPath, GitAppName);
            }

            List<GitInstallation> candidates = new List<GitInstallation>();
            // add candidate locations in order of preference
            if (Where.FindApp(GitAppName, out shellPathValue))
            {
                // `Where.App` returns the path to the executable, truncate to the installation root
                if (shellPathValue.EndsWith(GitInstallation.AllVersionCmdPath, StringComparison.InvariantCultureIgnoreCase))
                {
                    shellPathValue = shellPathValue.Substring(0, shellPathValue.Length - GitInstallation.AllVersionCmdPath.Length);
                }

                candidates.Add(new GitInstallation(shellPathValue, KnownGitDistribution.GitForWindows64v2));
                candidates.Add(new GitInstallation(shellPathValue, KnownGitDistribution.GitForWindows32v2));
                candidates.Add(new GitInstallation(shellPathValue, KnownGitDistribution.GitForWindows32v1));
            }

            if (!String.IsNullOrEmpty(reg64HklmPath))
            {
                candidates.Add(new GitInstallation(reg64HklmPath, KnownGitDistribution.GitForWindows64v2));
            }
            if (!String.IsNullOrEmpty(programFiles32Path))
            {
                candidates.Add(new GitInstallation(programFiles64Path, KnownGitDistribution.GitForWindows64v2));
            }
            if (!String.IsNullOrEmpty(reg64HkcuPath))
            {
                candidates.Add(new GitInstallation(reg64HkcuPath, KnownGitDistribution.GitForWindows64v2));
            }
            if (!String.IsNullOrEmpty(reg32HklmPath))
            {
                candidates.Add(new GitInstallation(reg32HklmPath, KnownGitDistribution.GitForWindows32v2));
                candidates.Add(new GitInstallation(reg32HklmPath, KnownGitDistribution.GitForWindows32v1));
            }
            if (!String.IsNullOrEmpty(programFiles32Path))
            {
                candidates.Add(new GitInstallation(programFiles32Path, KnownGitDistribution.GitForWindows32v2));
                candidates.Add(new GitInstallation(programFiles32Path, KnownGitDistribution.GitForWindows32v1));
            }
            if (!String.IsNullOrEmpty(reg32HkcuPath))
            {
                candidates.Add(new GitInstallation(reg32HkcuPath, KnownGitDistribution.GitForWindows32v2));
                candidates.Add(new GitInstallation(reg32HkcuPath, KnownGitDistribution.GitForWindows32v1));
            }
            if (!String.IsNullOrEmpty(programDataPath))
            {
                candidates.Add(new GitInstallation(programDataPath, KnownGitDistribution.GitForWindows64v2));
                candidates.Add(new GitInstallation(programDataPath, KnownGitDistribution.GitForWindows32v2));
                candidates.Add(new GitInstallation(programDataPath, KnownGitDistribution.GitForWindows32v1));
            }
            if (!String.IsNullOrEmpty(appDataLocalPath))
            {
                candidates.Add(new GitInstallation(appDataLocalPath, KnownGitDistribution.GitForWindows64v2));
                candidates.Add(new GitInstallation(appDataLocalPath, KnownGitDistribution.GitForWindows32v2));
                candidates.Add(new GitInstallation(appDataLocalPath, KnownGitDistribution.GitForWindows32v1));
            }
            if (!String.IsNullOrEmpty(appDataRoamingPath))
            {
                candidates.Add(new GitInstallation(appDataRoamingPath, KnownGitDistribution.GitForWindows64v2));
                candidates.Add(new GitInstallation(appDataRoamingPath, KnownGitDistribution.GitForWindows32v2));
                candidates.Add(new GitInstallation(appDataRoamingPath, KnownGitDistribution.GitForWindows32v1));
            }

            HashSet<GitInstallation> pathSet = new HashSet<GitInstallation>();
            foreach (var candidate in candidates)
            {
                if (GitInstallation.IsValid(candidate))
                {
                    pathSet.Add(candidate);
                }
            }

            installations = pathSet.ToList();

            Trace.WriteLine("   " + installations.Count + " Git installation(s) found.");

            return installations.Count > 0;
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
            const string GitFolderName = ".git";
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
                                           && (String.Equals(sub.Name, GitFolderName, StringComparison.OrdinalIgnoreCase)
                                              || String.Equals(sub.Name, LocalConfigFileName, StringComparison.OrdinalIgnoreCase));
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
                        else if (result.Name == LocalConfigFileName && result is FileInfo)
                        {
                            path = result.FullName;
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
        /// Gets the path to the Git portable system configuration file.
        /// </summary>
        /// <param name="path">Path to the Git portable system configuration</param>
        /// <returns><see langword="True"/> if succeeds; <se
        public static bool GitPortableConfig(out string path)
        {
            const string PortableConfigFolder = "Git";
            const string PortableConfigFileName = "config";

            path = null;

            var portableConfigPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), PortableConfigFolder, PortableConfigFileName);

            if (File.Exists(portableConfigPath))
            {
                path = portableConfigPath;
            }

            return path != null;
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
            if (FindApp("git", out path))
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
