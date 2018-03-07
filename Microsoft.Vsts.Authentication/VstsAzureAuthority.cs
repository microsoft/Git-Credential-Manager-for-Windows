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
                var options = new NetworkRequestOptions(true)
                {
                    Authorization = accessToken,
                };

                var requestUri = await CreatePersonalAccessTokenRequestUri(targetUri, requireCompactToken);

                using (StringContent content = GetAccessTokenRequestBody(targetUri, accessToken, tokenScope, tokenDuration))
                using (var response = await Network.HttpPostAsync(targetUri, content, options))
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
            catch (Exception e)
            {
                Trace.WriteLine($"! an error occurred: {e.Message}");
            }

            Trace.WriteLine($"personal access token acquisition for '{targetUri}' failed.");

            return null;
        }

        public async Task<bool> PopulateTokenTargetId(TargetUri targetUri, Token accessToken)
        {
            if (targetUri is null)
                throw new ArgumentNullException(nameof(targetUri));
            if (accessToken is null)
                throw new ArgumentNullException(nameof(accessToken));

            string resultId = null;

            try
            {
                // Create an request to the VSTS deployment data end-point.
                var requestUri = GetConnectionDataUri(targetUri);
                var options = new NetworkRequestOptions(true)
                {
                    Authorization = accessToken,
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
                            resultId = match.Groups[1].Value;
                        }
                    }
                }
            }
            catch (WebException webException)
            {
                Trace.WriteLine($"server returned '{webException.Status}'.");
            }

            if (Guid.TryParse(resultId, out Guid instanceId))
            {
                Trace.WriteLine($"target identity is '{resultId}'.");
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
                    if (!response.IsSuccessStatusCode)
                    {
                        // Even if the service responded, if the issue isn't a 400 class response then
                        // the credentials were likely not rejected.
                        if ((int)response.StatusCode >= 400 && (int)response.StatusCode < 500)
                            return false;

                        Trace.WriteLine($"unable to validate credentials due to '{response.StatusCode}'.");
                        return true;
                    }
                }
            }
            catch(Exception exception)
            {
                Trace.WriteLine($"! error: '{exception.Message}'.");
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
                        if ((int)response.StatusCode >= 400 && (int)response.StatusCode < 500)
                            return false;

                        Trace.WriteLine($"unable to validate credentials due to '{response.StatusCode}'.");
                        return true;
                    }
                }
            }
            catch (Exception exception)
            {
                Trace.WriteLine($"! error: '{exception.Message}'.");
            };

            
            Trace.WriteLine($"token validation for '{targetUri}' failed.");
            return false;
        }

        internal static TargetUri GetConnectionDataUri(TargetUri targetUri)
        {
            const string VstsValidationUrlFormat = "{0}://{1}/_apis/connectiondata";

            if (targetUri is null)
                throw new ArgumentNullException(nameof(targetUri));

            // Create a URL to the connection data end-point, it's deployment level and "always on".
            string validationUrl = string.Format(System.Globalization.CultureInfo.InvariantCulture, 
                                                 VstsValidationUrlFormat,
                                                 targetUri.Scheme,
                                                 targetUri.DnsSafeHost);

            return new TargetUri(validationUrl, targetUri.ProxyUri?.ToString());
        }

        internal async Task<TargetUri> GetIdentityServiceUri(TargetUri targetUri)
        {
            const string LocationServiceUrlFormat = "https://{0}/_apis/ServiceDefinitions/LocationService2/951917AC-A960-4999-8464-E3F0AA25B381?api-version=1.0";

            if (targetUri is null)
                throw new ArgumentNullException(nameof(targetUri));

            var locationServiceUrl = string.Format(System.Globalization.CultureInfo.InvariantCulture, LocationServiceUrlFormat, targetUri.Host);
            var requestUri = new TargetUri(locationServiceUrl, targetUri.ProxyUri?.ToString());

            try
            {
                using (var response = await Network.HttpGetAsync(targetUri))
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
                }
            }
            catch (Exception exception)
            {
                Trace.WriteLine($"! error: '{exception.Message}'.");
                throw new VstsLocationServiceException($"Failed to find Identity Service for `{targetUri}`.", exception);
            }

            return null;
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

        private async Task<TargetUri> CreatePersonalAccessTokenRequestUri(TargetUri targetUri, bool requireCompactToken)
        {
            const string SessionTokenUrl = "_apis/token/sessiontokens?api-version=1.0";
            const string CompactTokenUrl = SessionTokenUrl + "&tokentype=compact";

            if (targetUri is null)
                throw new ArgumentNullException(nameof(targetUri));

            var idenityServiceUri = await GetIdentityServiceUri(targetUri);

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
