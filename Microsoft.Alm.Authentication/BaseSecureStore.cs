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

namespace Microsoft.Alm.Authentication
{
    public abstract class BaseSecureStore
    {
        public static readonly char[] IllegalCharacters = new[] { ':', ';', '\\', '?', '@', '=', '&', '%', '$' };

        protected void Delete(string targetName)
        {
            Trace.WriteLine("BaseSecureStore::Delete");

            try
            {
                if (!NativeMethods.CredDelete(targetName, NativeMethods.CredentialType.Generic, 0))
                {
                    int error = Marshal.GetLastWin32Error();
                    switch (error)
                    {
                        case NativeMethods.Win32Error.NotFound:
                        case NativeMethods.Win32Error.NoSuchLogonSession:
                            Trace.WriteLine("   credentials not found for " + targetName);
                            break;

                        default:
                            throw new Win32Exception(error, "Failed to delete credentials for " + targetName);
                    }
                }
            }
            catch (Exception exception)
            {
                Debug.WriteLine(exception);
            }
        }

        protected abstract string GetTargetName(TargetUri targetUri);

        protected void PurgeCredentials(string @namespace)
        {
            string filter = @namespace + "*";
            int count;
            IntPtr credentialArrayPtr;

            if (NativeMethods.CredEnumerate(filter, 0, out count, out credentialArrayPtr))
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

                    }
                }

                NativeMethods.CredFree(credentialArrayPtr);
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
            Trace.WriteLine("BaseSecureStore::ReadCredentials");

            Credential credentials = null;
            IntPtr credPtr = IntPtr.Zero;

            try
            {
                if (NativeMethods.CredRead(targetName, NativeMethods.CredentialType.Generic, 0, out credPtr))
                {
                    NativeMethods.Credential credStruct = (NativeMethods.Credential)Marshal.PtrToStructure(credPtr, typeof(NativeMethods.Credential));
                    int passwordLength = (int)credStruct.CredentialBlobSize;

                    string password = passwordLength > 0
                                    ? Marshal.PtrToStringUni(credStruct.CredentialBlob, passwordLength / sizeof(char))
                                    : String.Empty;
                    string username = credStruct.UserName ?? String.Empty;

                    credentials = new Credential(username, password);
                }
            }
            finally
            {
                if (credPtr != IntPtr.Zero)
                {
                    NativeMethods.CredFree(credPtr);
                }
            }

            return credentials;
        }

        protected Token ReadToken(string targetName)
        {
            Trace.WriteLine("BaseSecureStore::ReadToken");

            Token token = null;
            IntPtr credPtr = IntPtr.Zero;

            try
            {
                if (NativeMethods.CredRead(targetName, NativeMethods.CredentialType.Generic, 0, out credPtr))
                {
                    NativeMethods.Credential credStruct = (NativeMethods.Credential)Marshal.PtrToStructure(credPtr, typeof(NativeMethods.Credential));
                    if (credStruct.CredentialBlob != null && credStruct.CredentialBlobSize > 0)
                    {
                        int size = (int)credStruct.CredentialBlobSize;
                        byte[] bytes = new byte[size];
                        Marshal.Copy(credStruct.CredentialBlob, bytes, 0, size);

                        TokenType type;
                        if (Token.GetTypeFromFriendlyName(credStruct.UserName, out type))
                        {
                            Token.Deserialize(bytes, type, out token);
                        }
                    }
                }
            }
            finally
            {
                if (credPtr != IntPtr.Zero)
                {
                    NativeMethods.CredFree(credPtr);
                }
            }

            return token;
        }

        protected void WriteCredential(string targetName, Credential credentials)
        {
            Trace.WriteLine("BaseSecureStore::WriteCredential");

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
                    int errorCode = Marshal.GetLastWin32Error();
                    throw new Exception("Failed to write credentials", new Win32Exception(errorCode));
                }
            }
            finally
            {
                if (credential.CredentialBlob != IntPtr.Zero)
                {
                    Marshal.FreeCoTaskMem(credential.CredentialBlob);
                }
            }
        }

        protected void WriteToken(string targetName, Token token)
        {
            Trace.WriteLine("BaseSecureStore::WriteToken");

            byte[] bytes = null;
            if (Token.Serialize(token, out bytes))
            {
                string name;
                if (Token.GetFriendlyNameFromType(token.Type, out name))
                {
                    NativeMethods.Credential credential = new NativeMethods.Credential()
                    {
                        Type = NativeMethods.CredentialType.Generic,
                        TargetName = targetName,
                        CredentialBlobSize = (uint)bytes.Length,
                        Persist = NativeMethods.CredentialPersist.LocalMachine,
                        AttributeCount = 0,
                        UserName = name,
                    };
                    try
                    {
                        credential.CredentialBlob = Marshal.AllocCoTaskMem(bytes.Length);
                        Marshal.Copy(bytes, 0, credential.CredentialBlob, bytes.Length);

                        if (!NativeMethods.CredWrite(ref credential, 0))
                        {
                            int errorCode = Marshal.GetLastWin32Error();
                            throw new Exception("Failed to write credentials", new Win32Exception(errorCode));
                        }
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
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static void ValidateCredential(Credential credentials)
        {
            if (ReferenceEquals(credentials, null))
                throw new ArgumentNullException(nameof(credentials));
            if (credentials.Password.Length > NativeMethods.Credential.PasswordMaxLength)
                throw new ArgumentOutOfRangeException(nameof(credentials.Password));
            if (credentials.Username.Length > NativeMethods.Credential.UsernameMaxLength)
                throw new ArgumentOutOfRangeException(nameof(credentials.Username));
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        public static void ValidateTargetUri(TargetUri targetUri)
        {
            if (ReferenceEquals(targetUri, null))
                throw new ArgumentNullException(nameof(targetUri));
            if (!targetUri.IsAbsoluteUri || !targetUri.ActualUri.IsAbsoluteUri)
                throw new ArgumentException(nameof(targetUri.IsAbsoluteUri));
        }

        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
        internal static void ValidateToken(Token token)
        {
            if (ReferenceEquals(token, null))
                throw new ArgumentNullException(nameof(token));
            if (String.IsNullOrEmpty(token.Value))
                throw new ArgumentException(nameof(token.Value));
        }
    }
}
