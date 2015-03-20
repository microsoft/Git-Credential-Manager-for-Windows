using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;

namespace Microsoft.TeamFoundation.Git.Helpers.Authentication
{
    public class CredentialStore : BaseCredentialStore, ICredentialStore
    {
        internal CredentialStore(string prefix)
        {
            Debug.Assert(!String.IsNullOrWhiteSpace(prefix), "The prefix parameter value is invalid");

            _prefix = prefix;
        }

        private readonly string _prefix;

        /// <summary>
        /// Deleted credentials for target URI from the credential store
        /// </summary>
        /// <param name="targetUri">The URI of the target for which credentials are being deleted</param>
        public void DeleteCredentials(Uri targetUri)
        {
            BaseCredentialStore.ValidateTargetUri(targetUri);

            string targetName = this.GetTargetName(targetUri);
            try
            {
                this.Delete(targetName);
            }
            catch (Exception exception)
            {
                Debug.WriteLine(exception);
            }
        }
        /// <summary>
        /// Prompts the user for credentials (username & password) and stores the results
        /// </summary>
        /// <param name="targetUri">The URI of the target for which credentials prompted</param>
        /// <param name="credentials">The credentials from the store; null if failure</param>
        /// <returns>True if success; false if failure</returns>
        [SuppressMessage("Microsoft.Interoperability", "CA1404:CallGetLastErrorImmediatelyAfterPInvoke")]
        public bool PromptUserCredentials(Uri targetUri, out Credentials credentials)
        {
            BaseCredentialStore.ValidateTargetUri(targetUri);

            credentials = null;

            IntPtr inputBufferPtr = IntPtr.Zero;
            uint inputBufferSize = 0;
            IntPtr outputBufferPtr = IntPtr.Zero;
            uint outputBufferSize = 0;            

            try
            {
                // read credentials from the store if they exist
                if (this.ReadCredentials(targetUri, out credentials))
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
                    credentials = new Credentials(usernameBuffer.ToString(), passwordBuffer.ToString());
                    // write the credentials to the credential store
                    this.WriteCredentials(targetUri, credentials);
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
        /// <summary>
        /// Reads credentials for a target URI from the credential store
        /// </summary>
        /// <param name="targetUri">The URI of the target for which credentials are being read</param>
        /// <param name="credentials">The credentials from the store; null if failure</param>
        /// <returns>True if success; false if failure</returns>
        public bool ReadCredentials(Uri targetUri, out Credentials credentials)
        {
            BaseCredentialStore.ValidateTargetUri(targetUri);

            string targetName = this.GetTargetName(targetUri);
            credentials = this.Read(targetName);

            return credentials != null;
        }
        /// <summary>
        /// Writes credentials for a target URI to the credential store
        /// </summary>
        /// <param name="targetUri">The URI of the target for which credentials are being stored</param>
        /// <param name="credentials">The credentials to be stored</param>
        public void WriteCredentials(Uri targetUri, Credentials credentials)
        {
            BaseCredentialStore.ValidateTargetUri(targetUri);
            BaseCredentialStore.ValidateCredentials(credentials);

            string targetName = this.GetTargetName(targetUri);
            this.Write(targetName, credentials);
        }
        /// <summary>
        /// Formats a TargetName string based on the TargetUri base on the format started by git-credential-winstore
        /// </summary>
        /// <param name="targetUri">Uri of the target</param>
        /// <returns>Properly formatted TargetName string</returns>
        protected override string GetTargetName(Uri targetUri)
        {
            // use the format started by git-credential-winstore for maximum compatibility
            // see https://gitcredentialstore.codeplex.com/
            const string PrimaryNameFormat = "{0}:{1}://{2}";

            System.Diagnostics.Debug.Assert(targetUri != null, "The targetUri parameter is null");

            // trim any trailing slashes and/or whitespace for compat with git-credential-winstore
            string trimmedHostUrl = targetUri.Host
                                             .TrimEnd('/', '\\')
                                             .TrimEnd();
            string targetName = String.Format(PrimaryNameFormat, _prefix, targetUri.Scheme, trimmedHostUrl);
            return targetName;
        }
    }
}
