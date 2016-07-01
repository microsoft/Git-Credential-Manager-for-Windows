using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace Microsoft.Alm.Authentication
{
    /// <summary>
    /// Facilitates Azure Directory authentication.
    /// </summary>
    public sealed class VstsAadAuthentication : BaseVstsAuthentication, IVstsAadAuthentication
    {
        /// <summary>
        ///
        /// </summary>
        /// <param name="tenantId">
        /// <para>The unique identifier for the responsible Azure tenant.</para>
        /// <para>Use <see cref="BaseVstsAuthentication.GetAuthentication"/>
        /// to detect the tenant identity and create the authentication object.</para>
        /// </param>
        /// <param name="tokenScope">The scope of all access tokens acquired by the authority.</param>
        /// <param name="personalAccessTokenStore">The secure secret store for storing any personal
        /// access tokens acquired.</param>
        /// <param name="adaRefreshTokenStore">The secure secret store for storing any Azure tokens
        /// acquired.</param>
        public VstsAadAuthentication(
            Guid tenantId,
            VstsTokenScope tokenScope,
            ICredentialStore personalAccessTokenStore,
            ITokenStore adaRefreshTokenStore = null)
            : base(tokenScope,
                   personalAccessTokenStore,
                   adaRefreshTokenStore)
        {
            if (tenantId == Guid.Empty)
            {
                this.VstsAuthority = new VstsAzureAuthority(AzureAuthority.DefaultAuthorityHostUrl);
            }
            else
            {
                // create an authority host URL in the format of https://login.microsoft.com/12345678-9ABC-DEF0-1234-56789ABCDEF0
                string authorityHost = AzureAuthority.GetAuthorityUrl(tenantId);
                this.VstsAuthority = new VstsAzureAuthority(authorityHost);
            }
        }

        /// <summary>
        /// Test constructor which allows for using fake credential stores
        /// </summary>
        internal VstsAadAuthentication(
            ICredentialStore personalAccessTokenStore,
            ITokenStore adaRefreshTokenStore,
            ITokenStore vstsIdeTokenCache,
            IVstsAuthority vstsAuthority)
            : base(personalAccessTokenStore,
                   adaRefreshTokenStore,
                   vstsIdeTokenCache,
                   vstsAuthority)
        { }

        /// <summary>
        /// <para>Creates an interactive logon session, using ADAL secure browser GUI, which
        /// enables users to authenticate with the Azure tenant and acquire the necessary access
        /// tokens to exchange for a VSTS personal access token.</para>
        /// <para>Tokens acquired are stored in the secure secret stores provided during
        /// initialization.</para>
        /// </summary>
        /// <param name="targetUri">The unique identifier for the resource for which access is to
        /// be acquired.</param>
        /// <param name="requestCompactToken">
        /// <para>Requests a compact format personal access token; otherwise requests a standard
        /// personal access token.</para>
        /// <para>Compact tokens are necessary for clients which have restrictions on the size of
        /// the basic authentication header which they can create (example: Git).</para>
        /// </param>
        /// <returns><see langword="true"/> if a authentication and personal access token acquisition was successful; otherwise <see langword="false"/>.</returns>
        public bool InteractiveLogon(TargetUri targetUri, bool requestCompactToken)
        {
            BaseSecureStore.ValidateTargetUri(targetUri);

            Trace.WriteLine("VstsAadAuthentication::InteractiveLogon");

            try
            {
                TokenPair tokens;
                if ((tokens = this.VstsAuthority.AcquireToken(targetUri, this.ClientId, this.Resource, new Uri(RedirectUrl), null)) != null)
                {
                    Trace.WriteLine("   token acquisition succeeded.");

                    this.StoreRefreshToken(targetUri, tokens.RefeshToken);

                    return this.GeneratePersonalAccessToken(targetUri, tokens.AccessToken, requestCompactToken).Result;
                }
            }
            catch (AdalException)
            {
                Trace.WriteLine("   token acquisition failed.");
            }

            Trace.WriteLine("   interactive logon failed");
            return false;
        }

        /// <summary>
        /// <para>Uses credentials to authenticate with the Azure tenant and acquire the necessary
        /// access tokens to exchange for a VSTS personal access token.</para>
        /// <para>Tokens acquired are stored in the secure secret stores provided during
        /// initialization.</para>
        /// </summary>
        /// <param name="targetUri">The unique identifier for the resource for which access is to
        /// be acquired.</param>
        /// <param name="credentials">The credentials required to meet the criteria of the Azure
        /// tenant authentication challenge (i.e. username + password).</param>
        /// <param name="requestCompactToken">
        /// <para>Requests a compact format personal access token; otherwise requests a standard
        /// personal access token.</para>
        /// <para>Compact tokens are necessary for clients which have restrictions on the size of
        /// the basic authentication header which they can create (example: Git).</para>
        /// </param>
        /// <returns><see langword="true"/> if authentication and personal access token acquisition was successful; otherwise <see langword="false"/>.</returns>
        public async Task<bool> NoninteractiveLogonWithCredentials(TargetUri targetUri, Credential credentials, bool requestCompactToken)
        {
            BaseSecureStore.ValidateTargetUri(targetUri);
            Credential.Validate(credentials);

            Trace.WriteLine("VstsAadAuthentication::NoninteractiveLogonWithCredentials");

            try
            {
                TokenPair tokens;
                if ((tokens = await this.VstsAuthority.AcquireTokenAsync(targetUri, this.ClientId, this.Resource, credentials)) != null)
                {
                    Trace.WriteLine("   token acquisition succeeded");

                    this.StoreRefreshToken(targetUri, tokens.RefeshToken);

                    return await this.GeneratePersonalAccessToken(targetUri, tokens.AccessToken, requestCompactToken);
                }
            }
            catch (AdalException)
            {
                Trace.WriteLine("   token acquisition failed");
            }

            Trace.WriteLine("   non-interactive logon failed");
            return false;
        }

        /// <summary>
        /// <para>Uses Active Directory Federation Services to authenticate with the Azure tenant
        /// non-interactively and acquire the necessary access tokens to exchange for a VSTS personal
        /// access token.</para>
        /// <para>Tokens acquired are stored in the secure secret stores provided during
        /// initialization.</para>
        /// </summary>
        /// <param name="targetUri">The unique identifier for the resource for which access is to
        /// be acquired.</param>
        /// <param name="requestCompactToken">
        /// <para>Requests a compact format personal access token; otherwise requests a standard
        /// personal access token.</para>
        /// <para>Compact tokens are necessary for clients which have restrictions on the size of
        /// the basic authentication header which they can create (example: Git).</para>
        /// </param>
        /// <returns><see langword="true"/> if authentication and personal access token acquisition was successful; otherwise <see langword="false"/>.</returns>
        public async Task<bool> NoninteractiveLogon(TargetUri targetUri, bool requestCompactToken)
        {
            BaseSecureStore.ValidateTargetUri(targetUri);

            Trace.WriteLine("VstsAadAuthentication::NoninteractiveLogon");

            try
            {
                TokenPair tokens;
                if ((tokens = await this.VstsAuthority.AcquireTokenAsync(targetUri, this.ClientId, this.Resource)) != null)
                {
                    Trace.WriteLine("   token acquisition succeeded");

                    this.StoreRefreshToken(targetUri, tokens.RefeshToken);

                    return await this.GeneratePersonalAccessToken(targetUri, tokens.AccessToken, requestCompactToken);
                }
            }
            catch (AdalException)
            {
                Trace.WriteLine("   failed to acquire token from VstsAuthority.");
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
        /// <returns><see langword="true"/> if successful; <see langword="false"/> otherwise.</returns>
        public override bool SetCredentials(TargetUri targetUri, Credential credentials)
        {
            BaseSecureStore.ValidateTargetUri(targetUri);
            Credential.Validate(credentials);

            Trace.WriteLine("VstsMsaAuthentication::SetCredentials");
            Trace.WriteLine("   setting AAD credentials is not supported");

            // does nothing with VSTS AAD backed accounts
            return false;
        }
    }
}
