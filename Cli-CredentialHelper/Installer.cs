using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.Alm.Git;
using Microsoft.Win32;

namespace Microsoft.Alm.CredentialHelper
{
    internal class Installer
    {
        internal const string ParamPathKey = "--path";
        internal const string ParamPassiveKey = "--passive";
        internal const string ParamForceKey = "--force";
        private static readonly Version NetFxMinVersion = new Version(4, 5, 1);
        private static readonly IReadOnlyList<string> Files = new List<string>
        {
            "Microsoft.Alm.Authentication.dll",
            "Microsoft.Alm.Git.dll",
            "Microsoft.IdentityModel.Clients.ActiveDirectory.dll",
            "Microsoft.IdentityModel.Clients.ActiveDirectory.WindowsForms.dll",
            "git-credential-manager.exe"
        };

        public Installer()
        {
            var args = Environment.GetCommandLineArgs();

            // parse arguments
            for (int i = 2; i < args.Length; i++)
            {
                if (String.Equals(args[i], ParamPathKey, StringComparison.OrdinalIgnoreCase))
                {
                    if (args.Length > i + 1)
                    {
                        i += 1;
                        _customPath = args[i];

                        Trace.WriteLine("  " + ParamPathKey + " = '" + _customPath + "'.");
                    }
                }
                else if (String.Equals(args[i], ParamPassiveKey, StringComparison.OrdinalIgnoreCase))
                {
                    _isPassive = true;

                    Trace.WriteLine("  " + ParamPassiveKey + " = true.");
                }
                else if (String.Equals(args[i], ParamForceKey, StringComparison.OrdinalIgnoreCase))
                {
                    _isForced = true;

                    Trace.WriteLine("  " + ParamForceKey + " = true.");
                }
            }
        }

