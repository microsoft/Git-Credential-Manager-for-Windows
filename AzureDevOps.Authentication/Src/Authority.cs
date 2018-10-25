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
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Alm.Authentication;

using static System.StringComparer;
using Culture = System.Globalization.CultureInfo;

namespace AzureDevOps.Authentication
{
    /// <summary>
    /// Interfaces with Azure to perform authentication and identity services.
    /// </summary>
    internal class Authority : Base, IAuthority
    {
        /// <summary>
        /// The base URL for logon services in Azure.
        /// </summary>
        public const string AuthorityHostUrlBase = "https://login.microsoftonline.com";

        /// <summary>
        /// The root domain of Azure hosted repositories.
        /// </summary>
        public const string AzureBaseUrlHost = "azure.com";

        /// <summary>
        /// The common URL for logon services in Azure.
        /// </summary>
        public const string DefaultAuthorityHostUrl = AuthorityHostUrlBase + "/common";

        /// <summary>
        /// The root domain of Azure DevOps hosted repositories.
        /// </summary>
        public const string VstsBaseUrlHost = "visualstudio.com";

        /// <summary>
        /// Creates a new instance of `<see cref="Authority"/>`.
        /// </summary>
        /// <param name="authorityHostUrl">A non-default authority host URL; otherwise defaults to `<see cref="DefaultAuthorityHostUrl"/>`.</param>
        public Authority(RuntimeContext context, string authorityHostUrl)
            : base(context)
        {
            if (string.IsNullOrEmpty(authorityHostUrl))
                throw new ArgumentNullException(nameof(authorityHostUrl));
            if (!Uri.IsWellFormedUriString(authorityHostUrl, UriKind.Absolute))
            {
                var inner = new UriFormatException("Authority host URL must be absolute.");
                throw new ArgumentException(inner.Message, nameof(authorityHostUrl), inner);
            }

            AuthorityHostUrl = authorityHostUrl;
        }

        public Authority(RuntimeContext context)
            : this(context, DefaultAuthorityHostUrl)
        { }

        /// <summary>
        /// The URL used to interact with the Azure identity service.
        /// </summary>
        public string AuthorityHostUrl { get; protected set; }

