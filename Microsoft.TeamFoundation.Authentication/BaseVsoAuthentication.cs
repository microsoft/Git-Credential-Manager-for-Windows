using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace Microsoft.TeamFoundation.Authentication
{
    public abstract class BaseVsoAuthentication : BaseAuthentication
    {
        public const string DefaultResource = "499b84ac-1321-427f-aa17-267ca6975798";
        public const string DefaultClientId = "872cd9fa-d31f-45e0-9eab-6e460a02d1f1";
        public const string RedirectUrl = "urn:ietf:wg:oauth:2.0:oob";

        protected const string AdalRefreshPrefx = "ada";

        private BaseVsoAuthentication(VsoTokenScope scope, ITokenStore personalAccessTokenStore)
        {
            if (scope == null)
                throw new ArgumentNullException("scope", "The `scope` parameter is null or invalid.");
            if (personalAccessTokenStore == null)
                throw new ArgumentNullException("personalAccessTokenStore", "The `personalAccessTokenStore` paramter is null or invalid.");

            AdalTrace.TraceSource.Switch.Level = SourceLevels.Off;
            AdalTrace.LegacyTraceSwitch.Level = TraceLevel.Off;

            this.ClientId = DefaultClientId;
            this.Resource = DefaultResource;
            this.TokenScope = scope;
            this.PersonalAccessTokenStore = personalAccessTokenStore;
            this.AdaRefreshTokenStore = new SecretStore(AdalRefreshPrefx);
            this.VsoAuthority = new VsoAzureAuthority();
        }
        /// <summary>
        /// Invoked by a derived classes implementation.  allows custom back-ends to be used
        /// </summary>
        /// <param name="credentialPrefix"></param>
        /// <param name="tokenScope"></param>
        /// <param name="adaRefreshTokenStore"></param>
        /// <param name="personalAccessTokenStore"></param>
        protected BaseVsoAuthentication(
            VsoTokenScope tokenScope,
            ITokenStore personalAccessTokenStore,
            ITokenStore adaRefreshTokenStore = null)
            : this(tokenScope, personalAccessTokenStore)
        {
            this.AdaRefreshTokenStore = adaRefreshTokenStore ?? this.AdaRefreshTokenStore;
            this.VsoAdalTokenCache = new VsoAdalTokenCache();
            this.VsoIdeTokenCache = new TokenRegistry();
        }
        internal BaseVsoAuthentication(
            ITokenStore personalAccessTokenStore,
            ITokenStore adaRefreshTokenStore,
            ITokenStore vsoIdeTokenCache,
            IVsoAuthority vsoAuthority)
            : this(VsoTokenScope.ProfileRead, personalAccessTokenStore)
        {
            Debug.Assert(adaRefreshTokenStore != null, "The adaRefreshTokenStore paramter is null or invalid.");
            Debug.Assert(vsoIdeTokenCache != null, "The vsoIdeTokenCache paramter is null or invalid.");
            Debug.Assert(vsoAuthority != null, "The vsoAuthority paramter is null or invalid.");

            this.AdaRefreshTokenStore = adaRefreshTokenStore;
            this.VsoIdeTokenCache = vsoIdeTokenCache;
            this.VsoAuthority = vsoAuthority;
            this.VsoAdalTokenCache = IdentityModel.Clients.ActiveDirectory.TokenCache.DefaultShared;
        }

        public readonly string ClientId;
        public readonly string Resource;
        public readonly VsoTokenScope TokenScope;

        protected readonly IdentityModel.Clients.ActiveDirectory.TokenCache VsoAdalTokenCache;
        protected readonly ITokenStore VsoIdeTokenCache;

        internal ITokenStore PersonalAccessTokenStore { get; set; }
        internal ITokenStore AdaRefreshTokenStore { get; set; }
        internal IVsoAuthority VsoAuthority { get; set; }

        public override void DeleteCredentials(Uri targetUri)
        {
            BaseSecureStore.ValidateTargetUri(targetUri);

            Trace.WriteLine("BaseVsoAuthentication::DeleteCredentials");

            Token credentials = null;
            Token token = null;
            if (this.PersonalAccessTokenStore.ReadToken(targetUri, out credentials))
            {
                this.PersonalAccessTokenStore.DeleteToken(targetUri);
            }
            else if (this.AdaRefreshTokenStore.ReadToken(targetUri, out token))
            {
                this.AdaRefreshTokenStore.DeleteToken(targetUri);
            }
        }

        public override bool GetCredentials(Uri targetUri, out Credential credentials)
        {
            BaseSecureStore.ValidateTargetUri(targetUri);

            Trace.WriteLine("BaseVsoAuthentication::GetCredentials");

            Token personalAccessToken;
            if (this.PersonalAccessTokenStore.ReadToken(targetUri, out personalAccessToken))
            {
                Trace.WriteLine("   successfully retrieved stored credentials, updating credential cache");
            }

            if (personalAccessToken != null)
            {
                credentials = new Credential(String.Empty, personalAccessToken.Value);
                return true;
            }
            else
            {
                credentials = null;
                return false;
            }
        }

        public async Task<bool> RefreshCredentials(Uri targetUri, bool requireCompactToken)
        {
            BaseSecureStore.ValidateTargetUri(targetUri);

            Trace.WriteLine("BaseVsoAuthentication::RefreshCredentials");

            try
            {
                Token refreshToken = null;
                TokenPair tokens = null;

                // attempt to read from the local store
                if (this.AdaRefreshTokenStore.ReadToken(targetUri, out refreshToken))
                {
                    if ((tokens = await this.VsoAuthority.AcquireTokenByRefreshTokenAsync(targetUri, this.ClientId, this.Resource, refreshToken)) != null)
                    {
                        Trace.WriteLine("   Azure token found in primary cache.");

                        return await this.GeneratePersonalAccessToken(targetUri, tokens.AccessToken, requireCompactToken);
                    }
                }

                // attempt to utilize any fedauth tokens captured by the IDE
                if (this.VsoIdeTokenCache.ReadToken(targetUri, out refreshToken))
                {
                    Trace.WriteLine("   federated auth token found in IDE cache.");

                    return await this.GeneratePersonalAccessToken(targetUri, refreshToken, requireCompactToken);
                }

                // attempt to utlize any azure auth tokens cached by the IDE
                foreach (var item in this.VsoAdalTokenCache.ReadItems())
                {
                    tokens = new TokenPair(item.AccessToken, item.RefreshToken);

                    if (item.ExpiresOn > DateTimeOffset.UtcNow
                        && (await this.VsoAuthority.ValidateToken(targetUri, tokens.AccessToken)
                            || ((tokens = await this.VsoAuthority.AcquireTokenByRefreshTokenAsync(targetUri, this.ClientId, this.Resource, tokens.RefeshToken)) != null
                                && await this.VsoAuthority.ValidateToken(targetUri, tokens.AccessToken))))
                    {
                        Trace.WriteLine("   Azure token found in IDE cache.");

                        return await this.GeneratePersonalAccessToken(targetUri, tokens.AccessToken, requireCompactToken);
                    }
                    else
                    {
                        this.VsoAdalTokenCache.DeleteItem(item);
                    }
                }
            }
            catch (Exception exception)
            {
                Debug.WriteLine(exception);
            }

            Trace.WriteLine("   failed to refresh credentials.");
            return false;
        }

        public async Task<bool> ValidateCredentials(Uri targetUri, Credential credentials)
        {
            Trace.WriteLine("BaseVsoAuthentication::ValidateCredentials");

            return await this.VsoAuthority.ValidateCredentials(targetUri, credentials);
        }

        protected async Task<bool> GeneratePersonalAccessToken(Uri targetUri, Token accessToken, bool requestCompactToken)
        {
            Debug.Assert(targetUri != null, "The targetUri parameter is null");
            Debug.Assert(accessToken != null, "The accessToken parameter is null");

            Trace.WriteLine("BaseVsoAuthentication::GeneratePersonalAccessToken");

            Token personalAccessToken;
            if ((personalAccessToken = await this.VsoAuthority.GeneratePersonalAccessToken(targetUri, accessToken, TokenScope, requestCompactToken)) != null)
            {
                this.PersonalAccessTokenStore.WriteToken(targetUri, personalAccessToken);
            }

            return personalAccessToken != null;
        }

        protected void StoreRefreshToken(Uri targetUri, Token refreshToken)
        {
            Debug.Assert(targetUri != null, "The targetUri parameter is null");
            Debug.Assert(refreshToken != null, "The refreshToken parameter is null");

            Trace.WriteLine("BaseVsoAuthentication::StoreRefreshToken");

            this.AdaRefreshTokenStore.WriteToken(targetUri, refreshToken);
        }
    }
}
