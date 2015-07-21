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
    internal class VsoAzureAuthority : AzureAuthority, IVsoAuthority
    {
        /// <summary>
        /// The maximum wait time for a network request before timing out
        /// </summary>
        public const int RequestTimeout = 15 * 1000; // 15 second limit

        public VsoAzureAuthority(string authorityHostUrl = null)
            : base()
        {
            AuthorityHostUrl = authorityHostUrl ?? AuthorityHostUrl;
        }

        /// <summary>
        /// Generates a personal access token for use with Visual Studio Online.
        /// </summary>
        /// <param name="targetUri">
        /// The uniform resource indicator of the resource access tokens are being requested for.
        /// </param>
        /// <param name="accessToken"></param>
        /// <param name="tokenScope"></param>
        /// <param name="requireCompactToken"></param>
        /// <returns></returns>
        public async Task<Token> GeneratePersonalAccessToken(Uri targetUri, Token accessToken, VsoTokenScope tokenScope, bool requireCompactToken)
        {
            const string TokenAuthHost = "app.vssps.visualstudio.com";
            const string SessionTokenUrl = "https://" + TokenAuthHost + "/_apis/token/sessiontokens?api-version=1.0";
            const string CompactTokenUrl = SessionTokenUrl + "&tokentype=compact";
            const string TokenScopeJsonFormat = "{{ \"scope\" : \"{0}\" }}";
            const string HttpJsonContentType = "application/json";
            const string AccessTokenHeader = "Bearer";

            Debug.Assert(targetUri != null, "The targetUri parameter is null");
            Debug.Assert(accessToken != null && !String.IsNullOrWhiteSpace(accessToken.Value) && (accessToken.Type == TokenType.Access || accessToken.Type == TokenType.Federated), "The accessToken parameter is null or invalid");
            Debug.Assert(tokenScope != null);

            Trace.WriteLine("AzureAuthority::GeneratePersonalAccessToken");

            try
            {
                using (HttpClientHandler handler = new HttpClientHandler()
                {
                    MaxAutomaticRedirections = 2,
                    UseDefaultCredentials = true
                })
                using (HttpClient httpClient = new HttpClient(handler)
                {
                    Timeout = TimeSpan.FromMilliseconds(RequestTimeout)
                })
                {
                    string jsonContent = String.Format(TokenScopeJsonFormat, tokenScope);
                    StringContent content = new StringContent(jsonContent, Encoding.UTF8, HttpJsonContentType);

                    switch (accessToken.Type)
                    {
                        case TokenType.Access:
                            Trace.WriteLine("   using Azure access token to acquire personal access token");

                            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(AccessTokenHeader, accessToken.Value);
                            break;

                        case TokenType.Federated:
                            Trace.WriteLine("   using federated authentication token to acquire personal access token");

                            httpClient.DefaultRequestHeaders.Add("Cookie", accessToken.Value);
                            break;

                        default:
                            return null;
                    }

                    HttpResponseMessage response = await httpClient.PostAsync(requireCompactToken ? CompactTokenUrl : SessionTokenUrl,
                                                                              content);
                    if (response.StatusCode == HttpStatusCode.OK)
                    {
                        string responseText = await response.Content.ReadAsStringAsync();

                        Match tokenMatch = null;
                        if ((tokenMatch = Regex.Match(responseText, @"\s*""token""\s*:\s*""([^\""]+)""\s*", RegexOptions.Compiled | RegexOptions.IgnoreCase)).Success)
                        {
                            string tokenValue = tokenMatch.Groups[1].Value;
                            Token token = new Token(tokenValue, TokenType.Personal);

                            Trace.WriteLine("   personal access token aquisition succeeded.");

                            return token;
                        }
                    }
                    else
                    {
                        Console.Error.WriteLine("Received '{0}' from Visual Studio Online authority. Unable to generate personal access token.", response.ReasonPhrase);
                    }
                }
            }
            catch
            {
                Trace.WriteLine("   an error occured error.");
            }

            Trace.WriteLine("   personal access token aquisition failed.");

            return null;
        }

        public async Task<bool> ValidateCredentials(Uri targetUri, Credential credentials)
        {
            const string VsoValidationUrl = "_apis/connectiondata";

            Credential.Validate(credentials);

            Trace.WriteLine("VsoAzureAuthority::ValidateCredentials");

            string validationUrl = String.Format("{0}://{1}/{2}", targetUri.Scheme, targetUri.DnsSafeHost, VsoValidationUrl);

            try
            {
                string basicAuthHeader = "Basic " + Convert.ToBase64String(Encoding.ASCII.GetBytes(String.Format("{0}:{1}", credentials.Username, credentials.Password)));
                HttpWebRequest request = WebRequest.CreateHttp(validationUrl);
                request.Timeout = RequestTimeout;
                request.Headers.Add(HttpRequestHeader.Authorization, basicAuthHeader);

                HttpWebResponse response = await request.GetResponseAsync() as HttpWebResponse;

                Trace.WriteLine("   validation status code: " + response.StatusCode);

                return response.StatusCode == HttpStatusCode.OK;
            }
            catch
            {
                Trace.WriteLine("   credential validation failed");
            }

            return false;
        }

        public async Task<bool> ValidateToken(Uri targetUri, Token token)
        {
            const string VsoValidationUrl = "_apis/connectiondata";

            Token.Validate(token);

            Trace.WriteLine("VsoAzureAuthority::ValidateToken");

            if (token.Type == TokenType.Personal)
                return await this.ValidateCredentials(targetUri, (Credential)token);

            if (!(token.Type == TokenType.Access || token.Type == TokenType.Refresh))
                return false;

            string validationUrl = String.Format("{0}://{1}/{2}", targetUri.Scheme, targetUri.DnsSafeHost, VsoValidationUrl);

            try
            {
                string sessionAuthHeader = "Bearer " + token.Value;
                HttpWebRequest request = WebRequest.CreateHttp(validationUrl);
                request.Headers.Add(HttpRequestHeader.Authorization, sessionAuthHeader);
                request.Timeout = RequestTimeout;

                HttpWebResponse response = await request.GetResponseAsync() as HttpWebResponse;

                Trace.WriteLine("   validation status code: " + response.StatusCode);

                return response.StatusCode == HttpStatusCode.OK;
            }
            catch
            {
                Trace.WriteLine("   token validation failed");
            }

            return false;
        }
    }
}