        public async Task<Token> GeneratePersonalAccessToken(TargetUri targetUri, Token authorization, TokenScope tokenScope, bool requireCompactToken, TimeSpan? tokenDuration)
        {
            if (targetUri is null)
                throw new ArgumentNullException(nameof(targetUri));
            if (authorization is null)
                throw new ArgumentNullException(nameof(authorization));
            if (tokenScope is null)
                throw new ArgumentNullException(nameof(tokenScope));

            try
            {
                var requestUri = await CreatePersonalAccessTokenRequestUri(targetUri, authorization, requireCompactToken);
                var options = new NetworkRequestOptions(true)
                {
                    Authorization = authorization,
                };

                using (StringContent content = GetAccessTokenRequestBody(targetUri, tokenScope, tokenDuration))
                using (var response = await Network.HttpPostAsync(requestUri, content, options))
                {
                    if (response.IsSuccessStatusCode)
                    {
                        string responseText = response.Content.AsString;

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

                    Trace.WriteLine($"failed to acquire personal access token for '{targetUri}' [{(int)response.StatusCode}].");
                }
            }
            catch (Exception exception)
            {
                Trace.WriteException(exception);
            }

            Trace.WriteLine($"personal access token acquisition for '{targetUri}' failed.");

            return null;
        }

        public Task<Token> GeneratePersonalAccessToken(TargetUri targetUri, Token authorization, TokenScope tokenScope, bool requireCompactToken)
            => GeneratePersonalAccessToken(targetUri, authorization, tokenScope, requireCompactToken, null);

        /// <summary>
        /// Returns the properly formatted URL for the Azure authority given a tenant identity.
        /// </summary>
        /// <param name="tenantId">Identity of the tenant.</param>
        public static string GetAuthorityUrl(Guid tenantId)
        {
            return string.Format("{0}/{1:D}", AuthorityHostUrlBase, tenantId);
        }

        /// <summary>
        /// Acquires a <see cref="Token"/> from the authority via an interactive user logon prompt.
        /// <para/>
        /// Returns a `<see cref="Token"/>` is successful; otherwise <see langword="null"/>.
        /// </summary>
        /// <param name="targetUri">Uniform resource indicator of the resource access tokens are being requested for.</param>
        /// <param name="clientId">Identifier of the client requesting the token.</param>
        /// <param name="resource">Identifier of the target resource that is the recipient of the requested token.</param>
        /// <param name="redirectUri">Address to return to upon receiving a response from the authority.</param>
        /// <param name="queryParameters">optional value, appended as-is to the query string in the HTTP authentication request to the authority.</param>
        public async Task<Token> InteractiveAcquireToken(TargetUri targetUri, string clientId, string resource, Uri redirectUri, string queryParameters = null)
        {
            if (targetUri is null)
                throw new ArgumentNullException(nameof(targetUri));
            if (string.IsNullOrWhiteSpace(clientId))
                throw new ArgumentNullException(nameof(clientId));
            if (string.IsNullOrWhiteSpace(resource))
                throw new ArgumentNullException(nameof(resource));
            if (redirectUri is null)
                throw new ArgumentNullException(nameof(redirectUri));
            if (!redirectUri.IsAbsoluteUri)
                throw new ArgumentException(nameof(redirectUri));

            Token token = null;
            queryParameters = queryParameters ?? string.Empty;

            try
            {
                var authResult = await Adal.AcquireTokenAsync(AuthorityHostUrl,
                                                              resource,
                                                              clientId,
                                                              redirectUri,
                                                              queryParameters);

                if (Guid.TryParse(authResult.TenantId, out Guid tenantId))
                {
                    token = new Token(authResult.AccessToken, tenantId, TokenType.AzureAccess);
                }

                Trace.WriteLine($"authority host URL = '{AuthorityHostUrl}', token acquisition for tenant [{tenantId.ToString("N")}] succeeded.");
            }
            catch (AuthenticationException)
            {
                Trace.WriteLine($"authority host URL = '{AuthorityHostUrl}', token acquisition failed.");
            }

            return token;
        }

        /// <summary>
        /// Acquires a `<see cref="Token"/>` from the authority via an non-interactive user logon.
        /// <para/>
        /// Returns the acquired `<see cref="Token"/>` if successful; otherwise `<see langword="null"/>`.
        /// </summary>
        /// <param name="targetUri">Uniform resource indicator of the resource access tokens are being requested for.</param>
        /// <param name="clientId">Identifier of the client requesting the token.</param>
        /// <param name="resource">Identifier of the target resource that is the recipient of the requested token.</param>
        /// <param name="redirectUri">Address to return to upon receiving a response from the authority.</param>
        public async Task<Token> NoninteractiveAcquireToken(TargetUri targetUri, string clientId, string resource, Uri redirectUri)
        {
            if (targetUri is null)
                throw new ArgumentNullException(nameof(targetUri));
            if (string.IsNullOrWhiteSpace(clientId))
                throw new ArgumentNullException(nameof(clientId));
            if (string.IsNullOrWhiteSpace(resource))
                throw new ArgumentNullException(nameof(resource));
            if (redirectUri is null)
                throw new ArgumentNullException(nameof(redirectUri));
            if (!redirectUri.IsAbsoluteUri)
            {
                var inner = new UriFormatException("Uri is not absolute when an absolute Uri is required.");
                throw new ArgumentException(inner.Message, nameof(redirectUri), inner);
            }

            Token token = null;

            try
            {
                var authResult = await Adal.AcquireTokenAsync(AuthorityHostUrl,
                                                              resource,
                                                              clientId);

                if (Guid.TryParse(authResult.TenantId, out Guid tentantId))
                {
                    token = new Token(authResult.AccessToken, tentantId, TokenType.AzureAccess);

                    Trace.WriteLine($"token acquisition for authority host URL = '{AuthorityHostUrl}' succeeded.");
                }
            }
            catch (AuthenticationException)
            {
                Trace.WriteLine($"token acquisition for authority host URL = '{AuthorityHostUrl}' failed.");
            }

            return token;
        }

        public async Task<bool> PopulateTokenTargetId(TargetUri targetUri, Token authorization)
        {
            if (targetUri is null)
                throw new ArgumentNullException(nameof(targetUri));
            if (authorization is null)
                throw new ArgumentNullException(nameof(authorization));

            try
            {
                // Create an request to the Azure DevOps deployment data end-point.
                var requestUri = GetConnectionDataUri(targetUri);
                var options = new NetworkRequestOptions(true)
                {
                    Authorization = authorization,
                };

                // Send the request and wait for the response.
                using (var response = await Network.HttpGetAsync(requestUri, options))
                {
                    if (response.IsSuccessStatusCode)
                    {
                        string content = response.Content.AsString;
                        Match match;

                        if ((match = Regex.Match(content, @"""instanceId""\s*\:\s*""([^""]+)""", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)).Success
                            && match.Groups.Count == 2)
                        {
                            string resultId = match.Groups[1].Value;

                            if (Guid.TryParse(resultId, out Guid instanceId))
                            {
                                Trace.WriteLine($"target identity is '{resultId}'.");
                                authorization.TargetIdentity = instanceId;

                                return true;
                            }
                        }
                    }

                    Trace.WriteLine($"failed to acquire the token's target identity for `{requestUri?.QueryUri}` [{(int)response.StatusCode}].");
                }
            }
            catch (HttpRequestException exception)
            {
                Trace.WriteLine("failed to acquire the token's target identity for `{targetUri}`, an error happened before the server could respond.");
                Trace.WriteException(exception);
            }

            return false;
        }

        public async Task<bool> ValidateCredentials(TargetUri targetUri, Credential credentials)
        {
            if (targetUri is null)
                throw new ArgumentNullException(nameof(targetUri));

            return await ValidateSecret(targetUri, credentials);
        }

        public async Task<bool> ValidateToken(TargetUri targetUri, Token token)
        {
            if (targetUri is null)
                throw new ArgumentNullException(nameof(targetUri));

            if (token is null)
                return false;

            // Personal access tokens are effectively credentials, treat them as such.
            return (token.Type == TokenType.Personal)
                ? await ValidateSecret(targetUri, (Credential)token)
                : await ValidateSecret(targetUri, token);
        }

        internal static TargetUri GetConnectionDataUri(TargetUri targetUri)
        {
            const string AzureDevOpsValidationUrlPath = "_apis/connectiondata";

            if (targetUri is null)
                throw new ArgumentNullException(nameof(targetUri));

            // Create a URL to the connection data end-point, it's deployment level and "always on".
            string requestUrl = GetTargetUrl(targetUri, false);
            string validationUrl = requestUrl + AzureDevOpsValidationUrlPath;

            return targetUri.CreateWith(validationUrl);
        }

        internal async Task<TargetUri> GetIdentityServiceUri(TargetUri targetUri, Secret authorization)
        {
            const string LocationServiceUrlPathAndQuery = "_apis/ServiceDefinitions/LocationService2/951917AC-A960-4999-8464-E3F0AA25B381?api-version=1.0";

            if (targetUri is null)
                throw new ArgumentNullException(nameof(targetUri));
            if (authorization is null)
                throw new ArgumentNullException(nameof(authorization));

            string tenantUrl = GetTargetUrl(targetUri, false);
            var locationServiceUrl = tenantUrl + LocationServiceUrlPathAndQuery;
            var requestUri = targetUri.CreateWith(queryUrl: locationServiceUrl);
            var options = new NetworkRequestOptions(true)
            {
                Authorization = authorization,
            };

            try
            {
                using (var response = await Network.HttpGetAsync(requestUri, options))
                {
                    if (response.IsSuccessStatusCode)
                    {
                        string responseText = response.Content.AsString;

                        Match match;
                        if ((match = Regex.Match(responseText, @"\""location\""\:\""([^\""]+)\""", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)).Success)
                        {
                            string identityServiceUrl = match.Groups[1].Value;
                            var idenitityServiceUri = new Uri(identityServiceUrl, UriKind.Absolute);

                            return targetUri.CreateWith(idenitityServiceUri);
                        }
                    }

                    Trace.WriteLine($"failed to find Identity Service for '{requestUri?.QueryUri}' via location service [{(int)response.StatusCode}].");
                }
            }
            catch (Exception exception)
            {
                Trace.WriteException(exception);
                throw new LocationServiceException($"Helper for `{requestUri?.QueryUri}`.", exception);
            }

            return null;
        }

        /// <summary>
        /// Returns the properly formatted URL for the Azure authority given a tenant identity.
        /// </summary>
        /// <param name="tenantId">Identity of the tenant.</param>
        internal static string GetTargetUrl(TargetUri targetUri, bool keepUsername)
        {
            if (targetUri is null)
                throw new ArgumentNullException(nameof(targetUri));

            string requestUrl = targetUri.ToString(keepUsername, true, false);

            if (targetUri.Host.EndsWith(AzureBaseUrlHost, StringComparison.OrdinalIgnoreCase))
            {
                // Handle the Azure design where URL path is required to discover the authority.
                if (targetUri.ActualUri != null
                    && targetUri.ActualUri.IsAbsoluteUri
                    && targetUri.ActualUri.AbsolutePath.Length > 1)
                {
                    // Use the first segment of the actual URL to build the URL necessary to discover the authority.
                    requestUrl = requestUrl + targetUri.ActualUri.Segments[1];
                }
                // Handle the generic Azure {username}@{host} -> {host}/{username} transformation.
                else if (targetUri.ContainsUserInfo)
                {
                    string escapedUserInfo = Uri.EscapeUriString(targetUri.UserInfo);

                    requestUrl = $"{requestUrl}{escapedUserInfo}/";
                }
            }

            return requestUrl;
        }

        internal static bool IsAzureDevOpsUrl(TargetUri targetUri)
        {
            return (OrdinalIgnoreCase.Equals(targetUri.Scheme, Uri.UriSchemeHttp)
                    || OrdinalIgnoreCase.Equals(targetUri.Scheme, Uri.UriSchemeHttps))
                && (targetUri.DnsSafeHost.EndsWith(VstsBaseUrlHost, StringComparison.OrdinalIgnoreCase)
                    || targetUri.DnsSafeHost.EndsWith(AzureBaseUrlHost, StringComparison.OrdinalIgnoreCase));
        }

        internal async Task<bool> ValidateSecret(TargetUri targetUri, Secret secret)
        {
            const string AnonymousUserPattern = @"""properties""\s*:\s*{\s*""Account""\s*:\s*{\s*""\$type""\s*:\s*""System.String""\s*,\s*""\$value""\s*:\s*""Anonymous""}\s*}";

            if (targetUri is null)
                throw new ArgumentNullException(nameof(targetUri));

            if (secret is null)
                return false;

            // Create an request to the Azure DevOps deployment data end-point.
            var requestUri = GetConnectionDataUri(targetUri);
            var options = new NetworkRequestOptions(true)
            {
                Authorization = secret,
                CookieContainer = new CookieContainer(),
            };

            try
            {
                // Send the request and wait for the response.
                using (var response = await Network.HttpGetAsync(requestUri, options))
                {
                    HttpStatusCode statusCode = response.StatusCode;
                    string content = response?.Content?.AsString;

                    // If the server responds with content, and said content matches the anonymous details the credentials are invalid.
                    if (content != null && Regex.IsMatch(content, AnonymousUserPattern, RegexOptions.CultureInvariant | RegexOptions.IgnoreCase))
                    {
                        Trace.WriteLine($"credential validation for '{requestUri?.QueryUri}' failed.");

                        return false;
                    }

                    // If the service responded with a 2XX status code the credentials are valid.
                    if (statusCode >= HttpStatusCode.OK && statusCode < HttpStatusCode.Ambiguous)
                        return true;

                    Trace.WriteLine($"credential validation for '{requestUri?.QueryUri}' failed [{(int)response.StatusCode}].");

                    // Even if the service responded, if the issue isn't a 400 class response then the credentials were likely not rejected.
                    if (statusCode < HttpStatusCode.BadRequest || statusCode >= HttpStatusCode.InternalServerError)
                        return true;
                }
            }
            catch (HttpRequestException exception)
            {
                // Since we're unable to invalidate the credentials, return optimistic results.
                // This avoid credential invalidation due to network instability, etc.
                Trace.WriteLine($"unable to validate credentials for '{requestUri?.QueryUri}', failure occurred before server could respond.");
                Trace.WriteException(exception);

                return true;
            }
            catch (Exception exception)
            {
                Trace.WriteLine($"credential validation for '{requestUri?.QueryUri}' failed.");
                Trace.WriteException(exception);

                return false;
            }

            Trace.WriteLine($"credential validation for '{requestUri?.QueryUri}' failed.");
            return false;
        }

        private async Task<TargetUri> CreatePersonalAccessTokenRequestUri(TargetUri targetUri, Secret authorization, bool requireCompactToken)
        {
            const string SessionTokenUrl = "_apis/token/sessiontokens?api-version=1.0";
            const string CompactTokenUrl = SessionTokenUrl + "&tokentype=compact";

            if (targetUri is null)
                throw new ArgumentNullException(nameof(targetUri));
            if (authorization is null)
                throw new ArgumentNullException(nameof(authorization));

            var idenityServiceUri = await GetIdentityServiceUri(targetUri, authorization);

            if (idenityServiceUri is null)
                throw new LocationServiceException($"Failed to find Identity Service for `{targetUri}`.");

            string url = idenityServiceUri.ToString();

            url += requireCompactToken
                ? CompactTokenUrl
                : SessionTokenUrl;

            var requestUri = new Uri(url, UriKind.Absolute);

            return targetUri.CreateWith(requestUri);
        }

        private StringContent GetAccessTokenRequestBody(TargetUri targetUri, TokenScope tokenScope, TimeSpan? duration = null)
        {
            const string ContentBasicJsonFormat = "{{ \"scope\" : \"{0}\", \"displayName\" : \"Git: {1} on {2}\" }}";
            const string ContentTimedJsonFormat = "{{ \"scope\" : \"{0}\", \"displayName\" : \"Git: {1} on {2}\", \"validTo\": \"{3:u}\" }}";
            const string HttpJsonContentType = "application/json";

            if (targetUri is null)
                throw new ArgumentNullException(nameof(targetUri));
            if (tokenScope is null)
                throw new ArgumentNullException(nameof(tokenScope));

            string tokenUrl = GetTargetUrl(targetUri, false);

            Trace.WriteLine($"creating access token scoped to '{tokenScope}' for '{tokenUrl}'");

            string jsonContent = (duration.HasValue && duration.Value > TimeSpan.FromHours(1))
                ? string.Format(Culture.InvariantCulture, ContentTimedJsonFormat, tokenScope, tokenUrl, Settings.MachineName, DateTime.UtcNow + duration.Value)
                : string.Format(Culture.InvariantCulture, ContentBasicJsonFormat, tokenScope, tokenUrl, Settings.MachineName);
            var content = new StringContent(jsonContent, Encoding.UTF8, HttpJsonContentType);

            return content;
        }
    }
}
