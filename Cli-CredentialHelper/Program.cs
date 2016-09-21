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
using System.Linq;
using Bitbucket.Authentication;
using Microsoft.Alm.Authentication;

namespace Microsoft.Alm.Cli
{
    internal partial class Program
    {
        public const string Title = "Git Credential Manager for Windows";
        public const string Description = "Secure Git credential helper for Windows, by Microsoft";

        internal const string CommandApprove = "approve";
        internal const string CommandClear = "clear";
        internal const string CommandDelete = "delete";
        internal const string CommandDeploy = "deploy";
        internal const string CommandErase = "erase";
        internal const string CommandFill = "fill";
        internal const string CommandGet = "get";
        internal const string CommandInstall = "install";
        internal const string CommandReject = "reject";
        internal const string CommandRemove = "remove";
        internal const string CommandStore = "store";
        internal const string CommandUninstall = "uninstall";
        internal const string CommandVersion = "version";

        private static readonly List<string> CommandList = new List<string>
        {
            CommandApprove,
            CommandClear,
            CommandDelete,
            CommandDeploy,
            CommandErase,
            CommandFill,
            CommandGet,
            CommandInstall,
            CommandReject,
            CommandRemove,
            CommandStore,
            CommandUninstall,
            CommandVersion
        };

        private static void Clear()
        {
            Trace.WriteLine("Program::Clear");

            var args = Environment.GetCommandLineArgs();
            string url = null;
            bool forced = false;

            if (args.Length <= 2)
            {
                if (!StandardInputIsTty)
                {
                    Trace.WriteLine("   standard input is not TTY, abandoning prompt.");

                    return;
                }

                Trace.WriteLine("   prompting user for url.");

                Console.Out.WriteLine(" Target Url:");
                url = Console.In.ReadLine();
            }
            else
            {
                url = args[2];

                if (args.Length > 3)
                {
                    bool.TryParse(args[3], out forced);
                }
            }

            Trace.WriteLine("   url = " + url);

            Uri uri;
            if (Uri.TryCreate(url, UriKind.Absolute, out uri))
            {
                Trace.WriteLine("   targetUri = " + uri.AbsoluteUri + ".");

                OperationArguments operationArguments = new OperationArguments(uri);

                LoadOperationArguments(operationArguments);
                EnableTraceLogging(operationArguments);

                if (operationArguments.PreserveCredentials && !forced)
                {
                    Trace.Write("   attempting to delete preserved credentials without force.");
                    Trace.Write("   prompting user for interactivity.");

                    if (!StandardInputIsTty || !StandardErrorIsTty)
                    {
                        Trace.WriteLine("   standard input is not TTY, abandoning prompt.");
                        return;
                    }

                    Console.Error.WriteLine(" credentials are protected by perserve flag, clear anyways? [Y]es, [N]o.");

                    ConsoleKeyInfo key;
                    while ((key = Console.ReadKey(true)).Key != ConsoleKey.Escape)
                    {
                        if (key.KeyChar == 'N' || key.KeyChar == 'n')
                        {
                            Trace.Write("   use cancelled.");
                            return;
                        }

                        if (key.KeyChar == 'Y' || key.KeyChar == 'y')
                        {
                            Trace.Write("   use continued.");
                            break;
                        }
                    }
                }

                DeleteCredentials(operationArguments);
            }
        }

