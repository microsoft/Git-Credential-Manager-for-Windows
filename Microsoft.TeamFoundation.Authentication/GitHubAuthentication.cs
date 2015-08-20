using System;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;

namespace Microsoft.TeamFoundation.Authentication
{
    public class GithubAuthentication : IGithubAuthentication
    {
        /// <summary>
        /// The maximum wait time for a network request before timing out
        /// </summary>
        public const int RequestTimeout = 15 * 1000; // 15 second limit

        public GithubAuthentication(ICredentialStore personalAccessTokenStore)
        {
            if (personalAccessTokenStore == null)
                throw new ArgumentNullException("personalAccessTokenStore");

            _personalAccessTokenStore = personalAccessTokenStore;
            _githubAuthority = new GithubAuthority();
        }

        private readonly ICredentialStore _personalAccessTokenStore;
        private readonly IGithubAuthority _githubAuthority;

        public void DeleteCredentials(Uri targetUri)
        {
            throw new NotImplementedException();
        }

        public bool GetCredentials(Uri targetUri, out Credential credentials)
        {
            throw new NotImplementedException();
        }

        public bool InteractiveLogon(Uri targetUri, GithubTokenScope scope, out Credential credentials)
        {
            const int BufferReadSize = 32 * 1024;

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
                      .Append("/")
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

                Token token;

                GithubAuthenticationResult result;
                if ((result = _githubAuthority.AcquireToken(targetUri, username, password, null, scope, out token)) && token == null)
                {
                    buffer.Clear()
                          .AppendLine()
                          .Append("authentication code: ");
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

                    if (result = _githubAuthority.AcquireToken(targetUri, username, password, authenticationCode, scope, out token))
                    {
                        credentials = (Credential)token;
                    }
                }
            }

            credentials = null;
            return false;
        }

        public bool SetCredentials(Uri targetUri, Credential credentials)
        {
            throw new NotImplementedException();
        }
    }
}
