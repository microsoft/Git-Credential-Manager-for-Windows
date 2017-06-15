using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Alm.Authentication;
using Microsoft.Alm.Git;
using Microsoft.Win32.SafeHandles;
using Bitbucket = Atlassian.Bitbucket.Authentication;
using Github = GitHub.Authentication;

namespace Microsoft.Alm.Cli
{
    partial class Program
    {
        internal static class GitHubFunctions
        {
            public static bool AuthCodePrompt(TargetUri targetUri, Github.GitHubAuthenticationResultType resultType, string username, out string authenticationCode)
            {
                // ReadConsole 32768 fail, 32767 ok @linquize [https://github.com/Microsoft/Git-Credential-Manager-for-Windows/commit/a62b9a19f430d038dcd85a610d97e5f763980f85]
                const int BufferReadSize = 16 * 1024;

                Debug.Assert(targetUri != null);

                StringBuilder buffer = new StringBuilder(BufferReadSize);
                uint read = 0;
                uint written = 0;

                authenticationCode = null;

                NativeMethods.FileAccess fileAccessFlags = NativeMethods.FileAccess.GenericRead | NativeMethods.FileAccess.GenericWrite;
                NativeMethods.FileAttributes fileAttributes = NativeMethods.FileAttributes.Normal;
                NativeMethods.FileCreationDisposition fileCreationDisposition = NativeMethods.FileCreationDisposition.OpenExisting;
                NativeMethods.FileShare fileShareFlags = NativeMethods.FileShare.Read | NativeMethods.FileShare.Write;

                using (SafeFileHandle stdout = NativeMethods.CreateFile(NativeMethods.ConsoleOutName, fileAccessFlags, fileShareFlags, IntPtr.Zero, fileCreationDisposition, fileAttributes, IntPtr.Zero))
                using (SafeFileHandle stdin = NativeMethods.CreateFile(NativeMethods.ConsoleInName, fileAccessFlags, fileShareFlags, IntPtr.Zero, fileCreationDisposition, fileAttributes, IntPtr.Zero))
                {
                    string type = resultType == Github.GitHubAuthenticationResultType.TwoFactorApp
                        ? "app"
                        : "sms";

                    Git.Trace.WriteLine($"2fa type = '{type}'.");

                    buffer.AppendLine()
                          .Append("authcode (")
                          .Append(type)
                          .Append("): ");

                    if (!NativeMethods.WriteConsole(stdout, buffer, (uint)buffer.Length, out written, IntPtr.Zero))
                    {
                        int error = Marshal.GetLastWin32Error();
                        throw new Win32Exception(error, "Unable to write to standard output (" + NativeMethods.Win32Error.GetText(error) + ").");
                    }
                    buffer.Clear();

                    // read input from the user
                    if (!NativeMethods.ReadConsole(stdin, buffer, BufferReadSize, out read, IntPtr.Zero))
                    {
                        int error = Marshal.GetLastWin32Error();
                        throw new Win32Exception(error, "Unable to read from standard input (" + NativeMethods.Win32Error.GetText(error) + ").");
                    }

                    authenticationCode = buffer.ToString(0, (int)read);
                    authenticationCode = authenticationCode.Trim(NewLineChars);
                }

                return authenticationCode != null;
            }

            public static bool CredentialPrompt(TargetUri targetUri, out string username, out string password)
            {
                const string TitleMessage = "Please enter your GitHub credentials for ";

                Credential credential;
                if ((credential = _basicCredentialPrompt(targetUri, TitleMessage)) != null)
                {
                    username = credential.Username;
                    password = credential.Password;

                    return true;
                }

                username = null;
                password = null;

                return false;
            }
        }
    }
}