        private static void Delete()
        {
            Trace.WriteLine("Program::Erase");

            string[] args = Environment.GetCommandLineArgs();

            if (args.Length < 3)
                goto error_parse;

            string url = args[2];
            Uri uri = null;

            if (Uri.IsWellFormedUriString(url, UriKind.Absolute))
            {
                if (!Uri.TryCreate(url, UriKind.Absolute, out uri))
                    goto error_parse;
            }
            else
            {
                url = String.Format("{0}://{1}", Uri.UriSchemeHttps, url);
                if (!Uri.TryCreate(url, UriKind.Absolute, out uri))
                    goto error_parse;
            }

            var stdin = Console.OpenStandardInput();
            OperationArguments operationArguments = new OperationArguments(stdin);
            operationArguments.QueryUri = uri;

            LoadOperationArguments(operationArguments);

            BaseAuthentication authentication = CreateAuthentication(operationArguments);

            switch (operationArguments.Authority)
            {
                default:
                case AuthorityType.Basic:
                    Trace.WriteLine("   deleting basic credentials");
                    authentication.DeleteCredentials(operationArguments.TargetUri);
                    break;

                case AuthorityType.AzureDirectory:
                case AuthorityType.MicrosoftAccount:
                    Trace.WriteLine("   deleting VSTS credentials");
                    BaseVstsAuthentication vstsAuth = authentication as BaseVstsAuthentication;
                    vstsAuth.DeleteCredentials(operationArguments.TargetUri);
                    // call delete twice to purge any stored ADA tokens
                    vstsAuth.DeleteCredentials(operationArguments.TargetUri);
                    break;

                case AuthorityType.GitHub:
                    Trace.WriteLine("   deleting GitHub credentials");
                    GitHubAuthentication ghAuth = authentication as GitHubAuthentication;
                    ghAuth.DeleteCredentials(operationArguments.TargetUri);
                    break;

                case AuthorityType.Bitbucket:
                    Trace.WriteLine("   deleting Bitbucket credentials");
                    var bbAuth = authentication as BitbucketAuthentication;
                    bbAuth.DeleteCredentials(operationArguments.TargetUri);
                    break;
            }

            return;

            error_parse:
            Console.Error.WriteLine("Fatal: unable to parse target URI.");
        }

        private static void Deploy()
        {
            Trace.WriteLine("Program::Deploy");

            var installer = new Installer();
            installer.DeployConsole();

            Trace.WriteLine(String.Format("   Installer result = {0}.", installer.Result));
            Trace.WriteLine(String.Format("   Installer exit code = {0}.", installer.ExitCode));

            Environment.Exit(installer.ExitCode);
        }

        private static void Erase()
        {
            // parse the operations arguments from stdin (this is how git sends commands)
            // see: https://www.kernel.org/pub/software/scm/git/docs/technical/api-credentials.html
            // see: https://www.kernel.org/pub/software/scm/git/docs/git-credential.html
            var stdin = Console.OpenStandardInput();
            OperationArguments operationArguments = new OperationArguments(stdin);

            Debug.Assert(operationArguments != null, "The operationArguments is null");
            Debug.Assert(operationArguments.TargetUri != null, "The operationArgument.TargetUri is null");

            LoadOperationArguments(operationArguments);
            EnableTraceLogging(operationArguments);

            Trace.WriteLine("Program::Erase");
            Trace.WriteLine("   targetUri = " + operationArguments.TargetUri);

            if (operationArguments.PreserveCredentials)
            {
                Trace.WriteLine("   " + ConfigPreserveCredentialsKey + " = true");
                Trace.WriteLine("   canceling erase request.");
                return;
            }

            DeleteCredentials(operationArguments);
        }

        private static void Get()
        {
            // parse the operations arguments from stdin (this is how git sends commands)
            // see: https://www.kernel.org/pub/software/scm/git/docs/technical/api-credentials.html
            // see: https://www.kernel.org/pub/software/scm/git/docs/git-credential.html
            var stdin = Console.OpenStandardInput();
            OperationArguments operationArguments = new OperationArguments(stdin);

            if (ReferenceEquals(operationArguments, null))
                throw new ArgumentNullException("operationArguments");
            if (ReferenceEquals(operationArguments.TargetUri, null))
                throw new ArgumentNullException("operationArguments.TargetUri");

            Trace.WriteLine("Program::Get");
            Trace.WriteLine("   targetUri = " + operationArguments.TargetUri);

            LoadOperationArguments(operationArguments);
            EnableTraceLogging(operationArguments);

            QueryCredentials(operationArguments);

            var stdout = Console.OpenStandardOutput();
            operationArguments.WriteToStream(stdout);
        }

