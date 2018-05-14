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
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Alm.Authentication;
using Bitbucket = Atlassian.Bitbucket.Authentication;

namespace Microsoft.Alm.Cli
{
    internal static class BitbucketFunctions
    {
        public static bool CredentialPrompt(Program program, string titleMessage, TargetUri targetUri, out string username, out string password)
        {
            Credential credential;
            if ((credential = program.BasicCredentialPrompt(targetUri, titleMessage)) != null)
            {
                username = credential.Username;
                password = credential.Password;

                return true;
            }

            username = null;
            password = null;

            return false;
        }

        public static bool OAuthPrompt(Program program, string title, TargetUri targetUri, Bitbucket.AuthenticationResultType resultType, string username)
        {
            const int BufferReadSize = 16 * 1024;

            Debug.Assert(targetUri != null);

            var buffer = new StringBuilder(BufferReadSize);
            uint read = 0;
            uint written = 0;

            string accessToken = null;

            var fileAccessFlags = NativeMethods.FileAccess.GenericRead | NativeMethods.FileAccess.GenericWrite;
            var fileAttributes = NativeMethods.FileAttributes.Normal;
            var fileCreationDisposition = NativeMethods.FileCreationDisposition.OpenExisting;
            var fileShareFlags = NativeMethods.FileShare.Read | NativeMethods.FileShare.Write;

            using (var stdout = NativeMethods.CreateFile(NativeMethods.ConsoleOutName, fileAccessFlags, fileShareFlags,
                                                         IntPtr.Zero, fileCreationDisposition, fileAttributes, IntPtr.Zero))
            {
                using (var stdin = NativeMethods.CreateFile(NativeMethods.ConsoleInName, fileAccessFlags, fileShareFlags,
                                                            IntPtr.Zero, fileCreationDisposition, fileAttributes, IntPtr.Zero))
                {
                    buffer.AppendLine()
                          .Append(title)
                          .Append(" OAuth Access Token: ");

                    if (!NativeMethods.WriteConsole(stdout, buffer, (uint)buffer.Length, out written, IntPtr.Zero))
                    {
                        var error = Marshal.GetLastWin32Error();
                        throw new Win32Exception(error, "Unable to write to standard output (" + NativeMethods.Win32Error.GetText(error) + ").");
                    }
                    buffer.Clear();

                    // read input from the user
                    if (!NativeMethods.ReadConsole(stdin, buffer, BufferReadSize, out read, IntPtr.Zero))
                    {
                        var error = Marshal.GetLastWin32Error();
                        throw new Win32Exception(error, "Unable to read from standard input (" + NativeMethods.Win32Error.GetText(error) + ").");
                    }

                    accessToken = buffer.ToString(0, (int)read);
                    accessToken = accessToken.Trim(Program.NewLineChars);
                }
            }
            return accessToken != null;
        }
    }
}
