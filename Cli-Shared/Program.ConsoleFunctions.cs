using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Alm.Authentication;
using Microsoft.Alm.Git;
using Microsoft.Win32.SafeHandles;
using Bitbucket = Atlassian.Bitbucket.Authentication;
using Github = GitHub.Authentication;

namespace Microsoft.Alm.Cli
{
    partial class Program
    {
        internal static class ConsoleFunctions
        {
            public static Credential CredentialPrompt(TargetUri targetUri, string titleMessage)
            {
                // ReadConsole 32768 fail, 32767 ok @linquize [https://github.com/Microsoft/Git-Credential-Manager-for-Windows/commit/a62b9a19f430d038dcd85a610d97e5f763980f85]
                const int BufferReadSize = 16 * 1024;

                Debug.Assert(targetUri != null);

                if (!Program.StandardErrorIsTty || !Program.StandardInputIsTty)
                {
                    Git.Trace.WriteLine("not a tty detected, abandoning prompt.");
                    return null;
                }

                titleMessage = titleMessage ?? "Please enter your credentials for ";

                StringBuilder buffer = new StringBuilder(BufferReadSize);
                uint read = 0;
                uint written = 0;

                NativeMethods.FileAccess fileAccessFlags = NativeMethods.FileAccess.GenericRead | NativeMethods.FileAccess.GenericWrite;
                NativeMethods.FileAttributes fileAttributes = NativeMethods.FileAttributes.Normal;
                NativeMethods.FileCreationDisposition fileCreationDisposition = NativeMethods.FileCreationDisposition.OpenExisting;
                NativeMethods.FileShare fileShareFlags = NativeMethods.FileShare.Read | NativeMethods.FileShare.Write;

                using (SafeFileHandle stdout = NativeMethods.CreateFile(NativeMethods.ConsoleOutName, fileAccessFlags, fileShareFlags, IntPtr.Zero, fileCreationDisposition, fileAttributes, IntPtr.Zero))
                using (SafeFileHandle stdin = NativeMethods.CreateFile(NativeMethods.ConsoleInName, fileAccessFlags, fileShareFlags, IntPtr.Zero, fileCreationDisposition, fileAttributes, IntPtr.Zero))
                {
                    string username = null;
                    string password = null;

                    // read the current console mode
                    NativeMethods.ConsoleMode consoleMode;
                    if (!NativeMethods.GetConsoleMode(stdin, out consoleMode))
                    {
                        int error = Marshal.GetLastWin32Error();
                        throw new Win32Exception(error, "Unable to determine console mode (" + NativeMethods.Win32Error.GetText(error) + ").");
                    }

                    Git.Trace.WriteLine($"console mode = '{consoleMode}'.");

                    // instruct the user as to what they are expected to do
                    buffer.Append(titleMessage)
                          .Append(targetUri)
                          .AppendLine();
                    if (!NativeMethods.WriteConsole(stdout, buffer, (uint)buffer.Length, out written, IntPtr.Zero))
                    {
                        int error = Marshal.GetLastWin32Error();
                        throw new Win32Exception(error, "Unable to write to standard output (" + NativeMethods.Win32Error.GetText(error) + ").");
                    }

                    // clear the buffer for the next operation
                    buffer.Clear();

                    // prompt the user for the username wanted
                    buffer.Append("username: ");
                    if (!NativeMethods.WriteConsole(stdout, buffer, (uint)buffer.Length, out written, IntPtr.Zero))
                    {
                        int error = Marshal.GetLastWin32Error();
                        throw new Win32Exception(error, "Unable to write to standard output (" + NativeMethods.Win32Error.GetText(error) + ").");
                    }

                    // clear the buffer for the next operation
                    buffer.Clear();

                    // read input from the user
                    if (!NativeMethods.ReadConsole(stdin, buffer, BufferReadSize, out read, IntPtr.Zero))
                    {
                        int error = Marshal.GetLastWin32Error();
                        throw new Win32Exception(error, "Unable to read from standard input (" + NativeMethods.Win32Error.GetText(error) + ").");
                    }

                    // record input from the user into local storage, stripping any eol chars
                    username = buffer.ToString(0, (int)read);
                    username = username.Trim(Environment.NewLine.ToCharArray());

                    // clear the buffer for the next operation
                    buffer.Clear();

                    // set the console mode to current without echo input
                    NativeMethods.ConsoleMode consoleMode2 = consoleMode ^ NativeMethods.ConsoleMode.EchoInput;

                    try
                    {
                        if (!NativeMethods.SetConsoleMode(stdin, consoleMode2))
                        {
                            int error = Marshal.GetLastWin32Error();
                            throw new Win32Exception(error, "Unable to set console mode (" + NativeMethods.Win32Error.GetText(error) + ").");
                        }

                        Git.Trace.WriteLine($"console mode = '{consoleMode2}'.");

                        // prompt the user for password
                        buffer.Append("password: ");
                        if (!NativeMethods.WriteConsole(stdout, buffer, (uint)buffer.Length, out written, IntPtr.Zero))
                        {
                            int error = Marshal.GetLastWin32Error();
                            throw new Win32Exception(error, "Unable to write to standard output (" + NativeMethods.Win32Error.GetText(error) + ").");
                        }

                        // clear the buffer for the next operation
                        buffer.Clear();

                        // read input from the user
                        if (!NativeMethods.ReadConsole(stdin, buffer, BufferReadSize, out read, IntPtr.Zero))
                        {
                            int error = Marshal.GetLastWin32Error();
                            throw new Win32Exception(error, "Unable to read from standard input (" + NativeMethods.Win32Error.GetText(error) + ").");
                        }

                        // record input from the user into local storage, stripping any eol chars
                        password = buffer.ToString(0, (int)read);
                        password = password.Trim(Environment.NewLine.ToCharArray());
                    }
                    catch { throw; }
                    finally
                    {
                        // restore the console mode to its original value
                        NativeMethods.SetConsoleMode(stdin, consoleMode);

                        Git.Trace.WriteLine($"console mode = '{consoleMode}'.");
                    }

                    if (username != null && password != null)
                        return new Credential(username, password);
                }

                return null;
            }

            public static void Exit(int exitcode, string message, string path, int line, string name)
            {
                if (!string.IsNullOrWhiteSpace(message))
                {
                    Git.Trace.WriteLine(message, path, line, name);
                    Program.WriteLine(message);
                }

                Git.Trace.Flush();

                Environment.Exit(exitcode);
            }

            public static ConsoleKeyInfo ReadKey(bool intercept)
            {
                return (Program.StandardInputIsTty)
                    ? Console.ReadKey(intercept)
                    : new ConsoleKeyInfo(' ', ConsoleKey.Escape, false, false, false);
            }

            public static bool StandardHandleIsTty(NativeMethods.StandardHandleType handleType)
            {
                var standardHandle = NativeMethods.GetStdHandle(handleType);
                var handleFileType = NativeMethods.GetFileType(standardHandle);
                return handleFileType == NativeMethods.FileType.Char;
            }

            public static void Write(string message)
            {
                if (message == null)
                    return;

                Console.Error.WriteLine(message);
            }

            public static void WriteLine(string message)
            {
                Console.Error.WriteLine(message);
            }
        }
    }
}
