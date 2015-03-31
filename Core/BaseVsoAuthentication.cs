using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Microsoft.TeamFoundation.Git.Helpers.Authentication
{
    public abstract class BaseVsoAuthentication : BaseAuthentication
    {
        public const string DefaultResource = "499b84ac-1321-427f-aa17-267ca6975798";
        public const string DefaultClientId = "872cd9fa-d31f-45e0-9eab-6e460a02d1f1";
        public const string RedirectUrl = "urn:ietf:wg:oauth:2.0:oob";

        protected const string SecondaryCredentialPrefix = "alt-git";
        protected const string TokenPrefix = "adal-refresh";

        protected BaseVsoAuthentication()
        {
            AdalTrace.TraceSource.Switch.Level = SourceLevels.Off;
            AdalTrace.LegacyTraceSwitch.Level = TraceLevel.Off;

            this.ClientId = DefaultClientId;
            this.Resource = DefaultResource;
            this.PersonalAccessTokenStore = new CredentialStore(PrimaryCredentialPrefix);
            this.UserCredentialStore = new CredentialStore(SecondaryCredentialPrefix);
            this.AdaRefreshTokenStore = new TokenStore(TokenPrefix);
            this.PersonalAccessTokenCache = new CredentialCache(PrimaryCredentialPrefix);
            this.VsoAuthority = new AzureAuthority();
        }
        protected BaseVsoAuthentication(string resource, string clientId)
            : this()
        {
            this.ClientId = clientId ?? this.ClientId;
            this.Resource = resource ?? this.Resource;
        }
        internal BaseVsoAuthentication(ICredentialStore personalAccessToken, ICredentialStore userCredential, ITokenStore adaRefresh, IVsoAuthority vsoAuthority)
            : this()
        {
            this.PersonalAccessTokenStore = personalAccessToken;
            this.UserCredentialStore = userCredential;
            this.AdaRefreshTokenStore = adaRefresh;
            this.VsoAuthority = vsoAuthority;
        }

        public readonly string ClientId;
        public readonly string Resource;

        protected ICredentialStore PersonalAccessTokenStore { get; set; }
        protected ICredentialStore UserCredentialStore { get; set; }
        protected ITokenStore AdaRefreshTokenStore { get; set; }
        protected ICredentialStore PersonalAccessTokenCache { get; set; }

        internal IVsoAuthority VsoAuthority { get; set; }

        public override void DeleteCredentials(Uri targetUri)
        {
            BaseSecureStore.ValidateTargetUri(targetUri);

            Credential credentials = null;
            Token token = null;
            if (this.PersonalAccessTokenStore.ReadCredentials(targetUri, out credentials))
            {
                this.PersonalAccessTokenCache.DeleteCredentials(targetUri);
                this.PersonalAccessTokenStore.DeleteCredentials(targetUri);
            }
            else if (this.AdaRefreshTokenStore.ReadToken(targetUri, out token))
            {
                this.AdaRefreshTokenStore.DeleteToken(targetUri);
            }
            else if (this.UserCredentialStore.ReadCredentials(targetUri, out credentials))
            {
                this.UserCredentialStore.DeleteCredentials(targetUri);
            }
        }

        public override bool GetCredentials(Uri targetUri, out Credential credentials)
        {
            BaseSecureStore.ValidateTargetUri(targetUri);

            Trace.TraceInformation("Attempting to retrieve cached credentials");

            // check the in-memory cache first
            if (!this.PersonalAccessTokenCache.ReadCredentials(targetUri, out credentials))
            {
                Trace.TraceInformation("Unable to retrieve cached credentials, attempting stored credentials retrieval.");

                // fall-back to the on disk cache
                if (this.PersonalAccessTokenStore.ReadCredentials(targetUri, out credentials))
                {
                    Trace.TraceInformation("Successfully retrieved stored credentials, updating credential cache");

                    // update the in-memory cache for faster future look-ups
                    this.PersonalAccessTokenCache.WriteCredentials(targetUri, credentials);
                }
            }

            return credentials != null;
        }

        public abstract Task<bool> RefreshCredentials(Uri targetUri);

        public bool RequestUserCredentials(Uri targetUri, out Credential credentials)
        {
            BaseSecureStore.ValidateTargetUri(targetUri);

            return this.UserCredentialStore.PromptUserCredentials(targetUri, out credentials);
        }

        public async Task<bool> ValidateCredentials(Credential credentials)
        {
            return await this.VsoAuthority.ValidateCredentials(credentials);
        }

        protected async Task<bool> GeneratePersonalAccessToken(Uri targetUri, Token accessToken)
        {
            Debug.Assert(targetUri != null, "The targetUri parameter is null");
            Debug.Assert(accessToken != null, "The accessToken parameter is null");
            Debug.Assert(accessToken.Type == TokenType.Access, "The value of the accessToken parameter is not an access token");

            Credential personalAccessToken;
            if ((personalAccessToken = await this.VsoAuthority.GeneratePersonalAccessToken(targetUri, accessToken)) != null)
            {
                this.PersonalAccessTokenCache.WriteCredentials(targetUri, personalAccessToken);
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
