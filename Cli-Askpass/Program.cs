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

        private static void Askpass()
        {
            Match match;
            if ((match = AskCredentialRegex.Match(Environment.CommandLine)).Success)
            {
                Git.Trace.WriteLine("querying for HTTPS credentials.");

                if (match.Groups.Count < 3)
                    throw new ArgumentException("Unable to understand command.");

                string seeking = match.Groups[1].Value;
                string targetUrl = match.Groups[2].Value;

                Uri targetUri = new Uri(targetUrl);
                Credential credential = null;

                // config stored credentials come in the format of <username>[:<password>]@<url> with password being optional
                int tokenIndex = targetUrl.IndexOf('@');
                if (tokenIndex > 0)
                {
                    Git.Trace.WriteLine("'@' symbol found in URL, assuming credential prefix.");

                    string prefix = targetUrl.Substring(0, tokenIndex);
                    targetUrl = targetUrl.Substring(tokenIndex + 1, targetUrl.Length - tokenIndex - 1);

                    string username = null;
                    string password = null;

                    tokenIndex = prefix.IndexOf(':');
                    if (tokenIndex > 0)
                    {
                        Git.Trace.WriteLine("':' token found in credential prefix, parsing username & password.");

                        username = prefix.Substring(0, tokenIndex);
                        password = prefix.Substring(tokenIndex + 1, prefix.Length - tokenIndex - 1);
                    }

                    credential = new Credential(username, password);
                }

                if (Uri.TryCreate(targetUrl, UriKind.Absolute, out targetUri))
                {
                    Git.Trace.WriteLine($"success parsing URL, targetUri = '{targetUri}'.");

                    OperationArguments operationArguments = new OperationArguments(targetUri);

                    LoadOperationArguments(operationArguments);
                    EnableTraceLogging(operationArguments);

                    if (StringComparer.InvariantCultureIgnoreCase.Equals(seeking, "Username"))
                    {
                        if (string.IsNullOrEmpty(credential?.Username))
                        {
                            Git.Trace.WriteLine("username not supplied in config, need to query for value.");

                            QueryCredentials(operationArguments);
                            credential = new Credential(operationArguments.CredUsername, operationArguments.CredPassword);
                        }

                        if (!string.IsNullOrEmpty(credential?.Username))
                        {
                            Git.Trace.WriteLine($"username for '{targetUrl}' asked for and found.");

                            Console.Out.Write(credential.Username + "\n");
                            return;
                        }
                    }

                    if (StringComparer.InvariantCultureIgnoreCase.Equals(seeking, "Password"))
                    {
                        if (string.IsNullOrEmpty(credential?.Password))
                        {
                            Git.Trace.WriteLine("password not supplied in config, need to query for value.");

                            QueryCredentials(operationArguments);

                            // only honor the password if the stored credentials username was not supplied by or matches config
                            if (string.IsNullOrEmpty(credential?.Username)
                                || StringComparer.InvariantCultureIgnoreCase.Equals(credential.Username, operationArguments.CredUsername))
                            {
                                credential = new Credential(operationArguments.CredUsername, operationArguments.CredPassword);
                            }
                        }

                        if (!string.IsNullOrEmpty(credential?.Password))
                        {
                            Git.Trace.WriteLine($"password for '{targetUrl}' asked for and found.");

                            Console.Out.Write(credential.Password + "\n");
                            return;
                        }
                    }
                }
                else
                {
                    Git.Trace.WriteLine("unable to parse URL.");
                }
            }
            else if ((match = AskPasswordRegex.Match(Environment.CommandLine)).Success
                || (match = AskPassphraseRegex.Match(Environment.CommandLine)).Success)
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
                Askpass();
            }
            catch (AggregateException exception)
            {
                // print out more useful information when an `AggregateException` is encountered
                exception = exception.Flatten();

                // find the first inner exception which isn't an `AggregateException` with fallback to the canonical `.InnerException`
                Exception innerException = exception.InnerExceptions.FirstOrDefault(e => !(e is AggregateException))
                                        ?? exception.InnerException;

                Console.Error.WriteLine("Fatal: " + innerException.GetType().Name + " encountered.");
                Git.Trace.WriteLine("Fatal: " + exception.ToString());
                LogEvent(exception.ToString(), EventLogEntryType.Error);

                Environment.ExitCode = -1;
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine("Fatal: " + exception.GetType().Name + " encountered.");
                Git.Trace.WriteLine("Fatal: " + exception.ToString());
                LogEvent(exception.ToString(), EventLogEntryType.Error);

                Environment.ExitCode = -1;
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

            Console.Error.WriteLine("Unable to open help documentation.");
            Git.Trace.WriteLine("failed to open help documentation.");
        }
    }
}
