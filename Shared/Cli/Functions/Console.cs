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
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Alm.Authentication;
using Microsoft.Win32.SafeHandles;
using static Microsoft.Alm.NativeMethods;

namespace Microsoft.Alm.Cli
{
    internal static class ConsoleFunctions
    {
        public static Credential CredentialPrompt(Program program, TargetUri targetUri, string titleMessage)
        {
            // ReadConsole 32768 fail, 32767 OK @linquize [https://github.com/Microsoft/Git-Credential-Manager-for-Windows/commit/a62b9a19f430d038dcd85a610d97e5f763980f85]
            const int BufferReadSize = 16 * 1024;

            if (program is null)
                throw new ArgumentNullException(nameof(program));
            if (targetUri is null)
                throw new ArgumentNullException(nameof(targetUri));

            titleMessage = titleMessage ?? "Please enter your credentials for ";

            var buffer = new StringBuilder(BufferReadSize);
            uint read = 0;
            uint written = 0;

            ConsoleMode consoleMode = 0;
            var fileAccessFlags = NativeMethods.FileAccess.GenericRead
                                | NativeMethods.FileAccess.GenericWrite;
            var fileAttributes = NativeMethods.FileAttributes.Normal;
            var fileCreationDisposition = FileCreationDisposition.OpenExisting;
            var fileShareFlags = NativeMethods.FileShare.Read
                               | NativeMethods.FileShare.Write;

            using (SafeFileHandle stdout = CreateFile(fileName: ConsoleOutName, 
                                                 desiredAccess: fileAccessFlags, 
                                                     shareMode: fileShareFlags, 
                                            securityAttributes: IntPtr.Zero, 
                                           creationDisposition: fileCreationDisposition, 
                                            flagsAndAttributes: fileAttributes, 
                                                  templateFile: IntPtr.Zero))
            using (SafeFileHandle stdin = CreateFile(fileName: ConsoleInName,
                                                desiredAccess: fileAccessFlags,
                                                    shareMode: fileShareFlags,
                                           securityAttributes: IntPtr.Zero,
                                          creationDisposition: fileCreationDisposition,
                                           flagsAndAttributes: fileAttributes,
                                                 templateFile: IntPtr.Zero))
            {

                // Read the current console mode.
                if (stdin.IsInvalid || stdout.IsInvalid)
                {
                    program.Trace.WriteLine("not a tty detected, abandoning prompt.");
                    return null;
                }
                else if (!GetConsoleMode(consoleMode: out consoleMode,
                                       consoleHandle: stdin))
                {
                    int error = Marshal.GetLastWin32Error();
                    throw new Win32Exception(error, "Unable to determine console mode (" + Win32Error.GetText(error) + ").");
                }

                program.Trace.WriteLine($"console mode = '{consoleMode}'.");

                string username = null;
                string password = null;

                // Instruct the user as to what they are expected to do.
                buffer.Append(titleMessage)
                      .Append(targetUri)
                      .AppendLine();

                if (!WriteConsole(buffer: buffer,
                     consoleOutputHandle: stdout,
                    numberOfCharsToWrite: (uint)buffer.Length,
                    numberOfCharsWritten: out written,
                                reserved: IntPtr.Zero))
                {
                    int error = Marshal.GetLastWin32Error();
                    throw new Win32Exception(error, "Unable to write to standard output (" + Win32Error.GetText(error) + ").");
                }

                // Clear the buffer for the next operation.
                buffer.Clear();

                // Prompt the user for the username wanted.
                buffer.Append("username: ");
                if (!WriteConsole(buffer: buffer, 
                     consoleOutputHandle: stdout,             
                    numberOfCharsToWrite: (uint)buffer.Length,
                    numberOfCharsWritten: out written,
                                reserved: IntPtr.Zero))
                {
                    int error = Marshal.GetLastWin32Error();
                    throw new Win32Exception(error, "Unable to write to standard output (" + Win32Error.GetText(error) + ").");
                }

                // Clear the buffer for the next operation.
                buffer.Clear();

                // Read input from the user.
                if (!ReadConsole(buffer: buffer, 
                     consoleInputHandle: stdin,
                    numberOfCharsToRead: BufferReadSize,
                      numberOfCharsRead: out read,
                               reserved: IntPtr.Zero))
                {
                    int error = Marshal.GetLastWin32Error();
                    throw new Win32Exception(error, "Unable to read from standard input (" + Win32Error.GetText(error) + ").");
                }

                // Record input from the user into local storage, stripping any EOL chars.
                username = buffer.ToString(0, (int)read);
                username = username.Trim(program.Settings.NewLine.ToCharArray());

                // Clear the buffer for the next operation.
                buffer.Clear();

                // Set the console mode to current without echo input.
                ConsoleMode consoleMode2 = consoleMode ^ ConsoleMode.EchoInput;

                try
                {
                    if (!SetConsoleMode(consoleMode: consoleMode2,
                                      consoleHandle: stdin))
                    {
                        int error = Marshal.GetLastWin32Error();
                        throw new Win32Exception(error, "Unable to set console mode (" + Win32Error.GetText(error) + ").");
                    }

                    program.Trace.WriteLine($"console mode = '{(consoleMode2 & ConsoleMode.AllFlags)}'.");

                    // Prompt the user for password.
                    buffer.Append("password: ");
                    if (!WriteConsole(buffer: buffer, 
                         consoleOutputHandle: stdout,
                        numberOfCharsToWrite: (uint)buffer.Length,
                        numberOfCharsWritten: out written,
                                    reserved: IntPtr.Zero))
                    {
                        int error = Marshal.GetLastWin32Error();
                        throw new Win32Exception(error, "Unable to write to standard output (" + Win32Error.GetText(error) + ").");
                    }

                    // Clear the buffer for the next operation.
                    buffer.Clear();

                    // Read input from the user.
                    if (!ReadConsole(buffer: buffer, 
                         consoleInputHandle: stdin,                        
                        numberOfCharsToRead: BufferReadSize,
                          numberOfCharsRead: out read,
                                   reserved: IntPtr.Zero))
                    {
                        int error = Marshal.GetLastWin32Error();
                        throw new Win32Exception(error, "Unable to read from standard input (" + Win32Error.GetText(error) + ").");
                    }

                    // Record input from the user into local storage, stripping any EOL chars.
                    password = buffer.ToString(0, (int)read);
                    password = password.Trim(program.Settings.NewLine.ToCharArray());
                }
                finally
                {
                    // Restore the console mode to its original value.
                    SetConsoleMode(consoleMode: consoleMode,
                                 consoleHandle: stdin);

                    program.Trace.WriteLine($"console mode = '{consoleMode}'.");
                }

                if (username != null && password != null)
                    return new Credential(username, password);
            }

            return null;
        }

