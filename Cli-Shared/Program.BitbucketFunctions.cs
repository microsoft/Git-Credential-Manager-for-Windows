using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Alm.Authentication;
using Bitbucket = Atlassian.Bitbucket.Authentication;

namespace Microsoft.Alm.Cli
{
    partial class Program
    {
        internal static class BitbucketFunctions
        {
            public static bool CredentialPrompt(string titleMessage, TargetUri targetUri, out string username, out string password)
            {
                Credential credential;
                if ((credential = Program.BasicCredentialPrompt(targetUri, titleMessage)) != null)
                {
                    username = credential.Username;
                    password = credential.Password;

                    return true;
                }

                username = null;
                password = null;

                return false;
            }

            public static bool OAuthPrompt(string title, TargetUri targetUri, Bitbucket.AuthenticationResultType resultType, string username)
            {
                const int BufferReadSize = 16 * 1024;

                Debug.Assert(targetUri != null);

                var buffer = new StringBuilder(BufferReadSize);
                uint read = 0;
                uint written = 0;

                string accessToken = null;

                var fileAccessFlags = NativeMethods.FileAccess.GenericRead | NativeMethods.FileAccess.GenericWrite;
                var fileAttributes = NativeMethods.FileAttributes.Normal;
                var fileCreationDisposition = NativeMethods.FileCreationDisposition.OpenExisting;
                var fileShareFlags = NativeMethods.FileShare.Read | NativeMethods.FileShare.Write;

                using (var stdout = NativeMethods.CreateFile(NativeMethods.ConsoleOutName, fileAccessFlags, fileShareFlags,
                                                             IntPtr.Zero, fileCreationDisposition, fileAttributes, IntPtr.Zero))
                {
                    using (var stdin = NativeMethods.CreateFile(NativeMethods.ConsoleInName, fileAccessFlags, fileShareFlags,
                                                                IntPtr.Zero, fileCreationDisposition, fileAttributes, IntPtr.Zero))
                    {
                        buffer.AppendLine()
                              .Append(title)
                              .Append(" OAuth Access Token: ");

                        if (!NativeMethods.WriteConsole(stdout, buffer, (uint)buffer.Length, out written, IntPtr.Zero))
                        {
                            var error = Marshal.GetLastWin32Error();
                            throw new Win32Exception(error, "Unable to write to standard output (" + NativeMethods.Win32Error.GetText(error) + ").");
                        }
                        buffer.Clear();

                        // read input from the user
                        if (!NativeMethods.ReadConsole(stdin, buffer, BufferReadSize, out read, IntPtr.Zero))
                        {
                            var error = Marshal.GetLastWin32Error();
                            throw new Win32Exception(error, "Unable to read from standard input (" + NativeMethods.Win32Error.GetText(error) + ").");
                        }

                        accessToken = buffer.ToString(0, (int)read);
                        accessToken = accessToken.Trim(NewLineChars);
                    }
                }
                return accessToken != null;
            }
        }
    }
}
