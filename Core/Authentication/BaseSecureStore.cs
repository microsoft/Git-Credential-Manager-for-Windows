using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;

namespace Microsoft.TeamFoundation.Git.Helpers.Authentication
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

        protected abstract string GetTargetName(Uri targetUri);

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
        internal static void ValidateTargetUri(Uri targetUri)
        {
            if (targetUri == null)
                throw new ArgumentNullException("targetUri");
            if (!targetUri.IsAbsoluteUri)
                throw new ArgumentException("The target URI must be an absolute URI", "targetUri");
        }
    }
}
