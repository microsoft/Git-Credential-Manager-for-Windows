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

using static System.StringComparer;

namespace Microsoft.Alm.Authentication.Git
{
    public interface IWhere : IRuntimeService
    {
        /// <summary>
        /// Finds the "best" path to an app of a given name.
        /// <para/>
        /// Returns `<see langword="true"/>` if successful; otherwise `<see langword="false"/>`.
        /// </summary>
        /// <param name="name">
        /// The name of the application, without extension, to find.
        /// </param>
        /// <param name="path">
        /// Path to the first match file which the operating system considers executable.
        /// </param>
        bool FindApp(string name, out string path);

        /// <summary>
        /// Finds and returns path(s) to Git installation(s) in common locations.
        /// <para/>
        /// Returns `<see langword="true"/>` if successful; otherwise `<see langword="false"/>`.
        /// </summary>
        /// <param name="path">
        /// The local repository path if any.
        /// <para/>
        /// Used to find local Git configuration files.
        /// </param>
        /// <param name="distro">
        /// The distribution/version of Git used.
        /// <para/>
        /// Used to find system Git configuration files.
        /// </param>
        /// <param name="installations">
        /// The list of found Git installation if successful; otherwise `<see langword="null"/>`.
        /// </param>
        bool FindGitInstallation(string path, KnownDistribution distro, out Installation installation);

        /// <summary>
        /// Finds and returns path(s) to Git installation(s) in common locations.
        /// <para/>
        /// Returns `<see langword="true"/>` if successful; otherwise `<see langword="false"/>`.
        /// </summary>
        /// <param name="installations">The list of found Git installation if successful; otherwise `<see langword="null"/>`.</param>
        bool FindGitInstallations(out List<Installation> installations);

        /// <summary>
        /// Gets the path to Git's global configuration file.
        /// <para/>
        /// Returns `<see langword="true"/>` if successful; otherwise `<see langword="false"/>`.
        /// </summary>
        /// <param name="path">
        /// Path to Git's global configuration if successful; otherwise `<see langword="null"/>`.
        /// </param>
        bool GitGlobalConfig(out string path);

        /// <summary>
        /// Gets the path to Git's local configuration file based on the current working directory.
        /// <para/>
        /// Returns `<see langword="true"/>` if successful; otherwise `<see langword="false"/>`.
        /// </summary>
        /// <param name="path">
        /// Path to Git's local configuration if successful; otherwise `<see langword="null"/>`.
        /// </param>
        bool GitLocalConfig(out string path);

        /// <summary>
        /// Gets the path to Git's portable system configuration file.
        /// <para/>
        /// Searches starting with `<paramref name="startingDirectory"/>` working up towards the device root.
        /// <para/>
        /// Returns `<see langword="true"/>` if successful; otherwise `<see langword="false"/>`.
        /// </summary>
        /// <param name="startingDirectory">
        /// Working directory of the repository to find the configuration.
        /// </param>
        /// <param name="path">
        /// Path to Git's local configuration if successful; otherwise `<see langword="null"/>`.
        /// </param>
        bool GitLocalConfig(string startingDirectory, out string path);

        /// <summary>
        /// Gets the path to Git's portable system configuration file.
        /// <para/>
        /// Searches starting with `<see cref="Settings.CurrentDirectory"/>` working up towards the device root.
        /// <para/>
        /// Returns `<see langword="true"/>` if successful; otherwise `<see langword="false"/>`.
        /// </summary>
        /// <param name="path">
        /// Path to Git's portable system configuration if successful; otherwise `<see langword="null"/>`.
        /// </param>
        bool GitPortableConfig(out string path);

        /// <summary>
        /// Gets the path to Git's system configuration file.
        /// <para/>
        /// Returns `<see langword="true"/>` if successful; otherwise `<see langword="false"/>`.
        /// </summary>
        /// <param name="path">
        /// Path to Git's system configuration if successful; otherwise `<see langword="null"/>`.
        /// </param>
        bool GitSystemConfig(Installation installation, out string path);

