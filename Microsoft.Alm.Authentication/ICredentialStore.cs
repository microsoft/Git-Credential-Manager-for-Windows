using System;

namespace Microsoft.Alm.Authentication
{
    public interface ICredentialStore
    {
        void DeleteCredentials(Uri targetUri);
        bool ReadCredentials(Uri targetUri, out Credential credentials);
        void WriteCredentials(Uri targetUri, Credential credentials);
    }
}
