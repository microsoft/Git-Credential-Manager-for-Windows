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

using Git = Microsoft.Alm.Authentication.Git;

namespace Microsoft.Alm.Cli
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1806:DoNotIgnoreMethodResults")]
    internal partial class Program
    {
        public const string AssemblyTitle = "Git Credential Manager for Windows";
        public const string AssemblyDescription = "Secure Git credential helper for Windows, by Microsoft";

        internal const string CommandApprove = "approve";
        internal const string CommandClear = "clear";
        internal const string CommandConfig = "config";
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
            CommandConfig,
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

        internal Program(RuntimeContext context)
            : base(context)
        { }

        internal async Task Clear()
        {
            var args = Environment.GetCommandLineArgs();
            string url = null;
            bool forced = false;

            if (args.Length <= 2)
            {
                if (!StandardInputIsTty)
                {
                    Trace.WriteLine("standard input is not TTY, abandoning prompt.");

                    return;
                }

                Trace.WriteLine("prompting user for url.");

                WriteLine(" Target Url:");
                url = In.ReadLine();
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
                Trace.WriteLine($"converted '{url}' to '{uri.AbsoluteUri}'.");

                var operationArguments = new OperationArguments(Context, uri);

                await LoadOperationArguments(operationArguments);
                EnableTraceLogging(operationArguments);

                if (operationArguments.PreserveCredentials && !forced)
                {
                    Trace.WriteLine("attempting to delete preserved credentials without force, prompting user for interactivity.");

                    if (!StandardInputIsTty || !StandardErrorIsTty)
                    {
                        Trace.WriteLine("standard input is not TTY, abandoning prompt.");
                        return;
                    }

                    WriteLine(" credentials are protected by preserve flag, clear anyways? [Y]es, [N]o.");

                    ConsoleKeyInfo key;
                    while ((key = ReadKey(true)).Key != ConsoleKey.Escape)
                    {
                        if (key.KeyChar == 'N' || key.KeyChar == 'n')
                            return;

                        if (key.KeyChar == 'Y' || key.KeyChar == 'y')
                            break;
                    }
                }

                await DeleteCredentials(operationArguments);
            }
            else
            {
                Trace.WriteLine($"unable to parse input '{url}'.");
            }
        }

        internal async Task Config()
        {
            string[] args = Environment.GetCommandLineArgs();

            // Attempt to parse a target URI from the command line arguments.
            if (args.Length < 3 || !Uri.TryCreate(args[2], UriKind.Absolute, out Uri targetUri))
            {
                targetUri = new Uri("file://localhost");
            }

            // Create operation arguments, and load configuration data.
            OperationArguments operationArguments = new OperationArguments(Context, targetUri);

            await LoadOperationArguments(operationArguments);
            EnableTraceLogging(operationArguments);

            // Create a set of irrelevant environment variable entries.
            var irrelevantEntries = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "ToastSettings"
            };

            // Write out the environment variables.
            WriteLine("Environment Variables:");
            foreach (var entry in operationArguments.EnvironmentVariables)
            {
                // Skip well-known, irrelevant entries.
                if (irrelevantEntries.Contains(entry.Key))
                    continue;

                WriteLine($"  {entry.Key} = {entry.Value}");
            }
            WriteLine();

            // Write out the Git configuration.
            WriteLine("Git Configuration:");
            foreach (var entry in operationArguments.GitConfiguration)
            {
                WriteLine($"  [{entry.Level}] {entry.Key} = {entry.Value}");
            }
            WriteLine();

            // Write out the effective settings for GCM.
            WriteLine($"Effective Manager Configuration for {operationArguments.QueryUri.ToString()}:");
            WriteLine($"  Executable = {AssemblyTitle} v{Version.ToString(4)} ({ExecutablePath})");
            WriteLine($"  Authority = {operationArguments.Authority}");
            WriteLine($"  CustomNamespace = {operationArguments.CustomNamespace}");
            WriteLine($"  Interactivity = {operationArguments.Interactivity}");
            WriteLine($"  PreserveCredentials = {operationArguments.PreserveCredentials}");
            WriteLine($"  QueryUri = {operationArguments.QueryUri}");
            WriteLine($"  TargetUri = {operationArguments.TargetUri}");
            WriteLine($"  TokenDuration = {operationArguments.TokenDuration}");
            WriteLine($"  UseConfigLocal = {operationArguments.UseConfigLocal}");
            WriteLine($"  UseConfigSystem = {operationArguments.UseConfigSystem}");
            WriteLine($"  UseHttpPath = {operationArguments.UseHttpPath}");
            WriteLine($"  UseModalUi = {operationArguments.UseModalUi}");
            WriteLine($"  ValidateCredentials = {operationArguments.ValidateCredentials}");
            WriteLine($"  VstsTokenScope = {operationArguments.VstsTokenScope}");
            WriteLine($"  WriteLog = {operationArguments.WriteLog}");
        }

        internal async Task Delete()
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
                url = string.Format("{0}://{1}", Uri.UriSchemeHttps, url);
                if (!Uri.TryCreate(url, UriKind.Absolute, out uri))
                    goto error_parse;
            }

            using (var stdin = InStream)
            {
                OperationArguments operationArguments = new OperationArguments()
                {
                    QueryUri = uri
                };

                operationArguments.LoadFromStream(stdin);

                await LoadOperationArguments(operationArguments);

                BaseAuthentication authentication = await CreateAuthentication(operationArguments);

                switch (operationArguments.Authority)
                {
                    default:
                    case AuthorityType.Basic:
                        Trace.WriteLine($"deleting basic credentials for '{operationArguments.TargetUri}'.");
                        break;

                    case AuthorityType.AzureDirectory:
                    case AuthorityType.MicrosoftAccount:
                        Trace.WriteLine($"deleting VSTS credentials for '{operationArguments.TargetUri}'.");
                        break;

                    case AuthorityType.GitHub:
                        Trace.WriteLine($"deleting GitHub credentials for '{operationArguments.TargetUri}'.");
                        break;

                    case AuthorityType.Ntlm:
                        Trace.WriteLine($"deleting NTLM credentials for '{operationArguments.TargetUri}'.");
                        break;

                    case AuthorityType.Bitbucket:
                        Trace.WriteLine($"deleting Bitbucket credentials for '{operationArguments.Username}@{operationArguments.TargetUri}'.");
                        break;
                }

                await authentication.DeleteCredentials(operationArguments.TargetUri, operationArguments.Username);
            }

            return;

            error_parse:
            Die("Unable to parse target URI.");
        }

        internal Task Deploy()
        {
            var installer = new Installer(this);
            installer.DeployConsole();

            Trace.WriteLine($"Installer result = '{installer.Result}', exit code = {installer.ExitCode}.");

            Exit(installer.ExitCode);

            return Task.FromResult(true);
        }

        internal async Task Erase()
        {
            // parse the operations arguments from stdin (this is how git sends commands)
            // see: https://www.kernel.org/pub/software/scm/git/docs/technical/api-credentials.html
            // see: https://www.kernel.org/pub/software/scm/git/docs/git-credential.html
            using (var stdin = InStream)
            {
                OperationArguments operationArguments = new OperationArguments();
                operationArguments.LoadFromStream(stdin);

                await LoadOperationArguments(operationArguments);
                EnableTraceLogging(operationArguments);

                if (operationArguments.PreserveCredentials)
                {
                    Trace.WriteLine($"{KeyTypeName(KeyType.PreserveCredentials)} = true, canceling erase request.");
                    return;
                }

                await DeleteCredentials(operationArguments);
            }
        }

        internal async Task Get()
        {
            // parse the operations arguments from stdin (this is how git sends commands)
            // see: https://www.kernel.org/pub/software/scm/git/docs/technical/api-credentials.html
            // see: https://www.kernel.org/pub/software/scm/git/docs/git-credential.html
            using (var stdin = InStream)
            {
                OperationArguments operationArguments = new OperationArguments();
                operationArguments.LoadFromStream(stdin);

                await LoadOperationArguments(operationArguments);
                EnableTraceLogging(operationArguments);

                Credential credentials;
                if ((credentials = await QueryCredentials(operationArguments)) == null)
                {
                    Exit(-1, "Logon failed, use ctrl+c to cancel basic credential prompt.");
                }
                else
                {
                    using (var stdout = OutStream)
                    {
                        operationArguments.WriteToStream(stdout);
                    }
                }
            }
        }

        internal Task PrintHelpMessage()
        {
            const string HelpFileName = "git-credential-manager.html";

            WriteLine("usage: " + Name + ".exe [" + string.Join("|", CommandList) + "] [<args>]");

            List<Git.GitInstallation> installations;
            if (Where.FindGitInstallations(out installations))
            {
                foreach (var installation in installations)
                {
                    if (FileSystem.DirectoryExists(installation.Doc))
                    {
                        string doc = Path.Combine(installation.Doc, HelpFileName);

                        // if the help file exists, send it to the operating system to display to the user
                        if (FileSystem.FileExists(doc))
                        {
                            Trace.WriteLine($"opening help documentation '{doc}'.");

                            Process.Start(doc);

                            return Task.FromResult(true);
                        }
                    }
                }
            }

            Die("Unable to open help documentation.");
            return Task.FromResult(false);
        }

        internal Task Remove()
        {
            var installer = new Installer(this);
            installer.RemoveConsole();

            Trace.WriteLine($"Installer result = {installer.Result}, exit code = {installer.ExitCode}.");

            Exit(installer.ExitCode);
            return Task.FromResult(true);
        }

        internal async Task Store()
        {
            // parse the operations arguments from stdin (this is how git sends commands)
            // see: https://www.kernel.org/pub/software/scm/git/docs/technical/api-credentials.html
            // see: https://www.kernel.org/pub/software/scm/git/docs/git-credential.html
            using (var stdin = InStream)
            {
                OperationArguments operationArguments = new OperationArguments();
                operationArguments.LoadFromStream(stdin);

                Debug.Assert(operationArguments != null, "The operationArguments is null");
                Debug.Assert(operationArguments.Username != null, "The operaionArgument.Username is null");
                Debug.Assert(operationArguments.TargetUri != null, "The operationArgument.TargetUri is null");

                await LoadOperationArguments(operationArguments);
                EnableTraceLogging(operationArguments);

                var credentials = operationArguments.Credentials;
                BaseAuthentication authentication = await CreateAuthentication(operationArguments); ;

                switch (operationArguments.Authority)
                {
                    default:
                    case AuthorityType.Basic:
                        Trace.WriteLine($"storing basic credentials for '{operationArguments.TargetUri}'.");
                        break;

                    case AuthorityType.Bitbucket:
                        Trace.WriteLine($"storing Bitbucket credentials for '{operationArguments.TargetUri}'.");
                        break;

                    case AuthorityType.AzureDirectory:
                    case AuthorityType.MicrosoftAccount:
                        Trace.WriteLine($"storing VSTS credentials for '{operationArguments.TargetUri}'.");
                        break;

                    case AuthorityType.GitHub:
                        Trace.WriteLine($"storing GitHub credentials for '{operationArguments.TargetUri}'.");
                        break;

                    case AuthorityType.Ntlm:
                        Trace.WriteLine($"storing NTLM credentials for '{operationArguments.TargetUri}'.");
                        break;
                }

                await authentication.SetCredentials(operationArguments.TargetUri, credentials);
            }
        }

        [STAThread]
        private static void Main(string[] args)
        {
            var program = new Program(RuntimeContext.Default);

            program.Run(args);
        }

        private void Run(string[] args)
        {
            try
            {
                EnableDebugTrace();

                if (args.Length == 0
                    || string.Equals(args[0], "--help", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(args[0], "-h", StringComparison.OrdinalIgnoreCase)
                    || args[0].Contains('?'))
                {
                    PrintHelpMessage();
                    return;
                }

                PrintArgs(args);

                // List of `arg` => method associations (case-insensitive).
                var actions = new Dictionary<string, Func<Task>>(StringComparer.OrdinalIgnoreCase)
                {
                    { CommandApprove, Store },
                    { CommandClear, Clear },
                    { CommandConfig, Config },
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

                // Run the task as the child of an awaited task, yes this is wasteful
                // but it makes debugging so much easier and the actual costs is very
                // minimal - so it is worth it.
                Task.Run(async () =>
                {
                    // Invoke action specified by arg0.
                    if (actions.ContainsKey(args[0]))
                    {
                        await actions[args[0]]();
                    }
                }).Wait();
            }
            catch (AggregateException exception)
            {
                // Print out more useful information when an `AggregateException` is encountered.
                exception = exception.Flatten();

                // Find the first inner exception which isn't an `AggregateException` with fall-back
                // to the canonical `.InnerException`.
                Exception innerException = exception.InnerExceptions.FirstOrDefault(e => !(e is AggregateException))
                                        ?? exception.InnerException;

                Die(innerException);
            }
            catch (Exception exception)
            {
                Die(exception);
            }
        }
    }
}
