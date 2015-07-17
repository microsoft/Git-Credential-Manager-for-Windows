using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace Microsoft.TeamFoundation.Git.Helpers.Authentication
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

        public VsoAadAuthentication(string credentialPrefix, VsoTokenScope tokenScope, string resource = null, string clientId = null)
            : base(credentialPrefix, tokenScope, resource, clientId)
        {
            this.VsoAuthority = new VsoAzureAuthority();
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
            ITokenStore vsoIdeTokenCache,
            IVsoAuthority vsoAuthority)
            : base(personalAccessTokenStore,
                   personalAccessTokenCache,
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
                    this.StoreRefreshToken(targetUri, tokens.RefeshToken);

                    return Task.Run(async () => { return await this.GeneratePersonalAccessToken(targetUri, tokens.AccessToken, requestCompactToken); }).Result;
                }
            }
            catch (AdalException exception)
            {
                Trace.WriteLine("\tFailed to aquire token from VsoAuthority.");
                Debug.Write(exception);
            }

            Trace.WriteLine("\tInteractive logon failed");
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
                    this.StoreRefreshToken(targetUri, tokens.RefeshToken);

                    return await this.GeneratePersonalAccessToken(targetUri, tokens.AccessToken, requestCompactToken);
                }
            }
            catch (AdalException exception)
            {
                Trace.WriteLine("\tFailed to aquire token from VsoAuthority.");
                Debug.Write(exception);
            }

            Trace.WriteLine("\tNon-interactive logon failed");
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
                Trace.WriteLine("\tFailed to aquire token from VsoAuthority.");
                Debug.WriteLine(exception);
            }

            Trace.WriteLine("\tNon-interactive logon failed");
            return false;
        }

        public override bool SetCredentials(Uri targetUri, Credential credentials)
        {
            // does nothing with VSO AAD backed accounts
            return false;
        }
    }
}
