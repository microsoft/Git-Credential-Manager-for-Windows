using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;

namespace Microsoft.Alm.Authentication
{
    /// <summary>
    /// Facilitates GitHub simple and two-factor authentication
    /// </summary>
    public class GithubAuthentication : BaseAuthentication, IGithubAuthentication
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="tokenScope"></param>
        /// <param name="personalAccessTokenStore"></param>
        public GithubAuthentication(GithubTokenScope tokenScope, ICredentialStore personalAccessTokenStore)
        {
            if (tokenScope == null)
                throw new ArgumentNullException("tokenScope", "The parameter `tokenScope` is null or invalid.");
            if (personalAccessTokenStore == null)
                throw new ArgumentNullException("personalAccessTokenStore", "The parameter `personalAccessTokenStore` is null or invalid.");

            TokenScope = tokenScope;

            PersonalAccessTokenStore = personalAccessTokenStore;
            GithubAuthority = new GithubAuthority();
        }

        /// <summary>
        /// The desired scope of the authentication token to be requested.
        /// </summary>
        public readonly GithubTokenScope TokenScope;

        internal IGithubAuthority GithubAuthority { get; set; }
        internal ICredentialStore PersonalAccessTokenStore { get; set; }

        /// <summary>
        /// Deletes a <see cref="Credential"/> from the storage used by the authentication object.
        /// </summary>
        /// <param name="targetUri">
        /// The uniform resource indicator used to uniquely identitfy the credentials.
        /// </param>
        public override void DeleteCredentials(Uri targetUri)
        {
            BaseSecureStore.ValidateTargetUri(targetUri);

            Trace.WriteLine("GithubAuthentication::DeleteCredentials");

            Credential credentials = null;
            if (this.PersonalAccessTokenStore.ReadCredentials(targetUri, out credentials))
            {
                this.PersonalAccessTokenStore.DeleteCredentials(targetUri);
                Trace.WriteLine("   credentials deleted");
            }
        }

        /// <summary>
        /// Gets a configured authentication object for 'github.com'.
        /// </summary>
        /// <param name="targetUri">The uniform resource indicator of the resource which requires 
        /// authentication.</param>
        /// <param name="tokenScope">The desired scope of any personal access tokens aqcuired.</param>
        /// <param name="personalAccessTokenStore">A secure secret store for any personal access 
        /// tokens acquired.</param>
        /// <param name="authentication">(out) The authenitcation object if successful.</param>
        /// <returns>True if success; otherwise false.</returns>
        public static bool GetAuthentication(
            Uri targetUri,
            GithubTokenScope tokenScope,
            ICredentialStore personalAccessTokenStore,
            out BaseAuthentication authentication)
        {
            const string GitHubBaseUrlHost = "github.com";

            BaseSecureStore.ValidateTargetUri(targetUri);
            if (personalAccessTokenStore == null)
                throw new ArgumentNullException("personalAccessTokenStore", "The `personalAccessTokenStore` is null or invalid.");

            Trace.WriteLine("GithubAuthentication::GetAuthentication");

            if (targetUri.DnsSafeHost.EndsWith(GitHubBaseUrlHost, StringComparison.OrdinalIgnoreCase))
            {
                authentication = new GithubAuthentication(tokenScope, personalAccessTokenStore);
                Trace.WriteLine("   authentication for GitHub created");
            }
            else
            {
                authentication = null;
                Trace.WriteLine("   not github.com, authentication creation aborted");
            }

            return authentication != null;
        }

        /// <summary>
        /// Gets a <see cref="Credential"/> from the storage used by the authentication object.
        /// </summary>
        /// <param name="targetUri">
        /// The uniform resource indicator used to uniquely identitfy the credentials.
        /// </param>
        /// <param name="credentials">
        /// (out) A <see cref="Credential"/> object from the authentication object, 
        /// authority or storage; otherwise `null`, if successful.
        /// </param>
        /// <returns>True if successful; otherwise false.</returns>
        public override bool GetCredentials(Uri targetUri, out Credential credentials)
        {
            BaseSecureStore.ValidateTargetUri(targetUri);

            Trace.WriteLine("GithubAuthentication::GetCredentials");

            if (this.PersonalAccessTokenStore.ReadCredentials(targetUri, out credentials))
            {
                Trace.WriteLine("   successfully retrieved stored credentials, updating credential cache");
            }

            return credentials != null;
        }

