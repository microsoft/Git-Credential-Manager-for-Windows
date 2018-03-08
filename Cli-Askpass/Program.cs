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
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Alm.Authentication;

using Git = Microsoft.Alm.Authentication.Git;

namespace Microsoft.Alm.Cli
{
    internal partial class Program
    {
        public const string AssemblyTitle = "Askpass Utility for Windows";
        public const string AssemblyDesciption = "Secure askpass utility for Windows, by Microsoft";
        public const string DefinitionUrlPassphrase = "https://www.visualstudio.com/docs/git/gcm-ssh-passphrase";

        private static readonly Regex AskCredentialRegex = new Regex(@"(\S+)\s+for\s+['""]([^'""]+)['""]:\s*", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
        private static readonly Regex AskPassphraseRegex = new Regex(@"Enter\s+passphrase\s*for\s*key\s*['""]([^'""]+)['""]\:\s*", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
        private static readonly Regex AskPasswordRegex = new Regex(@"(\S+)'s\s+password:\s*", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
        private static readonly Regex AskAuthenticityRegex = new Regex(@"^\s*The authenticity of host '([^']+)' can't be established.\s+RSA key fingerprint is ([^\s:]+:[^\.]+).", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

        internal Program(Microsoft.Alm.Authentication.RuntimeContext context)
        {
            if (context is null)
                throw new ArgumentNullException(nameof(context));

            _context = context;

            Title = AssemblyTitle;

            DebuggerLaunch(this);
        }

        internal static bool TryParseUrlCredentials(string targetUrl, out string username, out string password)
        {
            // config stored credentials come in the format of <username>[:<password>]@<url> with
            // password being optional scheme terminator is actually "://" so we need adjust to get
            // the correct index
            int schemeTerminator = targetUrl.IndexOf(':') + 2;
            int credentialTerminator = targetUrl.IndexOf('@', schemeTerminator + 1);

            if (credentialTerminator > 0)
            {
                // only check within the credential portion of the url, don't look past the '@'
                // because the port token is the same as the username / password seperator.
                int credentialLength = credentialTerminator - schemeTerminator;
                credentialLength = Math.Max(0, credentialLength);

                // Now that we have a proper set of bounds for the credentials portion of the url,
                // check for a username:password pair.
                int passwordTerminator = targetUrl.IndexOf(':', schemeTerminator + 1, credentialLength);

                if (passwordTerminator > 0)
                {
                    username = targetUrl.Substring(schemeTerminator + 1, passwordTerminator - schemeTerminator - 1);
                    password = targetUrl.Substring(passwordTerminator + 1, credentialTerminator - passwordTerminator + 1);

                    // Unescape credentials
                    username = Uri.UnescapeDataString(username);
                    password = Uri.UnescapeDataString(password);
                }
                else
                {
                    username = targetUrl.Substring(schemeTerminator + 1, credentialTerminator - schemeTerminator - 1);
                    password = null;

                    // Unescape credentials
                    username = Uri.UnescapeDataString(username);
                }

                return true;
            }

            username = null;
            password = null;

            return false;
        }

        internal void Askpass(string[] args)
        {
            if (args == null || args.Length == 0)
                throw new ArgumentException("Arguments cannot be empty.");

            Gui.UserPromptKind promptKind = Gui.UserPromptKind.SshPassphrase;

            Match match;
            if ((match = AskPasswordRegex.Match(args[0])).Success)
            {
                promptKind = Gui.UserPromptKind.CredentialsPassword;
            }
            else if ((match = AskPassphraseRegex.Match(args[0])).Success)
            {
                promptKind = Gui.UserPromptKind.SshPassphrase;
            }

            if (match.Success)
            {
                _context.Trace.WriteLine("querying for passphrase key.");

                if (match.Groups.Count < 2)
                    throw new ArgumentException("Unable to understand command.");

                // string request = match.Groups[0].Value;
                string resource = match.Groups[1].Value;

                _context.Trace.WriteLine($"open dialog for '{resource}'.");

                System.Windows.Application application = new System.Windows.Application();
                Gui.UserPromptDialog prompt = new Gui.UserPromptDialog(promptKind, resource);
                application.Run(prompt);

                if (!prompt.Failed && !string.IsNullOrEmpty(prompt.Response))
                {
                    string passphase = prompt.Response;

                    _context.Trace.WriteLine("passphase acquired.");

                    Out.Write(passphase + "\n");
                    return;
                }

                Die("failed to interactively acquire credentials.");
            }

            if ((match = AskCredentialRegex.Match(args[0])).Success)
            {
                _context.Trace.WriteLine("querying for basic credentials.");

                if (match.Groups.Count < 3)
                    throw new ArgumentException("Unable to understand command.");

                string seeking = match.Groups[1].Value;
                string targetUrl = match.Groups[2].Value;

                string username = string.Empty;
                string password = string.Empty;
                Uri targetUri = null;

                // Since we're looking for HTTP(s) credentials, we can use NetFx `Uri` class.
                if (Uri.TryCreate(targetUrl, UriKind.Absolute, out targetUri))
                {
                    _context.Trace.WriteLine($"success parsing URL, targetUri = '{targetUri}'.");

                    if (TryParseUrlCredentials(targetUrl, out username, out password))
                    {
                        if (password != null
                            && seeking.Equals("Password", StringComparison.OrdinalIgnoreCase))
                        {
                            Out.Write(password + '\n');
                            return;
                        }

                        // print the username if it sought
                        if (seeking.Equals("Username", StringComparison.OrdinalIgnoreCase))
                        {
                            Out.Write(username + '\n');
                            return;
                        }
                    }

                    // create a target Url with the credential portion stripped, because Git doesn't
                    // report hosts with credentials
                    targetUrl = targetUri.Scheme + "://";

                    // Add the username@ portion of the url if it exists
                    if (username != null)
                    {
                        targetUrl += Uri.EscapeDataString(username);

                        targetUrl += '@';
                    }

                    targetUrl += targetUri.Host;

                    // retain the port if specified
                    if (!targetUri.IsDefaultPort)
                    {
                        targetUrl += $":{targetUri.Port}";
                    }

                    // retain the path if specified
                    if (!string.IsNullOrWhiteSpace(targetUri.LocalPath))
                    {
                        targetUrl += targetUri.LocalPath;
                    }

                    if (Uri.TryCreate(targetUrl, UriKind.Absolute, out targetUri))
                    {
                        _context.Trace.WriteLine($"success parsing URL, targetUri = '{targetUri}'.");

                        OperationArguments operationArguments = new OperationArguments(_context, targetUri);
                        operationArguments.SetCredentials(username ?? string.Empty, password ?? string.Empty);

                        // Load up the operation arguments, enable tracing, and query for credentials.
                        Task.Run(async () =>
                        {
                            await LoadOperationArguments(operationArguments);
                            EnableTraceLogging(operationArguments);

                            Credential credentials;
                            if ((credentials = await QueryCredentials(operationArguments)) != null)
                            {
                                if (seeking.Equals("Username", StringComparison.OrdinalIgnoreCase))
                                {
                                    _context.Trace.WriteLine($"username for '{targetUrl}' asked for and found.");

                                    Out.Write(credentials.Username + '\n');
                                    return;
                                }

                                if (seeking.Equals("Password", StringComparison.OrdinalIgnoreCase))
                                {
                                    _context.Trace.WriteLine($"password for '{targetUrl}' asked for and found.");

                                    Out.Write(credentials.Password + '\n');
                                    return;
                                }
                            }
                            else
                            {
                                _context.Trace.WriteLine($"user cancelled credential dialog.");
                                return;
                            }
                        }).Wait();
                    }
                    else
                    {
                        _context.Trace.WriteLine("error: unable to parse target URL.");
                    }
                }
                else
                {
                    _context.Trace.WriteLine("error: unable to parse supplied URL.");
                }

                Die($"failed to detect {seeking} in target URL.");
            }

            if ((match = AskAuthenticityRegex.Match(args[0])).Success)
            {
                string host = match.Groups[1].Value;
                string fingerprint = match.Groups[2].Value;

                _context.Trace.WriteLine($"requesting authorization to add {host} ({fingerprint}) to known hosts.");

                System.Windows.Application application = new System.Windows.Application();
                Gui.UserPromptDialog prompt = new Gui.UserPromptDialog(host, fingerprint);
                application.Run(prompt);

                if (prompt.Failed)
                {
                    _context.Trace.WriteLine("denied authorization of host.");
                    Out.Write("no\n");
                }
                else
                {
                    _context.Trace.WriteLine("approved authorization of host.");
                    Out.Write("yes\n");
                }

                return;
            }

            Die("failed to acquire credentials.");
        }

        internal void PrintHelpMessage()
        {
            const string HelpFileName = "git-askpass.html";

            Out.WriteLine("usage: git askpass '<user_prompt_text>'");

            List<Git.Installation> installations;
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
                            _context.Trace.WriteLine($"opening help documentation '{doc}'.");

                            Process.Start(doc);

                            return;
                        }
                    }
                }
            }

            Die("Unable to open help documentation.");
        }

        [STAThread]
        private static void Main(string[] args)
        {
            Program program = new Program(RuntimeContext.Default);

            program.Run(args);
        }

        private void Run(string[] args)
        {
            EnableDebugTrace();

            if (args.Length == 0
                || string.Equals(args[0], "--help", StringComparison.OrdinalIgnoreCase)
                || string.Equals(args[0], "-h", StringComparison.OrdinalIgnoreCase)
                || string.Equals(args[0], "\\?", StringComparison.Ordinal))
            {
                PrintHelpMessage();
                return;
            }

            PrintArgs(args);

            try
            {
                Askpass(args);
            }
            catch (AggregateException exception)
            {
                // print out more useful information when an `AggregateException` is encountered
                exception = exception.Flatten();

                // find the first inner exception which isn't an `AggregateException` with fallback
                // to the canonical `.InnerException`
                Exception innerException = exception.InnerExceptions.FirstOrDefault(e => !(e is AggregateException))
                                        ?? exception.InnerException;

                Die(innerException);
            }
            catch (Exception exception)
            {
                Die(exception);
            }

            Trace.Flush();
        }
    }
}