        [Flags]
        private enum test
        {
            A = 1 << 0,
            B = 1 << 1,
            C = 1 << 2,
        }

        [STAThread]
        private static void Main(string[] args)
        {
            try
            {
                EnableDebugTrace();

                if (args.Length == 0
                    || String.Equals(args[0], "--help", StringComparison.OrdinalIgnoreCase)
                    || String.Equals(args[0], "-h", StringComparison.OrdinalIgnoreCase)
                    || args[0].Contains('?'))
                {
                    PrintHelpMessage();
                    return;
                }

                // list of arg => method associations (case-insensitive)
                Dictionary<string, Action> actions = new Dictionary<string, Action>(StringComparer.OrdinalIgnoreCase)
                {
                    { CommandApprove, Store },
                    { CommandClear, Clear },
                    { CommandDelete, Delete },
                    { CommandDeploy, Deploy },
                    { CommandErase, Erase },
                    { CommandFill, Get },
                    { CommandGet, Get },
                    { CommandInstall, Deploy },
                    { CommandReject, Erase },
                    { CommandRemove, Remove },
                    { CommandStore, Store },
                    { CommandUninstall, Remove },
                    { CommandVersion, PrintVersion },
                };

                // invoke action specified by arg0
                if (actions.ContainsKey(args[0]))
                {
                    actions[args[0]]();
                }
            }
            catch (AggregateException exception)
            {
                // print out more useful information when an `AggregateException` is encountered
                exception = exception.Flatten();

                // find the first inner exception which isn't an `AggregateException` with fallback to the canonical `.InnerException`
                Exception innerException = exception.InnerExceptions.FirstOrDefault(e => !(e is AggregateException))
                                        ?? exception.InnerException;

                Console.Error.WriteLine("Fatal: " + innerException.GetType().Name + " encountered.");
                if (!String.IsNullOrWhiteSpace(innerException.Message))
                {
                    Console.Error.WriteLine("   " + innerException.Message);
                }

                Trace.WriteLine("Fatal: " + exception.ToString());
                LogEvent(exception.ToString(), EventLogEntryType.Error);

                Environment.ExitCode = -1;
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine("Fatal: " + exception.GetType().Name + " encountered.");
                if (!String.IsNullOrWhiteSpace(exception.Message))
                {
                    Console.Error.WriteLine("   " + exception.Message);
                }

                Trace.WriteLine("Fatal: " + exception.ToString());
                LogEvent(exception.ToString(), EventLogEntryType.Error);

                Environment.ExitCode = -1;
            }

            Trace.Flush();
        }

