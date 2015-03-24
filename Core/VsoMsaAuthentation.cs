using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System;
using System.Threading.Tasks;
using Debug = System.Diagnostics.Debug;

namespace Microsoft.TeamFoundation.Git.Helpers.Authentication
{
    public class VsoMsaAuthentation : BaseVsoAuthentication, IVsoAuthentication
    {
        public const string DefaultAuthorityHost = "https://login.live.com/";

        public VsoMsaAuthentation()
            : base(DefaultAuthorityHost)
        { }
        public VsoMsaAuthentation(string resource, Guid clientId)
            : base(DefaultAuthorityHost, resource, clientId)
        { }
        /// <summary>
        /// Test constructor which allows for using fake credential stores
        /// </summary>
        /// <param name="personalAccessToken"></param>
        /// <param name="userCredential"></param>
        /// <param name="adaRefresh"></param>
        internal VsoMsaAuthentation(ICredentialStore personalAccessToken, ICredentialStore userCredential, ITokenStore adaRefresh)
            : base(DefaultAuthorityHost, personalAccessToken, userCredential, adaRefresh)
        { }

        public override async Task<bool> InteractiveLogon(Uri targetUri, Credential credentials)
        {
            BaseSecureStore.ValidateTargetUri(targetUri);
            BaseSecureStore.ValidateCredentials(credentials);

            try
            {
                string clientId = this.ClientId.ToString("D");
                string resource = this.Resource;

                UserCredential userCredential = new UserCredential(credentials.Username, credentials.Password);
                AuthenticationContext authCtx = new AuthenticationContext(this.AuthorityHostUrl, IdentityModel.Clients.ActiveDirectory.TokenCache.DefaultShared);
                AuthenticationResult authResult = await authCtx.AcquireTokenAsync(resource, clientId, userCredential);

                this.StoreRefreshToken(targetUri, authResult);
                this.UserCredentialStore.WriteCredentials(targetUri, credentials);

                return await this.GeneratePersonalAccessToken(targetUri, authResult);
            }
            catch (AdalException exception)
            {
                Debug.Write(exception);
            }

            return false;
        }

        public override Task<bool> RefreshCredentials(Uri targetUri)
        {
            throw new NotImplementedException();
        }

        public override bool SetCredentials(Uri targetUri, Credential credentials)
        {
            BaseSecureStore.ValidateTargetUri(targetUri);
            BaseSecureStore.ValidateCredentials(credentials);

            var task = Task.Run<bool>(async () => { return await this.InteractiveLogon(targetUri, credentials); });
            task.Wait();

            if (task.IsFaulted)
                throw task.Exception;

            return task.Result;
        }
    }
}
