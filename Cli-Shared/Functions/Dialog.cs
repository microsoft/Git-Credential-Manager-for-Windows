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
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Alm.Authentication;

namespace Microsoft.Alm.Cli
{
    internal static class DialogFunctions
    {
        public static bool DisplayModal(Program program,
                                        ref NativeMethods.CredentialUiInfo credUiInfo,
                                        ref NativeMethods.CredentialPackFlags authPackage,
                                        IntPtr packedAuthBufferPtr,
                                        uint packedAuthBufferSize,
                                        IntPtr inBufferPtr,
                                        int inBufferSize,
                                        bool saveCredentials,
                                        NativeMethods.CredentialUiWindowsFlags flags,
                                        out string username,
                                        out string password)
        {
            int error;

            try
            {
                // open a standard Windows authentication dialog to acquire username + password credentials
                if ((error = NativeMethods.CredUIPromptForWindowsCredentials(credInfo: ref credUiInfo,
                                                                             authError: 0,
                                                                             authPackage: ref authPackage,
                                                                             inAuthBuffer: inBufferPtr,
                                                                             inAuthBufferSize: (uint)inBufferSize,
                                                                             outAuthBuffer: out packedAuthBufferPtr,
                                                                             outAuthBufferSize: out packedAuthBufferSize,
                                                                             saveCredentials: ref saveCredentials,
                                                                             flags: flags)) != NativeMethods.Win32Error.Success)
                {
                    Git.Trace.WriteLine($"credential prompt failed ('{NativeMethods.Win32Error.GetText(error)}').");

                    username = null;
                    password = null;

                    return false;
                }

                // use `StringBuilder` references instead of string so that they can be written to
                StringBuilder usernameBuffer = new StringBuilder(512);
                StringBuilder domainBuffer = new StringBuilder(256);
                StringBuilder passwordBuffer = new StringBuilder(512);
                int usernameLen = usernameBuffer.Capacity;
                int passwordLen = passwordBuffer.Capacity;
                int domainLen = domainBuffer.Capacity;

                // unpack the result into locally useful data
                if (!NativeMethods.CredUnPackAuthenticationBuffer(flags: authPackage,
                                                                  authBuffer: packedAuthBufferPtr,
                                                                  authBufferSize: packedAuthBufferSize,
                                                                  username: usernameBuffer,
                                                                  maxUsernameLen: ref usernameLen,
                                                                  domainName: domainBuffer,
                                                                  maxDomainNameLen: ref domainLen,
                                                                  password: passwordBuffer,
                                                                  maxPasswordLen: ref passwordLen))
                {
                    username = null;
                    password = null;

                    error = Marshal.GetLastWin32Error();
                    Git.Trace.WriteLine($"failed to unpack buffer ('{NativeMethods.Win32Error.GetText(error)}').");

                    return false;
                }

                Git.Trace.WriteLine("successfully acquired credentials from user.");

                username = usernameBuffer.ToString();
                password = passwordBuffer.ToString();

                return true;
            }
            finally
            {
                if (packedAuthBufferPtr != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(packedAuthBufferPtr);
                }
            }
        }

        public static Credential CredentialPrompt(Program program, TargetUri targetUri, string message)
        {
            Debug.Assert(targetUri != null);
            Debug.Assert(message != null);

            NativeMethods.CredentialUiInfo credUiInfo = new NativeMethods.CredentialUiInfo
            {
                BannerArt = IntPtr.Zero,
                CaptionText = program.Title,
                MessageText = message,
                Parent = IntPtr.Zero,
                Size = Marshal.SizeOf(typeof(NativeMethods.CredentialUiInfo))
            };
            NativeMethods.CredentialUiWindowsFlags flags = NativeMethods.CredentialUiWindowsFlags.Generic;
            NativeMethods.CredentialPackFlags authPackage = NativeMethods.CredentialPackFlags.None;
            IntPtr packedAuthBufferPtr = IntPtr.Zero;
            IntPtr inBufferPtr = IntPtr.Zero;
            uint packedAuthBufferSize = 0;
            bool saveCredentials = false;
            int inBufferSize = 0;
            string username;
            string password;

            if (program.ModalPromptDisplayDialog(ref credUiInfo,
                                                 ref authPackage,
                                                 packedAuthBufferPtr,
                                                 packedAuthBufferSize,
                                                 inBufferPtr,
                                                 inBufferSize,
                                                 saveCredentials,
                                                 flags,
                                                 out username,
                                                 out password))
            {
                return new Credential(username, password);
            }

            return null;
        }

        public static Credential PasswordPrompt(Program program, TargetUri targetUri, string message, string username)
        {
            Debug.Assert(targetUri != null);
            Debug.Assert(message != null);
            Debug.Assert(username != null);

            NativeMethods.CredentialUiInfo credUiInfo = new NativeMethods.CredentialUiInfo
            {
                BannerArt = IntPtr.Zero,
                CaptionText = program.Title,
                MessageText = message,
                Parent = IntPtr.Zero,
                Size = Marshal.SizeOf(typeof(NativeMethods.CredentialUiInfo))
            };
            NativeMethods.CredentialUiWindowsFlags flags = NativeMethods.CredentialUiWindowsFlags.Generic;
            NativeMethods.CredentialPackFlags authPackage = NativeMethods.CredentialPackFlags.None;
            IntPtr packedAuthBufferPtr = IntPtr.Zero;
            IntPtr inBufferPtr = IntPtr.Zero;
            uint packedAuthBufferSize = 0;
            bool saveCredentials = false;
            int inBufferSize = 0;
            string password;

            try
            {
                int error;

                // execute with `null` to determine buffer size always returns false when determining
                // size, only fail if `inBufferSize` looks bad
                NativeMethods.CredPackAuthenticationBuffer(flags: authPackage,
                                                           username: username,
                                                           password: string.Empty,
                                                           packedCredentials: IntPtr.Zero,
                                                           packedCredentialsSize: ref inBufferSize);
                if (inBufferSize <= 0)
                {
                    error = Marshal.GetLastWin32Error();
                    Git.Trace.WriteLine($"unable to determine credential buffer size ('{NativeMethods.Win32Error.GetText(error)}').");

                    return null;
                }

                inBufferPtr = Marshal.AllocHGlobal(inBufferSize);

                if (!NativeMethods.CredPackAuthenticationBuffer(flags: authPackage,
                                                                username: username,
                                                                password: string.Empty,
                                                                packedCredentials: inBufferPtr,
                                                                packedCredentialsSize: ref inBufferSize))
                {
                    error = Marshal.GetLastWin32Error();
                    Git.Trace.WriteLine($"unable to write to credential buffer ('{NativeMethods.Win32Error.GetText(error)}').");

                    return null;
                }

                if (program.ModalPromptDisplayDialog(ref credUiInfo,
                                                     ref authPackage,
                                                     packedAuthBufferPtr,
                                                     packedAuthBufferSize,
                                                     inBufferPtr,
                                                     inBufferSize,
                                                     saveCredentials,
                                                     flags,
                                                     out username,
                                                     out password))
                {
                    return new Credential(username, password);
                }
            }
            finally
            {
                if (inBufferPtr != IntPtr.Zero)
                {
                    Marshal.FreeCoTaskMem(inBufferPtr);
                }
            }

            return null;
        }
    }
}