        /// <summary>
        /// <para></para>
        /// <para>Tokens acquired are stored in the secure secret store provided during 
        /// initialization.</para>
        /// </summary>
        /// <param name="targetUri">The unique identifier for the resource for which access is to 
        /// be acquired.</param>
        /// <param name="credentials">(out) Credentials when acquision is successful; null otherwise.</param>
        /// <returns>True if success; otherwise false.</returns>
        public bool InteractiveLogon(Uri targetUri, out Credential credentials)
        {
            // ReadConsole 32768 fail, 32767 ok 
            // @linquize [https://github.com/Microsoft/Git-Credential-Manager-for-Windows/commit/a62b9a19f430d038dcd85a610d97e5f763980f85]
            const int BufferReadSize = 32 * 1024 - 7;

            StringBuilder buffer = new StringBuilder(BufferReadSize);
            uint read = 0;
            uint written = 0;

            NativeMethods.FileAccess fileAccessFlags = NativeMethods.FileAccess.GenericRead | NativeMethods.FileAccess.GenericWrite;
            NativeMethods.FileAttributes fileAttributes = NativeMethods.FileAttributes.Normal;
            NativeMethods.FileCreationDisposition fileCreationDisposition = NativeMethods.FileCreationDisposition.OpenExisting;
            NativeMethods.FileShare fileShareFlags = NativeMethods.FileShare.Read | NativeMethods.FileShare.Write;

            using (SafeFileHandle stdout = NativeMethods.CreateFile("CONOUT$", fileAccessFlags, fileShareFlags, IntPtr.Zero, fileCreationDisposition, fileAttributes, IntPtr.Zero))
            using (SafeFileHandle stdin = NativeMethods.CreateFile("CONIN$", fileAccessFlags, fileShareFlags, IntPtr.Zero, fileCreationDisposition, fileAttributes, IntPtr.Zero))
            {
                // read the current console mode
                NativeMethods.ConsoleMode consoleMode;
                if (!NativeMethods.GetConsoleMode(stdin, out consoleMode))
                {
                    int error = Marshal.GetLastWin32Error();
                    throw new Win32Exception(error, "Unable to determine console mode (" + error + ").");
                }

                // instruct the user as to what they are expected to do
                buffer.Append("Please enter your GitHub credentials for ")
                      .Append(targetUri.Scheme)
                      .Append("://")
                      .Append(targetUri.DnsSafeHost)
                      .Append(targetUri.PathAndQuery)
                      .AppendLine();
                if (!NativeMethods.WriteConsole(stdout, buffer, (uint)buffer.Length, out written, IntPtr.Zero))
                {
                    int error = Marshal.GetLastWin32Error();
                    throw new Win32Exception(error, "Unable to write to standard output (" + error + ").");
                }

                // clear the buffer for the next operation
                buffer.Clear();

                // prompt the user for the username wanted
                buffer.Append("username: ");
                if (!NativeMethods.WriteConsole(stdout, buffer, (uint)buffer.Length, out written, IntPtr.Zero))
                {
                    int error = Marshal.GetLastWin32Error();
                    throw new Win32Exception(error, "Unable to write to standard output (" + error + ").");
                }

                // clear the buffer for the next operation
                buffer.Clear();

                // read input from the user
                if (!NativeMethods.ReadConsole(stdin, buffer, BufferReadSize, out read, IntPtr.Zero))
                {
                    int error = Marshal.GetLastWin32Error();
                    throw new Win32Exception(error, "Unable to read from standard input (" + error + ").");
                }

                // record input from the user into local storage, stripping any eol chars
                string username = buffer.ToString(0, (int)read);
                username = username.Trim(Environment.NewLine.ToCharArray());

                // clear the buffer for the next operation
                buffer.Clear();

                // set the console mode to current without echo input
                NativeMethods.ConsoleMode consoleMode2 = consoleMode ^ NativeMethods.ConsoleMode.EchoInput;

                if (!NativeMethods.SetConsoleMode(stdin, consoleMode2))
                {
                    int error = Marshal.GetLastWin32Error();
                    throw new Win32Exception(error, "Unable to set console mode (" + error + ").");
                }

                // prompt the user for password
                buffer.Append("password: ");
                if (!NativeMethods.WriteConsole(stdout, buffer, (uint)buffer.Length, out written, IntPtr.Zero))
                {
                    int error = Marshal.GetLastWin32Error();
                    throw new Win32Exception(error, "Unable to write to standard output (" + error + ").");
                }

                // clear the buffer for the next operation
                buffer.Clear();

                // read input from the user
                if (!NativeMethods.ReadConsole(stdin, buffer, BufferReadSize, out read, IntPtr.Zero))
                {
                    int error = Marshal.GetLastWin32Error();
                    throw new Win32Exception(error, "Unable to read from standard input (" + error + ").");
                }

                // record input from the user into local storage, stripping any eol chars
                string password = buffer.ToString(0, (int)read);
                password = password.Trim(Environment.NewLine.ToCharArray());

                // clear the buffer for the next operation
                buffer.Clear();

                // restore the console mode to its original value
                if (!NativeMethods.SetConsoleMode(stdin, consoleMode))
                {
                    int error = Marshal.GetLastWin32Error();
                    throw new Win32Exception(error, "Unable to set console mode (" + error + ").");
                }

                GithubAuthenticationResult result;

                if (result = GithubAuthority.AcquireToken(targetUri, username, password, null, this.TokenScope).Result)
                {
                    Trace.WriteLine("   token aquisition succeeded");

                    credentials = (Credential)result.Token;
                    this.PersonalAccessTokenStore.WriteCredentials(targetUri, credentials);

                    return true;
                }
                else if (result == GithubAuthenticationResultType.TwoFactorApp 
                      || result == GithubAuthenticationResultType.TwoFactorSms)
                {
                    buffer.Clear()
                          .AppendLine()
                          .Append("authcode: ");
                    if (!NativeMethods.WriteConsole(stdout, buffer, (uint)buffer.Length, out written, IntPtr.Zero))
                    {
                        int error = Marshal.GetLastWin32Error();
                        throw new Win32Exception(error, "Unable to write to standard output (" + error + ").");
                    }
                    buffer.Clear();

                    // read input from the user
                    if (!NativeMethods.ReadConsole(stdin, buffer, BufferReadSize, out read, IntPtr.Zero))
                    {
                        int error = Marshal.GetLastWin32Error();
                        throw new Win32Exception(error, "Unable to read from standard input (" + error + ").");
                    }

                    string authenticationCode = buffer.ToString(0, (int)read);
                    authenticationCode = authenticationCode.Trim(Environment.NewLine.ToCharArray());

                    if (result = GithubAuthority.AcquireToken(targetUri, username, password, authenticationCode, this.TokenScope).Result)
                    {
                        Trace.WriteLine("   token aquisition succeeded");

                        credentials = (Credential)result.Token;
                        this.PersonalAccessTokenStore.WriteCredentials(targetUri, credentials);

                        return true;
                    }
                }
            }

            Trace.WriteLine("   interactive logon failed");
            credentials = null;
            return false;
        }

