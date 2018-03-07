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
using System.Linq;
using Microsoft.Alm.Authentication.Git;
using Microsoft.Win32;

using Git = Microsoft.Alm.Authentication.Git;

namespace Microsoft.Alm.Cli
{
    internal class Installer
    {
        internal const string ParamPathKey = "--path";
        internal const string ParamPassiveKey = "--passive";
        internal const string ParamForceKey = "--force";
        internal const string FailFace = "U_U";
        internal const string TadaFace = "^_^";

        //private static readonly Version NetFxMinVersion = new Version(4, 5, 1);
        private static readonly IReadOnlyList<string> FileList = new string[]
        {
            "Microsoft.Vsts.Authentication.dll",
            "Microsoft.Alm.Authentication.dll",
            "Microsoft.Alm.Authentication.Git.dll",
            "Microsoft.IdentityModel.Clients.ActiveDirectory.dll",
            "Microsoft.IdentityModel.Clients.ActiveDirectory.Platform.dll",
            "Bitbucket.Authentication.dll",
            "GitHub.Authentication.exe",
            "git-credential-manager.exe",
            "git-askpass.exe",
        };

        private static readonly IReadOnlyList<string> DocsList = new string[]
        {
            "git-askpass.html",
            "git-credential-manager.html",
        };

        public Installer(Program program)
        {
            _program = program;

            var args = Environment.GetCommandLineArgs();

            // parse arguments
            for (int i = 2; i < args.Length; i++)
            {
                if (string.Equals(args[i], ParamPathKey, StringComparison.OrdinalIgnoreCase))
                {
                    if (args.Length > i + 1)
                    {
                        i += 1;
                        _customPath = args[i];

                        Program.Context.Trace.WriteLine($"{ParamPathKey} = '{_customPath}'.");
                    }
                }
                else if (string.Equals(args[i], ParamPassiveKey, StringComparison.OrdinalIgnoreCase))
                {
                    _isPassive = true;

                    Program.Context.Trace.WriteLine($"{ParamPassiveKey} = true.");
                }
                else if (string.Equals(args[i], ParamForceKey, StringComparison.OrdinalIgnoreCase))
                {
                    _isForced = true;

                    Program.Context.Trace.WriteLine($"{ParamForceKey} = true.");
                }
            }
        }

        private string _customPath = null;
        private string _cygwinPath;
        private bool _isPassive = false;
        private bool _isForced = false;
        private Program _program;
        private TextWriter _stdout = null;
        private TextWriter _stderr = null;
        private string _userBinPath = null;

        public int ExitCode
        {
            get { return (int)Result; }
            set { Result = (ResultValue)value; }
        }

        public ResultValue Result { get; private set; }

        internal string CygwinPath
        {
            get
            {
                if (_cygwinPath == null)
                {
                    const string Cygwin32GitPath = @"cygwin\usr\libexec\git-core\";
                    const string Cygwin64GitPath = @"cygwin64\usr\libexec\git-core";

                    foreach (var drive in DriveInfo.GetDrives())
                    {
                        string path = Path.Combine(drive.RootDirectory.FullName, Cygwin64GitPath);

                        if (Program.FileSystem.DirectoryExists(path))
                        {
                            Program.Context.Trace.WriteLine($"cygwin directory found at '{path}'.");

                            _cygwinPath = path;
                            break;
                        }

                        path = Path.Combine(drive.RootDirectory.FullName, Cygwin32GitPath);

                        if (Program.FileSystem.DirectoryExists(path))
                        {
                            Program.Context.Trace.WriteLine($"cygwin directory found at '{path}'.");

                            _cygwinPath = path;
                            break;
                        }
                    }
                }

                return _cygwinPath;
            }
        }

        internal Program Program
        {
            get { return _program; }
        }