        private static string UserBinPath
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
                        && Directory.Exists(val1))
                    {
                        _userBinPath = val1;
                    }
                    // Git for Windows checks %HOMEDRIVE%%HOMEPATH% second
                    else if ((val1 = vars["HOMEDRIVE"] as string) != null && (val2 = vars["HOMEPATH"] as string) != null
                        && Directory.Exists(val3 = val1 + val2))
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
                    }
                }
                return _userBinPath;
            }
        }
        private static string _userBinPath = null;

        public int ExitCode
        {
            get { return (int)Result; }
            set { Result = (ResultValue)value; }
        }
        public ResultValue Result { get; private set; }

        private bool _isPassive = false;
        private bool _isForced = false;
        private string _customPath = null;
        private TextWriter _stdout = null;
        private TextWriter _stderr = null;

        public void DeployConsole()
        {
            Trace.WriteLine("Installer::DeployConsole");

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

                List<GitInstallation> installations = null;

                // use the custom installation path if supplied
                if (!String.IsNullOrEmpty(_customPath))
                {
                    if (!Directory.Exists(_customPath))
                    {
                        Console.Out.WriteLine();
                        Console.Error.WriteLine("Fatal: custom path does not exist: '{0}'. U_U", _customPath);
                        Pause();

                        Result = ResultValue.InvalidCustomPath;
                        return;
                    }

                    Console.Out.WriteLine();
                    Console.Out.WriteLine("Deploying to custom path: '{0}'.", _customPath);

                    // if the custom path points to a git location then treat it properly
                    GitInstallation installation;
                    if (Where.FindGitInstallation(_customPath, KnownGitDistribution.GitForWindows64v2, out installation)
                        || Where.FindGitInstallation(_customPath, KnownGitDistribution.GitForWindows32v2, out installation)
                        || Where.FindGitInstallation(_customPath, KnownGitDistribution.GitForWindows32v1, out installation))
                    {
                        Trace.Write("   Git found: " + installation.Path);

                        // track known Git installtations
                        installations = new List<GitInstallation>();
                        installations.Add(installation);
                    }
                }
                // since no custom installation path was supplied, use default logic
                else
                {
                    Console.Out.WriteLine();
                    Console.Out.WriteLine("Looking for Git installation(s)...");

                    if (Where.FindGitInstallations(out installations))
                    {
                        foreach (var installation in installations)
                        {
                            Console.Out.WriteLine("  {0}", installation.Path);
                        }
                    }
                }

                if (installations == null)
                {
                    Console.Out.WriteLine();
                    Console.Error.WriteLine("Fatal: Git was not detected, unable to continue. U_U");
                    Pause();

                    Result = ResultValue.GitNotFound;
                    return;
                }

                List<string> cleanedFiles;
                foreach (var installation in installations)
                {
                    Console.Out.WriteLine();
                    Console.Out.WriteLine("Deploying from '{0}' to '{1}'.", Program.Location, installation.Path);

                    if (CopyFiles(Program.Location, installation.Libexec, out cleanedFiles))
                    {
                        foreach (var file in cleanedFiles)
                        {
                            Console.Out.WriteLine("  {0}", file);
                        }

                        Console.Out.WriteLine("        {0} file(s) copied", cleanedFiles.Count);
                    }
                    else if (_isForced)
                    {
                        Console.Error.WriteLine("  deployment failed. U_U");
                    }
                    else
                    {
                        Console.Error.WriteLine("  deployment failed. U_U");
                        Pause();

                        Result = ResultValue.RemovalFailed;
                        return;
                    }
                }

                Console.Out.WriteLine();
                Console.Out.WriteLine("Deploying from '{0}' to '{1}'.", Program.Location, UserBinPath);

                if (!Directory.Exists(UserBinPath))
                {
                    Directory.CreateDirectory(UserBinPath);
                }

                if (CopyFiles(Program.Location, UserBinPath, out cleanedFiles))
                {
                    foreach (var file in cleanedFiles)
                    {
                        Console.Out.WriteLine("  {0}", file);
                    }

                    Console.Out.WriteLine("        {0} file(s) copied", cleanedFiles.Count);
                }
                else if (_isForced)
                {
                    Console.Error.WriteLine("  deployment failed. U_U");
                }
                else
                {
                    Console.Error.WriteLine("  deployment failed. U_U");
                    Pause();

                    Result = ResultValue.RemovalFailed;
                    return;
                }

                Configuration.Type types = Configuration.Type.Global;

                Configuration.Type updateTypes;
                if (SetGitConfig(installations, GitConfigAction.Set, types, out updateTypes))
                {
                    if ((updateTypes & Configuration.Type.Global) == Configuration.Type.Global)
                    {
                        Console.Out.WriteLine("Updated your ~/.gitconfig [git config --global]");
                    }
                    else
                    {
                        Console.Out.WriteLine();
                        Console.Error.WriteLine("Fatal: Unable to update your ~/.gitconfig correctly.");

                        Result = ResultValue.GitConfigGlobalFailed;
                        return;
                    }
                }

                // all necissary content has been deployed to the system
                Result = ResultValue.Success;

                Console.Out.WriteLine();
                Console.Out.WriteLine("Success! {0} was deployed! ^_^", Program.Title);
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

            Trace.WriteLine("Installer::DetectNetFx");

            // default to not found state
            version = null;

            string netfxString = null;
            Version netfxVerson = null;

            // query for existing installations of .NET
            if ((netfxString = Registry.GetValue(NetFxKeyClient, ValueName, DefaultValue) as String) != null
                    && Version.TryParse(netfxString, out netfxVerson)
                || (netfxString = Registry.GetValue(NetFxKeyFull, ValueName, DefaultValue) as String) != null
                    && Version.TryParse(netfxString, out netfxVerson))
            {
                Trace.WriteLine("   .NET version " + netfxVerson.ToString(3) + " detected.");

                version = netfxVerson;
            }

            return version != null;
        }

        public void RemoveConsole()
        {
            Trace.WriteLine("Installer::RemoveConsole");

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

                List<GitInstallation> installations = null;

                // use the custom installation path if supplied
                if (!String.IsNullOrEmpty(_customPath))
                {
                    if (!Directory.Exists(_customPath))
                    {
                        Console.Out.WriteLine();
                        Console.Error.WriteLine("Fatal: custom path does not exist: '{0}'. U_U", _customPath);
                        Pause();

                        Result = ResultValue.InvalidCustomPath;
                        return;
                    }

                    Console.Out.WriteLine();
                    Console.Out.WriteLine("Removing from custom path: '{0}'.", _customPath);

                    // if the custom path points to a git location then treat it properly
                    GitInstallation installation;
                    if (Where.FindGitInstallation(_customPath, KnownGitDistribution.GitForWindows64v2, out installation)
                        || Where.FindGitInstallation(_customPath, KnownGitDistribution.GitForWindows32v2, out installation)
                        || Where.FindGitInstallation(_customPath, KnownGitDistribution.GitForWindows32v1, out installation))
                    {
                        Trace.Write("   Git found: " + installation.Path);

                        // track known Git installtations
                        installations = new List<GitInstallation>();
                        installations.Add(installation);
                    }
                }
                // since no custom installation path was supplied, use default logic
                else
                {
                    Console.Out.WriteLine();
                    Console.Out.WriteLine("Looking for Git installation(s)...");

                    if (Where.FindGitInstallations(out installations))
                    {
                        foreach (var installation in installations)
                        {
                            Console.Out.WriteLine("  {0}", installation.Path);
                        }
                    }
                }

                if (installations == null)
                {
                    Console.Out.WriteLine();
                    Console.Error.WriteLine("Fatal: Git was not detected, unable to continue. U_U");
                    Pause();

                    Result = ResultValue.GitNotFound;
                    return;
                }

                Configuration.Type types = Configuration.Type.Global | Configuration.Type.System;

                Configuration.Type updateTypes;
                if (SetGitConfig(installations, GitConfigAction.Unset, types, out updateTypes))
                {
                    if ((updateTypes & Configuration.Type.System) == Configuration.Type.System)
                    {
                        Console.Out.WriteLine();
                        Console.Out.WriteLine("Updated your /etc/gitconfig [git config --system]");
                    }
                    else
                    {
                        Console.Out.WriteLine();

                        // updating /etc/gitconfig should not fail installation when forced
                        if (!_isForced)
                        {
                            // only 'fatal' when not forced
                            Console.Error.Write("Fatal: ");

                            Result = ResultValue.GitConfigSystemFailed;
                            return;
                        }

                        Console.Error.WriteLine("Unable to update your /etc/gitconfig correctly.");
                    }

                    if ((updateTypes & Configuration.Type.Global) == Configuration.Type.Global)
                    {
                        Console.Out.WriteLine("Updated your ~/.gitconfig [git config --global]");
                    }
                    else
                    {
                        Console.Out.WriteLine();
                        Console.Error.WriteLine("Fatal: Unable to update your ~/.gitconfig correctly.");

                        Result = ResultValue.GitConfigGlobalFailed;
                        return;
                    }
                }

                List<string> cleanedFiles;
                foreach (var installation in installations)
                {
                    Console.Out.WriteLine();
                    Console.Out.WriteLine("Removing from '{0}'.", installation.Path);

                    if (CleanFiles(installation.Libexec, out cleanedFiles))
                    {
                        foreach (var file in cleanedFiles)
                        {
                            Console.Out.WriteLine("  {0}", file);
                        }

                        Console.Out.WriteLine("        {0} file(s) cleaned", cleanedFiles.Count);
                    }
                    else if (_isForced)
                    {
                        Console.Error.WriteLine("  removal failed. U_U");
                    }
                    else
                    {
                        Console.Error.WriteLine("  removal failed. U_U");
                        Pause();

                        Result = ResultValue.RemovalFailed;
                        return;
                    }
                }

                if (Directory.Exists(UserBinPath))
                {
                    Console.Out.WriteLine();
                    Console.Out.WriteLine("Removing from '{0}'.", UserBinPath);

                    if (CleanFiles(UserBinPath, out cleanedFiles))
                    {
                        foreach (var file in cleanedFiles)
                        {
                            Console.Out.WriteLine("  {0}", file);
                        }

                        Console.Out.WriteLine("        {0} file(s) cleaned", cleanedFiles.Count);
                    }
                    else if (_isForced)
                    {
                        Console.Error.WriteLine("  removal failed. U_U");
                    }
                    else
                    {
                        Console.Error.WriteLine("  removal failed. U_U");
                        Pause();

                        Result = ResultValue.RemovalFailed;
                        return;
                    }
                }

                // all necissary content has been deployed to the system
                Result = ResultValue.Success;

                Console.Out.WriteLine();
                Console.Out.WriteLine("Success! {0} was removed! ^_^", Program.Title);
                Pause();
            }
            finally
            {
                SetOutput(true, true);
            }
        }

        public bool SetGitConfig(List<GitInstallation> installations, GitConfigAction action, Configuration.Type type, out Configuration.Type updated)
        {
            Trace.WriteLine("Installer::SetGitConfig");
            Trace.WriteLine("   action = " + action + ".");

            updated = Configuration.Type.None;

            if ((installations == null || installations.Count == 0) && !Where.FindGitInstallations(out installations))
            {
                Trace.WriteLine("   No Git installations detected to update.");
                return false;
            }

            if ((type & Configuration.Type.Global) == Configuration.Type.Global)
            {
                // the 0 entry in the installations list is the "preferred" instance of Git
                string gitCmdPath = installations[0].Git;
                string globalCmd = action == GitConfigAction.Set
                    ? "config --global credential.helper manager"
                    : "config --global --unset credential.helper";

                if (ExecuteGit(gitCmdPath, globalCmd, 0, 5))
                {
                    Trace.WriteLine("   updating ~/.gitconfig succeeded.");

                    updated |= Configuration.Type.Global;
                }
                else
                {
                    Trace.WriteLine("   updating ~/.gitconfig failed.");

                    Console.Out.WriteLine();
                    Console.Error.WriteLine("Fatal: Unable to update ~/.gitconfig.");
                    Pause();
                    return false;
                }
            }

            if ((type & Configuration.Type.System) == Configuration.Type.System)
            {
                string systemCmd = action == GitConfigAction.Set
                    ? "config --system credential.helper manager"
                    : "config --system --unset credential.helper";

                int successCount = 0;

                foreach (var installation in installations)
                {
                    if (ExecuteGit(installation.Git, systemCmd, 0, 5))
                    {
                        Trace.WriteLine("   updating /etc/gitconfig succeeded.");

                        successCount++;
                    }
                    else
                    {
                        Trace.WriteLine("   updating ~/.gitconfig failed.");
                    }
                }

                if (successCount == installations.Count)
                {
                    updated |= Configuration.Type.System;
                }
                else
                {
                    return false;
                }
            }

            return true;
        }

        private bool CleanFiles(string path, out List<string> cleanedFiles)
        {
            Trace.WriteLine("Installer::CleanFiles");

            cleanedFiles = new List<string>();

            if (!Directory.Exists(path))
            {
                Trace.WriteLine("   path '" + path + "' does not exist.");
                return false;
            }

            try
            {
                foreach (string file in Files)
                {
                    string target = Path.Combine(path, file);

                    Trace.WriteLine("   clean '" + target + "'.");

                    File.Delete(target);

                    cleanedFiles.Add(file);
                }

                return true;
            }
            catch
            {
                Trace.WriteLine("   clean failed.");
                return false;
            }
        }

        private bool CopyFiles(string srcPath, string dstPath, out List<string> copiedFiles)
        {
            Trace.WriteLine("Installer::CopyFiles");

            copiedFiles = new List<string>();

            if (!Directory.Exists(srcPath))
            {
                Trace.WriteLine("   source '" + srcPath + "' does not exist.");
                return false;
            }

            if (Directory.Exists(dstPath))
            {
                try
                {
                    foreach (string file in Files)
                    {
                        Trace.WriteLine("   copy '" + srcPath + "' to '" + dstPath + "'.");

                        string src = Path.Combine(srcPath, file);
                        string dst = Path.Combine(dstPath, file);

                        File.Copy(src, dst, true);

                        copiedFiles.Add(file);
                    }

                    return true;
                }
                catch
                {
                    Trace.WriteLine("   copy failed.");
                    return false;
                }
            }
            else
            {
                Trace.WriteLine("   destination '" + dstPath + "' does not exist.");
            }

            Trace.WriteLine("   copy failed.");
            return false;
        }

        private void DeployElevated()
        {
            Trace.WriteLine("Installer::DeployElevated");

            if (_isPassive)
            {
                this.Result = ResultValue.Unprivileged;
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
                if (!String.IsNullOrEmpty(_customPath))
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
                    Arguments = String.Format("/c \"{0}\" {1}", Program.ExecutablePath, arguments.ToString()),
                    UseShellExecute = true, // shellexecute for verb usage
                    Verb = "runas", // used to invoke elevation
                    WorkingDirectory = Program.Location,
                };

                Trace.WriteLine("   cmd " + options.Verb + " " + options.FileName + " " + options.Arguments);

                try
                {
                    // create the process
                    var elevated = Process.Start(options);

                    // wait for the process to complete
                    elevated.WaitForExit();

                    Trace.WriteLine("   process exited with " + elevated.ExitCode + ".");

                    // exit with the elevated process' exit code
                    this.ExitCode = elevated.ExitCode;
                }
                catch (Exception exception)
                {
                    Trace.WriteLine("   process failed with " + exception.Message);
                    this.Result = ResultValue.Unprivileged;
                }
            }
        }

        private bool ExecuteGit(string gitCmdPath, string command, params int[] allowedExitCodes)
        {
            if (String.IsNullOrEmpty(gitCmdPath) || String.IsNullOrEmpty(command))
                return false;

            if (!File.Exists(gitCmdPath))
                return false;

            var options = new ProcessStartInfo()
            {
                Arguments = command,
                FileName = gitCmdPath,
                CreateNoWindow = true,
                UseShellExecute = false,
            };

            Trace.WriteLine("   cmd " + options.FileName + " " + options.Arguments + ".");

            var gitProcess = Process.Start(options);

            gitProcess.WaitForExit();

            Trace.WriteLine("   Git exited with " + gitProcess.ExitCode + ".");

            if (allowedExitCodes != null && allowedExitCodes.Length > 0)
                return allowedExitCodes.Contains(gitProcess.ExitCode);
            else
                return gitProcess.ExitCode == 0;
        }

        private void Pause()
        {
            if (!_isPassive)
            {
                Console.Out.WriteLine();
                Console.Out.WriteLine("Press any key to continue...");
                Console.ReadKey();
            }
        }

        private void RemoveElevated()
        {
            Trace.WriteLine("Installer::RemoveElevated");

            if (_isPassive)
            {
                this.Result = ResultValue.Unprivileged;
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
                if (!String.IsNullOrEmpty(_customPath))
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
                    Arguments = String.Format("/c \"{0}\" {1}", Program.ExecutablePath, arguments.ToString()),
                    UseShellExecute = true, // shellexecute for verb usage
                    Verb = "runas", // used to invoke elevation
                    WorkingDirectory = Program.Location,
                };

                Trace.WriteLine("   cmd " + options.Verb + " " + options.FileName + " " + options.Arguments);

                try
                {
                    // create the process
                    var elevated = Process.Start(options);

                    // wait for the process to complete
                    elevated.WaitForExit();

                    Trace.WriteLine("   process exited with " + elevated.ExitCode + ".");

                    // exit with the elevated process' exit code
                    this.ExitCode = elevated.ExitCode;
                }
                catch (Exception exception)
                {
                    Trace.WriteLine("   process failed with " + exception.Message);
                    this.Result = ResultValue.Unprivileged;
                }
            }
        }

        private void SetOutput(bool muteStdout, bool muteStderr)
        {
            if (muteStdout)
            {
                _stdout = Console.Out;
                Console.SetOut(TextWriter.Null);
            }
            else if (_stdout != null)
            {
                Console.SetOut(_stdout);
                _stdout = null;
            }

            if (muteStderr)
            {
                _stderr = Console.Out;
                Console.SetOut(TextWriter.Null);
            }
            else if (_stderr != null)
            {
                Console.SetOut(_stderr);
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
