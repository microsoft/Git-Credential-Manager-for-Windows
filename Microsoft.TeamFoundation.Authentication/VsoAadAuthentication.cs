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
        /// The default authority host for all Azure Directory authentiation
        /// </summary>
        public const string DefaultAuthorityHost = " https://management.core.windows.net/";

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tenantId">
        /// <para>The unique identifier for the responsible Azure tenant.</para>
        /// <para>Use <see cref="BaseVsoAuthentication.GetAuthentication"/>
        /// to detect the tenant identity and create the the authentication object.</para>
        /// </param>
        /// <param name="tokenScope">The scope of all access tokens acquired by the authority.</param>
        /// <param name="personalAccessTokenStore">The secure secret store for storing any personal 
        /// access tokens acquired.</param>
        /// <param name="adaRefreshTokenStore">The secure secret store for storing any Azure tokens 
        /// aqcuired.</param>
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
                string authorityHost = AzureAuthority.GetAuthorityUrl(tenantId);
                this.VsoAuthority = new VsoAzureAuthority(authorityHost);
            }
        }

        /// <summary>
        /// Test constructor which allows for using fake credential stores
        /// </summary>
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

        /// <summary>
        /// <para>Creates an interactive logon session, using ADAL secure browser GUI, which 
        /// enables users to authenticate with the Azure tenant and acquire the necissary access 
        /// tokens to exchange for a VSO personal access token.</para>
        /// <para>Tokens acquired are stored in the secure secret stores provided during 
        /// initialization.</para>
        /// </summary>
        /// <param name="targetUri">The unique identifier for the resource for which access is to 
        /// be acquired.</param>
        /// <param name="requestCompactToken">
        /// <para>Requests a compact format personal access token; otherwise requests a standard 
        /// personal access token.</para>
        /// <para>Compact tokens are necissary for clients which have restrictions on the size of 
        /// the basic authenitcation header which they can create (example: Git).</para>
        /// </param>
        /// <returns>True if a authentication and pesonal access token acquisition was successful; otherwise false.</returns>
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
            catch (AdalException)
            {
                Trace.WriteLine("   token aquisition failed.");
            }

            Trace.WriteLine("   interactive logon failed");
            return false;
        }

        /// <summary>
        /// <para>Uses credentials to authenticate with the Azure tenant and acquire the necissary 
        /// access tokens to exchange for a VSO personal access token.</para>
        /// <para>Tokens acquired are stored in the secure secret stores provided during 
        /// initialization.</para>
        /// </summary>
        /// <param name="targetUri">The unique identifier for the resource for which access is to 
        /// be acquired.</param>
        /// <param name="credentials">The credentials required to meet the criteria of the Azure 
        /// tenent authentication challenge (i.e. username + password).</param>
        /// <param name="requestCompactToken">
        /// <para>Requests a compact format personal access token; otherwise requests a standard 
        /// personal access token.</para>
        /// <para>Compact tokens are necissary for clients which have restrictions on the size of 
        /// the basic authenitcation header which they can create (example: Git).</para>
        /// </param>
        /// <returns>True if a authentication and pesonal access token acquisition was successful; otherwise false.</returns>
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
            catch (AdalException)
            {
                Trace.WriteLine("   token aquisition failed");
            }

            Trace.WriteLine("   non-interactive logon failed");
            return false;
        }

        /// <summary>
        /// <para>Uses Active Directory Federation Services to authenticate with the Azure tenant 
        /// non-interatively and acquire the necissary access tokens to exchange for a VSO personal 
        /// access token.</para>
        /// <para>Tokens acquired are stored in the secure secret stores provided during 
        /// initialization.</para>
        /// </summary>
        /// <param name="targetUri">The unique identifier for the resource for which access is to 
        /// be acquired.</param>
        /// <param name="requestCompactToken">
        /// <para>Requests a compact format personal access token; otherwise requests a standard 
        /// personal access token.</para>
        /// <para>Compact tokens are necissary for clients which have restrictions on the size of 
        /// the basic authenitcation header which they can create (example: Git).</para>
        /// </param>
        /// <returns>True if a authentication and pesonal access token acquisition was successful; otherwise false.</returns>
        public async Task<bool> NoninteractiveLogon(Uri targetUri, bool requestCompactToken)
        {
            BaseSecureStore.ValidateTargetUri(targetUri);

            Trace.WriteLine("VsoAadAuthentication::NoninteractiveLogon");

            try
            {
                TokenPair tokens;
                if ((tokens = await this.VsoAuthority.AcquireTokenAsync(targetUri, this.ClientId, this.Resource)) != null)
                {
                    Trace.WriteLine("   token aquisition succeeded");

                    this.StoreRefreshToken(targetUri, tokens.RefeshToken);

                    return await this.GeneratePersonalAccessToken(targetUri, tokens.AccessToken, requestCompactToken);
                }
            }
            catch (AdalException)
            {
                Trace.WriteLine("   failed to acquire token from VsoAuthority.");
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
