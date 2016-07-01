using System;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace Microsoft.Alm.Authentication
{
    /// <summary>
    /// Base functionality for performing authentication operations against Visual Studio Online.
    /// </summary>
    public abstract class BaseVstsAuthentication : BaseAuthentication
    {
        public const string DefaultResource = "499b84ac-1321-427f-aa17-267ca6975798";
        public const string DefaultClientId = "872cd9fa-d31f-45e0-9eab-6e460a02d1f1";
        public const string RedirectUrl = "urn:ietf:wg:oauth:2.0:oob";

        protected const string AdalRefreshPrefix = "ada";

        private BaseVstsAuthentication(VstsTokenScope tokenScope, ICredentialStore personalAccessTokenStore)
        {
            if (tokenScope == null)
                throw new ArgumentNullException("scope", "The `scope` parameter is null or invalid.");
            if (personalAccessTokenStore == null)
                throw new ArgumentNullException("personalAccessTokenStore", "The `personalAccessTokenStore` parameter is null or invalid.");

            AdalTrace.TraceSource.Switch.Level = SourceLevels.Off;
            AdalTrace.LegacyTraceSwitch.Level = TraceLevel.Off;

            // attempt to purge any cached ada tokens.
            SecurityPurgeAdaTokens(new SecretStore(AdalRefreshPrefix));

            this.ClientId = DefaultClientId;
            this.Resource = DefaultResource;
            this.TokenScope = tokenScope;
            this.PersonalAccessTokenStore = personalAccessTokenStore;
            this.AdaRefreshTokenStore = new SecretCache(AdalRefreshPrefix);
            this.VstsAuthority = new VstsAzureAuthority();
        }
        /// <summary>
        /// Invoked by a derived classes implementation. Allows custom back-end implementations to be used.
        /// </summary>
        /// <param name="tokenScope">The desired scope of the acquired personal access token(s).</param>
        /// <param name="personalAccessTokenStore">The secret store for acquired personal access token(s).</param>
        /// <param name="adaRefreshTokenStore">The secret store for acquired Azure refresh token(s).</param>
        protected BaseVstsAuthentication(
            VstsTokenScope tokenScope,
            ICredentialStore personalAccessTokenStore,
            ITokenStore adaRefreshTokenStore = null)
            : this(tokenScope, personalAccessTokenStore)
        {
            this.AdaRefreshTokenStore = adaRefreshTokenStore ?? this.AdaRefreshTokenStore;
            this.VstsAdalTokenCache = new VstsAdalTokenCache();
            this.VstsIdeTokenCache = new TokenRegistry();
        }
        internal BaseVstsAuthentication(
            ICredentialStore personalAccessTokenStore,
            ITokenStore adaRefreshTokenStore,
            ITokenStore vstsIdeTokenCache,
            IVstsAuthority vstsAuthority)
            : this(VstsTokenScope.ProfileRead, personalAccessTokenStore)
        {
            Debug.Assert(adaRefreshTokenStore != null, "The adaRefreshTokenStore parameter is null or invalid.");
            Debug.Assert(vstsIdeTokenCache != null, "The vstsIdeTokenCache parameter is null or invalid.");
            Debug.Assert(vstsAuthority != null, "The vstsAuthority parameter is null or invalid.");

            this.AdaRefreshTokenStore = adaRefreshTokenStore;
            this.VstsIdeTokenCache = vstsIdeTokenCache;
            this.VstsAuthority = vstsAuthority;
            this.VstsAdalTokenCache = TokenCache.DefaultShared;
        }

        /// <summary>
        /// The application client identity by which access will be requested.
        /// </summary>
        public readonly string ClientId;
        /// <summary>
        /// The Azure resource for which access will be requested.
        /// </summary>
        public readonly string Resource;
        /// <summary>
        /// The desired scope of the authentication token to be requested.
        /// </summary>
        public readonly VstsTokenScope TokenScope;

        internal readonly TokenCache VstsAdalTokenCache;
        internal readonly ITokenStore VstsIdeTokenCache;

        internal ICredentialStore PersonalAccessTokenStore { get; set; }
        internal ITokenStore AdaRefreshTokenStore { get; set; }
        internal IVstsAuthority VstsAuthority { get; set; }
        internal Guid TenantId { get; set; }

        /// <summary>
        /// Deletes a set of stored credentials by their target resource.
        /// </summary>
        /// <param name="targetUri">The 'key' by which to identify credentials.</param>
        public override void DeleteCredentials(TargetUri targetUri)
        {
            BaseSecureStore.ValidateTargetUri(targetUri);

            Trace.WriteLine("BaseVstsAuthentication::DeleteCredentials");

            Credential credentials = null;
            Token token = null;
            if (this.PersonalAccessTokenStore.ReadCredentials(targetUri, out credentials))
            {
                this.PersonalAccessTokenStore.DeleteCredentials(targetUri);
            }
            else if (this.AdaRefreshTokenStore.ReadToken(targetUri, out token))
            {
                this.AdaRefreshTokenStore.DeleteToken(targetUri);
            }
        }

        /// <summary>
        /// Attempts to get a set of credentials from storage by their target resource.
        /// </summary>
        /// <param name="targetUri">The 'key' by which to identify credentials.</param>
        /// <param name="credentials">Credentials associated with the URI if successful;
        /// <see langword="null"/> otherwise.</param>
        /// <returns><see langword="true"/> if successful; <see langword="false" /> otherwise.</returns>
        public override bool GetCredentials(TargetUri targetUri, out Credential credentials)
        {
            BaseSecureStore.ValidateTargetUri(targetUri);

            Trace.WriteLine("BaseVstsAuthentication::GetCredentials");

            if (this.PersonalAccessTokenStore.ReadCredentials(targetUri, out credentials))
            {
                Trace.WriteLine("   successfully retrieved stored credentials, updating credential cache");
            }

            return credentials != null;
        }

        /// <summary>
        /// Attempts to generate a new personal access token (credentials) via use of a stored
        /// Azure refresh token, identified by the target resource.
        /// </summary>
        /// <param name="targetUri">The 'key' by which to identify the refresh token.</param>
        /// <param name="requireCompactToken">Generates a compact token if <see langword="true"/>;
        /// generates a self describing token if <see langword="false"/>.</param>
        /// <returns><see langword="true"/> if successful; <see langword="false"/> otherwise.</returns>
        public async Task<bool> RefreshCredentials(TargetUri targetUri, bool requireCompactToken)
        {
            BaseSecureStore.ValidateTargetUri(targetUri);

            Trace.WriteLine("BaseVstsAuthentication::RefreshCredentials");

            try
            {
                TokenPair tokens = null;

                Token refreshToken = null;
                // attempt to read from the local store
                if (this.AdaRefreshTokenStore.ReadToken(targetUri, out refreshToken))
                {
                    if ((tokens = await this.VstsAuthority.AcquireTokenByRefreshTokenAsync(targetUri, this.ClientId, this.Resource, refreshToken)) != null)
                    {
                        Trace.WriteLine("   Azure token found in primary cache.");

                        this.TenantId = tokens.AccessToken.TargetIdentity;

                        return await this.GeneratePersonalAccessToken(targetUri, tokens.AccessToken, requireCompactToken);
                    }
                }

                Token federatedAuthToken;
                // attempt to utilize any fedauth tokens captured by the IDE
                if (this.VstsIdeTokenCache.ReadToken(targetUri, out federatedAuthToken))
                {
                    Trace.WriteLine("   federated auth token found in IDE cache.");

                    return await this.GeneratePersonalAccessToken(targetUri, federatedAuthToken, requireCompactToken);
                }
            }
            catch (Exception exception)
            {
                Debug.WriteLine(exception);
            }

            Trace.WriteLine("   failed to refresh credentials.");
            return false;
        }

        /// <summary>
        /// Validates that a set of credentials grants access to the target resource.
        /// </summary>
        /// <param name="targetUri">The target resource to validate against.</param>
        /// <param name="credentials">The credentials to validate.</param>
        /// <returns><see langword="true"/> if successful; <see langword="false"/> otherwise.</returns>
        public async Task<bool> ValidateCredentials(TargetUri targetUri, Credential credentials)
        {
            Trace.WriteLine("BaseVstsAuthentication::ValidateCredentials");

            return await this.VstsAuthority.ValidateCredentials(targetUri, credentials);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="targetUri">The target resource for which to acquire the personal access
        /// token for.</param>
        /// <param name="accessToken">Azure Directory access token with privileges to grant access
        /// to the target resource.</param>
        /// <param name="requestCompactToken">Generates a compact token if <see langword="true"/>;
        /// generates a self describing token if <see langword="false"/>.</param>
        /// <returns><see langword="true"/> if successful; <see langword="false"/> otherwise.</returns>
        protected async Task<bool> GeneratePersonalAccessToken(TargetUri targetUri, Token accessToken, bool requestCompactToken)
        {
            Debug.Assert(targetUri != null, "The targetUri parameter is null");
            Debug.Assert(accessToken != null, "The accessToken parameter is null");

            Trace.WriteLine("BaseVstsAuthentication::GeneratePersonalAccessToken");

            Token personalAccessToken;
            if ((personalAccessToken = await this.VstsAuthority.GeneratePersonalAccessToken(targetUri, accessToken, TokenScope, requestCompactToken)) != null)
            {
                this.PersonalAccessTokenStore.WriteCredentials(targetUri, (Credential)personalAccessToken);
            }

            return personalAccessToken != null;
        }

        /// <summary>
        /// Stores an Azure Directory refresh token.
        /// </summary>
        /// <param name="targetUri">The 'key' by which to identify the token.</param>
        /// <param name="refreshToken">The token to be stored.</param>
        protected void StoreRefreshToken(TargetUri targetUri, Token refreshToken)
        {
            Debug.Assert(targetUri != null, "The targetUri parameter is null");
            Debug.Assert(refreshToken != null, "The refreshToken parameter is null");

            Trace.WriteLine("BaseVstsAuthentication::StoreRefreshToken");

            this.AdaRefreshTokenStore.WriteToken(targetUri, refreshToken);
        }

        /// <summary>
        /// Detects the backing authority of the end-point.
        /// </summary>
        /// <param name="targetUri">The resource which the authority protects.</param>
        /// <param name="tenantId">The identity of the authority tenant; <see cref="Guid.Empty"/> otherwise.</param>
        /// <returns><see langword="true"/> if the authority is Visual Studio Online; <see langword="false"/> otherwise</returns>
        public static bool DetectAuthority(TargetUri targetUri, out Guid tenantId)
        {
            const string VstsBaseUrlHost = "visualstudio.com";
            const string VstsResourceTenantHeader = "X-VSS-ResourceTenant";

            Trace.WriteLine("BaseAuthentication::DetectTenant");

            tenantId = Guid.Empty;

            if (targetUri.ActualUri.Host.EndsWith(VstsBaseUrlHost, StringComparison.OrdinalIgnoreCase))
            {
                Trace.WriteLine("   detected visualstudio.com, checking AAD vs MSA");

                string tenant = null;
                WebResponse response;

                try
                {
                    // build a request that we expect to fail, do not allow redirect to sign in url
                    var request = WebRequest.CreateHttp(targetUri);
                    request.UserAgent = Global.GetUserAgent();
                    request.Method = "HEAD";
                    request.AllowAutoRedirect = false;
                    // get the response from the server
                    response = request.GetResponse();
                }
                catch (WebException exception)
                {
                    response = exception.Response;
                }

                // if the response exists and we have headers, parse them
                if (response != null && response.SupportsHeaders)
                {
                    Trace.WriteLine("   server has responded");

                    // find the VSTS resource tenant entry
                    tenant = response.Headers[VstsResourceTenantHeader];

                    return !String.IsNullOrWhiteSpace(tenant)
                        && Guid.TryParse(tenant, out tenantId);
                }
            }

            Trace.WriteLine("   failed detection");

            // if all else fails, fallback to basic authentication
            return false;
        }

        /// <summary>
        /// Creates a new authentication broker based for the specified resource.
        /// </summary>
        /// <param name="targetUri">The resource for which authentication is being requested.</param>
        /// <param name="scope">The scope of the access being requested.</param>
        /// <param name="personalAccessTokenStore">Storage container for personal access token secrets.</param>
        /// <param name="adaRefreshTokenStore">Storage container for Azure access token secrets.</param>
        /// <param name="authentication">
        /// An implementation of <see cref="BaseAuthentication"/> if one was detected;
        /// <see langword="null"/> otherwise.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if an authority could be determined; <see langword="false"/> otherwise.
        /// </returns>
        public static bool GetAuthentication(
            TargetUri targetUri,
            VstsTokenScope scope,
            ICredentialStore personalAccessTokenStore,
            ITokenStore adaRefreshTokenStore,
            out BaseAuthentication authentication)
        {
            Trace.WriteLine("BaseVstsAuthentication::DetectAuthority");

            Guid tenantId;
            if (DetectAuthority(targetUri, out tenantId))
            {
                // empty Guid is MSA, anything else is AAD
                if (tenantId == Guid.Empty)
                {
                    Trace.WriteLine("   MSA authority detected");
                    authentication = new VstsMsaAuthentication(scope, personalAccessTokenStore, adaRefreshTokenStore);
                }
                else
                {
                    Trace.WriteLine("   AAD authority for tenant '" + tenantId + "' detected");
                    authentication = new VstsAadAuthentication(tenantId, scope, personalAccessTokenStore, adaRefreshTokenStore);
                    (authentication as VstsAadAuthentication).TenantId = tenantId;
                }
            }
            else
            {
                authentication = null;
            }

            return authentication != null;
        }

        /// <summary>
        /// Attempts to enumerate and delete any and all Azure Directory Authentication
        /// Refresh tokens caches by GCM asynchronously.
        /// </summary>
        /// <returns>A <see cref="Task"/> for the async action.</returns>
        private static Task SecurityPurgeAdaTokens(SecretStore adaStore)
        {
            // this can and should be done asynchronously to minimize user impact
            return Task.Run(() =>
            {
                adaStore.PurgeCredentials();
            });
        }
    }
}
