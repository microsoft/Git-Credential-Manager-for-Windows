using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Debug = System.Diagnostics.Debug;

namespace Microsoft.TeamFoundation.Git.Helpers.Authentication
{
    public abstract class BaseVsoAuthentication: BaseAuthentication, IVsoAuthentication
    {
        public static readonly string DefaultResource = "499b84ac-1321-427f-aa17-267ca6975798";
        public static readonly Guid DefaultClientId = new Guid("872cd9fa-d31f-45e0-9eab-6e460a02d1f1");

        protected const string SecondaryCredentialPrefix = "alt-git";
        protected const string TokenPrefix = "adal-refresh";

        protected BaseVsoAuthentication(string authorityHostUrl)
        {
            this.AuthorityHostUrl = authorityHostUrl;
            this.ClientId = DefaultClientId;
            this.Resource = DefaultResource;
            this.PersonalAccessTokenStore = new CredentialStore(PrimaryCredentialPrefix);
            this.UserCredentialStore = new CredentialStore(SecondaryCredentialPrefix);
            this.AdaRefreshTokenStore = new TokenStore(TokenPrefix);
        }
        protected BaseVsoAuthentication(string authorityHostUrl, string resource, Guid clientId)
            : this(authorityHostUrl)
        {
            this.ClientId = clientId;
            this.Resource = resource;
        }
        internal BaseVsoAuthentication(string authorityHostUrl, ICredentialStore personalAccessToken, ICredentialStore userCredential, ITokenStore adaRefresh)
            :this(authorityHostUrl)
        {
            this.PersonalAccessTokenStore = personalAccessToken;
            this.UserCredentialStore = userCredential;
            this.AdaRefreshTokenStore = adaRefresh;
        }

        public readonly string AuthorityHostUrl;
        public readonly Guid ClientId;
        public readonly string Resource;

        protected ICredentialStore PersonalAccessTokenStore { get; set; }
        protected ICredentialStore UserCredentialStore { get; set; }
        protected ITokenStore AdaRefreshTokenStore { get; set; }

        public override void DeleteCredentials(Uri targetUri)
        {
            BaseCredentialStore.ValidateTargetUri(targetUri);

            this.PersonalAccessTokenStore.DeleteCredentials(targetUri);
            this.UserCredentialStore.DeleteCredentials(targetUri);
            this.AdaRefreshTokenStore.DeleteToken(targetUri);
        }

        public override bool GetCredentials(Uri targetUri, out Credentials credentials)
        {
            BaseCredentialStore.ValidateTargetUri(targetUri);

            return this.PersonalAccessTokenStore.ReadCredentials(targetUri, out credentials);
        }                

        public abstract Task<bool> InteractiveLogon(Uri targetUri, Credentials credentials);

        protected async Task<bool> GeneratePersonalAccessToken(Uri targetUri, AuthenticationResult authResult)
        {
            const string VsspEndPointUrl = "https://app.vssps.visualstudio.com/_apis/token/sessiontokens?api-version=1.0&tokentype=compact";

            Debug.Assert(targetUri != null, "The targetUri parameter is null");
            Debug.Assert(authResult != null, "The authResult parameter is null");

            try
            {
                using (HttpClient httpClient = new HttpClient())
                {
                    StringContent content = new StringContent(String.Empty, Encoding.UTF8, "application/json");
                    httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(authResult.AccessTokenType, authResult.AccessToken);

                    HttpResponseMessage response = await httpClient.PostAsync(VsspEndPointUrl, content);
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        string responseText = await response.Content.ReadAsStringAsync();
                        Dictionary<string, string> values = JsonConvert.DeserializeObject<Dictionary<string, string>>(responseText);
                        if (values.ContainsKey("token"))
                        {
                            string token = values["token"];
                            Credentials personalAccessToken = new Credentials(token);
                            this.PersonalAccessTokenStore.WriteCredentials(targetUri, personalAccessToken);
                            return true;
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                Debug.WriteLine(exception);
            }

            return false;
        }

        protected void StoreRefreshToken(Uri targetUri, AuthenticationResult authResult)
        {
            Debug.Assert(targetUri != null, "The targetUri parameter is null");
            Debug.Assert(authResult != null, "The authResult parameter is null");

            Token refreshToken = new Token(authResult.RefreshToken);
            this.AdaRefreshTokenStore.WriteToken(targetUri, refreshToken);
        }
    }
}
