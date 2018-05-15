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
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static System.Globalization.CultureInfo;

namespace Microsoft.Alm.Authentication
{
    internal class VstsAzureAuthority : AzureAuthority, IVstsAuthority
    {
        public const string AzureBaseUrlHost = "azure.com";
        public const string VstsBaseUrlHost = "visualstudio.com";

        public VstsAzureAuthority(RuntimeContext context, string authorityHostUrl)
            : base(context)
        {
            AuthorityHostUrl = authorityHostUrl ?? AuthorityHostUrl;
        }

        public VstsAzureAuthority(RuntimeContext context)
            : this(context, null)
        { }

        public async Task<Token> GeneratePersonalAccessToken(TargetUri targetUri, Token authorization, VstsTokenScope tokenScope, bool requireCompactToken, TimeSpan? tokenDuration = null)
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

                    Trace.WriteLine($"failed to acquire personal access token for '{targetUri}' [{(int)response.StatusCode} {response.ReasonPhrase}].");
                }
            }
            catch (Exception exception)
            {
                Trace.WriteException(exception);
            }

            Trace.WriteLine($"personal access token acquisition for '{targetUri}' failed.");

            return null;
        }

        public async Task<bool> PopulateTokenTargetId(TargetUri targetUri, Token authorization)
        {
            if (targetUri is null)
                throw new ArgumentNullException(nameof(targetUri));
            if (authorization is null)
                throw new ArgumentNullException(nameof(authorization));

            try
            {
                // Create an request to the VSTS deployment data end-point.
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
                        string content = await response.Content.ReadAsStringAsync();
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

                    Trace.WriteLine($"failed to acquire the token's target identity for `{targetUri}` [{(int)response.StatusCode} {response.ReasonPhrase}].");
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
            if (credentials is null)
                throw new ArgumentNullException(nameof(credentials));

            try
            {
                // Create an request to the VSTS deployment data end-point.
                var requestUri = GetConnectionDataUri(targetUri);
                var options = new NetworkRequestOptions(true)
                {
                    Authorization = credentials,
                };

                // Send the request and wait for the response.
                using (var response = await Network.HttpGetAsync(requestUri, options))
                {
                    if (response.IsSuccessStatusCode)
                        return true;

                    Trace.WriteLine($"credential validation for '{targetUri}' failed [{(int)response.StatusCode} {response.ReasonPhrase}].");

                    // Even if the service responded, if the issue isn't a 400 class response then
                    // the credentials were likely not rejected.
                    if ((int)response.StatusCode < 400 && (int)response.StatusCode >= 500)
                        return true;
                }
            }
            catch (Exception exception)
            {
                Trace.WriteException(exception);
            }

            Trace.WriteLine($"credential validation for '{targetUri}' failed.");
            return false;
        }

        public async Task<bool> ValidateToken(TargetUri targetUri, Token token)
        {
            if (targetUri is null)
                throw new ArgumentNullException(nameof(targetUri));
            if (token is null)
                throw new ArgumentNullException(nameof(token));

            // Personal access tokens are effectively credentials, treat them as such.
            if (token.Type == TokenType.Personal)
                return await ValidateCredentials(targetUri, (Credential)token);

            try
            {
                // Create an request to the VSTS deployment data end-point.
                var requestUri = GetConnectionDataUri(targetUri);
                var options = new NetworkRequestOptions(true)
                {
                    Authorization = token,
                };

                // Send the request and wait for the response.
                using (var response = await Network.HttpGetAsync(requestUri, options))
                {
                    if (!response.IsSuccessStatusCode)
                    {
                        // Even if the service responded, if the issue isn't a 400 class response then
                        // the credentials were likely not rejected.
                        if ((int)response.StatusCode < 400 && (int)response.StatusCode >= 500)
                        {
                            Trace.WriteLine($"unable to validate credentials for '{targetUri}', unexpected response [{(int)response.StatusCode} {response.ReasonPhrase}].");

                            return true;
                        }

                        Trace.WriteLine($"credential validation for '{targetUri}' failed [{(int)response.StatusCode} {response.ReasonPhrase}].");

                        return false;
                    }
                }
            }
            catch (HttpRequestException exception)
            {
                // Since we're unable to invalidate the credentials, return optimistic results.
                // This avoid credential invalidation due to network instability, etc.
                Trace.WriteLine($"unable to validate credentials for '{targetUri}', failure occurred before server could respond.");
                Trace.WriteException(exception);

                return true;
            }
            catch (Exception exception)
            {
                Trace.WriteException(exception);
            };

            Trace.WriteLine($"token validation for '{targetUri}' failed.");
            return false;
        }

        internal static TargetUri GetConnectionDataUri(TargetUri targetUri)
        {
            const string VstsValidationUrlPath = "_apis/connectiondata";

            if (targetUri is null)
                throw new ArgumentNullException(nameof(targetUri));

            // Create a URL to the connection data end-point, it's deployment level and "always on".
            string requestUrl = GetTargetUrl(targetUri);
            string validationUrl = requestUrl + VstsValidationUrlPath;

            return new TargetUri(validationUrl, targetUri.ProxyUri?.ToString());
        }

        internal async Task<TargetUri> GetIdentityServiceUri(TargetUri targetUri, Secret authorization)
        {
            const string LocationServiceUrlPathAndQuery = "_apis/ServiceDefinitions/LocationService2/951917AC-A960-4999-8464-E3F0AA25B381?api-version=1.0";

            if (targetUri is null)
                throw new ArgumentNullException(nameof(targetUri));
            if (authorization is null)
                throw new ArgumentNullException(nameof(authorization));

            string tenantUrl = GetTargetUrl(targetUri);
            var locationServiceUrl = tenantUrl + LocationServiceUrlPathAndQuery;
            var requestUri = new TargetUri(locationServiceUrl, targetUri.ProxyUri?.ToString());
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
                        using (HttpContent content = response.Content)
                        {
                            string responseText = await content.ReadAsStringAsync();

                            Match match;
                            if ((match = Regex.Match(responseText, @"\""location\""\:\""([^\""]+)\""", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)).Success)
                            {
                                string identityServiceUrl = match.Groups[1].Value;
                                var idenitityServiceUri = new Uri(identityServiceUrl, UriKind.Absolute);

                                return new TargetUri(idenitityServiceUri, targetUri.ProxyUri);
                            }
                        }
                    }

                    Trace.WriteLine($"failed to find Identity Service for '{targetUri}' via location service [{(int)response.StatusCode} {response.ReasonPhrase}].");
                }
            }
            catch (Exception exception)
            {
                Trace.WriteException(exception);
                throw new VstsLocationServiceException($"Failed to find Identity Service for `{targetUri}`.", exception);
            }

            return null;
        }

        internal static string GetTargetUrl(TargetUri targetUri)
        {
            string requestUrl = targetUri.ToString(false, true, false);

            // Handle the Azure userinfo -> path conversion.AzureBaseUrlHost
            if (targetUri.Host.EndsWith(AzureBaseUrlHost, StringComparison.OrdinalIgnoreCase)
                && targetUri.ContainsUserInfo)
            {
                string escapedUserInfo = Uri.EscapeUriString(targetUri.UserInfo);

                requestUrl = requestUrl + escapedUserInfo + "/";
            }

            return requestUrl;
        }

        internal static bool IsVstsUrl(TargetUri targetUri)
        {
            return (StringComparer.OrdinalIgnoreCase.Equals(targetUri.Scheme, Uri.UriSchemeHttp)
                    || StringComparer.OrdinalIgnoreCase.Equals(targetUri.Scheme, Uri.UriSchemeHttps))
                && (targetUri.DnsSafeHost.EndsWith(VstsBaseUrlHost, StringComparison.OrdinalIgnoreCase)
                    || targetUri.DnsSafeHost.EndsWith(AzureBaseUrlHost, StringComparison.OrdinalIgnoreCase));
        }

        private StringContent GetAccessTokenRequestBody(TargetUri targetUri, VstsTokenScope tokenScope, TimeSpan? duration = null)
        {
            const string ContentBasicJsonFormat = "{{ \"scope\" : \"{0}\", \"displayName\" : \"Git: {1} on {2}\" }}";
            const string ContentTimedJsonFormat = "{{ \"scope\" : \"{0}\", \"displayName\" : \"Git: {1} on {2}\", \"validTo\": \"{3:u}\" }}";
            const string HttpJsonContentType = "application/json";

            if (targetUri is null)
                throw new ArgumentNullException(nameof(targetUri));
            if (tokenScope is null)
                throw new ArgumentNullException(nameof(tokenScope));

            string tokenUrl = GetTargetUrl(targetUri);

            Trace.WriteLine($"creating access token scoped to '{tokenScope}' for '{targetUri}'");

            string jsonContent = (duration.HasValue && duration.Value > TimeSpan.FromHours(1))
                ? string.Format(InvariantCulture, ContentTimedJsonFormat, tokenScope, tokenUrl, Environment.MachineName, DateTime.UtcNow + duration.Value)
                : string.Format(InvariantCulture, ContentBasicJsonFormat, tokenScope, tokenUrl, Environment.MachineName);
            StringContent content = new StringContent(jsonContent, Encoding.UTF8, HttpJsonContentType);

            return content;
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
                throw new VstsLocationServiceException($"Failed to find Identity Service for `{targetUri}`.");

            string url = idenityServiceUri.ToString();

            url += requireCompactToken
                ? CompactTokenUrl
                : SessionTokenUrl;

            var requestUri = new Uri(url, UriKind.Absolute);

            return new TargetUri(requestUri, targetUri.ProxyUri);
        }
    }
}
