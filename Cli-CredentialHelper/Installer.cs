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
        private const string ParamPathKey = "--path";
        private const string ParamGitPathKey = "--git-path";
        private const string ParamUnattendKey = "--unattend";
        private const string ParamSkipFxKey = "--ignore-netfx";
        private static readonly Version NetFxMinVersion = new Version(4, 5, 1);
        private static readonly IReadOnlyList<string> Files = new List<string>
        {
            "Microsoft.Alm.Authentication.dll",
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
                else if (String.Equals(args[i], ParamGitPathKey, StringComparison.OrdinalIgnoreCase))
                {
                    if (args.Length > i + 1)
                    {
                        i += 1;
                        _customGit = args[i];

                        Trace.WriteLine("  " + ParamGitPathKey + " = '" + _customGit + "'.");
                    }
                }
                else if (String.Equals(args[i], ParamUnattendKey, StringComparison.OrdinalIgnoreCase))
                {
                    _unattended = true;

                    Trace.WriteLine("  " + ParamUnattendKey + " = true.");
                }
                else if (String.Equals(args[i], ParamSkipFxKey, StringComparison.OrdinalIgnoreCase))
                {
                    _skipNetfxTest = true;

                    Trace.WriteLine("  " + ParamUnattendKey + " = true.");
                }
            }
        }

        public int ExitCode
        {
            get { return (int)Result; }
            set { Result = (ResultValue)value; }
        }
        public ResultValue Result { get; private set; }

        private bool _unattended = false;
        private bool _skipNetfxTest = false;
        private string _customGit = null;
        private string _customPath = null;
        private string _pathToGit = null;

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

            return version != null
                && version >= NetFxMinVersion;
        }

        public void RunConsole()
        {
            Trace.WriteLine("Installer::RunConsole");

            // installation requires elevated privilages to copy files into %ProgramFiles%
            System.Security.Principal.WindowsIdentity identity = System.Security.Principal.WindowsIdentity.GetCurrent();
            System.Security.Principal.WindowsPrincipal principal = new System.Security.Principal.WindowsPrincipal(identity);
            if (principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator))
            {
                Console.Out.WriteLine("Hello, I'll install the {0}", Program.Title);

                Version netfxVersion;
                if (_skipNetfxTest || DetectNetFx(out netfxVersion))
                {
                    if (!String.IsNullOrWhiteSpace(_customPath))
                    {
                        if (!Directory.Exists(_customPath))
                        {
                            Console.Error.WriteLine();
                            Console.Error.WriteLine("Fatal: custom path does not exist: '{0}'. U_U", _customPath);
                            Pause();

                            Result = ResultValue.InvalidCustomPath;
                            return;
                        }
                        else
                        {
                            Console.Out.WriteLine();
                            Console.Out.WriteLine("Deploying to custom path: '{0}'.", _customPath);

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

                                Result = ResultValue.FileCopyFailed;
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine();
                        Console.WriteLine("Looking for Git installation(s)...");

                        List<string> paths;
                        if (Where.Git(out _pathToGit, out paths))
                        {
                            foreach (string path in paths)
                            {
                                Console.Out.WriteLine("  {0}", path);
                            }

                            foreach (string path in paths)
                            {
                                Console.Out.WriteLine();
                                Console.Out.WriteLine("Deploying from '{0}' to '{1}'.", Program.Location, path);

                                List<string> copiedFiles;
                                if (CopyFiles(Program.Location, path, out copiedFiles))
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

                                    Result = ResultValue.FileCopyFailed;
                                    break;
                                }
                            }
                        }
                        else
                        {
                            Console.Error.WriteLine();
                            Console.Error.WriteLine("Fatal: Git was not detected, unable to continue. U_U");
                            Pause();

                            Result = ResultValue.InvalidPathToGit;
                        }

                        if (Result == ResultValue.Success)
                        {
                            Console.Out.WriteLine();

                            if (SetGitConfig("system"))
                            {
                                Console.Out.WriteLine("Updated your /etc/gitconfig [git config --system]");

                                if (SetGitConfig("global"))
                                {
                                    Console.Out.WriteLine("Updated your ~/.gitconfig [git config --global]");
                                }
                                else
                                {
                                    Console.Error.WriteLine();
                                    Console.Error.WriteLine("Fatal: Unable to update your ~/.gitconfig correctly.");

                                    Result = ResultValue.GitConfigGlobalFailed;
                                }
                            }
                            else
                            {
                                Result = ResultValue.GitConfigGlobalFailed;
                                return;
                            }

                            if (Result == ResultValue.Success)
                            {
                                Console.Out.WriteLine();
                                Console.Out.WriteLine("Success! {0} was installed! ^_^", Program.Title);
                            }
                            else
                            {

                            }
                        }

                        Pause();
                    }
                }
                else
                {
                    Console.Error.WriteLine();

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
                }
            }
            else
            {
                RunElevated();

                if (Result == ResultValue.Unprivileged)
                {
                    Console.Error.WriteLine("The operation was canceled by the user.");
                }
            }
        }

        public bool SetGitConfig(string configName)
        {
            if (String.IsNullOrWhiteSpace(configName))
                throw new ArgumentNullException("configName", "The configName parameter cannot be null.");
            if (!Configuration.LegalConfigNames.Contains(configName))
                throw new ArgumentException("Only legal values are " + String.Join(", ", Configuration.LegalConfigNames) + ".", "configName");

            Trace.WriteLine("Installer::UpdateConfig");

            if (File.Exists(_pathToGit))
            {
                var options = new ProcessStartInfo()
                {
                    Arguments = String.Format("config --{0} credential.helper manager", configName),
                    FileName = _pathToGit,
                    CreateNoWindow = true,
                    UseShellExecute = true,
                };

                Trace.WriteLine("   cmd " + options.FileName + " " + options.Arguments + ".");

                var gitProcess = Process.Start(options);

                gitProcess.WaitForExit();

                Trace.WriteLine("   Git exited with " + gitProcess.ExitCode + ".");

                return gitProcess.ExitCode == 0;
            }

            return false;
        }

        private bool CopyFiles(string srcPath, string dstPath, out List<string> copedFiles)
        {
            Trace.WriteLine("Installer::CopyFiles");

            copedFiles = new List<string>();

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

                        copedFiles.Add(file);
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

        private void Pause()
        {
            if (!_unattended)
            {
                Console.Out.WriteLine();
                Console.Out.WriteLine("Press any key to continue...");
                Console.ReadKey();
            }
        }

        private void RunElevated()
        {
            Trace.WriteLine("Installer::RunElevated");

            /* cannot install while not elevated (need access to %PROGRAMFILES%), re-launch 
               self as an elevated process with identical arguments. */

            // build arguments
            var arguments = new System.Text.StringBuilder("install");
            if (_unattended)
            {
                arguments.Append(" ")
                         .Append(ParamUnattendKey);
            }
            if (_skipNetfxTest)
            {
                arguments.Append(" ")
                         .Append(ParamSkipFxKey);
            }
            if (!String.IsNullOrEmpty(_customGit))
            {
                arguments.Append(" ")
                         .Append(ParamGitPathKey)
                         .Append(" \"")
                         .Append(_customGit)
                         .Append("\"");
            }
            if (!String.IsNullOrEmpty(_customPath))
            {
                arguments.Append(" ")
                         .Append(ParamSkipFxKey)
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

        public enum ResultValue : int
        {
            UnknownFailure = -1,
            Success = 0,
            InvalidPathToGit,
            InvalidCustomPath,
            FileCopyFailed,
            NetFxNotFound,
            Unprivileged,
            GitConfigGlobalFailed,
            GitConfigSystemFailed,
        }
    }
}