        internal string UserBinPath
        {
            get
            {
                if (_userBinPath == null)
                {
                    string val1 = null;
                    string val2 = null;
                    string val3 = null;
                    var vars = Environment.GetEnvironmentVariables(EnvironmentVariableTarget.Process);

                    // Git for Windows checks %HOME% first
                    if ((val1 = vars["HOME"] as string) != null
                        && Program.FileSystem.DirectoryExists(val1))
                    {
                        _userBinPath = val1;
                    }
                    // Git for Windows checks %HOMEDRIVE%%HOMEPATH% second
                    else if ((val1 = vars["HOMEDRIVE"] as string) != null && (val2 = vars["HOMEPATH"] as string) != null
                        && Program.FileSystem.DirectoryExists(val3 = val1 + val2))
                    {
                        _userBinPath = val3;
                    }
                    // Git for Windows checks %USERPROFILE% last
                    else if ((val1 = vars["USERPROFILE"] as string) != null)
                    {
                        _userBinPath = val1;
                    }

                    if (_userBinPath != null)
                    {
                        // Git for Windows adds %HOME%\bin to %PATH%
                        _userBinPath = Path.Combine(_userBinPath, "bin");

                        Program.Context.Trace.WriteLine($"user bin found at '{_userBinPath}'.");
                    }
                }
                return _userBinPath;
            }
        }

