using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System;
using System.Threading.Tasks;
using Debug = System.Diagnostics.Debug;

namespace Microsoft.TeamFoundation.Git.Helpers.Authentication
{
    public sealed class VsoMsaAuthentation : BaseVsoAuthentication, IVsoMsaAuthentication
    {
        public const string DefaultAuthorityHost = "https://login.windows.net/live.com";
        public const string RedirectUrl = "urn:ietf:wg:oauth:2.0:oob";

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

        public bool PromptLogon(Uri targetUri)
        {
            BaseSecureStore.ValidateTargetUri(targetUri);

            try
            {
                string clientId = this.ClientId.ToString("D");
                string resource = this.Resource;

                AuthenticationContext authCtx = new AuthenticationContext(this.AuthorityHostUrl, IdentityModel.Clients.ActiveDirectory.TokenCache.DefaultShared);
                AuthenticationResult authResult = authCtx.AcquireToken(resource, clientId, new Uri(RedirectUrl), PromptBehavior.Always, UserIdentifier.AnyUser, "domain_hint=live.com&display=popup");

                this.StoreRefreshToken(targetUri, authResult);

                return Task.Run(async () => { return await this.GeneratePersonalAccessToken(targetUri, authResult); }).Result;
            }
            catch (AdalException exception)
            {
                Debug.Write(exception);
            }

            return false;
        }

        public override async Task<bool> RefreshCredentials(Uri targetUri)
        {
            BaseSecureStore.ValidateTargetUri(targetUri);

            try
            {
                string clientId = this.ClientId.ToString("D");
                string resource = this.Resource;

                Token refreshToken = null;
                if (this.AdaRefreshTokenStore.ReadToken(targetUri, out refreshToken))
                {
                    AuthenticationContext authCtx = new AuthenticationContext(this.AuthorityHostUrl, IdentityModel.Clients.ActiveDirectory.TokenCache.DefaultShared);
                    AuthenticationResult authResult = await authCtx.AcquireTokenByRefreshTokenAsync(refreshToken.Value, clientId, resource);

                    return await this.GeneratePersonalAccessToken(targetUri, authResult);
                }
            }
            catch (Exception exception)
            {
                Debug.WriteLine(exception);
            }

            return false;
        }

        public override bool SetCredentials(Uri targetUri, Credential credentials)
        {
            throw new NotSupportedException();
        }
    }
}
