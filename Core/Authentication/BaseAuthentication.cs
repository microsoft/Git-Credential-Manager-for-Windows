using System;

namespace Microsoft.TeamFoundation.Git.Helpers.Authentication
{
    public abstract class BaseAuthentication : IAuthentication
    {
        public abstract void DeleteCredentials(Uri targetUri);

        public abstract bool GetCredentials(Uri targetUri, out Credential credentials);

        public abstract bool SetCredentials(Uri targetUri, Credential credentials);
    }
}
