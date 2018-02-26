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
using System.Threading.Tasks;
using Microsoft.Alm.Authentication;
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
            var trace = program.Context.Trace;

            // parse arguments
            for (int i = 2; i < args.Length; i++)
            {
                if (string.Equals(args[i], ParamPathKey, StringComparison.OrdinalIgnoreCase))
                {
                    if (args.Length > i + 1)
                    {
                        i += 1;
                        _customPath = args[i];

                        trace.WriteLine($"{ParamPathKey} = '{_customPath}'.");
                    }
                }
                else if (string.Equals(args[i], ParamPassiveKey, StringComparison.OrdinalIgnoreCase))
                {
                    _isPassive = true;

                    trace.WriteLine($"{ParamPassiveKey} = true.");
                }
                else if (string.Equals(args[i], ParamForceKey, StringComparison.OrdinalIgnoreCase))
                {
                    _isForced = true;

                    trace.WriteLine($"{ParamForceKey} = true.");
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

                    var fs = _program.Context.FileSystem;
                    var trace = _program.Context.Trace;

                    foreach (var drive in fs.GetDriveRoots())
                    {
                        string path = Path.Combine(drive, Cygwin64GitPath);

                        if (fs.DirectoryExists(path))
                        {
                            trace.WriteLine($"cygwin directory found at '{path}'.");

                            _cygwinPath = path;
                            break;
                        }

                        path = Path.Combine(drive, Cygwin32GitPath);

                        if (fs.DirectoryExists(path))
                        {
                            trace.WriteLine($"cygwin directory found at '{path}'.");

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

                    var fs = Program.Context.FileSystem;

                    // Git for Windows checks %HOME% first
                    if ((val1 = Environment.GetEnvironmentVariables(EnvironmentVariableTarget.Process)["HOME"] as string) != null
                        && fs.DirectoryExists(val1))
                    {
                        _userBinPath = val1;
                    }
                    // Git for Windows checks %HOMEDRIVE%%HOMEPATH% second
                    else if ((val1 = Environment.GetEnvironmentVariables(EnvironmentVariableTarget.Process)["HOMEDRIVE"] as string) != null
                            && (val2 = Environment.GetEnvironmentVariables(EnvironmentVariableTarget.Process)["HOMEPATH"] as string) != null
                        && fs.DirectoryExists(val3 = val1 + val2))
                    {
                        _userBinPath = val3;
                    }
                    // Git for Windows checks %USERPROFILE% last
                    else if ((val1 = Environment.GetEnvironmentVariables(EnvironmentVariableTarget.Process)["USERPROFILE"] as string) != null)
                    {
                        _userBinPath = val1;
                    }

                    if (_userBinPath != null)
                    {
                        // Git for Windows adds %HOME%\bin to %PATH%
                        _userBinPath = Path.Combine(_userBinPath, "bin");

                        _program.Context.Trace.WriteLine($"user bin found at '{_userBinPath}'.");
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

                var fs = _program.Context.FileSystem;
                var trace = _program.Context.Trace;
                var where = _program.Context.Where;

                List<Git.GitInstallation> installations = null;

                // use the custom installation path if supplied
                if (!string.IsNullOrEmpty(_customPath))
                {
                    if (!fs.DirectoryExists(_customPath))
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
                    Git.GitInstallation installation;
                    if (where.FindGitInstallation(_customPath, Git.KnownGitDistribution.GitForWindows64v2, out installation)
                        || where.FindGitInstallation(_customPath, Git.KnownGitDistribution.GitForWindows32v2, out installation)
                        || where.FindGitInstallation(_customPath, Git.KnownGitDistribution.GitForWindows32v1, out installation))
                    {
                        trace.WriteLine($"   Git found: '{installation.Path}'.");

                        // track known Git installations
                        installations = new List<Git.GitInstallation>
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

                    if (where.FindGitInstallations(out installations))
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
                    Program.WriteLine();
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

                        // Copy help documents.
                        if (fs.DirectoryExists(installation.Doc)
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

                if (!fs.DirectoryExists(UserBinPath))
                {
                    fs.CreateDirectory(UserBinPath);
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

                if (CygwinPath != null && fs.DirectoryExists(CygwinPath))
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

                Git.ConfigurationLevel types = Git.ConfigurationLevel.Global;

                Git.ConfigurationLevel updateTypes;
                if (SetGitConfig(installations, GitConfigAction.Set, types, out updateTypes))
                {
                    if ((updateTypes & Git.ConfigurationLevel.Global) == Git.ConfigurationLevel.Global)
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

            Version netfxVerson = null;

            // query for existing installations of .NET
            if (Registry.GetValue(NetFxKeyClient, ValueName, DefaultValue) is string netfxString && Version.TryParse(netfxString, out netfxVerson)
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

                var fs = Program.Context.FileSystem;
                var trace = Program.Context.Trace;
                var where = Program.Context.Where;

                List<Git.GitInstallation> installations = null;

                // Use the custom installation path if supplied.
                if (!string.IsNullOrEmpty(_customPath))
                {
                    if (!fs.DirectoryExists(_customPath))
                    {
                        Program.Out.WriteLine();
                        Program.WriteLine($"fatal: custom path does not exist: '{_customPath}'. U_U");
                        Pause();

                        Result = ResultValue.InvalidCustomPath;
                        return;
                    }

                    Program.Out.WriteLine();
                    Program.Out.WriteLine($"Removing from custom path: '{_customPath}'.");

                    // If the custom path points to a git location then treat it properly.
                    Git.GitInstallation installation;
                    if (where.FindGitInstallation(_customPath, Git.KnownGitDistribution.GitForWindows64v2, out installation)
                        || where.FindGitInstallation(_customPath, Git.KnownGitDistribution.GitForWindows32v2, out installation)
                        || where.FindGitInstallation(_customPath, Git.KnownGitDistribution.GitForWindows32v1, out installation))
                    {
                        trace.WriteLine($"Git found: '{installation.Path}'.");

                        // track known Git installations
                        installations = new List<Git.GitInstallation>
                        {
                            installation
                        };
                    }
                }
                // Since no custom installation path was supplied, use default logic.
                else
                {
                    Program.Out.WriteLine();
                    Program.Out.WriteLine("Looking for Git installation(s)...");

                    if (where.FindGitInstallations(out installations))
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

                Git.ConfigurationLevel types = Git.ConfigurationLevel.Global | Git.ConfigurationLevel.System;

                Git.ConfigurationLevel updateTypes;
                if (SetGitConfig(installations, GitConfigAction.Unset, types, out updateTypes))
                {
                    if ((updateTypes & Git.ConfigurationLevel.System) == Git.ConfigurationLevel.System)
                    {
                        Program.Out.WriteLine();
                        Program.Out.WriteLine("Updated your /etc/gitconfig [git config --system]");
                    }
                    else
                    {
                        Program.Out.WriteLine();

                        // Updating /etc/gitconfig should not fail installation when forced.
                        if (!_isForced)
                        {
                            // only 'fatal' when not forced
                            Program.Write("Fatal: ");

                            Result = ResultValue.GitConfigSystemFailed;
                            return;
                        }

                        Program.WriteLine("Unable to update your /etc/gitconfig correctly.");
                    }

                    if ((updateTypes & Git.ConfigurationLevel.Global) == Git.ConfigurationLevel.Global)
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

                        // Clean help documents.
                        if (fs.DirectoryExists(installation.Doc)
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

                if (fs.DirectoryExists(UserBinPath))
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

                if (CygwinPath != null && fs.DirectoryExists(CygwinPath))
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

                // All necessary content has been deployed to the system.
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

        public bool SetGitConfig(List<Git.GitInstallation> installations, GitConfigAction action, Git.ConfigurationLevel type, out Git.ConfigurationLevel updated)
        {
            var trace = Program.Context.Trace;
            var where = Program.Context.Where;

            trace.WriteLine($"action = '{action}'.");

            updated = Git.ConfigurationLevel.None;

            if ((installations == null || installations.Count == 0) && !where.FindGitInstallations(out installations))
            {
                trace.WriteLine("No Git installations detected to update.");
                return false;
            }

            if ((type & Git.ConfigurationLevel.Global) == Git.ConfigurationLevel.Global)
            {
                // the 0 entry in the installations list is the "preferred" instance of Git
                string gitCmdPath = installations[0].Git;
                string globalCmd = action == GitConfigAction.Set
                    ? "config --global credential.helper manager"
                    : "config --global --unset credential.helper";

                if (ExecuteGit(gitCmdPath, globalCmd, 0, 5))
                {
                    trace.WriteLine("updating ~/.gitconfig succeeded.");

                    updated |= Git.ConfigurationLevel.Global;
                }
                else
                {
                    trace.WriteLine("updating ~/.gitconfig failed.");

                    Program.Out.WriteLine();
                    Program.Error.WriteLine("Fatal: Unable to update ~/.gitconfig.");
                    Pause();
                    return false;
                }
            }

            if ((type & Git.ConfigurationLevel.System) == Git.ConfigurationLevel.System)
            {
                string systemCmd = action == GitConfigAction.Set
                    ? "config --system credential.helper manager"
                    : "config --system --unset credential.helper";

                int successCount = 0;

                foreach (var installation in installations)
                {
                    if (ExecuteGit(installation.Git, systemCmd, 0, 5))
                    {
                        trace.WriteLine("updating /etc/gitconfig succeeded.");

                        successCount++;
                    }
                    else
                    {
                        trace.WriteLine("updating ~/.gitconfig failed.");
                    }
                }

                if (successCount == installations.Count)
                {
                    updated |= Git.ConfigurationLevel.System;
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

            var fs = Program.Context.FileSystem;
            var trace = Program.Context.Trace;

            if (!fs.DirectoryExists(path))
            {
                trace.WriteLine($"path '{path}' does not exist.");
                return false;
            }

            try
            {
                foreach (string file in files)
                {
                    string target = Path.Combine(path, file);

                    trace.WriteLine($"clean '{target}'.");

                    fs.FileDelete(target);

                    cleanedFiles.Add(file);
                }

                return true;
            }
            catch
            {
                trace.WriteLine($"clean of '{path}' failed.");
                return false;
            }
        }

        private bool CopyFiles(string srcPath, string dstPath, IReadOnlyList<string> files, out List<string> copiedFiles)
        {
            if (srcPath is null)
                throw new ArgumentNullException(nameof(srcPath));
            if (dstPath is null)
                throw new ArgumentNullException(nameof(dstPath));
            if (files is null)
                throw new ArgumentNullException(nameof(files));

            var fs = Program.Context.FileSystem;
            var trace = Program.Context.Trace;

            copiedFiles = new List<string>();

            if (!fs.DirectoryExists(srcPath))
            {
                trace.WriteLine($"source '{srcPath}' does not exist.");
                return false;
            }

            if (fs.DirectoryExists(dstPath))
            {
                try
                {
                    foreach (string file in files)
                    {
                        trace.WriteLine($"copy '{file}' from '{srcPath}' to '{dstPath}'.");

                        string src = Path.Combine(srcPath, file);
                        string dst = Path.Combine(dstPath, file);

                        fs.FileCopy(src, dst, true);

                        copiedFiles.Add(file);
                    }

                    return true;
                }
                catch
                {
                    trace.WriteLine("copy failed.");
                    return false;
                }
            }
            else
            {
                trace.WriteLine($"destination '{dstPath}' does not exist.");
            }

            trace.WriteLine("copy failed.");
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

                var trace = Program.Context.Trace;

                // build process start options
                var options = new ProcessStartInfo()
                {
                    FileName = "cmd",
                    Arguments = $"/c \"{Program.ExecutablePath}\" {arguments}",
                    UseShellExecute = true, // shellexecute for verb usage
                    Verb = "runas", // used to invoke elevation
                    WorkingDirectory = Program.Location,
                };

                trace.WriteLine($"create process: cmd '{options.Verb}' '{options.FileName}' '{options.Arguments}' .");

                try
                {
                    // create the process
                    var elevated = Process.Start(options);

                    // wait for the process to complete
                    elevated.WaitForExit();

                    trace.WriteLine($"process exited with {elevated.ExitCode}.");

                    // exit with the elevated process' exit code
                    ExitCode = elevated.ExitCode;
                }
                catch (Exception exception)
                {
                    trace.WriteLine($"process failed with '{exception.Message}'");
                    Result = ResultValue.Unprivileged;
                }
            }
        }

        private bool ExecuteGit(string gitCmdPath, string command, params int[] allowedExitCodes)
        {
            if (string.IsNullOrEmpty(gitCmdPath) || string.IsNullOrEmpty(command))
                return false;

            var fs = Program.Context.FileSystem;
            var trace = Program.Context.Trace;

            if (!fs.FileExists(gitCmdPath))
                return false;

            var options = new ProcessStartInfo()
            {
                Arguments = command,
                FileName = gitCmdPath,
                CreateNoWindow = true,
                UseShellExecute = false,
            };

            trace.WriteLine($"create process: cmd '{options.FileName}' '{options.Arguments}' .");

            var gitProcess = Process.Start(options);

            gitProcess.WaitForExit();

            trace.WriteLine($"Git exited with {gitProcess.ExitCode}.");

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

                var trace = Program.Context.Trace;

                trace.WriteLine($"create process: cmd '{options.Verb}' '{options.FileName}' '{options.Arguments}' .");

                try
                {
                    // create the process
                    var elevated = Process.Start(options);

                    // wait for the process to complete
                    elevated.WaitForExit();

                    trace.WriteLine($"process exited with {elevated.ExitCode}.");

                    // exit with the elevated process' exit code
                    ExitCode = elevated.ExitCode;
                }
                catch (Exception exception)
                {
                    trace.WriteLine($"! process failed with '{exception.Message}'.");
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