        /// <summary>
        /// <para></para>
        /// <para>Tokens acquired are stored in the secure secret store provided during 
        /// initialization.</para>
        /// </summary>
        /// <param name="targetUri">The unique identifier for the resource for which access is to 
        /// be acquired.</param>
        /// <param name="username">The username of the account for which access is to be acquired.</param>
        /// <param name="password">The password of the account for which access is to be acquired.</param>
        /// <param name="authenticationCode">The two-factor authentication code for use in access acquision.</param>
        /// <returns>True if success; otherwise false.</returns>
        public async Task<bool> NoninteractiveLogonWithCredentials(Uri targetUri, string username, string password, string authenticationCode = null)
        {
            BaseSecureStore.ValidateTargetUri(targetUri);
            if (String.IsNullOrWhiteSpace(username))
                throw new ArgumentNullException("username", "The `username` parameter is null or invalid.");
            if (String.IsNullOrWhiteSpace(password))
                throw new ArgumentNullException("username", "The `password` parameter is null or invalid.");

            Trace.WriteLine("GithubAuthentication::NoninteractiveLogonWithCredentials");

            GithubAuthenticationResult result;
            if (result = await GithubAuthority.AcquireToken(targetUri, username, password, authenticationCode, this.TokenScope))
            {
                Trace.WriteLine("   token aquisition succeeded");

                PersonalAccessTokenStore.WriteCredentials(targetUri, (Credential)result.Token);

                return true;
            }

            Trace.WriteLine("   non-interactive logon failed");
            return false;
        }

        /// <summary>
        /// Sets a <see cref="Credential"/> in the storage used by the authentication object.
        /// </summary>
        /// <param name="targetUri">
        /// The uniform resource indicator used to uniquely identitfy the credentials.
        /// </param>
        /// <param name="credentials">The value to be stored.</param>
        /// <returns>True if successful; otherwise false.</returns>
        public override bool SetCredentials(Uri targetUri, Credential credentials)
        {
            BaseSecureStore.ValidateTargetUri(targetUri);
            Credential.Validate(credentials);

            Trace.WriteLine("GithubAuthentication::SetCredentials");

            PersonalAccessTokenStore.WriteCredentials(targetUri, credentials);

            return true;
        }

        /// <summary>
        /// Validates that a set of credentials grants access to the target resource.
        /// </summary>
        /// <param name="targetUri">The unique identifier for the resource for which credentials 
        /// are being validated against.</param>
        /// <param name="credentials">The credentials to validate.</param>
        /// <returns>True is successful; otherwise false.</returns>
        public async Task<bool> ValidateCredentials(Uri targetUri, Credential credentials)
        {
            BaseSecureStore.ValidateTargetUri(targetUri);
            Credential.Validate(credentials);

            Trace.WriteLine("GithubAuthentication::ValidateCredentials");

            return await GithubAuthority.ValidateCredentials(targetUri, credentials);
        }
    }
}
