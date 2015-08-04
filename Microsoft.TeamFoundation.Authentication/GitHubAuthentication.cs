using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;

namespace Microsoft.TeamFoundation.Authentication
{
    public class GithubAuthentication : IGithubAuthentication
    {
        public GithubAuthentication(ICredentialStore personalAccessTokenStore)
        {
            if (personalAccessTokenStore == null)
                throw new ArgumentNullException("personalAccessTokenStore");

            _personalAccessTokenStore = personalAccessTokenStore;
        }

        private readonly ICredentialStore _personalAccessTokenStore;

        public void DeleteCredentials(Uri targetUri)
        {
            throw new NotImplementedException();
        }

        public bool GetCredentials(Uri targetUri, out Credential credentials)
        {
            throw new NotImplementedException();
        }

        public bool InteractiveLogon(Uri targetUri, out Credential credentials)
        {
            StringBuilder buffer = new StringBuilder();
            uint read = 0;

            SafeFileHandle output = NativeMethods.CreateFile("CONOUT$", NativeMethods.FileAccess.GenericWrite, NativeMethods.FileShare.Write, IntPtr.Zero, NativeMethods.FileCreationDisposition.OpenExisting, NativeMethods.FileAttributes.Normal, IntPtr.Zero);
            buffer.Append("username:");
            NativeMethods.WriteConsole(output, buffer, (uint)buffer.Length, out read, IntPtr.Zero);

            SafeFileHandle input = NativeMethods.CreateFile("CONIN$", NativeMethods.FileAccess.GenericRead, NativeMethods.FileShare.Read, IntPtr.Zero, NativeMethods.FileCreationDisposition.OpenExisting, NativeMethods.FileAttributes.Normal, IntPtr.Zero);
            
            if (NativeMethods.ReadConsole(input, buffer, 128, out read, IntPtr.Zero))
            {
                Console.Error.WriteLine(buffer.ToString(0, (int)read));
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
