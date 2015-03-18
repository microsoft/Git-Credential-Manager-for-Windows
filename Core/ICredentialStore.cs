using System;

namespace Microsoft.TeamFoundation.Git.Helpers.Authentication
{
    public interface ICredentialStore
    {
        void DeleteCredentials(Uri targetUri);
        bool ReadCredentials(Uri targetUri, out Credentials credentials);
        void WriteCredentials(Uri targetUri, Credentials credentials);
    }
}
