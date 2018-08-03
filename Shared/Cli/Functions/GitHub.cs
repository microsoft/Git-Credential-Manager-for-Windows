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
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Alm.Authentication;
using Microsoft.Win32.SafeHandles;
using static Microsoft.Alm.NativeMethods;
using Github = GitHub.Authentication;

namespace Microsoft.Alm.Cli
{
    internal static class GitHubFunctions
    {
        public static bool AuthCodePrompt(Program program, TargetUri targetUri, Github.GitHubAuthenticationResultType resultType, string username, out string authenticationCode)
        {
            // ReadConsole 32768 fail, 32767 ok @linquize [https://github.com/Microsoft/Git-Credential-Manager-for-Windows/commit/a62b9a19f430d038dcd85a610d97e5f763980f85]
            const int BufferReadSize = 16 * 1024;

            if (program is null)
                throw new ArgumentNullException(nameof(program));
            if (targetUri is null)
                throw new ArgumentNullException(nameof(targetUri));

            var trace = program.Trace;

            StringBuilder buffer = new StringBuilder(BufferReadSize);
            uint read = 0;
            uint written = 0;

            authenticationCode = null;

            var fileAccessFlags = FileAccess.GenericRead
                                | FileAccess.GenericWrite;
            var fileAttributes = FileAttributes.Normal;
            var fileCreationDisposition = FileCreationDisposition.OpenExisting;
            var fileShareFlags = FileShare.Read
                               | FileShare.Write;

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
                string type = resultType == Github.GitHubAuthenticationResultType.TwoFactorApp
                    ? "app"
                    : "sms";

                trace.WriteLine($"2fa type = '{type}'.");

                buffer.AppendLine()
                      .Append("authcode (")
                      .Append(type)
                      .Append("): ");

                if (!WriteConsole(buffer: buffer,
                     consoleOutputHandle: stdout,
                    numberOfCharsToWrite: (uint)buffer.Length,
                    numberOfCharsWritten: out written,
                                reserved: IntPtr.Zero))
                {
                    int error = Marshal.GetLastWin32Error();
                    throw new Win32Exception(error, "Unable to write to standard output (" + NativeMethods.Win32Error.GetText(error) + ").");
                }
                buffer.Clear();

                // read input from the user
                if (!ReadConsole(buffer: buffer,
                     consoleInputHandle: stdin,
                    numberOfCharsToRead: BufferReadSize,
                      numberOfCharsRead: out read,
                               reserved: IntPtr.Zero))
                {
                    int error = Marshal.GetLastWin32Error();
                    throw new Win32Exception(error, "Unable to read from standard input (" + Win32Error.GetText(error) + ").");
                }

                authenticationCode = buffer.ToString(0, (int)read);
                authenticationCode = authenticationCode.Trim(program.NewLineChars);
            }

            return authenticationCode != null;
        }

        public static bool CredentialPrompt(Program program, TargetUri targetUri, out string username, out string password)
        {
            const string TitleMessage = "Please enter your GitHub credentials for ";

            Credential credential;
            if ((credential = program.BasicCredentialPrompt(targetUri, TitleMessage)) != null)
            {
                username = credential.Username;
                password = credential.Password;

                return true;
            }

            username = null;
            password = null;

            return false;
        }
    }
}