        private static void PrintHelpMessage()
        {
            Trace.WriteLine("Program::PrintHelpMessage");

            Console.Out.WriteLine("usage: git credential-manager [" + String.Join("|", CommandList) + "] [<args>]");
            Console.Out.WriteLine();
            Console.Out.WriteLine("Command Line Options:");
            Console.Out.WriteLine();
            Console.Out.WriteLine("  " + CommandDeploy + "       Deploys the " + Title + " package and sets");
            Console.Out.WriteLine("               Git configuration to use the helper.");
            Console.Out.WriteLine();
            Console.Out.WriteLine("    " + Installer.ParamPathKey + "     Specifies a path for the installer to deploy to.");
            Console.Out.WriteLine("               If a path is provided, the installer will not seek additional");
            Console.Out.WriteLine("               Git installations to modify.");
            Console.Out.WriteLine();
            Console.Out.WriteLine("    " + Installer.ParamPassiveKey + "  Instructs the installer to not prompt the user for input");
            Console.Out.WriteLine("               during deployment and restricts output to error messages only.");
            Console.Out.WriteLine("               When combined with " + Installer.ParamForceKey + " all output is eliminated; only the");
            Console.Out.WriteLine("               return code can be used to validate success.");
            Console.Out.WriteLine();
            Console.Out.WriteLine("    " + Installer.ParamForceKey + "    Instructs the installer to proceed with deployment even if");
            Console.Out.WriteLine("               prerequisites are not met or errors are encountered.");
            Console.Out.WriteLine("               When combined with " + Installer.ParamPassiveKey + " all output is eliminated; only the");
            Console.Out.WriteLine("               return code can be used to validate success.");
            Console.Out.WriteLine();
            Console.Out.WriteLine("  " + CommandRemove + "       Removes the " + Title + " package");
            Console.Out.WriteLine("               and unsets Git configuration to no longer use the helper.");
            Console.Out.WriteLine();
            Console.Out.WriteLine("    " + Installer.ParamPathKey + "     Specifies a path for the installer to remove from.");
            Console.Out.WriteLine("               If a path is provided, the installer will not seek additional");
            Console.Out.WriteLine("               Git installations to modify.");
            Console.Out.WriteLine();
            Console.Out.WriteLine("    " + Installer.ParamPassiveKey + "  Instructs the installer to not prompt the user for input");
            Console.Out.WriteLine("               during removal and restricts output to error messages only.");
            Console.Out.WriteLine("               When combined with " + Installer.ParamForceKey + " all output is eliminated; only the");
            Console.Out.WriteLine("               return code can be used to validate success.");
            Console.Out.WriteLine();
            Console.Out.WriteLine("    " + Installer.ParamForceKey + "    Instructs the installer to proceed with removal even if");
            Console.Out.WriteLine("               prerequisites are not met or errors are encountered.");
            Console.Out.WriteLine("               When combined with " + Installer.ParamPassiveKey + " all output is eliminated; only the");
            Console.Out.WriteLine("               return code can be used to validate success.");
            Console.Out.WriteLine();
            Console.Out.WriteLine("  " + CommandDelete + "       Removes stored credentials for a given URL.");
            Console.Out.WriteLine("               Any future attempts to authenticate with the remote will require");
            Console.Out.WriteLine("               authentication steps to be completed again.");
            Console.Out.WriteLine();
            Console.Out.WriteLine("      `git credential-manager clear <url>`");
            Console.Out.WriteLine();
            Console.Out.WriteLine("  " + CommandVersion + "       Displays the current version.");

            Console.Out.WriteLine();
            PrintConfigurationHelp();
            Console.Out.WriteLine();
        }

        private static void Remove()
        {
            Trace.WriteLine("Program::Remove");

            var installer = new Installer();
            installer.RemoveConsole();

            Trace.WriteLine(String.Format("   Installer result = {0}.", installer.Result));
            Trace.WriteLine(String.Format("   Installer exit code = {0}.", installer.ExitCode));

            Environment.Exit(installer.ExitCode);
        }

        private static void Store()
        {
            // parse the operations arguments from stdin (this is how git sends commands)
            // see: https://www.kernel.org/pub/software/scm/git/docs/technical/api-credentials.html
            // see: https://www.kernel.org/pub/software/scm/git/docs/git-credential.html
            var stdin = Console.OpenStandardInput();
            OperationArguments operationArguments = new OperationArguments(stdin);

            Debug.Assert(operationArguments != null, "The operationArguments is null");
            Debug.Assert(operationArguments.CredUsername != null, "The operaionArgument.Username is null");
            Debug.Assert(operationArguments.TargetUri != null, "The operationArgument.TargetUri is null");

            LoadOperationArguments(operationArguments);
            EnableTraceLogging(operationArguments);

            Trace.WriteLine("Program::Store");
            Trace.WriteLine("   targetUri = " + operationArguments.TargetUri);

            BaseAuthentication authentication = CreateAuthentication(operationArguments);
            Credential credentials = new Credential(operationArguments.CredUsername, operationArguments.CredPassword);
            authentication.SetCredentials(operationArguments.TargetUri, credentials);
        }
    }
}
