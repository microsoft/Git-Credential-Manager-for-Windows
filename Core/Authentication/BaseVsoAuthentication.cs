using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace Microsoft.TeamFoundation.Git.Helpers.Authentication
{
    public abstract class BaseVsoAuthentication : BaseAuthentication
    {
        public const string DefaultResource = "499b84ac-1321-427f-aa17-267ca6975798";
        public const string DefaultClientId = "872cd9fa-d31f-45e0-9eab-6e460a02d1f1";
        public const string RedirectUrl = "urn:ietf:wg:oauth:2.0:oob";

        protected const string AdalRefreshPrefx = "adal-refresh";

        private BaseVsoAuthentication()
        {
            AdalTrace.TraceSource.Switch.Level = SourceLevels.Off;
            AdalTrace.LegacyTraceSwitch.Level = TraceLevel.Off;

            this.ClientId = DefaultClientId;
            this.Resource = DefaultResource;
            this.TokenScope = VsoTokenScope.ProfileRead;
            this.AdaRefreshTokenStore = new TokenStore(AdalRefreshPrefx);
            this.VsoAuthority = new AzureAuthority();
        }
        protected BaseVsoAuthentication(string credentialPrefix, VsoTokenScope tokenScope, string resource, string clientId)
            : this()
        {
            this.PersonalAccessTokenCache = new TokenStore(credentialPrefix);
            this.PersonalAccessTokenStore = new TokenStore(credentialPrefix);
            this.ClientId = clientId ?? this.ClientId;
            this.Resource = resource ?? this.Resource;
            this.TokenScope = tokenScope ?? this.TokenScope;
            this.VsoAdalTokenCache = new VsoAdalTokenCache();
            this.VsoIdeTokenCache = new TokenRegistry();
        }
        internal BaseVsoAuthentication(
            ITokenStore personalAccessTokenStore,
            ITokenStore personalAccessTokenCache,
            ITokenStore adaRefreshTokenStore,
            ITokenStore vsoIdeTokenCache,
            IAadAuthority vsoAuthority)
            : this()
        {
            this.PersonalAccessTokenStore = personalAccessTokenStore;
            this.PersonalAccessTokenCache = personalAccessTokenCache;
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
        internal ITokenStore PersonalAccessTokenCache { get; set; }

        internal IAadAuthority VsoAuthority { get; set; }

        public override void DeleteCredentials(Uri targetUri)
        {
            BaseSecureStore.ValidateTargetUri(targetUri);

            Token credentials = null;
            Token token = null;
            if (this.PersonalAccessTokenStore.ReadToken(targetUri, out credentials))
            {
                this.PersonalAccessTokenCache.DeleteToken(targetUri);
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

            Trace.TraceInformation("Attempting to retrieve cached credentials");

            Token personalAccessToken;
            // check the in-memory cache first
            if (!this.PersonalAccessTokenCache.ReadToken(targetUri, out personalAccessToken))
            {
                Trace.TraceInformation("Unable to retrieve cached credentials, attempting stored credentials retrieval.");

                // fall-back to the on disk cache
                if (this.PersonalAccessTokenStore.ReadToken(targetUri, out personalAccessToken))
                {
                    Trace.TraceInformation("Successfully retrieved stored credentials, updating credential cache");

                    // update the in-memory cache for faster future look-ups
                    this.PersonalAccessTokenCache.WriteToken(targetUri, personalAccessToken);
                }
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

            Trace.TraceInformation("BaseVsoAuthentication::RefreshCredentials");

            try
            {
                Token refreshToken = null;
                Tokens tokens = null;

                // attempt to read from the local store
                if (this.AdaRefreshTokenStore.ReadToken(targetUri, out refreshToken))
                {
                    if ((tokens = await this.VsoAuthority.AcquireTokenByRefreshTokenAsync(targetUri, this.ClientId, this.Resource, refreshToken)) != null)
                    {
                        Trace.TraceInformation("Azure token found in primary cache.");

                        return await this.GeneratePersonalAccessToken(targetUri, tokens.AccessToken, requireCompactToken);
                    }
                }

                // attempt to utilize any fedauth tokens captured by the IDE
                if (this.VsoIdeTokenCache.ReadToken(targetUri, out refreshToken))
                {
                    Trace.TraceInformation("Federated auth token found in IDE cache.");

                    return await this.GeneratePersonalAccessToken(targetUri, refreshToken, requireCompactToken);
                }

                // attempt to utlize any azure auth tokens cached by the IDE
                foreach (var item in this.VsoAdalTokenCache.ReadItems())
                {
                    tokens = new Tokens(item.AccessToken, item.RefreshToken);

                    if (item.ExpiresOn > DateTimeOffset.UtcNow
                        && (await this.VsoAuthority.ValidateToken(targetUri, tokens.AccessToken)
                            || ((tokens = await this.VsoAuthority.AcquireTokenByRefreshTokenAsync(targetUri, this.ClientId, this.Resource, tokens.RefeshToken)) != null
                                && await this.VsoAuthority.ValidateToken(targetUri, tokens.AccessToken))))
                    {
                        Trace.TraceInformation("Azure token found in IDE cache.");

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

            return false;
        }

        public async Task<bool> ValidateCredentials(Uri targetUri, Credential credentials)
        {
            return await this.VsoAuthority.ValidateCredentials(targetUri, credentials);
        }

        protected async Task<bool> GeneratePersonalAccessToken(Uri targetUri, Token accessToken, bool requestCompactToken)
        {
            Debug.Assert(targetUri != null, "The targetUri parameter is null");
            Debug.Assert(accessToken != null, "The accessToken parameter is null");

            Token personalAccessToken;
            if ((personalAccessToken = await this.VsoAuthority.GeneratePersonalAccessToken(targetUri, accessToken, TokenScope, requestCompactToken)) != null)
            {
                this.PersonalAccessTokenCache.WriteToken(targetUri, personalAccessToken);
                this.PersonalAccessTokenStore.WriteToken(targetUri, personalAccessToken);
            }

            return personalAccessToken != null;
        }

        protected void StoreRefreshToken(Uri targetUri, Token refreshToken)
        {
            Debug.Assert(targetUri != null, "The targetUri parameter is null");
            Debug.Assert(refreshToken != null, "The refreshToken parameter is null");

            this.AdaRefreshTokenStore.WriteToken(targetUri, refreshToken);
        }
    }
}
