using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Microsoft.TeamFoundation.Git.Helpers.Authentication
{
    internal class AzureAuthority : IAzureAuthority, ILiveAuthority, IVsoAuthority
    {
        public const string AuthorityHostUrl = "https://login.windows.net/common";

        public Tokens AcquireToken(string clientId, string resource, Uri redirectUri, string queryParameters = null)
        {
            Debug.Assert(!String.IsNullOrWhiteSpace(clientId), "The clientId parameter is null or empty");
            Debug.Assert(!String.IsNullOrWhiteSpace(resource), "The resource parameter is null or empty");
            Debug.Assert(redirectUri != null, "The redirectUri parameter is null");
            Debug.Assert(redirectUri.IsAbsoluteUri, "The redirectUri parameter is not an absolute Uri");

            Tokens tokens = null;
            queryParameters = queryParameters ?? String.Empty;

            try
            {
                AuthenticationContext authCtx = new AuthenticationContext(AuthorityHostUrl);
                AuthenticationResult authResult = authCtx.AcquireToken(resource, clientId, redirectUri, PromptBehavior.Always, UserIdentifier.AnyUser, queryParameters);
                tokens = new Tokens(authResult);

                Trace.TraceInformation("AzureAuthority::AcquireToken succeeded.");
            }
            catch (AdalException exception)
            {
                Trace.TraceError("AzureAuthority::AcquireToken failed.");
                Trace.TraceError(exception.Message);
                Debug.Write(exception);
            }

            return tokens;
        }

        public async Task<Tokens> AcquireTokenAsync(string clientId, string resource, Credential credentials = null)
        {
            Debug.Assert(!String.IsNullOrWhiteSpace(clientId), "The clientId parameter is null or empty");
            Debug.Assert(!String.IsNullOrWhiteSpace(resource), "The resource parameter is null or empty");

            Tokens tokens = null;

            try
            {
                UserCredential userCredential = credentials == null ? new UserCredential() : new UserCredential(credentials.Username, credentials.Password);
                AuthenticationContext authCtx = new AuthenticationContext(AuthorityHostUrl, IdentityModel.Clients.ActiveDirectory.TokenCache.DefaultShared);
                AuthenticationResult authResult = await authCtx.AcquireTokenAsync(resource, clientId, userCredential);
                tokens = new Tokens(authResult);

                Trace.TraceInformation("AzureAuthority::AcquireTokenAsync succeeded.");
            }
            catch (AdalException exception)
            {
                Trace.TraceError("AzureAuthority::AcquireTokenAsync failed.");
                Trace.TraceError(exception.Message);
                Debug.WriteLine(exception);
            }

            return tokens;
        }

        public async Task<Tokens> AcquireTokenByRefreshTokenAsync(string clientId, string resource, Token refreshToken)
        {
            Debug.Assert(!String.IsNullOrWhiteSpace(clientId), "The clientId parameter is null or empty");
            Debug.Assert(!String.IsNullOrWhiteSpace(resource), "The resource parameter is null or empty");
            Debug.Assert(refreshToken != null, "The refreshToken parameter is null");
            Debug.Assert(refreshToken.Type == TokenType.Refresh, "The value of refreshToken parameter is not a refresh token");
            Debug.Assert(!String.IsNullOrWhiteSpace(refreshToken.Value), "The value of refreshToken parameter is null or empty");

            Tokens tokens = null;

            try
            {
                AuthenticationContext authCtx = new AuthenticationContext(AuthorityHostUrl, IdentityModel.Clients.ActiveDirectory.TokenCache.DefaultShared);
                AuthenticationResult authResult = await authCtx.AcquireTokenByRefreshTokenAsync(refreshToken.Value, clientId, resource);
                tokens = new Tokens(authResult);

                Trace.TraceInformation("AzureAuthority::AcquireTokenByRefreshTokenAsync succeeded.");
            }
            catch (AdalException exception)
            {
                Trace.TraceError("AzureAuthority::AcquireTokenByRefreshTokenAsync failed.");
                Trace.TraceError(exception.Message);
                Debug.WriteLine(exception);
            }

            return tokens;
        }

        public async Task<Credential> GeneratePersonalAccessToken(Uri targetUri, Token accessToken)
        {
            const string VsspEndPointUrl = "https://app.vssps.visualstudio.com/_apis/token/sessiontokens?api-version=1.0&tokentype=compact";

            Debug.Assert(targetUri != null, "The targetUri parameter is null");
            Debug.Assert(accessToken != null, "The accessToken parameter is null");
            Debug.Assert(accessToken.Type == TokenType.Access, "The value of the accessToken parameter is not an access token");

            Trace.TraceInformation("Generating Personal Access Token for {0}", targetUri);

            try
            {
                using (HttpClient httpClient = new HttpClient())
                {
                    StringContent content = new StringContent(String.Empty, Encoding.UTF8, "application/json");
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Authorization", "Bearer" + accessToken.Value);
                    httpClient.DefaultRequestHeaders.Add("X-TFS-FedAuthRedirect", "Suppress");

                    HttpResponseMessage response = await httpClient.PostAsync(VsspEndPointUrl, content);
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        string responseText = await response.Content.ReadAsStringAsync();

                        Trace.TraceInformation("Personal Access Token generation success.");

                        Match tokenMatch = null;
                        if ((tokenMatch = Regex.Match(responseText, @"\s*""token""\s*:\s*""(\S+)""\s*", RegexOptions.Compiled | RegexOptions.IgnoreCase)).Success)
                        {
                            string token = tokenMatch.Groups[1].Value;
                            Credential personalAccessToken = new Credential(token, String.Empty);

                            Trace.TraceInformation("AzureAuthority::GeneratePersonalAccessToken succeeded.");

                            return personalAccessToken;
                        }
                    }
                    else
                    {
                        Trace.TraceError("AzureAuthority::GeneratePersonalAccessToken failed (1).");

                        Console.Error.WriteLine("Received {0} from Visual Studio Online authority. Unable to generate personal access token.", response.ReasonPhrase);
                    }
                }
            }
            catch (Exception exception)
            {
                Trace.TraceError("AzureAuthority::GeneratePersonalAccessToken failed (2).");
                Debug.WriteLine(exception);
            }

            return null;
        }

        public async Task<bool> ValidateCredentials(Credential credentials)
        {
            const string VsoValidationUrl = "https://app.vssps.visualstudio.com/_apis/profile/profiles/me?api-version=1.0";

            Credential.Validate(credentials);

            try
            {
                string basicAuthHeader = "Basic " + Convert.ToBase64String(ASCIIEncoding.ASCII.GetBytes(String.Format("{0}:{1}", credentials.Username, credentials.Password)));
                HttpWebRequest request = WebRequest.CreateHttp(VsoValidationUrl);
                request.Headers.Add(HttpRequestHeader.Authorization, basicAuthHeader);
                HttpWebResponse response = await request.GetResponseAsync() as HttpWebResponse;
                Trace.TraceInformation("validation status code: {0}", response.StatusCode);
                return response.StatusCode == HttpStatusCode.OK;
            }
            catch (Exception exception)
            {
                Debug.WriteLine(exception);
            }

            return false;
        }
    }
}
