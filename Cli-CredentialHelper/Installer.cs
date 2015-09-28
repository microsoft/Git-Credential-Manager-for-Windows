using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
                    }
                }
                else if (String.Equals(args[i], ParamGitPathKey, StringComparison.OrdinalIgnoreCase))
                {
                    if (args.Length > i + 1)
                    {
                        i += 1;
                        _customGitPath = args[i];
                    }
                }
                else if (String.Equals(args[i], ParamUnattendKey, StringComparison.OrdinalIgnoreCase))
                {
                    _unattended = true;
                }
                else if (String.Equals(args[i], ParamSkipFxKey, StringComparison.OrdinalIgnoreCase))
                {
                    _skipNetfxTest = true;
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
        private string _customGitPath = null;
        private string _customPath = null;
        private string _pathToGit = null;

        public bool DetectGit(out List<string> paths)
        {
            string regNatPath = Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Git_is1", "InstallLocation", String.Empty) as String;
            string pf32path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), @"Git");

            paths = new List<string>();

            GitPaths[] candidates;
            if (Environment.Is64BitOperatingSystem && Environment.Is64BitProcess)
            {
                string regWowPath = Registry.GetValue(@"HKEY_LOCAL_MACHINE\SOFTWARE\Wow6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Git_is1", "InstallLocation", String.Empty) as String;
                string pf64path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), @"Git");

                candidates = new []
                {
                    new GitPaths(regNatPath, GitPaths.Version2_64bit),
                    new GitPaths(pf64path, GitPaths.Version2_64bit),
                    new GitPaths(regNatPath, GitPaths.Version2_32bit),
                    new GitPaths(pf64path, GitPaths.Version2_32bit),
                    new GitPaths(regNatPath, GitPaths.Version1_32bit),
                    new GitPaths(pf64path, GitPaths.Version1_32bit),
                    new GitPaths(regWowPath, GitPaths.Version2_32bit),
                    new GitPaths(pf32path, GitPaths.Version2_32bit),
                    new GitPaths(regWowPath, GitPaths.Version1_32bit),
                    new GitPaths(pf32path, GitPaths.Version1_32bit),
                };
            }
            else
            {
                candidates = new []
                {
                    new GitPaths(regNatPath, GitPaths.Version2_32bit),
                    new GitPaths(pf32path, GitPaths.Version2_32bit),
                    new GitPaths(regNatPath, GitPaths.Version1_32bit),
                    new GitPaths(pf32path, GitPaths.Version1_32bit),
                };
            }

            HashSet<string> pathSet = new HashSet<string>();
            foreach (var candidate in candidates)
            {
                if (Directory.Exists(candidate.Libexec) && File.Exists(candidate.Cmd))
                {
                    pathSet.Add(candidate.Path.TrimEnd('\\'));

                    if (String.IsNullOrEmpty(_pathToGit))
                    {
                        _pathToGit = candidate.Cmd;
                    }
                }
            }

            paths = pathSet.ToList();

            return paths.Count > 0;
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
            if ((netfxString = Registry.GetValue(NetFxKeyClient, ValueName, DefaultValue) as String) != null
                    && Version.TryParse(netfxString, out netfxVerson)
                || (netfxString = Registry.GetValue(NetFxKeyFull, ValueName, DefaultValue) as String) != null
                    && Version.TryParse(netfxString, out netfxVerson))
            {
                version = netfxVerson;
            }

            return version != null
                && version >= NetFxMinVersion;
        }

        public void RunConsole()
        {
            // installation requires elevated privilages to copy files into %ProgramFiles%
            System.Security.Principal.WindowsIdentity identity = System.Security.Principal.WindowsIdentity.GetCurrent();
            System.Security.Principal.WindowsPrincipal principal = new System.Security.Principal.WindowsPrincipal(identity);
            if (principal.IsInRole(System.Security.Principal.WindowsBuiltInRole.Administrator))
            {
                Console.Out.WriteLine("Hello, I'll install the {0}", Program.Title);

                Version netfxVersion;
                if (_skipNetfxTest || DetectNetFx(out netfxVersion))
                {
                    bool deployed = false;

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
                            if (CopyFiles(Environment.CurrentDirectory, _customPath, out copiedFiles))
                            {
                                foreach (string file in copiedFiles)
                                {
                                    Console.Out.WriteLine("  {0}", file);
                                }

                                Console.Out.WriteLine("        {0} file(s) copied", copiedFiles.Count);

                                deployed = true;
                            }
                            else
                            {
                                Console.Error.WriteLine("  deployment failed. U_U");
                                Pause();

                                Result = ResultValue.DeploymentFailed;
                            }
                        }
                    }
                    else
                    {
                        Console.WriteLine();
                        Console.WriteLine("Looking for Git installation(s)...");

                        List<string> paths;
                        if (DetectGit(out paths))
                        {
                            foreach (string path in paths)
                            {
                                Console.Out.WriteLine("  {0}", path);
                            }

                            foreach (string path in paths)
                            {
                                Console.Out.WriteLine();
                                Console.Out.WriteLine("Deploying from '{0}' to '{1}'.", Environment.CurrentDirectory, path);

                                List<string> copiedFiles;
                                if (CopyFiles(Environment.CurrentDirectory, path, out copiedFiles))
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
                                }
                            }

                            deployed = true;
                        }
                        else
                        {
                            Console.Error.WriteLine();
                            Console.Error.WriteLine("Fatal: Git was not detected, unable to continue. U_U");
                            Pause();

                            Result = ResultValue.InvalidPathToGit;
                        }

                        if (deployed && UpdateConfig())
                        {
                            Console.Out.WriteLine();
                            Console.Out.WriteLine("Updated your ~/.gitconfig [git config --global]");

                            Console.Out.WriteLine();
                            Console.Out.WriteLine("Success! {0} was installed! ^_^", Program.Title);
                            Console.Out.WriteLine();

                            Result = ResultValue.Success;
                        }
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
            }
        }

        public bool UpdateConfig()
        {
            if (File.Exists(_pathToGit))
            {
                var options = new ProcessStartInfo()
                {
                    Arguments = "config --global credential.helper manager",
                    FileName = _pathToGit,
                    CreateNoWindow = true,
                    UseShellExecute = true,
                };
                var gitProcess = Process.Start(options);

                gitProcess.WaitForExit();

                return gitProcess.ExitCode == 0;
            }

            return false;
        }

        private bool CopyFiles(string srcPath, string dstPath, out List<string> copedFiles)
        {
            copedFiles = new List<string>();

            if (Directory.Exists(dstPath))
            {
                try
                {
                    foreach (string file in Files)
                    {
                        string src = Path.Combine(srcPath, file);
                        string dst = Path.Combine(dstPath, file);

                        File.Copy(src, dst, true);

                        copedFiles.Add(file);
                    }

                    return true;
                }
                catch
                {
                    return false;
                }
            }

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
            if (!String.IsNullOrEmpty(_customGitPath))
            {
                arguments.Append(" ")
                         .Append(ParamGitPathKey)
                         .Append(" \"")
                         .Append(_customGitPath)
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
                FileName = Program.Name,
                Arguments = arguments.ToString(),
                Verb = "runas", // used to invoke elevation
                WorkingDirectory = Environment.CurrentDirectory,
            };

            // create the process
            var elevated = Process.Start(options);

            // wait for the process to complete
            elevated.WaitForExit();

            // exit with the elevated process' exit code
            this.ExitCode = elevated.ExitCode;
        }

        public enum ResultValue : int
        {
            Success = 0,
            InvalidPathToGit,
            InvalidCustomPath,
            DeploymentFailed,
            NetFxNotFound,
            Invalid,
        }

        private struct GitPaths : IEquatable<GitPaths>
        {
            public const string Version1_32bit = @"libexec\git-core\";
            public const string Version2_32bit = @"mingw32\libexec\git-core\";
            public const string Version2_64bit = @"mingw64\libexec\git-core\";

            public GitPaths(string path, string libexec)
            {
                if (String.IsNullOrEmpty(path))
                    throw new ArgumentNullException("path", "The path parameter cannot be null or empty.");
                if (String.IsNullOrEmpty(libexec))
                    throw new ArgumentNullException("libexec", "The libexec parameter cannot be null or empty.");

                Path = path;
                Libexec = System.IO.Path.Combine(path, libexec);
                Cmd = System.IO.Path.Combine(path, @"cmd", "git.exe");
            }

            public readonly string Path;
            public readonly string Libexec;
            public readonly string Cmd;

            public override bool Equals(object obj)
            {
                if (!(obj is GitPaths))
                    return false;

                return this == (GitPaths)obj;
            }

            public bool Equals(GitPaths other)
            {
                return this == other;
            }

            public override int GetHashCode()
            {
                return Path.GetHashCode();
            }

            public static bool operator ==(GitPaths path1, GitPaths path2)
            {
                return String.Equals(path1.Libexec, path2.Libexec, StringComparison.OrdinalIgnoreCase);
            }

            public static bool operator !=(GitPaths path1, GitPaths path2)
            {
                return !(path1 == path2);
            }
        }
    }
}
