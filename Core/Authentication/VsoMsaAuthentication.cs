using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Debug = System.Diagnostics.Debug;

namespace Microsoft.TeamFoundation.Git.Helpers.Authentication
{
    public sealed class VsoMsaAuthentication : BaseVsoAuthentication, IVsoMsaAuthentication
    {
        public const string DefaultAuthorityHost = "https://login.microsoftonline.com/live.com";

        public VsoMsaAuthentication(
            string credentialPrefix,
            VsoTokenScope scope,
            string resource = null,
            string clientId = null)
            : base(credentialPrefix, scope, resource, clientId)
        {
            this.LiveAuthority = new AzureAuthority(DefaultAuthorityHost);
        }
        /// <summary>
        /// Test constructor which allows for using fake credential stores
        /// </summary>
        /// <param name="personalAccessTokenStore"></param>
        /// <param name="adaRefreshTokenStore"></param>
        /// <param name="liveAuthority"></param>
        /// <param name="vsoAuthority"></param>
        internal VsoMsaAuthentication(
            ITokenStore personalAccessTokenStore,
            ITokenStore personalAccessTokenCache,
            ITokenStore adaRefreshTokenStore,
            ILiveAuthority liveAuthority,
            IVsoAuthority vsoAuthority)
            : base(personalAccessTokenStore,
                   personalAccessTokenCache,
                   adaRefreshTokenStore,
                   vsoAuthority)
        {
            this.LiveAuthority = liveAuthority;
        }

        internal ILiveAuthority LiveAuthority { get; set; }

        public bool InteractiveLogon(Uri targetUri, bool requireCompactToken)
        {
            const string QueryParameterDomainHints = "domain_hint=live.com&display=popup";

            BaseSecureStore.ValidateTargetUri(targetUri);

            try
            {
                Tokens tokens;
                if ((tokens = this.LiveAuthority.AcquireToken(this.ClientId, this.Resource, new Uri(RedirectUrl), QueryParameterDomainHints)) != null)
                {
                    this.StoreRefreshToken(targetUri, tokens.RefeshToken);

                    return Task.Run(async () => { return await this.GeneratePersonalAccessToken(targetUri, tokens.AccessToken, requireCompactToken); }).Result;
                }
            }
            catch (AdalException exception)
            {
                Debug.Write(exception);
            }

            return false;
        }

        public override bool SetCredentials(Uri targetUri, Credential credentials)
        {
            // does nothing with VSO AAD backed accounts
            return false;
        }
    }
}
