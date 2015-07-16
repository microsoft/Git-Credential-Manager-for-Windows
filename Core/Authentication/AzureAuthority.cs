using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace Microsoft.TeamFoundation.Git.Helpers.Authentication
{
    internal class AzureAuthority : IAzureAuthority, ILiveAuthority, IAadAuthority
    {
        public const string AuthorityHostUrlBase = "https://login.microsoftonline.com";
        public const string DefaultAuthorityHostUrl = AuthorityHostUrlBase + "/common";

        public AzureAuthority(string authorityHostUrl = DefaultAuthorityHostUrl)
        {
            AuthorityHostUrl = authorityHostUrl;
            _adalTokenCache = new VsoAdalTokenCache();
        }

        private readonly VsoAdalTokenCache _adalTokenCache;

        public string AuthorityHostUrl { get; }

        public Tokens AcquireToken(Uri targetUri, string clientId, string resource, Uri redirectUri, string queryParameters = null)
        {
            Debug.Assert(targetUri != null && targetUri.IsAbsoluteUri, "The targetUri parameter is null or invalid");
            Debug.Assert(!String.IsNullOrWhiteSpace(clientId), "The clientId parameter is null or empty");
            Debug.Assert(!String.IsNullOrWhiteSpace(resource), "The resource parameter is null or empty");
            Debug.Assert(redirectUri != null, "The redirectUri parameter is null");
            Debug.Assert(redirectUri.IsAbsoluteUri, "The redirectUri parameter is not an absolute Uri");

            Tokens tokens = null;
            queryParameters = queryParameters ?? String.Empty;

            try
            {
                AuthenticationContext authCtx = new AuthenticationContext(AuthorityHostUrl, _adalTokenCache);
                AuthenticationResult authResult = authCtx.AcquireToken(resource, clientId, redirectUri, PromptBehavior.Always, UserIdentifier.AnyUser, queryParameters);
                tokens = new Tokens(authResult);

                Trace.TraceInformation("AzureAuthority::AcquireToken succeeded.");
            }
            catch (AdalException exception)
            {
                Trace.TraceError("AzureAuthority::AcquireToken failed.");
                Debug.Write(exception);
            }

            return tokens;
        }

        public async Task<Tokens> AcquireTokenAsync(Uri targetUri, string clientId, string resource, Credential credentials = null)
        {
            Debug.Assert(targetUri != null && targetUri.IsAbsoluteUri, "The targetUri parameter is null or invalid");
            Debug.Assert(!String.IsNullOrWhiteSpace(clientId), "The clientId parameter is null or empty");
            Debug.Assert(!String.IsNullOrWhiteSpace(resource), "The resource parameter is null or empty");

            Tokens tokens = null;

            try
            {
                UserCredential userCredential = credentials == null ? new UserCredential() : new UserCredential(credentials.Username, credentials.Password);
                AuthenticationContext authCtx = new AuthenticationContext(AuthorityHostUrl, _adalTokenCache);
                AuthenticationResult authResult = await authCtx.AcquireTokenAsync(resource, clientId, userCredential);
                tokens = new Tokens(authResult);

                Trace.TraceInformation("AzureAuthority::AcquireTokenAsync succeeded.");
            }
            catch (AdalException exception)
            {
                Trace.TraceError("AzureAuthority::AcquireTokenAsync failed.");
                Debug.WriteLine(exception);
            }

            return tokens;
        }

        public async Task<Tokens> AcquireTokenByRefreshTokenAsync(Uri targetUri, string clientId, string resource, Token refreshToken)
        {
            Debug.Assert(targetUri != null && targetUri.IsAbsoluteUri, "The targetUri parameter is null or invalid");
            Debug.Assert(!String.IsNullOrWhiteSpace(clientId), "The clientId parameter is null or empty");
            Debug.Assert(!String.IsNullOrWhiteSpace(resource), "The resource parameter is null or empty");
            Debug.Assert(refreshToken != null, "The refreshToken parameter is null");
            Debug.Assert(refreshToken.Type == TokenType.Refresh, "The value of refreshToken parameter is not a refresh token");
            Debug.Assert(!String.IsNullOrWhiteSpace(refreshToken.Value), "The value of refreshToken parameter is null or empty");

            Tokens tokens = null;

            try
            {
                AuthenticationContext authCtx = new AuthenticationContext(AuthorityHostUrl, _adalTokenCache);
                AuthenticationResult authResult = await authCtx.AcquireTokenByRefreshTokenAsync(refreshToken.Value, clientId, resource);
                tokens = new Tokens(authResult);

                Trace.TraceInformation("AzureAuthority::AcquireTokenByRefreshTokenAsync succeeded.");
            }
            catch (AdalException exception)
            {
                Trace.TraceError("AzureAuthority::AcquireTokenByRefreshTokenAsync failed.");
                Debug.WriteLine(exception);
            }

            return tokens;
        }

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

            Trace.TraceInformation("Generating Personal Access Token for {0}", targetUri);

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
                            Token token = new Token(tokenValue, TokenType.VsoPat);

                            Trace.TraceInformation("AzureAuthority::GeneratePersonalAccessToken succeeded.");

                            return token;
                        }
                    }
                    else
                    {
                        Console.Error.WriteLine("Received {0} from Visual Studio Online authority. Unable to generate personal access token.", response.ReasonPhrase);
                    }
                }
            }
            catch
            {
                Trace.TraceError("Personal access token generation failed unexpectedly.");
            }

            Trace.TraceError("AzureAuthority::GeneratePersonalAccessToken failed.");

            return null;
        }

        public async Task<bool> ValidateCredentials(Uri targetUri, Credential credentials)
        {
            const string VsoValidationUrl = "_apis/connectiondata";

            Credential.Validate(credentials);

            string validationUrl = String.Format("{0}://{1}/{2}", targetUri.Scheme, targetUri.DnsSafeHost, VsoValidationUrl);

            try
            {
                string basicAuthHeader = "Basic " + Convert.ToBase64String(Encoding.ASCII.GetBytes(String.Format("{0}:{1}", credentials.Username, credentials.Password)));
                HttpWebRequest request = WebRequest.CreateHttp(validationUrl);
                request.Headers.Add(HttpRequestHeader.Authorization, basicAuthHeader);
                HttpWebResponse response = await request.GetResponseAsync() as HttpWebResponse;
                Trace.TraceInformation("validation status code: {0}", response.StatusCode);
                return response.StatusCode == HttpStatusCode.OK;
            }
            catch
            {
                Trace.TraceError("credential validation failed");
            }

            return false;
        }

        public async Task<bool> ValidateToken(Uri targetUri, Token token)
        {
            const string VsoValidationUrl = "_apis/connectiondata";

            Token.Validate(token);

            if (token.Type == TokenType.VsoPat)
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
                Trace.TraceInformation("validation status code: {0}", response.StatusCode);
                return response.StatusCode == HttpStatusCode.OK;
            }
            catch
            {
                Trace.TraceError("token validation failed");
            }

            return false;
        }
    }
}
