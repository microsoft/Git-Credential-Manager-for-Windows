using System;
using System.Diagnostics;
using System.IO;
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
            const string AccessTokenHeader = "Bearer";

            Debug.Assert(targetUri != null, "The targetUri parameter is null");
            Debug.Assert(accessToken != null && !String.IsNullOrWhiteSpace(accessToken.Value) && (accessToken.Type == TokenType.Access || accessToken.Type == TokenType.Federated), "The accessToken parameter is null or invalid");
            Debug.Assert(tokenScope != null);

            Trace.WriteLine("VsoAzureAuthority::GeneratePersonalAccessToken");

            try
            {
                // create a `HttpClient` with a minimum number of redirects, default creds, and a reasonable timeout (access token generation seems to hang occasionally)
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

                    if (await PopulateTokenTargetId(targetUri, accessToken))
                    {
                        StringContent content = GetAccessTokenRequestBody(targetUri, accessToken, tokenScope);
                        string requestUrl = requireCompactToken ? CompactTokenUrl : SessionTokenUrl;

                        HttpResponseMessage response = await httpClient.PostAsync(requestUrl, content);
                        if (response.StatusCode == HttpStatusCode.OK)
                        {
                            string responseText = await response.Content.ReadAsStringAsync();

                            if (!String.IsNullOrWhiteSpace(responseText))
                            {
                                // find the 'token : <value>' portion of the result content, if any
                                Match tokenMatch = null;
                                if ((tokenMatch = Regex.Match(responseText, @"\s*""token""\s*:\s*""([^\""]+)""\s*", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)).Success)
                                {
                                    string tokenValue = tokenMatch.Groups[1].Value;
                                    Token token = new Token(tokenValue, TokenType.Personal);

                                    Trace.WriteLine("   personal access token aquisition succeeded.");

                                    return token;
                                }
                            }
                        }
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

        public async Task<bool> PopulateTokenTargetId(Uri targetUri, Token accessToken)
        {
            Debug.Assert(targetUri != null && targetUri.IsAbsoluteUri, "The targetUri parameter is null or invalid");
            Debug.Assert(accessToken != null && !String.IsNullOrWhiteSpace(accessToken.Value) && (accessToken.Type == TokenType.Access || accessToken.Type == TokenType.Federated), "The accessToken parameter is null or invalid");

            Trace.WriteLine("VsoAzureAuthority::PopulateTokenTargetId");

            string resultId = null;
            Guid instanceId;

            try
            {
                // create an request to the VSO deployment data end-point
                var request = GetConnectionDataRequest(targetUri, accessToken);

                // send the request and wait for the response
                using (var response = await request.GetResponseAsync())
                using (var stream = response.GetResponseStream())
                using (var reader = new StreamReader(stream, Encoding.UTF8))
                {
                    string content = await reader.ReadToEndAsync();
                    Match match;

                    if ((match = Regex.Match(content, @"""instanceId""\s*\:\s*""([^""]+)""", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)).Success
                        && match.Groups.Count == 2)
                    {
                        resultId = match.Groups[1].Value;
                    }
                }
            }
            catch (WebException webException)
            {
                Trace.WriteLine("   server returned " + webException.Status);
            }

            if (Guid.TryParse(resultId, out instanceId))
            {
                accessToken.TargetId = instanceId;

                return true;
            }

            return false;
        }

        /// <summary>
        /// Validates that <see cref="Credential"/> are valid to grant access to the Visual Studio 
        /// Online service represented by the <paramref name="targetUri"/> parameter.
        /// </summary>
        /// <param name="targetUri">Uniform resource identifier for a VSO service.</param>
        /// <param name="credentials">
        /// <see cref="Credential"/> expected to grant access to the VSO service.
        /// </param>
        /// <returns>True if successful; otherwise false.</returns>
        public async Task<bool> ValidateCredentials(Uri targetUri, Credential credentials)
        {
            Debug.Assert(targetUri != null && targetUri.IsAbsoluteUri, "The targetUri parameter is null or invalid");
            Debug.Assert(credentials != null, "The credentials parameter is null or invalid");

            Trace.WriteLine("VsoAzureAuthority::ValidateCredentials");

            try
            {
                // create an request to the VSO deployment data end-point
                HttpWebRequest request = GetConnectionDataRequest(targetUri, credentials);

                // send the request and wait for the response
                using (HttpWebResponse response = await request.GetResponseAsync() as HttpWebResponse)
                {
                    // we're looking for 'OK 200' here, anything else is failure
                    Trace.WriteLine("   server returned: " + response.StatusCode);
                    return response.StatusCode == HttpStatusCode.OK;
                }
            }
            catch (WebException webException)
            {
                Trace.WriteLine("   server returned: " + webException.Message);
            }
            catch
            {
                Trace.WriteLine("   unexpected error");
            }

            Trace.WriteLine("   credential validation failed");
            return false;
        }

        /// <summary>
        /// <para>Validates that <see cref="Token"/> are valid to grant access to the Visual Studio 
        /// Online service represented by the <paramref name="targetUri"/> parameter.</para>
        /// <para>Tokens of <see cref="TokenType.Refresh"/> cannot grant access, and
        /// therefore always fail - this does not mean the token is invalid.</para>
        /// </summary>
        /// <param name="targetUri">Uniform resource identifier for a VSO service.</param>
        /// <param name="token">
        /// <see cref="Token"/> expected to grant access to the VSO service.
        /// </param>
        /// <returns>True if successful; otherwise false.</returns>
        public async Task<bool> ValidateToken(Uri targetUri, Token token)
        {
            Debug.Assert(targetUri != null && targetUri.IsAbsoluteUri, "The targetUri parameter is null or invalid");
            Debug.Assert(token != null && (token.Type == TokenType.Access || token.Type == TokenType.Federated), "The token parameter is null or invalid");

            Trace.WriteLine("VsoAzureAuthority::ValidateToken");

            // personal access tokens are effectively credentials, treat them as such
            if (token.Type == TokenType.Personal)
                return await this.ValidateCredentials(targetUri, (Credential)token);

            try
            {
                // create an request to the VSO deployment data end-point
                HttpWebRequest request = GetConnectionDataRequest(targetUri, token);

                // send the request and wait for the response
                using (HttpWebResponse response = await request.GetResponseAsync() as HttpWebResponse)
                {
                    // we're looking for 'OK 200' here, anything else is failure
                    Trace.WriteLine("   server returned: " + response.StatusCode);
                    return response.StatusCode == HttpStatusCode.OK;
                }
            }
            catch (WebException webException)
            {
                Trace.WriteLine("   server returned: " + webException.Message);
            }
            catch
            {
                Trace.WriteLine("   unexpected error");
            }

            Trace.WriteLine("   token validation failed");
            return false;
        }

        private StringContent GetAccessTokenRequestBody(Uri targetUri, Token accessToken, VsoTokenScope tokenScope)
        {
            const string ContentJsonFormat = "{{ \"scope\" : \"{0}\", \"targetAccounts\" : [\"{1}\"], \"displayName\" : \"Git Access for {2}\" }}";
            const string HttpJsonContentType = "application/json";

            Debug.Assert(accessToken != null && (accessToken.Type == TokenType.Access || accessToken.Type == TokenType.Federated), "The accessToken parameter is null or invalid");
            Debug.Assert(tokenScope != null, "The tokenScope parameter is null");

            Trace.WriteLine("   creating access token scoped to '" + tokenScope + "' for '" + accessToken.TargetId + "'");

            string jsonContent = String.Format(ContentJsonFormat, tokenScope, accessToken.TargetId, targetUri);
            StringContent content = new StringContent(jsonContent, Encoding.UTF8, HttpJsonContentType);

            return content;
        }

        private HttpWebRequest GetConnectionDataRequest(Uri targetUri, Credential credentials)
        {
            const string BasicPrefix = "Basic ";
            const string UsernamePasswordFormat = "{0}:{1}";

            Debug.Assert(targetUri != null && targetUri.IsAbsoluteUri, "The targetUri parameter is null or invalid");
            Debug.Assert(credentials != null, "The credentials parameter is null or invalid");

            // create an request to the VSO deployment data end-point
            HttpWebRequest request = GetConnectionDataRequest(targetUri);

            // credentials are packed into the 'Authorization' header as a base64 encoded pair
            string credPair = String.Format(UsernamePasswordFormat, credentials.Username, credentials.Password);
            byte[] credBytes = Encoding.ASCII.GetBytes(credPair);
            string base64enc = Convert.ToBase64String(credBytes);
            string basicAuthHeader = BasicPrefix + base64enc;
            request.Headers.Add(HttpRequestHeader.Authorization, basicAuthHeader);

            return request;
        }

        private HttpWebRequest GetConnectionDataRequest(Uri targetUri, Token token)
        {
            const string BearerPrefix = "Bearer ";

            Debug.Assert(targetUri != null && targetUri.IsAbsoluteUri, "The targetUri parameter is null or invalid");
            Debug.Assert(token != null && (token.Type == TokenType.Access || token.Type == TokenType.Federated), "The token parameter is null or invalid");

            Trace.WriteLine("VsoAzureAuthority::GetConnectionDataRequest");

            // create an request to the VSO deployment data end-point
            HttpWebRequest request = GetConnectionDataRequest(targetUri);

            // different types of tokens are packed differently
            switch (token.Type)
            {
                case TokenType.Access:
                    Trace.WriteLine("   validating adal access token");

                    // adal access tokens are packed into the Authorization header
                    string sessionAuthHeader = BearerPrefix + token.Value;
                    request.Headers.Add(HttpRequestHeader.Authorization, sessionAuthHeader);
                    break;

                case TokenType.Federated:
                    Trace.WriteLine("   validating federated authentication token");

                    // federated authentication tokens are sent as cookie(s)
                    request.Headers.Add(HttpRequestHeader.Cookie, token.Value);
                    break;

                default:
                    Trace.WriteLine("   unsupported token type");
                    break;
            }

            return request;
        }

        private HttpWebRequest GetConnectionDataRequest(Uri targetUri)
        {
            const string VsoValidationUrlFormat = "https://{0}/_apis/connectiondata";

            Debug.Assert(targetUri != null && targetUri.IsAbsoluteUri, "The targetUri parameter is null or invalid");

            // create a url to the connection data end-point, it's deployment level and "always on".
            string validationUrl = String.Format(VsoValidationUrlFormat, targetUri.DnsSafeHost);

            // start building the request, only supports GET
            HttpWebRequest request = WebRequest.CreateHttp(validationUrl);
            request.Timeout = RequestTimeout;

            return request;
        }
    }
}
