using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.TeamFoundation.Git.Helpers.Networking
{
    class Program
    {
        static void Main(string[] args)
        {
            EnableDebugTrace();

            if (args.Length == 0 || args[0].Contains('?'))
            {
                PrintHelpMessage();
                return;
            }

            // parse the operations arguments from stdin (this is how git sends commands)
            // see: https://www.kernel.org/pub/software/scm/git/docs/technical/api-remote.html
            // see: https://www.kernel.org/pub/software/scm/git/docs/git-remote-helpers.html
            // see: https://www.kernel.org/pub/software/scm/git/docs/git-remote.html

            using (Stream stdin = Console.OpenStandardInput())
            using (Stream stdout = Console.OpenStandardOutput())
            using (StreamReader reader = new StreamReader(stdin, Encoding.UTF8))
            using (StreamWriter writer = new StreamWriter(stdout, Encoding.UTF8))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (line.Length == 0)
                        break;

                    if (String.Equals(line, "capabilities", StringComparison.OrdinalIgnoreCase))
                    {
                        writer.Write("connect\n");
                        writer.Write("export\n");
                        writer.Write("fetch\n");
                        writer.Write("import\n");
                        writer.Write("list\n");
                        writer.Write("option\n");
                        writer.Write("push\n");
                        writer.Write("\n");
                    }
                    else if (line.StartsWith("connect ", StringComparison.OrdinalIgnoreCase))
                    {

                    }
                    else if (line.StartsWith("export", StringComparison.OrdinalIgnoreCase))
                    {

                    }
                    else if (line.StartsWith("fetch", StringComparison.OrdinalIgnoreCase))
                    {

                    }
                    else if (line.StartsWith("import", StringComparison.OrdinalIgnoreCase))
                    {

                    }
                    else if (line.StartsWith("list", StringComparison.OrdinalIgnoreCase))
                    {

                    }
                    else if (line.StartsWith("option", StringComparison.OrdinalIgnoreCase))
                    {
                        string option = line.Substring("option".Length + 1);
                        string name, value;
                        int idx = option.IndexOf(' ');

                        if (idx >= 0)
                        {
                            name = option.Substring(0, idx);
                            value = option.Substring(idx + 1);
                        }
                        else
                        {
                            name = option;
                            value = "true";
                        }

                        if (name.Equals("check-connectivity", StringComparison.OrdinalIgnoreCase))
                        {

                        }
                        else if (name.StartsWith("depth", StringComparison.OrdinalIgnoreCase))
                        {

                        }
                        else if (name.StartsWith("dry-run", StringComparison.OrdinalIgnoreCase))
                        {

                        }
                        else if (name.StartsWith("followtags", StringComparison.OrdinalIgnoreCase))
                        {

                        }
                        else if (name.StartsWith("progress", StringComparison.OrdinalIgnoreCase))
                        {

                        }
                        else if (name.StartsWith("servpath", StringComparison.OrdinalIgnoreCase))
                        {

                        }
                        else if (name.StartsWith("verbosity", StringComparison.OrdinalIgnoreCase))
                        {

                        }
                    }
                    else if (line.StartsWith("push", StringComparison.OrdinalIgnoreCase))
                    {

                    }
                }
            }
        }

        private static void PrintHelpMessage()
        {
            Console.Out.WriteLine("usage: git-remote-https <command> [<args>]");
            Console.Out.WriteLine();
            Console.Out.WriteLine("Commands:");
            Console.Out.WriteLine("   need");
            Console.Out.WriteLine("   to");
            Console.Out.WriteLine("   design");
            Console.Out.WriteLine("   this");
            Console.Out.WriteLine();
            Console.Out.WriteLine("Configuration Options:");
            Console.Out.WriteLine("   need");
            Console.Out.WriteLine("   to");
            Console.Out.WriteLine("   design");
            Console.Out.WriteLine("   this");
            Console.Out.WriteLine();
            Console.Out.WriteLine("Sample Configuration:");
            Console.Out.WriteLine("   need");
            Console.Out.WriteLine("   to");
            Console.Out.WriteLine("   design");
            Console.Out.WriteLine("   this");
        }

        [Conditional("DEBUG")]
        private static void EnableDebugTrace()
        {
            // use the stderr stream for the trace as stdout is used in the cross-process communcations protocol
            Trace.Listeners.Add(new ConsoleTraceListener(useErrorStream: true));
        }
    }
}
