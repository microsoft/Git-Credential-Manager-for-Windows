using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace Microsoft.TeamFoundation.Authentication
{
    /// <summary>
    /// Facilitates Azure Directory authentication.
    /// </summary>
    public sealed class VsoAadAuthentication : BaseVsoAuthentication, IVsoAadAuthentication
    {
        /// <summary>
        /// 
        /// </summary>
        public const string DefaultAuthorityHost = " https://management.core.windows.net/";

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tenantId"></param>
        /// <param name="tokenScope"></param>
        /// <param name="personalAccessTokenStore"></param>
        /// <param name="adaRefreshTokenStore"></param>
        public VsoAadAuthentication(
            Guid tenantId,
            VsoTokenScope tokenScope,
            ICredentialStore personalAccessTokenStore,
            ITokenStore adaRefreshTokenStore = null)
            : base(tokenScope,
                   personalAccessTokenStore,
                   adaRefreshTokenStore)
        {
            if (tenantId == Guid.Empty)
            {
                this.VsoAuthority = new VsoAzureAuthority(DefaultAuthorityHost);
            }
            else
            {
                // create an authority host url in the format of https://login.microsoft.com/12345678-9ABC-DEF0-1234-56789ABCDEF0
                string authorityHost = String.Format("{0}/{1:D}", AzureAuthority.AuthorityHostUrlBase, tenantId);
                this.VsoAuthority = new VsoAzureAuthority(authorityHost);
            }
        }
        /// <summary>
        /// Test constructor which allows for using fake credential stores
        /// </summary>
        /// <param name="personalAccessTokenStore"></param>
        /// <param name="userCredential"></param>
        /// <param name="adaRefreshTokenStore"></param>
        internal VsoAadAuthentication(
            ICredentialStore personalAccessTokenStore,
            ITokenStore adaRefreshTokenStore,
            ITokenStore vsoIdeTokenCache,
            IVsoAuthority vsoAuthority)
            : base(personalAccessTokenStore,
                   adaRefreshTokenStore,
                   vsoIdeTokenCache,
                   vsoAuthority)
        { }

        public bool InteractiveLogon(Uri targetUri, bool requestCompactToken)
        {
            BaseSecureStore.ValidateTargetUri(targetUri);

            Trace.WriteLine("VsoAadAuthentication::InteractiveLogon");

            try
            {
                TokenPair tokens;
                if ((tokens = this.VsoAuthority.AcquireToken(targetUri, this.ClientId, this.Resource, new Uri(RedirectUrl), null)) != null)
                {
                    Trace.WriteLine("   token aqusition succeeded.");

                    this.StoreRefreshToken(targetUri, tokens.RefeshToken);

                    return this.GeneratePersonalAccessToken(targetUri, tokens.AccessToken, requestCompactToken).Result;
                }
            }
            catch (AdalException exception)
            {
                Trace.WriteLine("   token aquisition failed.");
                Debug.Write(exception);
            }

            Trace.WriteLine("   interactive logon failed");
            return false;
        }

        public async Task<bool> NoninteractiveLogonWithCredentials(Uri targetUri, Credential credentials, bool requestCompactToken)
        {
            BaseSecureStore.ValidateTargetUri(targetUri);
            Credential.Validate(credentials);

            Trace.WriteLine("VsoAadAuthentication::NoninteractiveLogonWithCredentials");

            try
            {
                TokenPair tokens;
                if ((tokens = await this.VsoAuthority.AcquireTokenAsync(targetUri, this.ClientId, this.Resource, credentials)) != null)
                {
                    Trace.WriteLine("   token aquisition succeeded");

                    this.StoreRefreshToken(targetUri, tokens.RefeshToken);

                    return await this.GeneratePersonalAccessToken(targetUri, tokens.AccessToken, requestCompactToken);
                }
            }
            catch (AdalException exception)
            {
                Trace.WriteLine("   token aquisition failed");
                Debug.Write(exception);
            }

            Trace.WriteLine("   non-interactive logon failed");
            return false;
        }

        public async Task<bool> NoninteractiveLogon(Uri targetUri, bool requestCompactToken)
        {
            BaseSecureStore.ValidateTargetUri(targetUri);

            Trace.WriteLine("VsoAadAuthentication::NoninteractiveLogon");

            try
            {
                TokenPair tokens;
                if ((tokens = await this.VsoAuthority.AcquireTokenAsync(targetUri, this.ClientId, this.Resource)) != null)
                {
                    this.StoreRefreshToken(targetUri, tokens.RefeshToken);

                    return await this.GeneratePersonalAccessToken(targetUri, tokens.AccessToken, requestCompactToken);
                }
            }
            catch (AdalException exception)
            {
                Trace.WriteLine("   failed to acquire token from VsoAuthority.");
                Debug.WriteLine(exception);
            }

            Trace.WriteLine("   non-interactive logon failed");
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
            Trace.WriteLine("   setting AAD credentials is not supported");

            // does nothing with VSO AAD backed accounts
            return false;
        }
    }
}
