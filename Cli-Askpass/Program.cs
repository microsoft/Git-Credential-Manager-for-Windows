using Microsoft.Alm.Authentication;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Microsoft.Alm.Cli
{
    internal partial class Program
    {
        public const string Title = "SSH Key Manager for Windows";
        public const string Description = "Secure SSH key helper for Windows, by Microsoft";
        public const string DefinitionUrlPassphrase = "https://www.visualstudio.com/docs/git/gcm-ssh-passphrase";

        private static readonly Regex AskCredentialRegex = new Regex(@"\s*(\S+)\s+for\s+'([^']+)':\s*$", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
        private static readonly Regex AskPassphraseRegex = new Regex(@"\s*\""Enter\s+passphrase\s+for\s+key\s+'([^']+)':\s+\""\s*$", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
        private static readonly Regex AskPasswordRegex = new Regex(@"\s*\""([^']+)'s\s+password:\s+\""\s*$", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

        private static void Askpass(string[] args)
        {
            if (args == null || args.Length == 0)
                throw new ArgumentException("Arguments cannot be empty.");

            Match match;
            if ((match = AskCredentialRegex.Match(args[0])).Success)
            {
                Git.Trace.WriteLine("querying for HTTPS credentials.");

                if (match.Groups.Count < 3)
                    throw new ArgumentException("Unable to understand command.");

                string seeking = match.Groups[1].Value;
                string targetUrl = match.Groups[2].Value;

                Uri targetUri;

                if (Uri.TryCreate(targetUrl, UriKind.Absolute, out targetUri))
                {
                    Git.Trace.WriteLine($"success parsing URL, targetUri = '{targetUri}'.");

                    // config stored credentials come in the format of <username>[:<password>]@<url> with password being optional
                    // scheme terminator is actually "://" so we need adjust to get the correct index
                    int schemeTerminator = targetUrl.IndexOf(':') + 2;
                    int credentialTerminator = targetUrl.IndexOf('@', schemeTerminator + 1);

                    if (credentialTerminator > 0)
                    {
                        Git.Trace.WriteLine("'@' symbol found in URL, assuming credential prefix.");

                        string username = null;
                        string password = null;

                        int passwordTerminator = targetUrl.IndexOf(':', schemeTerminator + 1);

                        if (passwordTerminator > 0)
                        {
                            Git.Trace.WriteLine("':' symbol found in URL, assuming credential prefix contains password.");

                            username = targetUrl.Substring(schemeTerminator + 1, passwordTerminator - schemeTerminator - 1);
                            password = targetUrl.Substring(passwordTerminator + 1, credentialTerminator - passwordTerminator + 1);

                            // print the password if it sought
                            if (seeking.Equals("Password", StringComparison.OrdinalIgnoreCase))
                            {
                                Console.Out.Write(password + '\n');
                                return;
                            }
                        }
                        else
                        {
                            username = targetUrl.Substring(schemeTerminator + 1, credentialTerminator - schemeTerminator - 1);
                        }

                        // print the username if it sought
                        if (seeking.Equals("Username", StringComparison.OrdinalIgnoreCase))
                        {
                            Console.Out.Write(username + '\n');
                            return;
                        }
                    }

                    // create a target Url with the credential portion stripped, because Git doesn't report hosts with credentials
                    targetUrl = String.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}://{1}", targetUri.Scheme, targetUri.Host);

                    // retain the port if specified
                    if (!targetUri.IsDefaultPort)
                    {
                        targetUrl += $":{targetUri.Port}";
                    }

                    // retain the path if specified
                    if (!String.IsNullOrWhiteSpace(targetUri.LocalPath))
                    {
                        targetUrl += targetUri.LocalPath;
                    }

                    if (Uri.TryCreate(targetUrl, UriKind.Absolute, out targetUri))
                    {
                        Git.Trace.WriteLine($"success parsing URL, targetUri = '{targetUri}'.");

                        OperationArguments operationArguments = new OperationArguments(targetUri);

                        // load up the operation arguments, enable tracing, and query for credentials
                        LoadOperationArguments(operationArguments);
                        EnableTraceLogging(operationArguments);

                        if (QueryCredentials(operationArguments))
                        {
                            if (seeking.Equals("Username", StringComparison.OrdinalIgnoreCase))
                            {
                                Git.Trace.WriteLine($"username for '{targetUrl}' asked for and found.");

                                Console.Out.Write(operationArguments.CredUsername + '\n');
                                return;
                            }

                            if (seeking.Equals("Password", StringComparison.OrdinalIgnoreCase))
                            {
                                Git.Trace.WriteLine($"password for '{targetUrl}' asked for and found.");

                                Console.Out.Write(operationArguments.CredPassword + '\n');
                                return;
                            }
                        }
                    }
                    else
                    {
                        Git.Trace.WriteLine("error: unable to parse target URL.");
                    }
                }
                else
                {
                    Git.Trace.WriteLine("error: unable to parse supplied URL.");
                }

                Git.Trace.WriteLine($"failed to detect {seeking} in target URL.");
            }

            if ((match = AskPasswordRegex.Match(args[0])).Success
                || (match = AskPassphraseRegex.Match(args[0])).Success)
            {
                Git.Trace.WriteLine("querying for passphrase key.");

                if (match.Groups.Count < 2)
                    throw new ArgumentException("Unable to understand command.");

                string request = match.Groups[0].Value;
                string resource = match.Groups[1].Value;

                Git.Trace.WriteLine($"open dialog for '{resource}'.");

                System.Windows.Application application = new System.Windows.Application();
                Gui.PassphraseWindow prompt = new Gui.PassphraseWindow(resource);
                application.Run(prompt);

                if (!prompt.Cancelled && !string.IsNullOrEmpty(prompt.Passphrase))
                {
                    string passphase = prompt.Passphrase;

                    Git.Trace.WriteLine("passphase acquired.");

                    Console.Out.Write(passphase + "\n");
                    return;
                }

                Git.Trace.WriteLine("failed to interactively acquire credentials.");
            }

            Git.Trace.WriteLine("failed to acquire credentials.");
        }

        [STAThread]
        private static void Main(string[] args)
        {
            EnableDebugTrace();

            if (args.Length > 0
                && (String.Equals(args[0], "--help", StringComparison.OrdinalIgnoreCase)
                    || String.Equals(args[0], "-h", StringComparison.OrdinalIgnoreCase)
                    || args[0].Contains('?')))
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

                // find the first inner exception which isn't an `AggregateException` with fallback to the canonical `.InnerException`
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

        private static void PrintHelpMessage()
        {
            const string HelpFileName = "git-askpass.html";

            Console.Out.WriteLine("usage: git askpass '<user_prompt_text>'");

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

            Die("Unable to open help documentation.");
        }
    }
}
