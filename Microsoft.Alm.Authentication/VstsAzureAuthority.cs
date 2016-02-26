using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Microsoft.Alm.Authentication
{
    internal class VstsAzureAuthority : AzureAuthority, IVstsAuthority
    {
        /// <summary>
        /// The maximum wait time for a network request before timing out
        /// </summary>
        public const int RequestTimeout = 15 * 1000; // 15 second limit

        public VstsAzureAuthority(string authorityHostUrl = null)
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
        public async Task<Token> GeneratePersonalAccessToken(TargetUri targetUri, Token accessToken, VstsTokenScope tokenScope, bool requireCompactToken)
        {
            const string AccessTokenHeader = "Bearer";

            Debug.Assert(targetUri != null, "The targetUri parameter is null");
            Debug.Assert(accessToken != null && !String.IsNullOrWhiteSpace(accessToken.Value) && (accessToken.Type == TokenType.Access || accessToken.Type == TokenType.Federated), "The accessToken parameter is null or invalid");
            Debug.Assert(tokenScope != null);

            Trace.WriteLine("VstsAzureAuthority::GeneratePersonalAccessToken");

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
                    httpClient.DefaultRequestHeaders.Add("User-Agent", Global.GetUserAgent());

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
                        Uri requestUri;
                        if (TryCreateRequestUri(targetUri, requireCompactToken, out requestUri))
                        {
                            Trace.WriteLine("   request url is " + requestUri);

                            using (StringContent content = GetAccessTokenRequestBody(targetUri, accessToken, tokenScope))
                            using (HttpResponseMessage response = await httpClient.PostAsync(requestUri, content))
                            {
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
                }
            }
            catch
            {
                Trace.WriteLine("   an error occured error.");
            }

            Trace.WriteLine("   personal access token aquisition failed.");

            return null;
        }

        public async Task<bool> PopulateTokenTargetId(TargetUri targetUri, Token accessToken)
        {
            Debug.Assert(targetUri != null && targetUri.IsAbsoluteUri, "The targetUri parameter is null or invalid");
            Debug.Assert(accessToken != null && !String.IsNullOrWhiteSpace(accessToken.Value) && (accessToken.Type == TokenType.Access || accessToken.Type == TokenType.Federated), "The accessToken parameter is null or invalid");

            Trace.WriteLine("VstsAzureAuthority::PopulateTokenTargetId");

            string resultId = null;
            Guid instanceId;

            try
            {
                // create an request to the VSTS deployment data end-point
                HttpWebRequest request = GetConnectionDataRequest(targetUri, accessToken);

                Trace.WriteLine(String.Format("   access token end-point is {0} {1}", request.Method, request.RequestUri));

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
                Trace.WriteLine("   target identity is " + resultId);
                accessToken.TargetIdentity = instanceId;

                return true;
            }

            return false;
        }

        /// <summary>
        /// Validates that <see cref="Credential"/> are valid to grant access to the Visual Studio 
        /// Online service represented by the <paramref name="targetUri"/> parameter.
        /// </summary>
        /// <param name="targetUri">Uniform resource identifier for a VSTS service.</param>
        /// <param name="credentials">
        /// <see cref="Credential"/> expected to grant access to the VSTS service.
        /// </param>
        /// <returns>True if successful; otherwise false.</returns>
        public async Task<bool> ValidateCredentials(TargetUri targetUri, Credential credentials)
        {
            Debug.Assert(targetUri != null && targetUri.IsAbsoluteUri, "The targetUri parameter is null or invalid");
            Debug.Assert(credentials != null, "The credentials parameter is null or invalid");

            Trace.WriteLine("VstsAzureAuthority::ValidateCredentials");

            try
            {
                // create an request to the VSTS deployment data end-point
                HttpWebRequest request = GetConnectionDataRequest(targetUri, credentials);

                Trace.WriteLine("   validating credentials against " + request.RequestUri);

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
        /// <param name="targetUri">Uniform resource identifier for a VSTS service.</param>
        /// <param name="token">
        /// <see cref="Token"/> expected to grant access to the VSTS service.
        /// </param>
        /// <returns>True if successful; otherwise false.</returns>
        public async Task<bool> ValidateToken(TargetUri targetUri, Token token)
        {
            Debug.Assert(targetUri != null && targetUri.IsAbsoluteUri, "The targetUri parameter is null or invalid");
            Debug.Assert(token != null && (token.Type == TokenType.Access || token.Type == TokenType.Federated), "The token parameter is null or invalid");

            Trace.WriteLine("VstsAzureAuthority::ValidateToken");

            // personal access tokens are effectively credentials, treat them as such
            if (token.Type == TokenType.Personal)
                return await this.ValidateCredentials(targetUri, (Credential)token);

            try
            {
                // create an request to the VSTS deployment data end-point
                HttpWebRequest request = GetConnectionDataRequest(targetUri, token);

                Trace.WriteLine("   validating token against " + request.Host);

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

        private StringContent GetAccessTokenRequestBody(TargetUri targetUri, Token accessToken, VstsTokenScope tokenScope)
        {
            const string ContentJsonFormat = "{{ \"scope\" : \"{0}\", \"targetAccounts\" : [\"{1}\"], \"displayName\" : \"Git: {2} on {3}\" }}";
            const string HttpJsonContentType = "application/json";

            Debug.Assert(accessToken != null && (accessToken.Type == TokenType.Access || accessToken.Type == TokenType.Federated), "The accessToken parameter is null or invalid");
            Debug.Assert(tokenScope != null, "The tokenScope parameter is null");

            Trace.WriteLine("   creating access token scoped to '" + tokenScope + "' for '" + accessToken.TargetIdentity + "'");

            string jsonContent = String.Format(ContentJsonFormat, tokenScope, accessToken.TargetIdentity, targetUri, Environment.MachineName);
            StringContent content = new StringContent(jsonContent, Encoding.UTF8, HttpJsonContentType);

            return content;
        }

        private HttpWebRequest GetConnectionDataRequest(TargetUri targetUri, Credential credentials)
        {
            const string BasicPrefix = "Basic ";
            const string UsernamePasswordFormat = "{0}:{1}";

            Debug.Assert(targetUri != null && targetUri.IsAbsoluteUri, "The targetUri parameter is null or invalid");
            Debug.Assert(credentials != null, "The credentials parameter is null or invalid");

            // create an request to the VSTS deployment data end-point
            HttpWebRequest request = GetConnectionDataRequest(targetUri);

            // credentials are packed into the 'Authorization' header as a base64 encoded pair
            string credPair = String.Format(UsernamePasswordFormat, credentials.Username, credentials.Password);
            byte[] credBytes = Encoding.ASCII.GetBytes(credPair);
            string base64enc = Convert.ToBase64String(credBytes);
            string basicAuthHeader = BasicPrefix + base64enc;
            request.Headers.Add(HttpRequestHeader.Authorization, basicAuthHeader);

            return request;
        }

        private HttpWebRequest GetConnectionDataRequest(TargetUri targetUri, Token token)
        {
            const string BearerPrefix = "Bearer ";

            Debug.Assert(targetUri != null && targetUri.IsAbsoluteUri, "The targetUri parameter is null or invalid");
            Debug.Assert(token != null && (token.Type == TokenType.Access || token.Type == TokenType.Federated), "The token parameter is null or invalid");

            Trace.WriteLine("VstsAzureAuthority::GetConnectionDataRequest");

            // create an request to the VSTS deployment data end-point
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

        private HttpWebRequest GetConnectionDataRequest(TargetUri targetUri)
        {
            const string VstsValidationUrlFormat = "{0}://{1}/_apis/connectiondata";

            Debug.Assert(targetUri != null && targetUri.IsAbsoluteUri, "The targetUri parameter is null or invalid");

            // create a url to the connection data end-point, it's deployment level and "always on".
            string validationUrl = String.Format(VstsValidationUrlFormat, targetUri.Scheme, targetUri.DnsSafeHost);

            // start building the request, only supports GET
            HttpWebRequest request = WebRequest.CreateHttp(validationUrl);
            request.Timeout = RequestTimeout;

            return request;
        }

        private bool TryCreateRequestUri(TargetUri targetUri, bool requireCompactToken, out Uri requestUri)
        {
            const string TokenAuthHostFormat = "app.vssps.{0}";
            const string SessionTokenUrl = "https://" + TokenAuthHostFormat + "/_apis/token/sessiontokens?api-version=1.0";
            const string CompactTokenUrl = SessionTokenUrl + "&tokentype=compact";

            Debug.Assert(targetUri != null, $"The `targetUri` parameter is null.");

            requestUri = null;

            if (targetUri == null)
                return false;

            // the host name can be something like foo.visualstudio.com in which case we
            // need the "foo." prefix removed.
            string host = targetUri.Host;
            int first = targetUri.Host.IndexOf('.');
            int last = targetUri.Host.LastIndexOf('.');

            // since the first and last index of '.' do not agree, substring after the first
            if (first != last)
            {
                host = targetUri.Host.Substring(first + 1);
            }

            host = requireCompactToken
                ? String.Format(SessionTokenUrl, host)
                : String.Format(CompactTokenUrl, host);

            return Uri.TryCreate(host, UriKind.Absolute, out requestUri);
        }
    }
}
