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

using Microsoft.Alm.Authentication;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

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

        private static readonly IReadOnlyList<string> CommandList = new string[]
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
            var args = Environment.GetCommandLineArgs();
            string url = null;
            bool forced = false;

            if (args.Length <= 2)
            {
                if (!StandardInputIsTty)
                {
                    Git.Trace.WriteLine("standard input is not TTY, abandoning prompt.");

                    return;
                }

                Git.Trace.WriteLine("prompting user for url.");

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

            Uri uri;
            if (Uri.TryCreate(url, UriKind.Absolute, out uri))
            {
                Git.Trace.WriteLine($"converted '{url}' to '{uri.AbsoluteUri}'.");

                OperationArguments operationArguments = new OperationArguments(uri);

                LoadOperationArguments(operationArguments);
                EnableTraceLogging(operationArguments);

                if (operationArguments.PreserveCredentials && !forced)
                {
                    Git.Trace.WriteLine("attempting to delete preserved credentials without force, prompting user for interactivity.");

                    if (!StandardInputIsTty || !StandardErrorIsTty)
                    {
                        Git.Trace.WriteLine("standard input is not TTY, abandoning prompt.");
                        return;
                    }

                    Console.Error.WriteLine(" credentials are protected by perserve flag, clear anyways? [Y]es, [N]o.");

                    ConsoleKeyInfo key;
                    while ((key = Console.ReadKey(true)).Key != ConsoleKey.Escape)
                    {
                        if (key.KeyChar == 'N' || key.KeyChar == 'n')
                            return;

                        if (key.KeyChar == 'Y' || key.KeyChar == 'y')
                            break;
                    }
                }

                DeleteCredentials(operationArguments);
            }
            else
            {
                Git.Trace.WriteLine($"unable to parse input '{url}'.");
            }
        }

        private static void Delete()
        {
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
                    Git.Trace.WriteLine($"deleting basic credentials for '{operationArguments.TargetUri}'.");
                    authentication.DeleteCredentials(operationArguments.TargetUri);
                    break;

                case AuthorityType.AzureDirectory:
                case AuthorityType.MicrosoftAccount:
                    Git.Trace.WriteLine("deleting VSTS credentials for '{operationArguments.TargetUri}'.");
                    BaseVstsAuthentication vstsAuth = authentication as BaseVstsAuthentication;
                    vstsAuth.DeleteCredentials(operationArguments.TargetUri);
                    // call delete twice to purge any stored ADA tokens
                    vstsAuth.DeleteCredentials(operationArguments.TargetUri);
                    break;

                case AuthorityType.GitHub:
                    Git.Trace.WriteLine("deleting GitHub credentials for '{operationArguments.TargetUri}'.");
                    GitHubAuthentication ghAuth = authentication as GitHubAuthentication;
                    ghAuth.DeleteCredentials(operationArguments.TargetUri);
                    break;
            }

            return;

            error_parse:
            Git.Trace.WriteLine($"Fatal: unable to parse target URI.");
            Console.Error.WriteLine("Fatal: unable to parse target URI.");
        }

        private static void Deploy()
        {
            var installer = new Installer();
            installer.DeployConsole();

            Git.Trace.WriteLine($"Installer result = '{installer.Result}', exit code = {installer.ExitCode}.");

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

            if (operationArguments.PreserveCredentials)
            {
                Git.Trace.WriteLine($"{ConfigPreserveCredentialsKey} = true, canceling erase request.");
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

                PrintArgs(args);

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

                Git.Trace.WriteLine("Fatal: " + exception.ToString());
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

                Git.Trace.WriteLine("Fatal: " + exception.ToString());
                LogEvent(exception.ToString(), EventLogEntryType.Error);

                Environment.ExitCode = -1;
            }

            Trace.Flush();
        }

        private static void PrintHelpMessage()
        {
            const string HelpFileName = "git-credential-manager.html";

            Console.Out.WriteLine("usage: git credential-manager [" + String.Join("|", CommandList) + "] [<args>]");

            List<Git.GitInstallation> installations;
            if (Git.Where.FindGitInstallations(out installations))
            {
                foreach (var installation in installations)
                {
                    if (Directory.Exists(installation.Doc))
                    {
                        string doc = Path.Combine(installation.Doc, HelpFileName);

                        // if the help file exists, send it to the operating system to display to the user
                        if (File.Exists(doc))
                        {
                            Git.Trace.WriteLine($"opening help documentation '{doc}'.");

                            Process.Start(doc);

                            return;
                        }
                    }
                }
            }

            Console.Error.WriteLine("Unable to open help documentation.");
            Git.Trace.WriteLine("failed to open help documentation.");
        }

        private static void Remove()
        {
            var installer = new Installer();
            installer.RemoveConsole();

            Git.Trace.WriteLine($"Installer result = {installer.Result}, exit code = {installer.ExitCode}.");

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

            BaseAuthentication authentication = CreateAuthentication(operationArguments);
            Credential credentials = new Credential(operationArguments.CredUsername, operationArguments.CredPassword);
            authentication.SetCredentials(operationArguments.TargetUri, credentials);
        }
    }
}
