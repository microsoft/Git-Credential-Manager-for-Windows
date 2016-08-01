using System;
using System.Diagnostics;
using System.Linq;
using Microsoft.Alm.Authentication;

namespace Microsoft.Alm.Cli
{
    internal partial class Program
    {
        public const string Title = "SSH Key Manager for Windows";
        public const string Description = "Secure SSH key helper for Windows, by Microsoft";

        internal const string AskpassUsername = "Username";
        internal const string AskpsssPassword = "Password";

        private static bool Askpass()
        {
            Trace.WriteLine("Program::Askpass");

            string[] args = Environment.GetCommandLineArgs();
            string targetUrl = args[3]?.Trim('\'', ':');

            Uri targetUri = null;
            Credential credential = null;

            // config stored credentials come in the format of <username>[:<password>]@<url> with password being optional
            int tokenIndex = targetUrl.IndexOf('@');
            if (tokenIndex > 0)
            {
                Trace.WriteLine("   '@' symbol found in URL, assuming credential prefix.");

                string prefix = targetUrl.Substring(0, tokenIndex);
                targetUrl = targetUrl.Substring(tokenIndex + 1, targetUrl.Length - tokenIndex - 1);

                string username = null;
                string password = null;

                tokenIndex = prefix.IndexOf(':');
                if (tokenIndex > 0)
                {
                    Trace.WriteLine("   ':' token found in credential prefix, parsing username & password.");

                    username = prefix.Substring(0, tokenIndex);
                    password = prefix.Substring(tokenIndex + 1, prefix.Length - tokenIndex - 1);
                }

                credential = new Credential(username, password);
            }

            if (Uri.TryCreate(targetUrl, UriKind.Absolute, out targetUri))
            {
                Trace.WriteLine("   success parsing URL, targetUri = " + targetUri);

                OperationArguments operationArguments = new OperationArguments(targetUri);

                LoadOperationArguments(operationArguments);
                EnableTraceLogging(operationArguments);

                QueryCredentials(operationArguments);

                if (StringComparer.InvariantCultureIgnoreCase.Equals(args[1], AskpassUsername))
                {
                    if (string.IsNullOrEmpty(credential?.Username))
                    {
                        Trace.WriteLine("   username not supplied in config, need to query for value.");

                        QueryCredentials(operationArguments);
                        credential = new Credential(operationArguments.CredUsername, operationArguments.CredPassword);
                    }

                    if (!string.IsNullOrEmpty(credential?.Username))
                    {
                        Trace.WriteLine("   username for '{0}' asked for and found.", targetUrl);

                        Console.Out.Write(credential.Username + "\n");
                        return true;
                    }
                }

                if (StringComparer.InvariantCultureIgnoreCase.Equals(args[1], AskpsssPassword))
                {
                    if (string.IsNullOrEmpty(credential?.Password))
                    {
                        Trace.WriteLine("   password not supplied in config, need to query for value.");

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
                        Trace.WriteLine("   password for '{0}' asked for and found.", targetUrl);

                        Console.Out.Write(credential.Password + "\n");
                        return true;
                    }
                }
            }
            else
            {
                Trace.WriteLine("   unable to parse URL.");
            }

            Trace.WriteLine("   credentials not found.");

            return false;
        }

        private static void Main(string[] args)
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
                Trace.WriteLine("Fatal: " + exception.ToString());
                LogEvent(exception.ToString(), EventLogEntryType.Error);
            }
            catch (Exception exception)
            {
                Console.Error.WriteLine("Fatal: " + exception.GetType().Name + " encountered.");
                Trace.WriteLine("Fatal: " + exception.ToString());
                LogEvent(exception.ToString(), EventLogEntryType.Error);
            }

            Trace.Flush();
        }

        private static void PrintHelpMessage()
        {
            Console.Out.WriteLine("usage: git askpass '<user_prompt_text>'");

            Console.Out.WriteLine();
            PrintConfigurationHelp();
            Console.Out.WriteLine();
        }
    }
}
