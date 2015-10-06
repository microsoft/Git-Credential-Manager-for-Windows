using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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

        public int ExitCode
        {
            get { return (int)Result; }
            set { Result = (ResultValue)value; }
        }
        public ResultValue Result { get; private set; }

        private bool _isPassive = false;
        private bool _isForced = false;
        private string _customPath = null;
        private string _gitCmdPath = null;

        public void DeployConsole()
        {
            Trace.WriteLine("Installer::DeployConsole");

            if (_isPassive)
            {
                Console.SetOut(TextWriter.Null);
                if (_isForced)
                {
                    Console.SetError(TextWriter.Null);
                }
            }

            System.Security.Principal.WindowsIdentity identity = System.Security.Principal.WindowsIdentity.GetCurrent();
            System.Security.Principal.WindowsPrincipal principal = new System.Security.Principal.WindowsPrincipal(identity);
            if (!principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator))
            {
                DeployElevated();
                return;
            }

            // if installation is not forced, insure that an acceptable version of .NET is installed on the system
            Version netfxVersion;
            if (!(_isForced || DetectNetFx(out netfxVersion) && netfxVersion >= NetFxMinVersion))
            {
                Console.Out.WriteLine();

                if (netfxVersion == null)
                {
                    Console.Error.WriteLine("Fatal: failed to detect the Microsoft .NET Framework. Make sure it is installed. U_U");
                }
                else
                {
                    Console.Error.WriteLine("Fatal: detected Microsoft .NET Framework version {0:3}. {1:3} or newer is required. U_U", netfxVersion, NetFxMinVersion);
                }

                Console.Error.WriteLine("Don't know where to get the Microsoft .NET Framework? Try http://bit.ly/1kE08Rz.");
                Pause();

                Result = ResultValue.NetFxNotFound;
                return;
            }

            List<GitInstallation> installations = null;

            // use the custom installation path if supplied
            if (!String.IsNullOrEmpty(_customPath))
            {
                Console.Out.WriteLine();
                Console.Out.WriteLine("Deploying to custom path: '{0}'.", _customPath);

                if (!Directory.Exists(_customPath))
                {
                    if (_isForced)
                    {
                        try
                        {
                            Directory.CreateDirectory(_customPath);
                        }
                        catch
                        {
                            Console.Out.WriteLine();
                            Console.Error.WriteLine("Fatal: custom path is invalid: '{0}'. U_U", _customPath);
                            Pause();

                            Result = ResultValue.InvalidCustomPath;
                            return;
                        }
                    }
                    else
                    {
                        Console.Out.WriteLine();
                        Console.Error.WriteLine("Fatal: custom path does not exist: '{0}'. U_U", _customPath);
                        Pause();

                        Result = ResultValue.InvalidCustomPath;
                        return;
                    }
                }

                // if the custom path points to a git location then treat it properly
                GitInstallation installation;
                if (Where.FindGitInstallation(_customPath, KnownGitDistribution.GitForWindows64v2, out installation)
                    || Where.FindGitInstallation(_customPath, KnownGitDistribution.GitForWindows32v2, out installation)
                    || Where.FindGitInstallation(_customPath, KnownGitDistribution.GitForWindows32v1, out installation))
                {
                    // track known Git installtations
                    installations = new List<GitInstallation>();
                    installations.Add(installation);
                    // set the path to Git
                    _gitCmdPath = installation.Cmd;

                    List<string> copiedFiles;
                    if (CopyFiles(Program.Location, installation.Libexec, out copiedFiles))
                    {
                        foreach (string file in copiedFiles)
                        {
                            Console.Out.WriteLine("  {0}", file);
                        }

                        Console.Out.WriteLine("        {0} file(s) copied", copiedFiles.Count);
                    }
                    else
                    {
                        Console.Error.WriteLine("  deployment failed. U_U");
                        Pause();

                        Result = ResultValue.DeploymentFailed;
                        return;
                    }
                }
                else
                {
                    List<string> copiedFiles;
                    if (CopyFiles(Program.Location, _customPath, out copiedFiles))
                    {
                        foreach (string file in copiedFiles)
                        {
                            Console.Out.WriteLine("  {0}", file);
                        }

                        Console.Out.WriteLine("        {0} file(s) copied", copiedFiles.Count);
                    }
                    else
                    {
                        Console.Error.WriteLine("  deployment failed. U_U");
                        Pause();

                        Result = ResultValue.DeploymentFailed;
                        return;
                    }
                }
            }
            // since no custom installation path was supplied, use default logic
            else
            {
                Console.WriteLine();
                Console.WriteLine("Looking for Git installation(s)...");

                if (Where.FindGitInstallations(out installations))
                {
                    _gitCmdPath = installations[0].Cmd;

                    foreach (var installation in installations)
                    {
                        Console.Out.WriteLine("  {0}", installation.Path);
                    }

                    foreach (var installation in installations)
                    {
                        Console.Out.WriteLine();
                        Console.Out.WriteLine("Deploying from '{0}' to '{1}'.", Program.Location, installation.Path);

                        List<string> copiedFiles;
                        if (CopyFiles(Program.Location, installation.Libexec, out copiedFiles))
                        {
                            foreach (string file in copiedFiles)
                            {
                                Console.Out.WriteLine("  {0}", file);
                            }

                            Console.Out.WriteLine("        {0} file(s) copied", copiedFiles.Count);
                        }
                        else
                        {
                            Console.Error.WriteLine("  deployment failed. U_U");
                            Pause();

                            Result = ResultValue.DeploymentFailed;

                            // stop installing now if not a forced installation
                            if (!_isForced)
                                break;
                        }
                    }
                }
                else
                {
                    Console.Out.WriteLine();
                    Console.Error.WriteLine("Fatal: Git was not detected, unable to continue. U_U");
                    Pause();

                    Result = ResultValue.GitNotFound;
                    return;
                }
            }

            // all necissary content has been deployed to the system
            Result = ResultValue.Success;

            // deployment is complete, configure git installations
            if (SetSystemConfig(installations))
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

            // only set ~/.gitconfig if not a custom path
            if (String.IsNullOrWhiteSpace(_customPath))
            {
                if (SetGlobalConfig())
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

            Console.Out.WriteLine();
            Console.Out.WriteLine("Success! {0} was deployed! ^_^", Program.Title);
            Pause();
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

            if (_isPassive)
            {
                Console.SetOut(TextWriter.Null);
                if (_isForced)
                {
                    Console.SetError(TextWriter.Null);
                }
            }

            List<GitInstallation> installations = null;

            // use the custom installation path if supplied
            if (!String.IsNullOrEmpty(_customPath))
            {
                Console.Out.WriteLine();
                Console.Out.WriteLine("Removing from custom path: '{0}'.", _customPath);

                if (!Directory.Exists(_customPath))
                {
                    Console.Out.WriteLine();
                    Console.Error.WriteLine("Fatal: custom path does not exist: '{0}'. U_U", _customPath);
                    Pause();

                    Result = ResultValue.InvalidCustomPath;
                    return;
                }

                // if the custom path points to a git location then treat it properly
                GitInstallation installation;
                if (Where.FindGitInstallation(_customPath, KnownGitDistribution.GitForWindows64v2, out installation)
                    || Where.FindGitInstallation(_customPath, KnownGitDistribution.GitForWindows32v2, out installation)
                    || Where.FindGitInstallation(_customPath, KnownGitDistribution.GitForWindows32v1, out installation))
                {
                    // track known Git installtations
                    installations = new List<GitInstallation>();
                    installations.Add(installation);
                    // set the path to Git
                    _gitCmdPath = installation.Cmd;

                    List<string> cleanedFiles;
                    if (CleanFiles(installation.Libexec, out cleanedFiles))
                    {
                        foreach (string file in cleanedFiles)
                        {
                            Console.Out.WriteLine("  {0}", file);
                        }

                        Console.Out.WriteLine("        {0} file(s) cleaned", cleanedFiles.Count);
                    }
                    else
                    {
                        Console.Error.WriteLine("  removal failed. U_U");
                        Pause();

                        Result = ResultValue.DeploymentFailed;
                        return;
                    }
                }
                else
                {
                    List<string> cleanedFiles;
                    if (CleanFiles(_customPath, out cleanedFiles))
                    {
                        foreach (string file in cleanedFiles)
                        {
                            Console.Out.WriteLine("  {0}", file);
                        }

                        Console.Out.WriteLine("        {0} file(s) cleaned", cleanedFiles.Count);
                    }
                    else
                    {
                        Console.Error.WriteLine("  removal failed. U_U");
                        Pause();

                        Result = ResultValue.DeploymentFailed;
                        return;
                    }
                }
            }
            // since no custom installation path was supplied, use default logic
            else
            {
                Console.WriteLine();
                Console.WriteLine("Looking for Git installation(s)...");

                if (Where.FindGitInstallations(out installations))
                {
                    _gitCmdPath = installations[0].Cmd;

                    foreach (var installation in installations)
                    {
                        Console.Out.WriteLine("  {0}", installation.Path);
                    }

                    foreach (var installation in installations)
                    {
                        Console.Out.WriteLine();
                        Console.Out.WriteLine("Removing from '{0}'.", installation.Path);

                        List<string> cleanedFiles;
                        if (CleanFiles(installation.Libexec, out cleanedFiles))
                        {
                            foreach (string file in cleanedFiles)
                            {
                                Console.Out.WriteLine("  {0}", file);
                            }

                            Console.Out.WriteLine("        {0} file(s) cleaned", cleanedFiles.Count);
                        }
                        else
                        {
                            Console.Error.WriteLine("  removal failed. U_U");
                            Pause();

                            Result = ResultValue.DeploymentFailed;

                            // stop installing now if not a forced installation
                            if (!_isForced)
                                break;
                        }
                    }
                }
                else
                {
                    Console.Out.WriteLine();
                    Console.Error.WriteLine("Fatal: Git was not detected, unable to continue. U_U");
                    Pause();

                    Result = ResultValue.GitNotFound;
                    return;
                }
            }

            // all necissary content has been deployed to the system
            Result = ResultValue.Success;

            // removal is complete, configure git installations
            if (UnsetSystemConfig(installations))
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

            // only set ~/.gitconfig if not a custom path
            if (String.IsNullOrWhiteSpace(_customPath))
            {
                if (UnsetGlobalConfig())
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

            Console.Out.WriteLine();
            Console.Out.WriteLine("Success! {0} was removed! ^_^", Program.Title);
            Pause();
        }

        public bool SetGlobalConfig(string gitCmdPath = null)
        {
            Trace.WriteLine("Installer::SetGlobalConfig");

            // try hard to avoid throwing an exception
            gitCmdPath = gitCmdPath ?? _gitCmdPath;
            if (gitCmdPath == null)
            {
                List<GitInstallation> installations;
                if (Where.FindGitInstallations(out installations))
                {
                    gitCmdPath = installations[0].Cmd;
                }
            }

            if (gitCmdPath != null && File.Exists(gitCmdPath))
            {
                var options = new ProcessStartInfo()
                {
                    Arguments = "config --global credential.helper manager",
                    FileName = gitCmdPath,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                };

                Trace.WriteLine("   cmd " + options.FileName + " " + options.Arguments + ".");

                var gitProcess = Process.Start(options);

                gitProcess.WaitForExit();

                Trace.WriteLine("   Git exited with " + gitProcess.ExitCode + ".");

                return gitProcess.ExitCode == 0;
            }

            return false;
        }

        public bool SetSystemConfig(List<GitInstallation> installations = null)
        {
            if (installations == null && !Where.FindGitInstallations(out installations))
                return false;

            Trace.WriteLine("Installer::SetSystemConfig");

            int successCount = 0;

            foreach (var install in installations)
            {
                if (File.Exists(install.Cmd))
                {
                    var options = new ProcessStartInfo()
                    {
                        Arguments = "config --system credential.helper manager",
                        FileName = install.Cmd,
                        CreateNoWindow = true,
                        UseShellExecute = false,
                    };

                    Trace.WriteLine("   cmd " + options.FileName + " " + options.Arguments + ".");

                    var gitProcess = Process.Start(options);

                    gitProcess.WaitForExit();

                    Trace.WriteLine("   Git exited with " + gitProcess.ExitCode + ".");

                    // record at least a single success
                    if (gitProcess.ExitCode == 0)
                    {
                        successCount += 1;
                    }
                }
            }

            return successCount == installations.Count;
        }

        public bool UnsetGlobalConfig(string gitCmdPath = null)
        {
            Trace.WriteLine("Installer::UnsetGlobalConfig");

            // try hard to avoid throwing an exception
            gitCmdPath = gitCmdPath ?? _gitCmdPath;
            if (gitCmdPath == null)
            {
                List<GitInstallation> installations;
                if (Where.FindGitInstallations(out installations))
                {
                    gitCmdPath = installations[0].Cmd;
                }
            }

            if (gitCmdPath != null && File.Exists(gitCmdPath))
            {
                var options = new ProcessStartInfo()
                {
                    Arguments = "config --global --unset credential.helper",
                    FileName = gitCmdPath,
                    CreateNoWindow = true,
                    UseShellExecute = false,
                };

                Trace.WriteLine("   cmd " + options.FileName + " " + options.Arguments + ".");

                var gitProcess = Process.Start(options);

                gitProcess.WaitForExit();

                Trace.WriteLine("   Git exited with " + gitProcess.ExitCode + ".");

                return gitProcess.ExitCode == 0;
            }

            return false;
        }

        public bool UnsetSystemConfig(List<GitInstallation> installations = null)
        {
            if (installations == null && !Where.FindGitInstallations(out installations))
                return false;

            Trace.WriteLine("Installer::UnsetSystemConfig");

            int successCount = 0;

            foreach (var install in installations)
            {
                if (File.Exists(install.Cmd))
                {
                    var options = new ProcessStartInfo()
                    {
                        Arguments = "config --system --unset credential.helper",
                        FileName = install.Cmd,
                        CreateNoWindow = true,
                        UseShellExecute = false,
                    };

                    Trace.WriteLine("   cmd " + options.FileName + " " + options.Arguments + ".");

                    var gitProcess = Process.Start(options);

                    gitProcess.WaitForExit();

                    Trace.WriteLine("   Git exited with " + gitProcess.ExitCode + ".");

                    // record at least a single success
                    if (gitProcess.ExitCode == 0)
                    {
                        successCount += 1;
                    }
                }
            }

            return successCount == installations.Count;
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
        }
    }
}