        public void DeployConsole()
        {
            SetOutput(_isPassive, _isPassive && _isForced);
            try
            {
                System.Security.Principal.WindowsIdentity identity = System.Security.Principal.WindowsIdentity.GetCurrent();
                System.Security.Principal.WindowsPrincipal principal = new System.Security.Principal.WindowsPrincipal(identity);
                if (!principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator))
                {
                    DeployElevated();
                    return;
                }

                List<Installation> installations = null;

                // use the custom installation path if supplied
                if (!string.IsNullOrEmpty(_customPath))
                {
                    if (!Program.FileSystem.DirectoryExists(_customPath))
                    {
                        Program.LogEvent("No Git installation found, unable to continue deployment.", EventLogEntryType.Error);
                        Program.Out.WriteLine();
                        Program.WriteLine($"Fatal: custom path does not exist: '{_customPath}'. {FailFace}");
                        Pause();

                        Result = ResultValue.InvalidCustomPath;
                        return;
                    }

                    Program.Out.WriteLine();
                    Program.Out.WriteLine($"Deploying to custom path: '{_customPath}'.");

                    // if the custom path points to a git location then treat it properly
                    Installation installation;
                    if (Program.Context.Where.FindGitInstallation(_customPath, KnownDistribution.GitForWindows64v2, out installation)
                        || Program.Context.Where.FindGitInstallation(_customPath, KnownDistribution.GitForWindows32v2, out installation)
                        || Program.Context.Where.FindGitInstallation(_customPath, KnownDistribution.GitForWindows32v1, out installation))
                    {
                        Program.Context.Trace.WriteLine($"   Git found: '{installation.Path}'.");

                        // track known Git installations
                        installations = new List<Installation>
                        {
                            installation
                        };
                    }

                    Program.LogEvent($"Custom path deployed to: '{_customPath}'", EventLogEntryType.Information);
                }
                // since no custom installation path was supplied, use default logic
                else
                {
                    Program.Out.WriteLine();
                    Program.Out.WriteLine("Looking for Git installation(s)...");

                    if (Program.Context.Where.FindGitInstallations(out installations))
                    {
                        foreach (var installation in installations)
                        {
                            Program.Out.WriteLine($"  {installation.Path}");
                        }
                    }
                }

                if (installations == null)
                {
                    Program.LogEvent("No Git installation found, unable to continue.", EventLogEntryType.Error);
                    Program.Out.WriteLine();
                    Program.WriteLine("Fatal: Git was not detected, unable to continue. {FailFace}");
                    Pause();

                    Result = ResultValue.GitNotFound;
                    return;
                }

                List<string> copiedFiles;
                foreach (var installation in installations)
                {
                    Program.Out.WriteLine();
                    Program.Out.WriteLine($"Deploying from '{Program.Location}' to '{installation.Path}'.");

                    if (CopyFiles(Program.Location, installation.Libexec, FileList, out copiedFiles))
                    {
                        int copiedCount = copiedFiles.Count;

                        foreach (var file in copiedFiles)
                        {
                            Program.Out.WriteLine($"  {file}");
                        }

                        // copy help documents
                        if (Program.FileSystem.DirectoryExists(installation.Doc)
                            && CopyFiles(Program.Location, installation.Doc, DocsList, out copiedFiles))
                        {
                            copiedCount += copiedFiles.Count;

                            foreach (var file in copiedFiles)
                            {
                                Program.Out.WriteLine($"  {file}");
                            }
                        }

                        Program.LogEvent($"Deployment to '{installation.Path}' succeeded.", EventLogEntryType.Information);
                        Program.Out.WriteLine($"     {copiedCount} file(s) copied");
                    }
                    else if (_isForced)
                    {
                        Program.LogEvent($"Deployment to '{installation.Path}' failed.", EventLogEntryType.Warning);
                        Program.WriteLine($"  deployment failed. {FailFace}");
                    }
                    else
                    {
                        Program.LogEvent($"Deployment to '{installation.Path}' failed.", EventLogEntryType.Error);
                        Program.WriteLine($"  deployment failed. {FailFace}");
                        Pause();

                        Result = ResultValue.DeploymentFailed;
                        return;
                    }
                }

                Program.Out.WriteLine();
                Program.Out.WriteLine($"Deploying from '{Program.Location}' to '{UserBinPath}'.");

                if (!Program.FileSystem.DirectoryExists(UserBinPath))
                {
                    Program.FileSystem.CreateDirectory(UserBinPath);
                }

                if (CopyFiles(Program.Location, UserBinPath, FileList, out copiedFiles))
                {
                    int copiedCount = copiedFiles.Count;

                    foreach (var file in copiedFiles)
                    {
                        Program.Out.WriteLine($"  {file}");
                    }

                    if (CopyFiles(Program.Location, UserBinPath, DocsList, out copiedFiles))
                    {
                        copiedCount = copiedFiles.Count;

                        foreach (var file in copiedFiles)
                        {
                            Program.Out.WriteLine($"  {file}");
                        }
                    }

                    Program.LogEvent($"Deployment to '{UserBinPath}' succeeded.", EventLogEntryType.Information);
                    Program.Out.WriteLine($"     {copiedCount} file(s) copied");
                }
                else if (_isForced)
                {
                    Program.LogEvent($"Deployment to '{UserBinPath}' failed.", EventLogEntryType.Warning);
                    Program.WriteLine($"  deployment failed. {FailFace}");
                }
                else
                {
                    Program.LogEvent($"Deployment to '{UserBinPath}' failed.", EventLogEntryType.Error);
                    Program.WriteLine($"  deployment failed. {FailFace}");
                    Pause();

                    Result = ResultValue.DeploymentFailed;
                    return;
                }

                if (CygwinPath != null && Program.FileSystem.DirectoryExists(CygwinPath))
                {
                    if (CopyFiles(Program.Location, CygwinPath, FileList, out copiedFiles))
                    {
                        int copiedCount = copiedFiles.Count;

                        foreach (var file in copiedFiles)
                        {
                            Program.Out.WriteLine($"  {file}");
                        }

                        if (CopyFiles(Program.Location, CygwinPath, DocsList, out copiedFiles))
                        {
                            copiedCount = copiedFiles.Count;

                            foreach (var file in copiedFiles)
                            {
                                Program.Out.WriteLine($"  {file}");
                            }
                        }

                        Program.LogEvent($"Deployment to '{CygwinPath}' succeeded.", EventLogEntryType.Information);
                        Program.Out.WriteLine($"     {copiedCount} file(s) copied");
                    }
                    else if (_isForced)
                    {
                        Program.LogEvent($"Deployment to '{CygwinPath}' failed.", EventLogEntryType.Warning);
                        Program.WriteLine($"  deployment failed. {FailFace}");
                    }
                    else
                    {
                        Program.LogEvent($"Deployment to '{CygwinPath}' failed.", EventLogEntryType.Error);
                        Program.WriteLine($"  deployment failed. {FailFace}");
                        Pause();

                        Result = ResultValue.DeploymentFailed;
                        return;
                    }
                }

                ConfigurationLevel types = ConfigurationLevel.Global;

                ConfigurationLevel updateTypes;
                if (SetGitConfig(installations, GitConfigAction.Set, types, out updateTypes))
                {
                    if ((updateTypes & ConfigurationLevel.Global) == ConfigurationLevel.Global)
                    {
                        Program.Out.WriteLine("Updated your ~/.gitconfig [git config --global]");
                    }
                    else
                    {
                        Program.Out.WriteLine();
                        Program.WriteLine("Fatal: Unable to update your ~/.gitconfig correctly.");

                        Result = ResultValue.GitConfigGlobalFailed;
                        return;
                    }
                }

                // all necessary content has been deployed to the system
                Result = ResultValue.Success;

                Program.LogEvent($"{Program.Title} v{Program.Version.ToString(3)} successfully deployed.", EventLogEntryType.Information);
                Program.Out.WriteLine();
                Program.Out.WriteLine($"Success! {Program.Title} was deployed! {TadaFace}");
                Pause();
            }
            finally
            {
                SetOutput(true, true);
            }
        }

