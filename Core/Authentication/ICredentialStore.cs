using System;

namespace Microsoft.TeamFoundation.Git.Helpers.Authentication
{
    public interface ICredentialStore
    {
        void DeleteCredentials(Uri targetUri);
        bool ReadCredentials(Uri targetUri, out Credential credentials);
        bool PromptUserCredentials(Uri targetUri, out Credential credentials);
        void WriteCredentials(Uri targetUri, Credential credentials);
    }
}