        public static void Exit(Program program, int exitcode, string message, string path, int line, string name)
        {
            if (program is null)
                throw new ArgumentNullException(nameof(program));

            if (!string.IsNullOrWhiteSpace(message))
            {
                program.Trace.WriteLine(message, path, line, name);
                program.WriteLine(message);
            }

            program.Trace.Flush();

            program.Settings.Exit(exitcode);
        }

        public static Stream OpenStandardErrorStream(Program program)
            => Console.OpenStandardError();

        public static TextWriter OpenStandardErrorWriter(Program program)
            => Console.Error;

        public static Stream OpenStandardInputStream(Program program)
            => Console.OpenStandardInput();

        public static TextReader OpenStandardInputReader(Program program)
            => Console.In;

        public static Stream OpenStandardOutputStream(Program program)
            => Console.OpenStandardOutput();

        public static TextWriter OpenStandardOutputWriter(Program program)
            => Console.Out;

        public static ConsoleKeyInfo ReadKey(Program program, bool intercept)
        {
            return (program.StandardInputIsTty)
                ? Console.ReadKey(intercept)
                : new ConsoleKeyInfo(' ', ConsoleKey.Escape, false, false, false);
        }

        public static void SetStandardErrorWriter(Program program, TextWriter writer)
            => Console.SetError(writer);

        public static void SetStandardInputReader(Program program, TextReader reader)
            => Console.SetIn(reader);

        public static void SetStandardOutputWriter(Program program, TextWriter writer)
            => Console.SetOut(writer);

        public static bool StandardHandleIsTty(Program program, StandardHandleType handleType)
        {
            var standardHandle = GetStdHandle(std: handleType);
            var handleFileType = GetFileType(fileHandle: standardHandle);
            return handleFileType == FileType.Char;
        }

        public static void Write(Program program, string message)
        {
            if (message == null)
                return;

            Console.Error.WriteLine(message);
        }

        public static void WriteLine(Program program, string message)
        {
            Console.Error.WriteLine(message);
        }
    }
}