        public bool DetectNetFx(out Version version)
        {
            const string NetFxKeyBase = @"HKEY_LOCAL_MACHINE\Software\Microsoft\Net Framework Setup\NDP\v4\";
            const string NetFxKeyClient = NetFxKeyBase + @"\Client";
            const string NetFxKeyFull = NetFxKeyBase + @"\Full";
            const string ValueName = "Version";
            const string DefaultValue = "0.0.0";

            // default to not found state
            version = null;

            string netfxString = null;
            Version netfxVerson = null;

            // query for existing installations of .NET
            if ((netfxString = Registry.GetValue(NetFxKeyClient, ValueName, DefaultValue) as string) != null
                    && Version.TryParse(netfxString, out netfxVerson)
                || (netfxString = Registry.GetValue(NetFxKeyFull, ValueName, DefaultValue) as string) != null
                    && Version.TryParse(netfxString, out netfxVerson))
            {
                Program.LogEvent($"NetFx version {netfxVerson.ToString(3)} detected.", EventLogEntryType.Information);
                Program.Context.Trace.WriteLine($"NetFx version {netfxVerson.ToString(3)} detected.");

                version = netfxVerson;
            }

            return version != null;
        }

        public void RemoveConsole()
        {
            SetOutput(_isPassive, _isPassive && _isForced);
            try
            {
                System.Security.Principal.WindowsIdentity identity = System.Security.Principal.WindowsIdentity.GetCurrent();
                System.Security.Principal.WindowsPrincipal principal = new System.Security.Principal.WindowsPrincipal(identity);
                if (!principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator))
                {
                    RemoveElevated();
                    return;
                }

                List<Installation> installations = null;

                // use the custom installation path if supplied
                if (!string.IsNullOrEmpty(_customPath))
                {
                    if (!Program.FileSystem.DirectoryExists(_customPath))
                    {
                        Program.Out.WriteLine();
                        Program.WriteLine($"fatal: custom path does not exist: '{_customPath}'. U_U");
                        Pause();

                        Result = ResultValue.InvalidCustomPath;
                        return;
                    }

                    Program.Out.WriteLine();
                    Program.Out.WriteLine($"Removing from custom path: '{_customPath}'.");

                    // if the custom path points to a git location then treat it properly
                    Installation installation;
                    if (Program.Context.Where.FindGitInstallation(_customPath, KnownDistribution.GitForWindows64v2, out installation)
                        || Program.Context.Where.FindGitInstallation(_customPath, KnownDistribution.GitForWindows32v2, out installation)
                        || Program.Context.Where.FindGitInstallation(_customPath, KnownDistribution.GitForWindows32v1, out installation))
                    {
                        Program.Context.Trace.WriteLine($"Git found: '{installation.Path}'.");

                        // track known Git installations
                        installations = new List<Installation>
                        {
                            installation
                        };
                    }
                }
                // since no custom installation path was supplied, use default logic
                else
                {
                    Program.Out.WriteLine();
                    Program.Out.WriteLine("Looking for Git installation(s)...");

                    if (Program.Context.Where.FindGitInstallations(out installations))
                    {
                        foreach (var installation in installations)
                        {
                            Program.Out.WriteLine($"  {installation.Path}");
                        }
                    }
                }

                if (installations == null)
                {
                    Program.LogEvent($"Git was not detected, unable to continue with removal.", EventLogEntryType.Error);
                    Program.Out.WriteLine();
                    Program.WriteLine("fatal: Git was not detected, unable to continue. U_U");
                    Pause();

                    Result = ResultValue.GitNotFound;
                    return;
                }

                ConfigurationLevel types = ConfigurationLevel.Global | ConfigurationLevel.System;

                ConfigurationLevel updateTypes;
                if (SetGitConfig(installations, GitConfigAction.Unset, types, out updateTypes))
                {
                    if ((updateTypes & ConfigurationLevel.System) == ConfigurationLevel.System)
                    {
                        Program.Out.WriteLine();
                        Program.Out.WriteLine("Updated your /etc/gitconfig [git config --system]");
                    }
                    else
                    {
                        Program.Out.WriteLine();

                        // updating /etc/gitconfig should not fail installation when forced
                        if (!_isForced)
                        {
                            // only 'fatal' when not forced
                            Program.Write("Fatal: ");

                            Result = ResultValue.GitConfigSystemFailed;
                            return;
                        }

                        Program.WriteLine("Unable to update your /etc/gitconfig correctly.");
                    }

                    if ((updateTypes & ConfigurationLevel.Global) == ConfigurationLevel.Global)
                    {
                        Program.Out.WriteLine("Updated your ~/.gitconfig [git config --global]");
                    }
                    else
                    {
                        Program.Out.WriteLine();
                        Program.WriteLine("Fatal: Unable to update your ~/.gitconfig correctly.");

                        Result = ResultValue.GitConfigGlobalFailed;
                        return;
                    }
                }

                List<string> cleanedFiles;
                foreach (var installation in installations)
                {
                    Program.Out.WriteLine();
                    Program.Out.WriteLine($"Removing from '{installation.Path}'.");

                    if (CleanFiles(installation.Libexec, FileList, out cleanedFiles))
                    {
                        int cleanedCount = cleanedFiles.Count;

                        foreach (var file in cleanedFiles)
                        {
                            Program.Out.WriteLine($"  {file}");
                        }

                        // clean help documents
                        if (Program.FileSystem.DirectoryExists(installation.Doc)
                            && CleanFiles(installation.Doc, DocsList, out cleanedFiles))
                        {
                            cleanedCount += cleanedFiles.Count;

                            foreach (var file in cleanedFiles)
                            {
                                Program.Out.WriteLine($"  {file}");
                            }
                        }

                        Program.Out.WriteLine($"     {cleanedCount} file(s) cleaned");
                    }
                    else if (_isForced)
                    {
                        Program.Error.WriteLine($"  removal failed. {FailFace}");
                    }
                    else
                    {
                        Program.Error.WriteLine($"  removal failed. {FailFace}");
                        Pause();

                        Result = ResultValue.RemovalFailed;
                        return;
                    }
                }

                if (Program.FileSystem.DirectoryExists(UserBinPath))
                {
                    Program.Out.WriteLine();
                    Program.Out.WriteLine($"Removing from '{UserBinPath}'.");

                    if (CleanFiles(UserBinPath, FileList, out cleanedFiles))
                    {
                        int cleanedCount = cleanedFiles.Count;

                        foreach (var file in cleanedFiles)
                        {
                            Program.Out.WriteLine($"  {file}");
                        }

                        if (CleanFiles(UserBinPath, DocsList, out cleanedFiles))
                        {
                            cleanedCount += cleanedFiles.Count;

                            foreach (var file in cleanedFiles)
                            {
                                Program.Out.WriteLine($"  {file}");
                            }
                        }

                        Program.Out.WriteLine($"     {cleanedCount} file(s) cleaned");
                    }
                    else if (_isForced)
                    {
                        Program.Error.WriteLine($"  removal failed. {FailFace}");
                    }
                    else
                    {
                        Program.Error.WriteLine($"  removal failed. {FailFace}");
                        Pause();

                        Result = ResultValue.RemovalFailed;
                        return;
                    }
                }

                if (CygwinPath != null && Program.FileSystem.DirectoryExists(CygwinPath))
                {
                    if (CleanFiles(CygwinPath, FileList, out cleanedFiles))
                    {
                        int cleanedCount = cleanedFiles.Count;

                        foreach (var file in cleanedFiles)
                        {
                            Program.Out.WriteLine($"  {file}");
                        }

                        if (CleanFiles(CygwinPath, DocsList, out cleanedFiles))
                        {
                            cleanedCount += cleanedFiles.Count;

                            foreach (var file in cleanedFiles)
                            {
                                Program.Out.WriteLine($"  {file}");
                            }
                        }

                        Program.Out.WriteLine($"     {cleanedCount} file(s) cleaned");
                    }
                }

                // all necessary content has been deployed to the system
                Result = ResultValue.Success;

                Program.LogEvent($"{Program.Title} successfully removed.", EventLogEntryType.Information);

                Program.Out.WriteLine();
                Program.Out.WriteLine($"Success! {Program.Title} was removed! {TadaFace}");
                Pause();
            }
            finally
            {
                SetOutput(true, true);
            }
        }

        public bool SetGitConfig(List<Installation> installations, GitConfigAction action, ConfigurationLevel type, out ConfigurationLevel updated)
        {
            Program.Context.Trace.WriteLine($"action = '{action}'.");

            updated = ConfigurationLevel.None;

            if ((installations == null || installations.Count == 0) && !Program.Context.Where.FindGitInstallations(out installations))
            {
                Program.Context.Trace.WriteLine("No Git installations detected to update.");
                return false;
            }

            if ((type & ConfigurationLevel.Global) == ConfigurationLevel.Global)
            {
                // the 0 entry in the installations list is the "preferred" instance of Git
                string gitCmdPath = installations[0].Git;
                string globalCmd = action == GitConfigAction.Set
                    ? "config --global credential.helper manager"
                    : "config --global --unset credential.helper";

                if (ExecuteGit(gitCmdPath, globalCmd, 0, 5))
                {
                    Program.Context.Trace.WriteLine("updating ~/.gitconfig succeeded.");

                    updated |= ConfigurationLevel.Global;
                }
                else
                {
                    Program.Context.Trace.WriteLine("updating ~/.gitconfig failed.");

                    Program.Out.WriteLine();
                    Program.Error.WriteLine("Fatal: Unable to update ~/.gitconfig.");
                    Pause();
                    return false;
                }
            }

            if ((type & ConfigurationLevel.System) == ConfigurationLevel.System)
            {
                string systemCmd = action == GitConfigAction.Set
                    ? "config --system credential.helper manager"
                    : "config --system --unset credential.helper";

                int successCount = 0;

                foreach (var installation in installations)
                {
                    if (ExecuteGit(installation.Git, systemCmd, 0, 5))
                    {
                        Program.Context.Trace.WriteLine("updating /etc/gitconfig succeeded.");

                        successCount++;
                    }
                    else
                    {
                        Program.Context.Trace.WriteLine("updating ~/.gitconfig failed.");
                    }
                }

                if (successCount == installations.Count)
                {
                    updated |= ConfigurationLevel.System;
                }
                else
                {
                    return false;
                }
            }

            return true;
        }

        private bool CleanFiles(string path, IReadOnlyList<string> files, out List<string> cleanedFiles)
        {
            cleanedFiles = new List<string>();

            if (!Program.FileSystem.DirectoryExists(path))
            {
                Program.Context.Trace.WriteLine($"path '{path}' does not exist.");
                return false;
            }

            try
            {
                foreach (string file in files)
                {
                    string target = Path.Combine(path, file);

                    Program.Context.Trace.WriteLine($"clean '{target}'.");

                    Program.FileSystem.FileDelete(target);

                    cleanedFiles.Add(file);
                }

                return true;
            }
            catch
            {
                Program.Context.Trace.WriteLine($"clean of '{path}' failed.");
                return false;
            }
        }

        private bool CopyFiles(string srcPath, string dstPath, IReadOnlyList<string> files, out List<string> copiedFiles)
        {
            copiedFiles = new List<string>();

            if (!Program.FileSystem.DirectoryExists(srcPath))
            {
                Program.Context.Trace.WriteLine($"source '{srcPath}' does not exist.");
                return false;
            }

            if (Program.FileSystem.DirectoryExists(dstPath))
            {
                try
                {
                    foreach (string file in files)
                    {
                        Program.Context.Trace.WriteLine($"copy '{file}' from '{srcPath}' to '{dstPath}'.");

                        string src = Path.Combine(srcPath, file);
                        string dst = Path.Combine(dstPath, file);

                        Program.FileSystem.FileCopy(src, dst, true);

                        copiedFiles.Add(file);
                    }

                    return true;
                }
                catch
                {
                    Program.Context.Trace.WriteLine("copy failed.");
                    return false;
                }
            }
            else
            {
                Program.Context.Trace.WriteLine($"destination '{dstPath}' does not exist.");
            }

            Program.Context.Trace.WriteLine("copy failed.");
            return false;
        }

        private void DeployElevated()
        {
            if (_isPassive)
            {
                Result = ResultValue.Unprivileged;
            }
            else
            {
                /* cannot install while not elevated (need access to %PROGRAMFILES%), re-launch
                   self as an elevated process with identical arguments. */

                // build arguments
                var arguments = new System.Text.StringBuilder("install");
                if (_isPassive)
                {
                    arguments.Append(" ")
                             .Append(ParamPassiveKey);
                }
                if (_isForced)
                {
                    arguments.Append(" ")
                             .Append(ParamForceKey);
                }
                if (!string.IsNullOrEmpty(_customPath))
                {
                    arguments.Append(" ")
                             .Append(ParamForceKey)
                             .Append(" \"")
                             .Append(_customPath)
                             .Append("\"");
                }

                // build process start options
                var options = new ProcessStartInfo()
                {
                    FileName = "cmd",
                    Arguments = $"/c \"{Program.ExecutablePath}\" {arguments}",
                    UseShellExecute = true, // shellexecute for verb usage
                    Verb = "runas", // used to invoke elevation
                    WorkingDirectory = Program.Location,
                };

                Program.Context.Trace.WriteLine($"create process: cmd '{options.Verb}' '{options.FileName}' '{options.Arguments}' .");

                try
                {
                    // create the process
                    var elevated = Process.Start(options);

                    // wait for the process to complete
                    elevated.WaitForExit();

                    Program.Context.Trace.WriteLine($"process exited with {elevated.ExitCode}.");

                    // exit with the elevated process' exit code
                    ExitCode = elevated.ExitCode;
                }
                catch (Exception exception)
                {
                    Program.Context.Trace.WriteLine($"process failed with '{exception.Message}'");
                    Result = ResultValue.Unprivileged;
                }
            }
        }

        private bool ExecuteGit(string gitCmdPath, string command, params int[] allowedExitCodes)
        {
            if (string.IsNullOrEmpty(gitCmdPath) || string.IsNullOrEmpty(command))
                return false;

            if (!Program.FileSystem.FileExists(gitCmdPath))
                return false;

            var options = new ProcessStartInfo()
            {
                Arguments = command,
                FileName = gitCmdPath,
                CreateNoWindow = true,
                UseShellExecute = false,
            };

            Program.Context.Trace.WriteLine($"create process: cmd '{options.FileName}' '{options.Arguments}' .");

            var gitProcess = Process.Start(options);

            gitProcess.WaitForExit();

            Program.Context.Trace.WriteLine($"Git exited with {gitProcess.ExitCode}.");

            if (allowedExitCodes != null && allowedExitCodes.Length > 0)
                return allowedExitCodes.Contains(gitProcess.ExitCode);
            else
                return gitProcess.ExitCode == 0;
        }

        private void Pause()
        {
            if (!_isPassive)
            {
                Program.Out.WriteLine();
                Program.Out.WriteLine("Press any key to continue...");
                Program.ReadKey();
            }
        }

        private void RemoveElevated()
        {
            if (_isPassive)
            {
                Result = ResultValue.Unprivileged;
            }
            else
            {
                /* cannot uninstall while not elevated (need access to %PROGRAMFILES%), re-launch
                   self as an elevated process with identical arguments. */

                // build arguments
                var arguments = new System.Text.StringBuilder("remove");
                if (_isPassive)
                {
                    arguments.Append(" ")
                             .Append(ParamPassiveKey);
                }
                if (_isForced)
                {
                    arguments.Append(" ")
                             .Append(ParamForceKey);
                }
                if (!string.IsNullOrEmpty(_customPath))
                {
                    arguments.Append(" ")
                             .Append(ParamForceKey)
                             .Append(" \"")
                             .Append(_customPath)
                             .Append("\"");
                }

                // build process start options
                var options = new ProcessStartInfo()
                {
                    FileName = "cmd",
                    Arguments = $"/c \"{Program.ExecutablePath}\" {arguments}",
                    UseShellExecute = true, // shellexecute for verb usage
                    Verb = "runas", // used to invoke elevation
                    WorkingDirectory = Program.Location,
                };

                Program.Context.Trace.WriteLine($"create process: cmd '{options.Verb}' '{options.FileName}' '{options.Arguments}' .");

                try
                {
                    // create the process
                    var elevated = Process.Start(options);

                    // wait for the process to complete
                    elevated.WaitForExit();

                    Program.Context.Trace.WriteLine($"process exited with {elevated.ExitCode}.");

                    // exit with the elevated process' exit code
                    ExitCode = elevated.ExitCode;
                }
                catch (Exception exception)
                {
                    Program.Context.Trace.WriteLine($"! process failed with '{exception.Message}'.");
                    Result = ResultValue.Unprivileged;
                }
            }
        }

        private void SetOutput(bool muteStdout, bool muteStderr)
        {
            if (muteStdout)
            {
                _stdout = Program.Out;
                Program.SetOut(TextWriter.Null);
            }
            else if (_stdout != null)
            {
                Program.SetOut(_stdout);
                _stdout = null;
            }

            if (muteStderr)
            {
                _stderr = Program.Out;
                Program.SetOut(TextWriter.Null);
            }
            else if (_stderr != null)
            {
                Program.SetOut(_stderr);
                _stderr = null;
            }
        }

        public enum ResultValue : int
        {
            UnknownFailure = -1,
            Success = 0,
            InvalidCustomPath,
            DeploymentFailed,
            NetFxNotFound,
            Unprivileged,
            GitConfigGlobalFailed,
            GitConfigSystemFailed,
            GitNotFound,
            RemovalFailed,
        }

        public enum GitConfigAction
        {
            Set,
            Unset,
        }
    }
}