        /// <summary>
        /// Gets the path to Git's XDG configuration file.
        /// <para/>
        /// Returns `<see langword="true"/>` if successful; otherwise `<see langword="false"/>`.
        /// </summary>
        /// <param name="path">
        /// Path to Git's XDG configuration file if successful; otherwise `<see langword="null"/>`.
        /// </param>
        bool GitXdgConfig(out string path);

        /// <summary>
        /// Returns the path to the user's home directory (~/ or %HOME%) that Git will rely on.
        /// </summary>
        string Home();
    }

    internal class Where : Base, IWhere
    {
        public Where(RuntimeContext context)
            : base(context)
        { }

        public Type ServiceType
            => typeof(IWhere);

        public bool FindApp(string name, out string path)
        {
            if (!string.IsNullOrWhiteSpace(name))
            {
                string pathext = Settings.GetEnvironmentVariable("PATHEXT");
                string envpath = Settings.GetEnvironmentVariable("PATH");

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
                        if (Storage.FileExists(value))
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

        public bool FindGitInstallation(string path, KnownDistribution distro, out Installation installation)
        {
            installation = new Installation(Context, path, distro);
            return installation.IsValid();
        }

        public bool FindGitInstallations(out List<Installation> installations)
        {
            const string GitAppName = @"Git";
            const string GitSubkeyName = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Git_is1";
            const string GitValueName = "InstallLocation";

            void ScanApplicationData(IList<Installation> output)
            {
                var appDataRoamingPath = string.Empty;

                if ((appDataRoamingPath = Settings.GetFolderPath(Environment.SpecialFolder.ApplicationData)) != null)
                {
                    appDataRoamingPath = Path.Combine(appDataRoamingPath, GitAppName);

                    output.Add(new Installation(Context, appDataRoamingPath, KnownDistribution.GitForWindows64v2));
                    output.Add(new Installation(Context, appDataRoamingPath, KnownDistribution.GitForWindows32v2));
                    output.Add(new Installation(Context, appDataRoamingPath, KnownDistribution.GitForWindows32v1));
                }

                var appDataLocalPath = string.Empty;

                if ((appDataLocalPath = Settings.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)) != null)
                {
                    appDataLocalPath = Path.Combine(appDataLocalPath, GitAppName);

                    output.Add(new Installation(Context, appDataLocalPath, KnownDistribution.GitForWindows64v2));
                    output.Add(new Installation(Context, appDataLocalPath, KnownDistribution.GitForWindows32v2));
                    output.Add(new Installation(Context, appDataLocalPath, KnownDistribution.GitForWindows32v1));
                }

                var programDataPath = string.Empty;

                if ((programDataPath = Settings.GetFolderPath(Environment.SpecialFolder.CommonApplicationData)) != null)
                {
                    programDataPath = Path.Combine(programDataPath, GitAppName);

                    output.Add(new Installation(Context, programDataPath, KnownDistribution.GitForWindows64v2));
                    output.Add(new Installation(Context, programDataPath, KnownDistribution.GitForWindows32v2));
                    output.Add(new Installation(Context, programDataPath, KnownDistribution.GitForWindows32v1));
                }
            }

            void ScanProgramFiles(IList<Installation> output)
            {
                var programFiles32Path = string.Empty;

                if ((programFiles32Path = Settings.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86)) != null)
                {
                    programFiles32Path = Path.Combine(programFiles32Path, GitAppName);

                    output.Add(new Installation(Context, programFiles32Path, KnownDistribution.GitForWindows32v2));
                    output.Add(new Installation(Context, programFiles32Path, KnownDistribution.GitForWindows32v1));
                }

                if (Settings.Is64BitOperatingSystem)
                {
                    var programFiles64Path = string.Empty;

                    if ((programFiles64Path = Settings.GetFolderPath(Environment.SpecialFolder.ProgramFiles)) != null)
                    {
                        programFiles64Path = Path.Combine(programFiles64Path, GitAppName);

                        output.Add(new Installation(Context, programFiles64Path, KnownDistribution.GitForWindows64v2));
                    }
                }
            }

            void ScanRegistry(IList<Installation> output)
            {
                var reg32HklmPath = Storage.RegistryReadString(RegistryHive.LocalMachine, RegistryView.Registry32, GitSubkeyName, GitValueName);
                var reg32HkcuPath = Storage.RegistryReadString(RegistryHive.CurrentUser, RegistryView.Registry32, GitSubkeyName, GitValueName);

                if (!string.IsNullOrEmpty(reg32HklmPath))
                {
                    output.Add(new Installation(Context, reg32HklmPath, KnownDistribution.GitForWindows32v2));
                    output.Add(new Installation(Context, reg32HklmPath, KnownDistribution.GitForWindows32v1));
                }

                if (!string.IsNullOrEmpty(reg32HkcuPath))
                {
                    output.Add(new Installation(Context, reg32HkcuPath, KnownDistribution.GitForWindows32v2));
                    output.Add(new Installation(Context, reg32HkcuPath, KnownDistribution.GitForWindows32v1));
                }

                if (Settings.Is64BitOperatingSystem)
                {
                    var reg64HklmPath = Storage.RegistryReadString(RegistryHive.LocalMachine, RegistryView.Registry64, GitSubkeyName, GitValueName);
                    var reg64HkcuPath = Storage.RegistryReadString(RegistryHive.CurrentUser, RegistryView.Registry64, GitSubkeyName, GitValueName);

                    if (!string.IsNullOrEmpty(reg64HklmPath))
                    {
                        output.Add(new Installation(Context, reg64HklmPath, KnownDistribution.GitForWindows64v2));
                    }

                    if (!string.IsNullOrEmpty(reg64HkcuPath))
                    {
                        output.Add(new Installation(Context, reg64HkcuPath, KnownDistribution.GitForWindows64v2));
                    }
                }
            }

            void ScanShellPath(IList<Installation> output)
            {
                var shellPathValue = string.Empty;

                if (FindApp(GitAppName, out shellPathValue))
                {
                    // `Where.App` returns the path to the executable, truncate to the installation root
                    if (shellPathValue.EndsWith(Installation.AllVersionCmdPath, StringComparison.OrdinalIgnoreCase))
                    {
                        shellPathValue = shellPathValue.Substring(0, shellPathValue.Length - Installation.AllVersionCmdPath.Length);
                    }

                    output.Add(new Installation(Context, shellPathValue, KnownDistribution.GitForWindows64v2));
                    output.Add(new Installation(Context, shellPathValue, KnownDistribution.GitForWindows32v2));
                    output.Add(new Installation(Context, shellPathValue, KnownDistribution.GitForWindows32v1));
                }
            }

            var candidates = new List<Installation>();

            ScanShellPath(candidates);
            ScanRegistry(candidates);
            ScanProgramFiles(candidates);
            ScanApplicationData(candidates);

            var pathSet = new HashSet<Installation>();
            foreach (var candidate in candidates)
            {
                if (candidate.IsValid())
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
            string path = Settings.GetEnvironmentVariable("HOME", EnvironmentVariableTarget.Process);

            if (path != null)
            {
                path = Settings.ExpandEnvironmentVariables(path);

                // If the path is good, return it.
                if (Storage.DirectoryExists(path))
                    return path;
            }

            // Absent the %HOME% variable, Git will construct it via %HOMEDRIVE%%HOMEPATH%, so we'll
            // attempt to that here and if it is valid, return it as %HOME%.
            string homeDrive = Settings.GetEnvironmentVariable("HOMEDRIVE", EnvironmentVariableTarget.Process);
            string homePath = Settings.GetEnvironmentVariable("HOMEPATH", EnvironmentVariableTarget.Process);

            if (homeDrive != null && homePath != null)
            {
                path = homeDrive + homePath;

                if (Storage.DirectoryExists(path))
                    return path;
            }

            // When all else fails, Git falls back to %USERPROFILE% as the user's home directory, so should we.
            return Settings.GetFolderPath(Environment.SpecialFolder.UserProfile);
        }

        public bool GitGlobalConfig(out string path)
        {
            const string GlobalConfigFileName = ".gitconfig";

            // Get the user's home directory, then append the global config file name.
            string home = Home();

            var globalPath = Path.Combine(home, GlobalConfigFileName);

            // If the path is valid, return it to the user.
            if (Storage.FileExists(globalPath))
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
                string directory = startingDirectory;
                string result = null;

                if (Storage.DirectoryExists(directory))
                {
                    string hasOdb(string dir)
                    {
                        if (dir == null || !Storage.DirectoryExists(dir))
                            return null;

                        foreach (var entryPath in Storage.EnumerateFileSystemEntries(dir))
                        {
                            if (entryPath is null)
                                continue;

                            string file = Storage.GetFileName(entryPath);

                            if (OrdinalIgnoreCase.Equals(file, GitFolderName)
                                && Storage.DirectoryExists(entryPath))
                                return entryPath;

                            if (OrdinalIgnoreCase.Equals(file, LocalConfigFileName)
                                && Storage.FileExists(entryPath))
                                return entryPath;
                        }

                        return null;
                    }

                    while (directory != null
                        && Storage.DirectoryExists(directory))
                    {
                        if ((result = hasOdb(directory)) != null)
                            break;

                        directory = Storage.GetParent(directory);
                    }

                    if (result != null)
                    {
                        result = Storage.GetFullPath(result);

                        if (Storage.DirectoryExists(result))
                        {
                            var localPath = Path.Combine(result, LocalConfigFileName);
                            if (Storage.FileExists(localPath))
                            {
                                path = localPath;
                                return true;
                            }
                        }
                        else if (Storage.FileExists(result)
                            && OrdinalIgnoreCase.Equals(Storage.GetFileName(result), LocalConfigFileName))
                        {
                            path = result;
                            return true;
                        }
                        else
                        {
                            // parse the file like gitdir: ../.git/modules/libgit2sharp
                            string content = null;

                            using (var stream = Storage.FileOpen(result, FileMode.Open, FileAccess.Read, FileShare.Read))
                            using (var reader = new StreamReader(stream))
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
                                    localPath = Storage.GetParent(result);
                                    localPath = Path.Combine(localPath, content);
                                }

                                if (Storage.DirectoryExists(localPath))
                                {
                                    localPath = Path.Combine(localPath, LocalConfigFileName);
                                    if (Storage.FileExists(localPath))
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
            return GitLocalConfig(Settings.CurrentDirectory, out path);
        }

        public bool GitPortableConfig(out string path)
        {
            const string PortableConfigFolder = "Git";
            const string PortableConfigFileName = "config";

            path = null;

            var portableConfigPath = Path.Combine(Settings.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), PortableConfigFolder, PortableConfigFileName);

            if (Storage.FileExists(portableConfigPath))
            {
                path = portableConfigPath;
            }

            return path != null;
        }

        public bool GitSystemConfig(Installation installation, out string path)
        {
            if (installation != null && Storage.FileExists(installation.Config))
            {
                path = installation.Path;
                return true;
            }
            // find Git on the local disk - the system config is stored relative to it
            else
            {
                List<Installation> installations;

                if (FindGitInstallations(out installations)
                    && Storage.FileExists(installations[0].Config))
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
            xdgConfigHome = Settings.GetEnvironmentVariable("XDG_CONFIG_HOME");

            if (Storage.DirectoryExists(xdgConfigHome))
            {
                xdgConfigPath = Path.Combine(xdgConfigHome, XdgConfigFolder, XdgConfigFileName);

                if (Storage.FileExists(xdgConfigPath))
                {
                    path = xdgConfigPath;
                    return true;
                }
            }

            // Fall back to using the %AppData% folder, and try again.
            xdgConfigHome = Settings.GetFolderPath(Environment.SpecialFolder.ApplicationData);

            xdgConfigPath = Path.Combine(xdgConfigHome, XdgConfigFolder, XdgConfigFileName);

            if (Storage.FileExists(xdgConfigPath))
            {
                path = xdgConfigPath;
                return true;
            }

            path = null;
            return false;
        }
    }
}
