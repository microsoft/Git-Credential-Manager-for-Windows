using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace Microsoft.TeamFoundation.Git.Helpers.Authentication
{
    public sealed class VsoAadAuthentication : BaseVsoAuthentication, IVsoAadAuthentication
    {
        public VsoAadAuthentication(VsoTokenScope scope, string resource = null, string clientId = null)
            : base(scope, resource, clientId)
        {
            this.AzureAuthority = new AzureAuthority();
        }
        /// <summary>
        /// Test constructor which allows for using fake credential stores
        /// </summary>
        /// <param name="personalAccessTokenStore"></param>
        /// <param name="userCredential"></param>
        /// <param name="adaRefreshTokenStore"></param>
        internal VsoAadAuthentication(
            ITokenStore personalAccessTokenStore,
            ITokenStore personalAccessTokenCache,
            ITokenStore adaRefreshTokenStore,
            IAzureAuthority azureAuthority,
            IVsoAuthority vsoAuthority)
            : base(personalAccessTokenStore,
                   personalAccessTokenCache,
                   adaRefreshTokenStore,
                   vsoAuthority)
        {
            this.AzureAuthority = azureAuthority;
        }

        internal IAzureAuthority AzureAuthority { get; set; }

        private readonly VsoAdalTokenCache _vsideCache = new VsoAdalTokenCache();

        public bool InteractiveLogon(Uri targetUri, bool requestCompactToken)
        {
            BaseSecureStore.ValidateTargetUri(targetUri);

            Trace.TraceInformation("launching interactive UX");

            try
            {
                Tokens tokens;
                if ((tokens = this.AzureAuthority.AcquireToken(this.ClientId, this.Resource, new Uri(RedirectUrl), null)) != null)
                {
                    this.StoreRefreshToken(targetUri, tokens.RefeshToken);

                    return Task.Run(async () => { return await this.GeneratePersonalAccessToken(targetUri, tokens.AccessToken, requestCompactToken); }).Result;
                }
            }
            catch (AdalException exception)
            {
                Debug.Write(exception);
            }

            return false;
        }

        public async Task<bool> NoninteractiveLogonWithCredentials(Uri targetUri, Credential credentials, bool requestCompactToken)
        {
            BaseSecureStore.ValidateTargetUri(targetUri);
            Credential.Validate(credentials);

            Trace.TraceInformation("Begin InteractiveLogon for {0}", targetUri);

            try
            {
                Tokens tokens;
                if ((tokens = await this.AzureAuthority.AcquireTokenAsync(this.ClientId, this.Resource, credentials)) != null)
                {
                    this.StoreRefreshToken(targetUri, tokens.RefeshToken);

                    return await this.GeneratePersonalAccessToken(targetUri, tokens.AccessToken, requestCompactToken);
                }
            }
            catch (AdalException exception)
            {
                Debug.Write(exception);
            }

            Trace.TraceInformation("InteractiveLogon failed");
            return false;
        }

        public async Task<bool> NoninteractiveLogon(Uri targetUri, bool requestCompactToken)
        {
            BaseSecureStore.ValidateTargetUri(targetUri);

            Trace.TraceInformation("attempting non-interactive logon");

            try
            {
                Tokens tokens;
                if ((tokens = await this.AzureAuthority.AcquireTokenAsync(this.ClientId, this.Resource)) != null)
                {
                    this.StoreRefreshToken(targetUri, tokens.RefeshToken);

                    return await this.GeneratePersonalAccessToken(targetUri, tokens.AccessToken, requestCompactToken);
                }
            }
            catch (AdalException exception)
            {
                Debug.WriteLine(exception);
                Trace.TraceError("Non-interactive logon failed");
            }

            return false;
        }

        public override async Task<bool> RefreshCredentials(Uri targetUri, bool requestCompactToken)
        {
            BaseSecureStore.ValidateTargetUri(targetUri);

            try
            {
                Token refreshToken = null;
                if (this.AdaRefreshTokenStore.ReadToken(targetUri, out refreshToken))
                {
                    Tokens tokens;
                    return ((tokens = await this.AzureAuthority.AcquireTokenByRefreshTokenAsync(this.ClientId, this.Resource, refreshToken)) != null
                        && await this.GeneratePersonalAccessToken(targetUri, tokens.AccessToken, requestCompactToken));
                }
                else
                {
                    Trace.TraceWarning("Failed to discover cached credentials. Fallback to VS IDE cached ADAL tokens.");

                    foreach (var item in _vsideCache.ReadItems())
                    {
                        refreshToken = new Token(item.RefreshToken, TokenType.Refresh);

                        Tokens tokens;
                        if ((tokens = await this.AzureAuthority.AcquireTokenByRefreshTokenAsync(this.ClientId, this.Resource, refreshToken)) != null
                            && await this.GeneratePersonalAccessToken(targetUri, tokens.AccessToken, requestCompactToken))
                        {
                            Trace.TraceInformation("VS IDE cached ADAL token used for Access Token Generation.");

                            this.AdaRefreshTokenStore.WriteToken(targetUri, refreshToken);

                            return true;
                        }
                    }
                }
            }
            catch (AdalException exception)
            {
                Debug.WriteLine(exception);
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
