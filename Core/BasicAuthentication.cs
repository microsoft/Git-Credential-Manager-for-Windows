using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.TeamFoundation.Git.Helpers.Authentication
{
    public sealed class BasicAuthentication : BaseAuthentication, IAuthentication
    {
        public BasicAuthentication()
        {
            this.CredentialStore = new CredentialStore(PrimaryCredentialPrefix);
        }
        internal BasicAuthentication(ICredentialStore testStore)
            :this()
        {
            this.CredentialStore = testStore;
        }

        private ICredentialStore CredentialStore;

        public override void DeleteCredentials(Uri targetUri)
        {
            this.CredentialStore.DeleteCredentials(targetUri);
        }

        public override bool GetCredentials(Uri targetUri, out Credential credentials)
        {
            return this.CredentialStore.ReadCredentials(targetUri, out credentials);
        }

        public override bool SetCredentials(Uri targetUri, Credential credentials)
        {
            this.CredentialStore.WriteCredentials(targetUri, credentials);
            return true;
        }
    }
}
