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
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Alm.Authentication;

using Git = Microsoft.Alm.Authentication.Git;

namespace Microsoft.Alm.Cli
{
    internal static class DialogFunctions
    {
        public static Task<Credential> DisplayModal(Program program,
                                                    NativeMethods.CredentialUiInfo credUiInfo,
                                                    NativeMethods.CredentialPackFlags authPackage,
                                                    IntPtr packedAuthBufferPtr,
                                                    uint packedAuthBufferSize,
                                                    IntPtr inBufferPtr,
                                                    int inBufferSize,
                                                    bool saveCredentials,
                                                    NativeMethods.CredentialUiWindowsFlags flags)
        {
            if (program is null)
                throw new ArgumentNullException(nameof(program));

            var trace = program.Context.Trace;

            return Task.Run(() =>
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
                        trace.WriteLine($"credential prompt failed ('{NativeMethods.Win32Error.GetText(error)}').");

                        return null;
                    }

                    // use `StringBuilder` references instead of string so that they can be written to
                    var usernameBuffer = new StringBuilder(512);
                    var domainBuffer = new StringBuilder(256);
                    var passwordBuffer = new StringBuilder(512);
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
                        error = Marshal.GetLastWin32Error();
                        trace.WriteLine($"failed to unpack buffer ('{NativeMethods.Win32Error.GetText(error)}').");

                        return null;
                    }

                    trace.WriteLine("successfully acquired credentials from user.");

                    var username = usernameBuffer.ToString();
                    var password = passwordBuffer.ToString();

                    return new Credential(username, password);
                }
                finally
                {
                    if (packedAuthBufferPtr != IntPtr.Zero)
                    {
                        Marshal.FreeHGlobal(packedAuthBufferPtr);
                    }
                }
            });
        }

        public static Task<Credential> CredentialPrompt(Program program, TargetUri targetUri, string message)
        {
            if (program is null)
                throw new ArgumentNullException(nameof(program));
            if (targetUri is null)
                throw new ArgumentNullException(nameof(targetUri));
            if (message is null)
                throw new ArgumentNullException(nameof(message));

            return Task.Run(() =>
            {
                var credUiInfo = new NativeMethods.CredentialUiInfo
                {
                    BannerArt = IntPtr.Zero,
                    CaptionText = program.Title,
                    MessageText = message,
                    Parent = IntPtr.Zero,
                    Size = Marshal.SizeOf(typeof(NativeMethods.CredentialUiInfo))
                };
                var flags = NativeMethods.CredentialUiWindowsFlags.Generic;
                var authPackage = NativeMethods.CredentialPackFlags.None;
                var packedAuthBufferPtr = IntPtr.Zero;
                var inBufferPtr = IntPtr.Zero;
                uint packedAuthBufferSize = 0;
                bool saveCredentials = false;
                int inBufferSize = 0;

                return program.ModalPromptDisplayDialog(credUiInfo,
                                                        authPackage,
                                                        packedAuthBufferPtr,
                                                        packedAuthBufferSize,
                                                        inBufferPtr,
                                                        inBufferSize,
                                                        saveCredentials,
                                                        flags);
            });
        }

        public static Task<Credential> PasswordPrompt(Program program, TargetUri targetUri, string message, string username)
        {
            if (program is null)
                throw new ArgumentNullException(nameof(program));
            if (targetUri is null)
                throw new ArgumentNullException(nameof(targetUri));
            if (message is null)
                throw new ArgumentNullException(nameof(message));
            if (username is null)
                throw new ArgumentNullException(nameof(username));

            var trace = program.Context.Trace;

            return Task.Run(() =>
            {
                var credUiInfo = new NativeMethods.CredentialUiInfo
                {
                    BannerArt = IntPtr.Zero,
                    CaptionText = program.Title,
                    MessageText = message,
                    Parent = IntPtr.Zero,
                    Size = Marshal.SizeOf(typeof(NativeMethods.CredentialUiInfo))
                };
                var flags = NativeMethods.CredentialUiWindowsFlags.Generic;
                var authPackage = NativeMethods.CredentialPackFlags.None;
                var packedAuthBufferPtr = IntPtr.Zero;
                var inBufferPtr = IntPtr.Zero;
                uint packedAuthBufferSize = 0;
                bool saveCredentials = false;
                int inBufferSize = 0;

                try
                {
                    int error;

                    // Execute with `null` to determine buffer size always returns false when determining
                    // size, only fail if `inBufferSize` looks bad.
                    NativeMethods.CredPackAuthenticationBuffer(flags: authPackage,
                                                               username: username,
                                                               password: string.Empty,
                                                               packedCredentials: IntPtr.Zero,
                                                               packedCredentialsSize: ref inBufferSize);
                    if (inBufferSize <= 0)
                    {
                        error = Marshal.GetLastWin32Error();
                        trace.WriteLine($"unable to determine credential buffer size ('{NativeMethods.Win32Error.GetText(error)}').");

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
                        trace.WriteLine($"unable to write to credential buffer ('{NativeMethods.Win32Error.GetText(error)}').");

                        return null;
                    }

                    return program.ModalPromptDisplayDialog(credUiInfo,
                                                            authPackage,
                                                            packedAuthBufferPtr,
                                                            packedAuthBufferSize,
                                                            inBufferPtr,
                                                            inBufferSize,
                                                            saveCredentials,
                                                            flags);
                }
                finally
                {
                    if (inBufferPtr != IntPtr.Zero)
                    {
                        Marshal.FreeCoTaskMem(inBufferPtr);
                    }
                }
            });
        }
    }
}
