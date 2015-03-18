using System;

namespace Microsoft.TeamFoundation.Git.Helpers.Authentication
{
    public interface IAuthentication
    {
        void DeleteCredentials(Uri targetUri);
        bool GetCredentials(Uri targetUri, out Credentials credentials);
        bool SetCredentials(Uri targetUri, Credentials credentials);
    }
}
