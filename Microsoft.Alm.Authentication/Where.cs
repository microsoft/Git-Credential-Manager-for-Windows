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
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace Microsoft.Alm.Authentication
{
    public interface IWhere
    {
        /// <summary>
        /// Finds the "best" path to an app of a given name.
        /// <para/>
        /// Returns `<see langword="true"/>` if successful; otherwise `<see langword="false"/>`.
        /// </summary>
        /// <param name="name">The name of the application, without extension, to find.</param>
        /// <param name="path">Path to the first match file which the operating system considers executable.</param>
        bool FindApp(string name, out string path);

        bool FindGitInstallation(string path, Git.KnownGitDistribution distro, out Git.GitInstallation installation);

        /// <summary>
        /// Finds and returns path(s) to Git installation(s) in common locations.
        /// <para/>
        /// Returns `<see langword="true"/>` if successful; otherwise `<see langword="false"/>`.
        /// </summary>
        /// <param name="installations">The list of found Git installation if successful; otherwise `<see langword="null"/>`.</param>
        bool FindGitInstallations(out List<Git.GitInstallation> installations);

        /// <summary>
        /// Gets the path to Git's global configuration file.
        /// <para/>
        /// Returns `<see langword="true"/>` if successful; otherwise `<see langword="false"/>`.
        /// </summary>
        /// <param name="path">Path to Git's global configuration if successful; otherwise `<see langword="null"/>`.</param>
        bool GitGlobalConfig(out string path);

        /// <summary>
        /// Gets the path to Git's local configuration file based on the current working directory.
        /// </summary>
        /// <param name="path">Path to Git's local configuration if successful; otherwise `<see langword="null"/>`.</param>
        /// <returns><see langword="True"/> if succeeds; <see langword="false"/> otherwise.</returns>
        bool GitLocalConfig(out string path);

        /// <summary>
        /// Gets the path to Git's portable system configuration file.
        /// <para/>
        /// Searches starting with `<paramref name="startingDirectory"/>` working up towards the device root.
        /// <para/>
        /// Returns `<see langword="true"/>` if successful; otherwise `<see langword="false"/>`.
        /// </summary>
        /// <param name="startingDirectory">Working directory of the repository to find the configuration.</param>
        /// <param name="path">Path to Git's local configuration if successful; otherwise `<see langword="null"/>`.</param>
        bool GitLocalConfig(string startingDirectory, out string path);

        /// <summary>
        /// Gets the path to Git's portable system configuration file.
        /// <para/>
        /// Searches starting with `<see cref="Environment.CurrentDirectory"/>` working up towards the device root.
        /// <para/>
        /// Returns `<see langword="true"/>` if successful; otherwise `<see langword="false"/>`.
        /// </summary>
        /// <param name="path">Path to Git's portable system configuration if successful; otherwise `<see langword="null"/>`.</param>
        bool GitPortableConfig(out string path);

        /// <summary>
        /// Gets the path to Git's system configuration file.
        /// <para/>
        /// Returns `<see langword="true"/>` if successful; otherwise `<see langword="false"/>`.
        /// </summary>
        /// <param name="path">Path to Git's system configuration if successful; otherwise `<see langword="null"/>`.</param>
        bool GitSystemConfig(Git.GitInstallation? installation, out string path);

        /// <summary>
        /// Gets the path to Git's XDG configuration file.
        /// <para/>
        /// Returns `<see langword="true"/>` if successful; otherwise `<see langword="false"/>`.
        /// </summary>
        /// <param name="path">Path to Git's XDG configuration file if successful; otherwise `<see langword="null"/>`.</param>
        bool GitXdgConfig(out string path);

        /// <summary>
        /// Returns the path to the user's home directory (~/ or %HOME%) that Git will rely on.
        /// </summary>
        string Home();
    }

    internal class Where : BaseType, IWhere
    {
        public Where(RuntimeContext context)
            : base(context)
        { }

        public bool FindApp(string name, out string path)
        {
            if (!string.IsNullOrWhiteSpace(name))
            {
                string pathext = Environment.GetEnvironmentVariable("PATHEXT");
                string envpath = Environment.GetEnvironmentVariable("PATH");

                if (string.IsNullOrEmpty(pathext) || string.IsNullOrEmpty(envpath))
                {
                    // The user is likely hosed, or a poorly crafted test case - either way avoid NRE
                    // from the .Split call.
                    path = null;
                    return false;
                }

                string[] exts = pathext.Split(';');
                string[] paths = envpath.Split(';');

                for (int i = 0; i < paths.Length; i++)
                {
                    if (string.IsNullOrWhiteSpace(paths[i]))
                        continue;

                    for (int j = 0; j < exts.Length; j++)
                    {
                        if (string.IsNullOrWhiteSpace(exts[j]))
                            continue;

                        string value = string.Format("{0}\\{1}{2}", paths[i], name, exts[j]);
                        if (FileSystem.FileExists(value))
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

        public bool FindGitInstallation(string path, Git.KnownGitDistribution distro, out Git.GitInstallation installation)
        {
            installation = new Git.GitInstallation(Context, path, distro);
            return Git.GitInstallation.IsValid(installation);
        }

        public bool FindGitInstallations(out List<Git.GitInstallation> installations)
        {
            const string GitAppName = @"Git";
            const string GitSubkeyName = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Git_is1";
            const string GitValueName = "InstallLocation";

            installations = null;

            var programFiles32Path = string.Empty;
            var programFiles64Path = string.Empty;
            var appDataRoamingPath = string.Empty;
            var appDataLocalPath = string.Empty;
            var programDataPath = string.Empty;
            var reg32HklmPath = string.Empty;
            var reg64HklmPath = string.Empty;
            var reg32HkcuPath = string.Empty;
            var reg64HkcuPath = string.Empty;
            var shellPathValue = string.Empty;

            using (var reg32HklmKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32))
            using (var reg32HkcuKey = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry32))
            using (var reg32HklmSubKey = reg32HklmKey?.OpenSubKey(GitSubkeyName))
            using (var reg32HkcuSubKey = reg32HkcuKey?.OpenSubKey(GitSubkeyName))
            {
                reg32HklmPath = reg32HklmSubKey?.GetValue(GitValueName, reg32HklmPath) as string;
                reg32HkcuPath = reg32HkcuSubKey?.GetValue(GitValueName, reg32HkcuPath) as string;
            }

            if ((programFiles32Path = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)) != null)
            {
                programFiles32Path = Path.Combine(programFiles32Path, GitAppName);
            }

            if (Environment.Is64BitOperatingSystem)
            {
                using (var reg64HklmKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64))
                using (var reg64HkcuKey = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64))
                using (var reg64HklmSubKey = reg64HklmKey?.OpenSubKey(GitSubkeyName))
                using (var reg64HkcuSubKey = reg64HkcuKey?.OpenSubKey(GitSubkeyName))
                {
                    reg64HklmPath = reg64HklmSubKey?.GetValue(GitValueName, reg64HklmPath) as string;
                    reg64HkcuPath = reg64HkcuSubKey?.GetValue(GitValueName, reg64HkcuPath) as string;
                }

                if ((programFiles64Path = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles)) != null)
                {
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

            List<Git.GitInstallation> candidates = new List<Git.GitInstallation>();
            // Add candidate locations in order of preference.
            if (FindApp(GitAppName, out shellPathValue))
            {
                // `Where.App` returns the path to the executable, truncate to the installation root
                if (shellPathValue.EndsWith(Git.GitInstallation.AllVersionCmdPath, StringComparison.OrdinalIgnoreCase))
                {
                    shellPathValue = shellPathValue.Substring(0, shellPathValue.Length - Git.GitInstallation.AllVersionCmdPath.Length);
                }

                candidates.Add(new Git.GitInstallation(Context, shellPathValue, Git.KnownGitDistribution.GitForWindows64v2));
                candidates.Add(new Git.GitInstallation(Context, shellPathValue, Git.KnownGitDistribution.GitForWindows32v2));
                candidates.Add(new Git.GitInstallation(Context, shellPathValue, Git.KnownGitDistribution.GitForWindows32v1));
            }

            if (!string.IsNullOrEmpty(reg64HklmPath))
            {
                candidates.Add(new Git.GitInstallation(Context, reg64HklmPath, Git.KnownGitDistribution.GitForWindows64v2));
            }
            if (!string.IsNullOrEmpty(programFiles32Path))
            {
                candidates.Add(new Git.GitInstallation(Context, programFiles64Path, Git.KnownGitDistribution.GitForWindows64v2));
            }
            if (!string.IsNullOrEmpty(reg64HkcuPath))
            {
                candidates.Add(new Git.GitInstallation(Context, reg64HkcuPath, Git.KnownGitDistribution.GitForWindows64v2));
            }
            if (!string.IsNullOrEmpty(reg32HklmPath))
            {
                candidates.Add(new Git.GitInstallation(Context, reg32HklmPath, Git.KnownGitDistribution.GitForWindows32v2));
                candidates.Add(new Git.GitInstallation(Context, reg32HklmPath, Git.KnownGitDistribution.GitForWindows32v1));
            }
            if (!string.IsNullOrEmpty(programFiles32Path))
            {
                candidates.Add(new Git.GitInstallation(Context, programFiles32Path, Git.KnownGitDistribution.GitForWindows32v2));
                candidates.Add(new Git.GitInstallation(Context, programFiles32Path, Git.KnownGitDistribution.GitForWindows32v1));
            }
            if (!string.IsNullOrEmpty(reg32HkcuPath))
            {
                candidates.Add(new Git.GitInstallation(Context, reg32HkcuPath, Git.KnownGitDistribution.GitForWindows32v2));
                candidates.Add(new Git.GitInstallation(Context, reg32HkcuPath, Git.KnownGitDistribution.GitForWindows32v1));
            }
            if (!string.IsNullOrEmpty(programDataPath))
            {
                candidates.Add(new Git.GitInstallation(Context, programDataPath, Git.KnownGitDistribution.GitForWindows64v2));
                candidates.Add(new Git.GitInstallation(Context, programDataPath, Git.KnownGitDistribution.GitForWindows32v2));
                candidates.Add(new Git.GitInstallation(Context, programDataPath, Git.KnownGitDistribution.GitForWindows32v1));
            }
            if (!string.IsNullOrEmpty(appDataLocalPath))
            {
                candidates.Add(new Git.GitInstallation(Context, appDataLocalPath, Git.KnownGitDistribution.GitForWindows64v2));
                candidates.Add(new Git.GitInstallation(Context, appDataLocalPath, Git.KnownGitDistribution.GitForWindows32v2));
                candidates.Add(new Git.GitInstallation(Context, appDataLocalPath, Git.KnownGitDistribution.GitForWindows32v1));
            }
            if (!string.IsNullOrEmpty(appDataRoamingPath))
            {
                candidates.Add(new Git.GitInstallation(Context, appDataRoamingPath, Git.KnownGitDistribution.GitForWindows64v2));
                candidates.Add(new Git.GitInstallation(Context, appDataRoamingPath, Git.KnownGitDistribution.GitForWindows32v2));
                candidates.Add(new Git.GitInstallation(Context, appDataRoamingPath, Git.KnownGitDistribution.GitForWindows32v1));
            }

            HashSet<Git.GitInstallation> pathSet = new HashSet<Git.GitInstallation>();
            foreach (var candidate in candidates)
            {
                if (Git.GitInstallation.IsValid(candidate))
                {
                    pathSet.Add(candidate);
                }
            }

            installations = pathSet.ToList();

            Trace.WriteLine($"found {installations.Count} Git installation(s).");

            return installations.Count > 0;
        }

        public string Home()
        {
            // Git relies on the %HOME% environment variable to represent the users home directory it
            // can contain embedded environment variables, so we need to expand it
            string path = Environment.GetEnvironmentVariable("HOME", EnvironmentVariableTarget.Process);

            if (path != null)
            {
                path = Environment.ExpandEnvironmentVariables(path);

                // If the path is good, return it.
                if (FileSystem.DirectoryExists(path))
                    return path;
            }

            // Absent the %HOME% variable, Git will construct it via %HOMEDRIVE%%HOMEPATH%, so we'll
            // attempt to that here and if it is valid, return it as %HOME%.
            string homeDrive = Environment.GetEnvironmentVariable("HOMEDRIVE", EnvironmentVariableTarget.Process);
            string homePath = Environment.GetEnvironmentVariable("HOMEPATH", EnvironmentVariableTarget.Process);

            if (homeDrive != null && homePath != null)
            {
                path = homeDrive + homePath;

                if (FileSystem.DirectoryExists(path))
                    return path;
            }

            // When all else fails, Git falls back to %USERPROFILE% as the user's home directory, so should we.
            return Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        }

        public bool GitGlobalConfig(out string path)
        {
            const string GlobalConfigFileName = ".gitconfig";

            // Get the user's home directory, then append the global config file name.
            string home = Home();

            var globalPath = Path.Combine(home, GlobalConfigFileName);

            // If the path is valid, return it to the user.
            if (FileSystem.FileExists(globalPath))
            {
                path = globalPath;
                return true;
            }

            path = null;
            return false;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1800:DoNotCastUnnecessarily")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times")]
        public bool GitLocalConfig(string startingDirectory, out string path)
        {
            const string GitFolderName = ".git";
            const string LocalConfigFileName = "config";

            if (!string.IsNullOrWhiteSpace(startingDirectory))
            {
                var dir = startingDirectory;

                if (FileSystem.DirectoryExists(dir))
                {
                    string hasOdb(string odbPath)
                    {
                        if (odbPath == null || !FileSystem.FileExists(odbPath))
                            return null;

                        foreach (var item in FileSystem.EnumerateFileSystemEntries(odbPath))
                        {
                            if (item != null
                                && FileSystem.FileExists(item)
                                && (GitFolderName.Equals(FileSystem.GetFileName(item), StringComparison.OrdinalIgnoreCase)
                                    || LocalConfigFileName.Equals(FileSystem.GetFileName(item), StringComparison.OrdinalIgnoreCase)))
                                return item;
                        }

                        return null;
                    }

                    string parent = null;
                    string result = null;

                    while (dir != null 
                        && FileSystem.DirectoryExists(dir) 
                        && (parent = FileSystem.GetParent(dir)) != null 
                        && (FileSystem.DirectoryExists(parent)))
                    {
                        if ((result = hasOdb(dir)) != null)
                            break;

                        dir = parent;
                    }

                    if (result != null && FileSystem.DirectoryExists(result))
                    {
                        if (FileSystem.DirectoryExists(result))
                        {
                            var localPath = Path.Combine(FileSystem.GetFullPath(result), LocalConfigFileName);
                            if (FileSystem.FileExists(localPath))
                            {
                                path = localPath;
                                return true;
                            }
                        }
                        else if (LocalConfigFileName.Equals(FileSystem.GetFileName(result), StringComparison.OrdinalIgnoreCase)
                            && FileSystem.FileExists(result))
                        {
                            path = FileSystem.GetFullPath(result);
                            return true;
                        }
                        else
                        {
                            // parse the file like gitdir: ../.git/modules/libgit2sharp
                            string content = null;

                            using (FileStream stream = FileSystem.FileOpen(result, FileMode.Open, FileAccess.Read, FileShare.Read))
                            using (StreamReader reader = new StreamReader(stream))
                            {
                                content = reader.ReadToEnd();
                            }

                            Match match;
                            if ((match = Regex.Match(content, @"gitdir\s*:\s*([^\r\n]+)", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)).Success
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
                                    localPath = FileSystem.GetParent(FileSystem.GetFullPath(result));
                                    localPath = Path.Combine(localPath, content);
                                }

                                if (FileSystem.DirectoryExists(localPath))
                                {
                                    localPath = Path.Combine(localPath, LocalConfigFileName);

                                    if (FileSystem.FileExists(localPath))
                                    {
                                        path = localPath;
                                        return true;
                                    }
                                }
                            }
                        }
                    }
                }
            }

            path = null;
            return false;
        }

        public bool GitLocalConfig(out string path)
        {
            return GitLocalConfig(Environment.CurrentDirectory, out path);
        }

        public bool GitPortableConfig(out string path)
        {
            const string PortableConfigFolder = "Git";
            const string PortableConfigFileName = "config";

            path = null;

            var portableConfigPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), PortableConfigFolder, PortableConfigFileName);

            if (FileSystem.FileExists(portableConfigPath))
            {
                path = portableConfigPath;
            }

            return path != null;
        }

        public bool GitSystemConfig(Git.GitInstallation? installation, out string path)
        {
            if (installation.HasValue && FileSystem.FileExists(installation.Value.Config))
            {
                path = installation.Value.Path;
                return true;
            }
            // find Git on the local disk - the system config is stored relative to it
            else
            {
                List<Git.GitInstallation> installations;

                if (FindGitInstallations(out installations)
                    && FileSystem.FileExists(installations[0].Config))
                {
                    path = installations[0].Config;
                    return true;
                }
            }

            path = null;
            return false;
        }

        public bool GitXdgConfig(out string path)
        {
            const string XdgConfigFolder = "Git";
            const string XdgConfigFileName = "config";

            string xdgConfigHome;
            string xdgConfigPath;

            // The XDG config home is defined by an environment variable.
            xdgConfigHome = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");

            if (FileSystem.DirectoryExists(xdgConfigHome))
            {
                xdgConfigPath = Path.Combine(xdgConfigHome, XdgConfigFolder, XdgConfigFileName);

                if (FileSystem.FileExists(xdgConfigPath))
                {
                    path = xdgConfigPath;
                    return true;
                }
            }

            // Fall back to using the %AppData% folder, and try again.
            xdgConfigHome = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

            xdgConfigPath = Path.Combine(xdgConfigHome, XdgConfigFolder, XdgConfigFileName);

            if (FileSystem.FileExists(xdgConfigPath))
            {
                path = xdgConfigPath;
                return true;
            }

            path = null;
            return false;
        }
    }
}
