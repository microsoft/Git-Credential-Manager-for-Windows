using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Microsoft.TeamFoundation.Git.Helpers.Authentication
{
    public abstract class BaseVsoAuthentication : BaseAuthentication
    {
        public const string DefaultResource = "499b84ac-1321-427f-aa17-267ca6975798";
        public const string DefaultClientId = "872cd9fa-d31f-45e0-9eab-6e460a02d1f1";
        public const string RedirectUrl = "urn:ietf:wg:oauth:2.0:oob";

        protected const string TokenPrefix = "adal-refresh";

        protected BaseVsoAuthentication()
        {
            AdalTrace.TraceSource.Switch.Level = SourceLevels.Off;
            AdalTrace.LegacyTraceSwitch.Level = TraceLevel.Off;

            this.ClientId = DefaultClientId;
            this.Resource = DefaultResource;
            this.PersonalAccessTokenStore = new TokenStore(PrimaryCredentialPrefix);
            this.AdaRefreshTokenStore = new TokenStore(TokenPrefix);
            this.PersonalAccessTokenCache = new TokenStore(PrimaryCredentialPrefix);
            this.VsoAuthority = new AzureAuthority();
        }
        protected BaseVsoAuthentication(string resource, string clientId)
            : this()
        {
            this.ClientId = clientId ?? this.ClientId;
            this.Resource = resource ?? this.Resource;
        }
        internal BaseVsoAuthentication(ITokenStore personalAccessTokenStore, ITokenStore personalAccessTokenCache, ITokenStore adaRefreshTokenStore, IVsoAuthority vsoAuthority)
            : this()
        {
            this.PersonalAccessTokenStore = personalAccessTokenStore;
            this.PersonalAccessTokenCache = personalAccessTokenCache;
            this.AdaRefreshTokenStore = adaRefreshTokenStore;
            this.VsoAuthority = vsoAuthority;
        }

        public readonly string ClientId;
        public readonly string Resource;

        internal ITokenStore PersonalAccessTokenStore { get; set; }
        internal ITokenStore AdaRefreshTokenStore { get; set; }
        internal ITokenStore PersonalAccessTokenCache { get; set; }

        internal IVsoAuthority VsoAuthority { get; set; }

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

        public abstract Task<bool> RefreshCredentials(Uri targetUri);

        public async Task<bool> ValidateCredentials(Credential credentials)
        {
            return await this.VsoAuthority.ValidateCredentials(credentials);
        }

        protected async Task<bool> GeneratePersonalAccessToken(Uri targetUri, Token accessToken)
        {
            Debug.Assert(targetUri != null, "The targetUri parameter is null");
            Debug.Assert(accessToken != null, "The accessToken parameter is null");
            Debug.Assert(accessToken.Type == TokenType.Access, "The value of the accessToken parameter is not an access token");

            Token personalAccessToken;
            if ((personalAccessToken = await this.VsoAuthority.GeneratePersonalAccessToken(targetUri, accessToken)) != null)
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
