using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace Microsoft.TeamFoundation.Git.Helpers.Authentication
{
    public sealed class VsoMsaAuthentication : BaseVsoAuthentication, IVsoMsaAuthentication
    {
        public const string DefaultAuthorityHost = AzureAuthority.AuthorityHostUrlBase + "/live.com";

        public VsoMsaAuthentication(
            VsoTokenScope tokenScope,
            ITokenStore personalAccessTokenStore,
            ITokenStore adaRefreshTokenStore = null)
            : base(tokenScope,
                   personalAccessTokenStore,
                   adaRefreshTokenStore)
        {
            this.VsoAuthority = new VsoAzureAuthority(DefaultAuthorityHost);
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
            ITokenStore adaRefreshTokenStore,
            ITokenStore vsoIdeTokenCache,
            IVsoAuthority liveAuthority)
            : base(personalAccessTokenStore,
                   adaRefreshTokenStore,
                   vsoIdeTokenCache,
                   liveAuthority)
        { }

        /// <summary>
        /// Opens an interactive logon prompt to aquire aquire an authentication token from the
        /// Microsoft Live authentication and identity service.
        /// </summary>
        /// <param name="targetUri">
        /// The uniform resource indicator of the resource access tokens are being requested for.
        /// </param>
        /// <param name="requireCompactToken">
        /// True if a compact access token is required; false if a standard token is acceptable.
        /// </param>
        /// <returns>True if successfull; otherwise false.</returns>
        public bool InteractiveLogon(Uri targetUri, bool requireCompactToken)
        {
            const string QueryParameterDomainHints = "domain_hint=live.com&display=popup";

            Trace.WriteLine("VsoMsaAuthentication::InteractiveLogon");

            BaseSecureStore.ValidateTargetUri(targetUri);

            try
            {
                TokenPair tokens;
                if ((tokens = this.VsoAuthority.AcquireToken(targetUri, this.ClientId, this.Resource, new Uri(RedirectUrl), QueryParameterDomainHints)) != null)
                {
                    Trace.WriteLine("   token successfully aquired.");

                    this.StoreRefreshToken(targetUri, tokens.RefeshToken);

                    return Task.Run(async () => { return await this.GeneratePersonalAccessToken(targetUri, tokens.AccessToken, requireCompactToken); }).Result;
                }
            }
            catch (AdalException exception)
            {
                Debug.Write(exception);
            }

            Trace.WriteLine("   failed to aquire token.");
            return false;
        }
        /// <summary>
        /// Sets credentials for future use with this authentication object.
        /// </summary>
        /// <remarks>Not supported.</remarks>
        /// <param name="targetUri">
        /// The uniform resource indicator of the resource access tokens are being set for.
        /// </param>
        /// <param name="credentials">The credentials being set.</param>
        /// <returns>True if successful; false otherwise.</returns>
        public override bool SetCredentials(Uri targetUri, Credential credentials)
        {
            BaseSecureStore.ValidateTargetUri(targetUri);
            Credential.Validate(credentials);

            Trace.WriteLine("VsoMsaAuthentication::SetCredentials");
            Trace.WriteLine("   setting MSA credentials is not supported");

            // does nothing with VSO MSA backed accounts
            return false;
        }
    }
}
