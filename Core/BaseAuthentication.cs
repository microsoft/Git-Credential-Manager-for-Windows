using System;

namespace Microsoft.TeamFoundation.Git.Helpers.Authentication
{
    public abstract class BaseAuthentication : IAuthentication
    {
        protected const string PrimaryCredentialPrefix = "git";

        public abstract void DeleteCredentials(Uri targetUri);

        public abstract bool GetCredentials(Uri targetUri, out Credential credentials);

        public abstract bool SetCredentials(Uri targetUri, Credential credentials);
    }
}
