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
                    CookieContainer = new CookieContainer(),
                    UseCookies = true,
                    UseDefaultCredentials = true
                })
                using (HttpClient httpClient = new HttpClient(handler)
                {
                    Timeout = TimeSpan.FromSeconds(15)
                })
                {
                    string jsonContent = String.Format(TokenScopeJsonFormat, tokenScope);
                    StringContent content = new StringContent(jsonContent, Encoding.UTF8, HttpJsonContentType);

                    switch (accessToken.Type)
                    {
                        case TokenType.Access:
                            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(AccessTokenHeader, accessToken.Value);
                            break;

                        case TokenType.Federated:
                            string[] chunks = accessToken.Value.Split(';');

                            foreach (string chunk in chunks)
                            {
                                int seperator = chunk.IndexOf('=');
                                if (seperator > 0)
                                {
                                    string name = chunk.Substring(0, seperator);
                                    string value = chunk.Substring(seperator + 1, chunk.Length - seperator - 1);

                                    Cookie cookie = new Cookie(name, value, "/", TokenAuthHost);
                                    handler.CookieContainer.Add(cookie);
                                }
                            }
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

                            Trace.WriteLine("\tpersonal access token generation succeeded.");

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
                Trace.WriteLine("\tan error occured error.");
            }

            Trace.WriteLine("\tpersonal access token generation failed.");

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
                request.Headers.Add(HttpRequestHeader.Authorization, basicAuthHeader);
                HttpWebResponse response = await request.GetResponseAsync() as HttpWebResponse;
                Trace.WriteLine("\tvalidation status code: " + response.StatusCode);
                return response.StatusCode == HttpStatusCode.OK;
            }
            catch
            {
                Trace.WriteLine("\tcredential validation failed");
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
                request.Timeout = 15 * 1000;
                HttpWebResponse response = await request.GetResponseAsync() as HttpWebResponse;
                Trace.WriteLine("\tvalidation status code: " + response.StatusCode);
                return response.StatusCode == HttpStatusCode.OK;
            }
            catch
            {
                Trace.WriteLine("\ttoken validation failed");
            }

            return false;
        }
    }
}
