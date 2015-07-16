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
        /// <summary>
        /// Prompts the user for credentials (username & password) and stores the results
        /// </summary>
        /// <param name="targetUri">The URI of the target for which credentials prompted</param>
        /// <param name="credentials">The credentials from the store; null if failure</param>
        /// <returns>True if success; false if failure</returns>
        [SuppressMessage("Microsoft.Interoperability", "CA1404:CallGetLastErrorImmediatelyAfterPInvoke")]
        public bool PromptUserCredentials(Uri targetUri, out Credential credentials)
        {
            ValidateTargetUri(targetUri);

            credentials = null;

            IntPtr inputBufferPtr = IntPtr.Zero;
            uint inputBufferSize = 0;
            IntPtr outputBufferPtr = IntPtr.Zero;
            uint outputBufferSize = 0;

            try
            {
                string targetName = this.GetTargetName(targetUri);

                // read credentials from the store if they exist
                if ((credentials = this.ReadCredentials(targetName)) != null)
                {
                    // pack them into the input buffer for display back to the user
                    if (!NativeMethods.CredPackAuthenticationBuffer(NativeMethods.CRED_PACK.GENERIC_CREDENTIALS, credentials.Username, String.Empty, inputBufferPtr, ref inputBufferSize))
                    {
                        int errorcode = Marshal.GetLastWin32Error();
                        throw new Exception("Error creating prompt", new Win32Exception(errorcode));
                    }
                }
                // prompt the user for credentials using the secure win32 UX
                // the results will be packed into the output buffer
                uint authPackage = 0;
                bool save = false;
                NativeMethods.CREDUI_INFO credInfo = new NativeMethods.CREDUI_INFO()
                {
                    pszCaptionText = "Git Credentials",
                    pszMessageText = "Enter your credentials for: " + targetUri.AbsoluteUri
                };
                credInfo.cbSize = Marshal.SizeOf(credInfo);

                NativeMethods.CREDUI_ERROR result;
                if ((result = NativeMethods.CredUIPromptForWindowsCredentials(ref credInfo, 0, ref authPackage, inputBufferPtr, inputBufferSize, out outputBufferPtr, out outputBufferSize, ref save, NativeMethods.CREDUIWIN.GENERIC)) == NativeMethods.CREDUI_ERROR.NO_ERROR)
                {
                    // allocate string buffers for the results of unpacking the output buffer
                    StringBuilder domainBuffer = new StringBuilder(255);
                    uint domainSize = 255;
                    StringBuilder passwordBuffer = new StringBuilder(255);
                    uint passwordSize = 255;
                    StringBuilder usernameBuffer = new StringBuilder(255);
                    uint usernameSize = 255;
                    // unpack the output buffer into the string buffers
                    if (!NativeMethods.CredUnPackAuthenticationBuffer(0, outputBufferPtr, outputBufferSize, usernameBuffer, ref usernameSize, domainBuffer, ref domainSize, passwordBuffer, ref passwordSize))
                    {
                        int errorcode = Marshal.GetLastWin32Error();
                        throw new Exception("Error reading credentials from prompt", new Win32Exception(errorcode));
                    }
                    // convert the username and password buffers into a credential object
                    credentials = new Credential(usernameBuffer.ToString(), passwordBuffer.ToString());
                    Credential.Validate(credentials);
                    // write the credentials to the credential store
                    this.WriteCredential(targetName, credentials);
                }
                else
                {
                    if (result == NativeMethods.CREDUI_ERROR.ERROR_CANCELLED)
                    {
                        // this is a non-error
                        Trace.TraceWarning("credential collection cancelled");
                        credentials = null;
                    }
                    else
                    {
                        int errorCode = Marshal.GetLastWin32Error();
                        throw new Exception("Credential UX error", new Win32Exception(errorCode));
                    }
                }
            }
            catch (Exception exception)
            {
                Debug.Write(exception);
            }
            finally
            {
                if (inputBufferPtr != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(inputBufferPtr);
                }
                if (outputBufferPtr != IntPtr.Zero)
                {
                    Marshal.FreeHGlobal(outputBufferPtr);
                }
            }

            return credentials != null;
        }

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

        protected Credential ReadCredentials(string targetName)
        {
            Credential credentials = null;
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
            Token token = null;
            IntPtr credPtr = IntPtr.Zero;

            try
            {
                if (NativeMethods.CredRead(targetName, NativeMethods.CRED_TYPE.GENERIC, 0, out credPtr))
                {
                    NativeMethods.CREDENTIAL credStruct = (NativeMethods.CREDENTIAL)Marshal.PtrToStructure(credPtr, typeof(NativeMethods.CREDENTIAL));
                    int size = (int)credStruct.CredentialBlobSize;
                    byte[] bytes = new byte[size];
                    Marshal.Copy(credStruct.CredentialBlob, bytes, 0, size);

                    Token.Deserialize(bytes, out token);
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

        protected void WriteToken(string targetName, Token token, string name)
        {
            byte[] bytes = null;
            if (Token.Serialize(token, out bytes))
            {
                NativeMethods.CREDENTIAL credential = new NativeMethods.CREDENTIAL()
                {
                    Type = NativeMethods.CRED_TYPE.GENERIC,
                    TargetName = targetName,
                    CredentialBlobSize = (uint)bytes.Length,
                    Persist = NativeMethods.CRED_PERSIST.LOCAL_MACHINE,
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

        internal static void ValidateTargetUri(Uri targetUri)
        {
            if (targetUri == null)
                throw new ArgumentNullException("targetUri");
            if (!targetUri.IsAbsoluteUri)
                throw new ArgumentException("The target URI must be an absolute URI", "targetUri");
        }
    }
}
