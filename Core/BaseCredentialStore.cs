using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace Microsoft.TeamFoundation.Git.Helpers.Authentication
{
    public abstract class BaseCredentialStore
    {
        protected void Delete(string targetName)
        {
            try
            {
                if (!NativeMethods.CredDelete(targetName, NativeMethods.CRED_TYPE.GENERIC, 0))
                {
                    int errorCode = Marshal.GetLastWin32Error();
                    switch (errorCode)
                    {
                        case NativeMethods.CREDENTIAL_ERROR_NOT_FOUND:
                            Trace.TraceWarning("Credentials not found for " + targetName);
                            break;
                        default:
                            throw new Exception("Failed to delete credentials for " + targetName, new Win32Exception(errorCode));
                    }
                }
            }
            catch (Exception exception)
            {
                Debug.WriteLine(exception);
            }
        }

        protected abstract string GetTargetName(Uri targetUri);
        protected Credentials Read(string targetName)
        {
            Credentials credentials = null;
            IntPtr credPtr = IntPtr.Zero;

            try
            {
                if (NativeMethods.CredRead(targetName, NativeMethods.CRED_TYPE.GENERIC, 0, out credPtr))
                {
                    NativeMethods.CREDENTIAL credStruct = (NativeMethods.CREDENTIAL)Marshal.PtrToStructure(credPtr, typeof(NativeMethods.CREDENTIAL));
                    int passwordLength = (int)credStruct.CredentialBlobSize;

                    string password = passwordLength > 0
                                    ? Marshal.PtrToStringUni(credStruct.CredentialBlob, passwordLength / sizeof(char))
                                    : String.Empty;
                    string username = credStruct.UserName;

                    credentials = new Credentials(username, password);
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

        protected void Write(string targetName, Credentials credentials)
        {
            NativeMethods.CREDENTIAL credential = new NativeMethods.CREDENTIAL()
            {
                Type = NativeMethods.CRED_TYPE.GENERIC,
                TargetName = targetName,
                CredentialBlob = Marshal.StringToCoTaskMemUni(credentials.Password),
                CredentialBlobSize = (uint)Encoding.Unicode.GetByteCount(credentials.Password),
                Persist = NativeMethods.CRED_PERSIST.LOCAL_MACHINE,
                AttributeCount = 0,
                UserName = credentials.Username,
            };

            if (!NativeMethods.CredWrite(ref credential, 0))
            {
                int errorCode = Marshal.GetLastWin32Error();
                throw new Exception("Failed to write credentials", new Win32Exception(errorCode));
            }

            Marshal.FreeCoTaskMem(credential.CredentialBlob);
        }

        internal static void ValidateCredentials(Credentials credentials)
        {
            if (credentials == null)
                throw new ArgumentNullException("credentials");
            if (credentials.Password == null)
                throw new ArgumentException("The Password field of the Credentials object cannot be null", "credentials");
            if (String.IsNullOrEmpty(credentials.Username))
                throw new ArgumentException("The Username field of the Credentials object cannot be null or empty", "credentials");
            if (credentials.Username.Length > NativeMethods.CREDENTIAL_USERNAME_MAXLEN)
                throw new ArgumentOutOfRangeException("credentials", "The Username field of the Credentials object cannot be longer than 513 characters");
        }
        internal static void ValidateTargetUri(Uri targetUri)
        {
            if (targetUri == null)
                throw new ArgumentNullException("targetUri");
            if (!targetUri.IsAbsoluteUri)
                throw new ArgumentException("The target URI must be an absolute URI", "targetUri");
        }
    }
}
