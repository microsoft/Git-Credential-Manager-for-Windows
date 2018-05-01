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
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace Microsoft.Alm.Authentication
{
    public abstract class BaseSecureStore : Base
    {
        public static readonly char[] IllegalCharacters = new[] { ':', ';', '\\', '?', '@', '=', '&', '%', '$' };

        protected BaseSecureStore(RuntimeContext context)
            : base(context)
        { }

        protected bool Delete(string targetName)
        {
            try
            {
                if (!NativeMethods.CredDelete(targetName, NativeMethods.CredentialType.Generic, 0))
                {
                    int error = Marshal.GetLastWin32Error();
                    switch (error)
                    {
                        case NativeMethods.Win32Error.NotFound:
                        case NativeMethods.Win32Error.NoSuchLogonSession:
                            Trace.WriteLine($"credentials not found for '{targetName}'.");
                            break;

                        default:
                            throw new Win32Exception(error, "Failed to delete credentials for " + targetName);
                    }
                }
                else
                {
                    Trace.WriteLine($"credentials for '{targetName}' deleted from store.");
                }
            }
            catch (Exception exception)
            {
                Debug.WriteLine(exception);
                return false;
            }

            return true;
        }

        protected IEnumerable<Secret> EnumerateCredentials(string @namespace)
        {
            string filter = @namespace ?? string.Empty + "*";

            if (NativeMethods.CredEnumerate(filter, 0, out int count, out IntPtr credentialArrayPtr))
            {
                Trace.WriteLine($"{count} credentials enumerated from secret store.");

                try
                {
                    for (int i = 0; i < count; i += 1)
                    {
                        int offset = i * Marshal.SizeOf(typeof(IntPtr));
                        IntPtr credentialPtr = Marshal.ReadIntPtr(credentialArrayPtr, offset);

                        if (credentialPtr != IntPtr.Zero)
                        {
                            NativeMethods.Credential credStruct = Marshal.PtrToStructure<NativeMethods.Credential>(credentialPtr);
                            int passwordLength = (int)credStruct.CredentialBlobSize;

                            string password = passwordLength > 0
                                            ? Marshal.PtrToStringUni(credStruct.CredentialBlob, passwordLength / sizeof(char))
                                            : string.Empty;
                            string username = credStruct.UserName ?? string.Empty;

                            var credentials = new Credential(username, password);

                            yield return credentials;
                        }
                    }
                }
                finally
                {
                    NativeMethods.CredFree(credentialArrayPtr);
                }
            }
            else
            {
                int error = Marshal.GetLastWin32Error();
                if (error != NativeMethods.Win32Error.FileNotFound
                    && error != NativeMethods.Win32Error.NotFound)
                {
                    Debug.Fail("Failed with error code " + error.ToString("X"));
                }
            }
        }

        protected abstract string GetTargetName(TargetUri targetUri);

        protected void PurgeCredentials(string @namespace)
        {
            string filter = @namespace ?? string.Empty + "*";

            if (NativeMethods.CredEnumerate(filter, 0, out int count, out IntPtr credentialArrayPtr))
            {
                try
                {
                    for (int i = 0; i < count; i += 1)
                    {
                        int offset = i * Marshal.SizeOf(typeof(IntPtr));
                        IntPtr credentialPtr = Marshal.ReadIntPtr(credentialArrayPtr, offset);

                        if (credentialPtr != IntPtr.Zero)
                        {
                            NativeMethods.Credential credential = Marshal.PtrToStructure<NativeMethods.Credential>(credentialPtr);

                            if (!NativeMethods.CredDelete(credential.TargetName, credential.Type, 0))
                            {
                                int error = Marshal.GetLastWin32Error();
                                if (error != NativeMethods.Win32Error.FileNotFound)
                                {
                                    Debug.Fail("Failed with error code " + error.ToString("X"));
                                }
                            }
                            else
                            {
                                Trace.WriteLine($"credentials for '{@namespace}' purged from store.");
                            }
                        }
                    }
                }
                finally
                {
                    NativeMethods.CredFree(credentialArrayPtr);
                }
            }
            else
            {
                int error = Marshal.GetLastWin32Error();
                if (error != NativeMethods.Win32Error.FileNotFound
                    && error != NativeMethods.Win32Error.NotFound)
                {
                    Debug.Fail("Failed with error code " + error.ToString("X"));
                }
            }
        }

        protected Credential ReadCredentials(string targetName)
        {
            Credential credentials = null;

            if (NativeMethods.CredRead(targetName, NativeMethods.CredentialType.Generic, 0, out IntPtr credPtr))
            {
                try
                {
                    NativeMethods.Credential credStruct = (NativeMethods.Credential)Marshal.PtrToStructure(credPtr, typeof(NativeMethods.Credential));
                    int passwordLength = (int)credStruct.CredentialBlobSize;

                    string password = passwordLength > 0
                                    ? Marshal.PtrToStringUni(credStruct.CredentialBlob, passwordLength / sizeof(char))
                                    : string.Empty;
                    string username = credStruct.UserName ?? string.Empty;

                    credentials = new Credential(username, password);

                    Trace.WriteLine($"credentials for '{targetName}' read from store.");
                }
                catch (Exception exception)
                {
                    Debug.WriteLine(exception);

                    Trace.WriteLine($"failed to read credentials: {exception.GetType().Name}.");

                    return null;
                }
                finally
                {
                    if (credPtr != IntPtr.Zero)
                    {
                        NativeMethods.CredFree(credPtr);
                    }
                }
            }

            return credentials;
        }

        protected Token ReadToken(string targetName)
        {
            Token token = null;
            IntPtr credPtr = IntPtr.Zero;

            if (NativeMethods.CredRead(targetName, NativeMethods.CredentialType.Generic, 0, out credPtr))
            {
                try
                {
                    NativeMethods.Credential credStruct = (NativeMethods.Credential)Marshal.PtrToStructure(credPtr, typeof(NativeMethods.Credential));
                    if (credStruct.CredentialBlob != null && credStruct.CredentialBlobSize > 0)
                    {
                        int size = (int)credStruct.CredentialBlobSize;
                        var bytes = new byte[size];
                        Marshal.Copy(credStruct.CredentialBlob, bytes, 0, size);

                        if (Token.GetTypeFromFriendlyName(credStruct.UserName, out TokenType type))
                        {
                            Token.Deserialize(Context, bytes, type, out token);
                        }

                        Trace.WriteLine($"token for '{targetName}' read from store.");
                    }
                }
                catch (Exception exception)
                {
                    Debug.WriteLine(exception);

                    Trace.WriteLine($"failed to read credentials: {exception.GetType().Name}.");

                    return null;
                }
                finally
                {
                    if (credPtr != IntPtr.Zero)
                    {
                        NativeMethods.CredFree(credPtr);
                    }
                }
            }

            return token;
        }

        protected bool WriteCredential(string targetName, Credential credentials)
        {
            if (targetName is null)
                throw new ArgumentNullException(nameof(targetName));
            if (credentials is null)
                throw new ArgumentNullException(nameof(credentials));

            NativeMethods.Credential credential = new NativeMethods.Credential()
            {
                Type = NativeMethods.CredentialType.Generic,
                TargetName = targetName,
                CredentialBlob = Marshal.StringToCoTaskMemUni(credentials.Password),
                CredentialBlobSize = (uint)Encoding.Unicode.GetByteCount(credentials.Password),
                Persist = NativeMethods.CredentialPersist.LocalMachine,
                AttributeCount = 0,
                UserName = credentials.Username,
            };
            try
            {
                if (!NativeMethods.CredWrite(ref credential, 0))
                {
                    int error = Marshal.GetLastWin32Error();
                    throw new Win32Exception(error, "Failed to write credentials");
                }

                Trace.WriteLine($"credentials for '{targetName}' written to store.");
            }
            catch (Exception exception)
            {
                Debug.WriteLine(exception);

                Trace.WriteLine($"failed to write credentials: {exception.GetType().Name}.");

                return false;
            }
            finally
            {
                if (credential.CredentialBlob != IntPtr.Zero)
                {
                    Marshal.FreeCoTaskMem(credential.CredentialBlob);
                }
            }

            return true;
        }

        protected bool WriteToken(string targetName, Token token)
        {
            if (targetName is null)
                throw new ArgumentNullException(nameof(targetName));
            if (token is null)
                throw new ArgumentNullException(nameof(token));

            if (Token.Serialize(Context, token, out byte[] bytes))
            {
                string name;
                if (Token.GetFriendlyNameFromType(token.Type, out name))
                {
                    NativeMethods.Credential credential = new NativeMethods.Credential()
                    {
                        Type = NativeMethods.CredentialType.Generic,
                        TargetName = targetName,
                        CredentialBlob = Marshal.AllocCoTaskMem(bytes.Length),
                        CredentialBlobSize = (uint)bytes.Length,
                        Persist = NativeMethods.CredentialPersist.LocalMachine,
                        AttributeCount = 0,
                        UserName = name,
                    };
                    try
                    {
                        Marshal.Copy(bytes, 0, credential.CredentialBlob, bytes.Length);

                        if (!NativeMethods.CredWrite(ref credential, 0))
                        {
                            int error = Marshal.GetLastWin32Error();
                            throw new Win32Exception(error, "Failed to write credentials");
                        }

                        Trace.WriteLine($"token for '{targetName}' written to store.");
                    }
                    catch (Exception exception)
                    {
                        Debug.WriteLine(exception);

                        Trace.WriteLine($"failed to write credentials: {exception.GetType().Name}.");

                        return false;
                    }
                    finally
                    {
                        if (credential.CredentialBlob != IntPtr.Zero)
                        {
                            Marshal.FreeCoTaskMem(credential.CredentialBlob);
                        }
                    }
                }
            }

            return true;
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static void ValidateTargetUri(TargetUri targetUri)
        {
            if (targetUri is null)
                throw new ArgumentNullException(nameof(targetUri));
            if (!targetUri.IsAbsoluteUri)
            {
                var innerException = new UriFormatException("URI is not an absolute.");
                throw new ArgumentException(innerException.Message, nameof(targetUri), innerException);
            }
        }
    }
}
