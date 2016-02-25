using System;

namespace Microsoft.Alm.Authentication
{
    public interface ICredentialStore
    {
        void DeleteCredentials(TargetUri targetUri);
        bool ReadCredentials(TargetUri targetUri, out Credential credentials);
        void WriteCredentials(TargetUri targetUri, Credential credentials);
    }
}
