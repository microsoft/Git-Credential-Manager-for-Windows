/**** Git Credential Manager for Windows ****
 *
 * Copyright (c) Microsoft Corporation
 * All rights reserved.
 *
 * MIT License
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the """"Software""""), to deal
 * in the Software without restriction, including without limitation the rights to
 * use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
 * the Software, and to permit persons to whom the Software is furnished to do so,
 * subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED *AS IS*, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
 * FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
 * COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN
 * AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
 * WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE."
**/

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
        public VstsAzureAuthority(RuntimeContext context, string authorityHostUrl = null)
            : base(context)
        {
            AuthorityHostUrl = authorityHostUrl ?? AuthorityHostUrl;
        }

        /// <summary>
        /// Generates a personal access token for use with Visual Studio Team Services.
        /// <para/>
        /// Returns the acquired token if successful; otherwise <see langword="null"/>;
        /// </summary>
        /// <param name="targetUri">The uniform resource indicator of the resource access tokens are being requested for.</param>
        /// <param name="accessToken">Access token granted by the identity authority (Azure).</param>
        /// <param name="tokenScope">The requested access scopes to be granted to the token.</param>
        /// <param name="requireCompactToken">`<see langword="true"/>` if requesting a compact format token; otherwise `<see langword="false"/>`.</param>
        /// <param name="tokenDuration">
        /// The requested lifetime of the requested token.
        /// <para/>
        /// The authority granting the token decides the actual lifetime of any token granted, regardless of the duration requested.
        /// </param>
        public async Task<Token> GeneratePersonalAccessToken(TargetUri targetUri, Token accessToken, VstsTokenScope tokenScope, bool requireCompactToken, TimeSpan? tokenDuration = null)
        {
            BaseSecureStore.ValidateTargetUri(targetUri);
            BaseSecureStore.ValidateToken(accessToken);
            if (tokenScope is null)
                throw new ArgumentNullException(nameof(tokenScope));

            try
            {
                using (HttpClient httpClient = CreateHttpClient(targetUri, accessToken))
                {
                    if (await PopulateTokenTargetId(targetUri, accessToken))
                    {
                        Uri requestUri = await CreatePersonalAccessTokenRequestUri(httpClient, targetUri, requireCompactToken);

                        using (StringContent content = GetAccessTokenRequestBody(targetUri, accessToken, tokenScope, tokenDuration))
                        using (HttpResponseMessage response = await httpClient.PostAsync(requestUri, content))
                        {
                            if (response.IsSuccessStatusCode)
                            {
                                string responseText = await response.Content.ReadAsStringAsync();

                                if (!string.IsNullOrWhiteSpace(responseText))
                                {
                                    // Find the 'token : <value>' portion of the result content, if any.
                                    Match tokenMatch = null;
                                    if ((tokenMatch = Regex.Match(responseText, @"\s*""token""\s*:\s*""([^\""]+)""\s*", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)).Success)
                                    {
                                        string tokenValue = tokenMatch.Groups[1].Value;
                                        Token token = new Token(tokenValue, TokenType.Personal);

                                        Trace.WriteLine($"personal access token acquisition for '{targetUri}' succeeded.");

                                        return token;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Trace.WriteLine($"! an error occurred: {e.Message}");
            }

            Trace.WriteLine($"personal access token acquisition for '{targetUri}' failed.");

            return null;
        }

        public async Task<bool> PopulateTokenTargetId(TargetUri targetUri, Token accessToken)
        {
            BaseSecureStore.ValidateTargetUri(targetUri);
            BaseSecureStore.ValidateToken(accessToken);

            string resultId = null;
            Guid instanceId;

            try
            {
                // Create an request to the VSTS deployment data end-point.
                HttpWebRequest request = GetConnectionDataRequest(targetUri, accessToken);

                Trace.WriteLine($"access token end-point is '{request.Method}' '{request.RequestUri}'.");

                // Send the request and wait for the response.
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
                Trace.WriteLine($"server returned '{webException.Status}'.");
            }

            if (Guid.TryParse(resultId, out instanceId))
            {
                Trace.WriteLine($"target identity is {resultId}.");
                accessToken.TargetIdentity = instanceId;

                return true;
            }

            return false;
        }

        /// <summary>
        /// Validates that a `<see cref="Credential"/>` is valid to grant access to the VSTS resource referenced by `<paramref name="targetUri"/>`.
        /// <para/>
        /// Returns `<see langword="true"/>` if successful; otherwise `<see langword="false"/>`.
        /// </summary>
        /// <param name="targetUri">URI of the VSTS resource.</param>
        /// <param name="credentials">`<see cref="Credential"/>` expected to grant access to the VSTS service.</param>
        public async Task<bool> ValidateCredentials(TargetUri targetUri, Credential credentials)
        {
            BaseSecureStore.ValidateTargetUri(targetUri);
            BaseSecureStore.ValidateCredential(credentials);

            try
            {
                // Create an request to the VSTS deployment data end-point.
                HttpWebRequest request = GetConnectionDataRequest(targetUri, credentials);

                Trace.WriteLine($"validating credentials against '{request.RequestUri}'.");

                // Send the request and wait for the response.
                using (HttpWebResponse response = await request.GetResponseAsync() as HttpWebResponse)
                {
                    // We're looking for 'OK 200' here, anything else is failure
                    Trace.WriteLine($"server returned: ({response.StatusCode}).");
                    return response.StatusCode == HttpStatusCode.OK;
                }
            }
            catch (WebException webException)
            {
                // Avoid invalidation credentials based on what is likely a networking problem.
                switch (webException.Status)
                {
                    case WebExceptionStatus.ConnectFailure:
                    case WebExceptionStatus.ConnectionClosed:
                    case WebExceptionStatus.NameResolutionFailure:
                    case WebExceptionStatus.ProxyNameResolutionFailure:
                    case WebExceptionStatus.ReceiveFailure:
                    case WebExceptionStatus.RequestCanceled:
                    case WebExceptionStatus.RequestProhibitedByCachePolicy:
                    case WebExceptionStatus.RequestProhibitedByProxy:
                    case WebExceptionStatus.SecureChannelFailure:
                    case WebExceptionStatus.SendFailure:
                    case WebExceptionStatus.TrustFailure:
                        {
                            Trace.WriteLine($"unable to validate credentials due to '{webException.Status}'.");

                            return true;
                        }
                }

                // Even if the service responded, if the issue isn't a 400 class response then
                // the credentials were likely not rejected.
                if (webException.Response is HttpWebResponse response)
                {
                    int statusCode = (int)response.StatusCode;

                    if (statusCode < 400 && statusCode >= 500)
                    {
                        Trace.WriteLine($"server returned: ({statusCode}).");

                        return true;
                    }
                }

                Trace.WriteLine($"server returned: '{webException.Message}.");
            }
            catch
            {
                Trace.WriteLine("! unexpected error");
            }

            Trace.WriteLine($"credential validation for '{targetUri}' failed.");
            return false;
        }

        /// <summary>
        /// Validates that a `<see cref="Token"/>` is valid to grant access to the VSTS resource referenced by `<paramref name="targetUri"/>`.
        /// <para/>
        /// Returns `<see langword="true"/>` if successful; otherwise `<see langword="false"/>`.
        /// </summary>
        /// <param name="targetUri">URI of the VSTS resource.</param>
        /// <param name="token">`<see cref="Token"/>` expected to grant access to the VSTS resource.</param>
        public async Task<bool> ValidateToken(TargetUri targetUri, Token token)
        {
            BaseSecureStore.ValidateTargetUri(targetUri);
            BaseSecureStore.ValidateToken(token);

            // Personal access tokens are effectively credentials, treat them as such.
            if (token.Type == TokenType.Personal)
                return await ValidateCredentials(targetUri, (Credential)token);

            try
            {
                // Create an request to the VSTS deployment data end-point.
                HttpWebRequest request = GetConnectionDataRequest(targetUri, token);

                Trace.WriteLine($"validating token against '{request.Host}'.");

                // Send the request and wait for the response.
                using (HttpWebResponse response = await request.GetResponseAsync() as HttpWebResponse)
                {
                    // We're looking for 'OK 200' here, anything else is failure.
                    Trace.WriteLine($"server returned: '{response.StatusCode}'.");
                    return response.StatusCode == HttpStatusCode.OK;
                }
            }
            catch (WebException webException)
            {
                Trace.WriteLine($"! server returned: '{webException.Message}'.");
            }
            catch
            {
                Trace.WriteLine("! unexpected error");
            }

            Trace.WriteLine($"token validation for '{targetUri}' failed.");
            return false;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
        internal static HttpClient CreateHttpClient(TargetUri targetUri, Token accessToken)
        {
            const string AccessTokenHeader = "Bearer";
            const string FederatedTokenHeader = "Cookie";

            Debug.Assert(targetUri != null, $"The `{nameof(targetUri)}` parameter is null.");
            Debug.Assert(accessToken != null && !string.IsNullOrWhiteSpace(accessToken.Value), $"The `{nameof(accessToken)}' is null or invalid.");

            HttpClient httpClient = CreateHttpClient(targetUri);

            switch (accessToken.Type)
            {
                case TokenType.Access:
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(AccessTokenHeader, accessToken.Value);
                    break;

                case TokenType.Federated:
                    httpClient.DefaultRequestHeaders.Add(FederatedTokenHeader, accessToken.Value);
                    break;

                default:
                    return null;
            }

            return httpClient;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
        internal static HttpClient CreateHttpClient(TargetUri targetUri, Credential credentials)
        {
            const string CredentialHeader = "Basic";

            Debug.Assert(targetUri != null, $"The `{nameof(targetUri)}` parameter is null.");

            HttpClient httpClient = CreateHttpClient(targetUri);

            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(CredentialHeader, GetBase64EncodedCredentials(credentials));

            return httpClient;
        }

        internal HttpWebRequest GetConnectionDataRequest(Uri uri, Token token)
        {
            const string BearerPrefix = "Bearer ";

            Debug.Assert(uri != null && uri.IsAbsoluteUri, $"The `{nameof(uri)}` parameter is null or invalid");
            Debug.Assert(token != null && (token.Type == TokenType.Access || token.Type == TokenType.Federated), $"The `{nameof(token)}` parameter is null or invalid");

            // Create an request to the VSTS deployment data end-point.
            HttpWebRequest request = GetConnectionDataRequest(uri);

            // Different types of tokens are packed differently.
            switch (token.Type)
            {
                case TokenType.Access:
                    Trace.WriteLine($"validating ADAL access token for '{uri}'.");

                    // ADAL access tokens are packed into the Authorization header.
                    string sessionAuthHeader = BearerPrefix + token.Value;
                    request.Headers.Add(HttpRequestHeader.Authorization, sessionAuthHeader);
                    break;

                case TokenType.Federated:
                    Trace.WriteLine($"validating federated authentication token for '{uri}'.");

                    // Federated authentication tokens are sent as cookie(s).
                    request.Headers.Add(HttpRequestHeader.Cookie, token.Value);
                    break;

                default:
                    Trace.WriteLine("! unsupported token type.");
                    break;
            }

            return request;
        }

        internal static HttpWebRequest GetConnectionDataRequest(TargetUri targetUri, Credential credentials)
        {
            Debug.Assert(targetUri != null && targetUri.IsAbsoluteUri, "The targetUri parameter is null or invalid");
            Debug.Assert(credentials != null, "The credentials parameter is null or invalid");

            // Create an request to the VSTS deployment data end-point.
            HttpWebRequest request = GetConnectionDataRequest(targetUri);

            // Credentials are packed into the 'Authorization' header as a base64 encoded pair.
            string basicAuthHeader = GetBasicAuthorizationHeader(credentials);
            request.Headers.Add(HttpRequestHeader.Authorization, basicAuthHeader);

            return request;
        }

        internal static HttpWebRequest GetConnectionDataRequest(TargetUri targetUri)
        {
            const string VstsValidationUrlFormat = "{0}://{1}/_apis/connectiondata";

            Debug.Assert(targetUri != null && targetUri.IsAbsoluteUri, "The targetUri parameter is null or invalid");

            // Create a URL to the connection data end-point, it's deployment level and "always on".
            string validationUrl = string.Format(System.Globalization.CultureInfo.InvariantCulture, VstsValidationUrlFormat, targetUri.Scheme, targetUri.DnsSafeHost);

            // Start building the request, only supports GET.
            HttpWebRequest request = WebRequest.CreateHttp(validationUrl);
            request.Timeout = Global.RequestTimeout;
            request.UserAgent = Global.UserAgent;
            request.MaximumAutomaticRedirections = Global.MaxAutomaticRedirections;

            return request;
        }

        internal static async Task<Uri> GetIdentityServiceUri(HttpClient client, TargetUri targetUri)
        {
            const string LocationServiceUrlFormat = "https://{0}/_apis/ServiceDefinitions/LocationService2/951917AC-A960-4999-8464-E3F0AA25B381?api-version=1.0";

            Debug.Assert(client != null, $"The `{nameof(client)}` parameter is null.");
            Debug.Assert(targetUri != null && targetUri.IsAbsoluteUri, $"The `{nameof(targetUri)}` parameter is null or invalid");

            string locationServiceUrl = string.Format(System.Globalization.CultureInfo.InvariantCulture, LocationServiceUrlFormat, targetUri.Host);
            Uri idenitityServiceUri = null;

            using (HttpResponseMessage response = await client.GetAsync(locationServiceUrl))
            {
                if (response.IsSuccessStatusCode)
                {
                    using (HttpContent content = response.Content)
                    {
                        string responseText = await content.ReadAsStringAsync();

                        Match match;
                        if ((match = Regex.Match(responseText, @"\""location\""\:\""([^\""]+)\""", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)).Success)
                        {
                            string identityServiceUrl = match.Groups[1].Value;
                            idenitityServiceUri = new Uri(identityServiceUrl, UriKind.Absolute);
                        }
                    }
                }
            }

            return idenitityServiceUri;
        }

        private StringContent GetAccessTokenRequestBody(TargetUri targetUri, Token accessToken, VstsTokenScope tokenScope, TimeSpan? duration = null)
        {
            const string ContentBasicJsonFormat = "{{ \"scope\" : \"{0}\", \"targetAccounts\" : [\"{1}\"], \"displayName\" : \"Git: {2} on {3}\" }}";
            const string ContentTimedJsonFormat = "{{ \"scope\" : \"{0}\", \"targetAccounts\" : [\"{1}\"], \"displayName\" : \"Git: {2} on {3}\", \"validTo\": \"{4:u}\" }}";
            const string HttpJsonContentType = "application/json";

            if (accessToken is null)
                throw new ArgumentNullException(nameof(accessToken));
            if (tokenScope is null)
                throw new ArgumentNullException(nameof(tokenScope));

            Trace.WriteLine($"creating access token scoped to '{tokenScope}' for '{accessToken.TargetIdentity}'");

            string jsonContent = (duration.HasValue && duration.Value > TimeSpan.FromHours(1))
                ? string.Format(ContentTimedJsonFormat, tokenScope, accessToken.TargetIdentity, targetUri, Environment.MachineName, DateTime.UtcNow + duration.Value)
                : string.Format(ContentBasicJsonFormat, tokenScope, accessToken.TargetIdentity, targetUri, Environment.MachineName);
            StringContent content = new StringContent(jsonContent, Encoding.UTF8, HttpJsonContentType);

            return content;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
        private static HttpClient CreateHttpClient(TargetUri targetUri)
        {
            if (targetUri is null)
                throw new ArgumentNullException(nameof(targetUri));

            HttpClient httpClient = new HttpClient(targetUri.HttpClientHandler)
            {
                Timeout = TimeSpan.FromMilliseconds(Global.RequestTimeout),
            };

            httpClient.DefaultRequestHeaders.Add("User-Agent", Global.UserAgent);

            return httpClient;
        }

        private static async Task<Uri> CreatePersonalAccessTokenRequestUri(HttpClient client, TargetUri targetUri, bool requireCompactToken)
        {
            const string SessionTokenUrl = "_apis/token/sessiontokens?api-version=1.0";
            const string CompactTokenUrl = SessionTokenUrl + "&tokentype=compact";

            if (client == null)
                throw new ArgumentNullException(nameof(client));

            BaseSecureStore.ValidateTargetUri(targetUri);

            Uri idenityServiceUri = await GetIdentityServiceUri(client, targetUri);

            if (idenityServiceUri == null)
                throw new VstsLocationServiceException($"Failed to find Identity Service for {targetUri}");

            string url = idenityServiceUri.ToString();

            url += requireCompactToken
                ? CompactTokenUrl
                : SessionTokenUrl;

            return new Uri(url, UriKind.Absolute);
        }

        private static string GetBase64EncodedCredentials(Credential credentials)
        {
            const string UsernamePasswordFormat = "{0}:{1}";

            if (credentials is null)
                throw new ArgumentNullException(nameof(credentials));

            string credPair = string.Format(UsernamePasswordFormat, credentials.Username, credentials.Password);
            byte[] credBytes = Encoding.ASCII.GetBytes(credPair);
            string base64enc = Convert.ToBase64String(credBytes);

            return base64enc;
        }

        private static string GetBasicAuthorizationHeader(Credential credentials)
        {
            const string BasicPrefix = "Basic ";

            if (credentials is null)
                throw new ArgumentNullException(nameof(credentials));

            // Credentials are packed into the 'Authorization' header as a base64 encoded pair.
            string base64enc = GetBase64EncodedCredentials(credentials);
            string basicAuthHeader = BasicPrefix + base64enc;

            return basicAuthHeader;
        }
    }
}
