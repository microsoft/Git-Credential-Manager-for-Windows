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
using System.IO;
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
            if (targetUri is null)
                throw new ArgumentNullException(nameof(targetUri));
            if (accessToken is null)
                throw new ArgumentNullException(nameof(accessToken));
            if (tokenScope is null)
                throw new ArgumentNullException(nameof(tokenScope));

            if (accessToken.Type != TokenType.AzureAccess
                && accessToken.Type != TokenType.AzureFederated)
            {
                var inner = new InvalidOperationException("Only Azure Acess and Federated Authentication Tokens can be used to generate VSTS Personal Access Tokens.");
                throw new ArgumentException(inner.Message, nameof(accessToken), inner);
            }

            try
            {
                if (await PopulateTokenTargetId(targetUri, accessToken))
                {
                    // Configure the HTTP client handler to not choose an authentication strategy for us
                    // because we want to deliver the complete payload to the caller.
                    var options = new NetworkRequestOptions(false)
                    {
                        Authentication = accessToken,
                        Flags = NetworkRequestOptionFlags.UseCredentials | NetworkRequestOptionFlags.UseProxy,
                    };
                    TargetUri requestUri = await CreatePersonalAccessTokenRequestUri(targetUri, accessToken, requireCompactToken);

                    using (StringContent content = GetAccessTokenRequestBody(targetUri, accessToken, tokenScope, tokenDuration))
                    using (HttpResponseMessage response = await Network.HttpPostAsync(requestUri, content, options))
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
                                    Token token = new Token(tokenValue, TokenType.PersonalAccess);

                                    Trace.WriteLine($"personal access token acquisition for '{targetUri}' succeeded.");

                                    return token;
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
                var connectionDataUri = GetConnectionDataUri(targetUri);

                Trace.WriteLine($"access token end-point is 'GET' '{connectionDataUri.ToString(false, false, true)}'.");

                var options = NetworkRequestOptions.Default;
                options.Authentication = accessToken;

                // Send the request and wait for the response.
                using (var response = await Network.HttpGetAsync(connectionDataUri, options))
                using (var stream = await response.Content.ReadAsStreamAsync())
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
            if (targetUri is null)
                throw new ArgumentNullException(nameof(targetUri));
            if (credentials is null)
                throw new ArgumentNullException(nameof(credentials));

            try
            {
                // Create an request to the VSTS deployment data end-point.
                var connectionDataUri = GetConnectionDataUri(targetUri);

                Trace.WriteLine($"validating credentials against '{connectionDataUri}'.");

                var options = new NetworkRequestOptions(true)
                {
                    Authentication = credentials,
                };

                // Send the request and wait for the response.
                using (var response = await Network.HttpGetAsync(connectionDataUri, options))
                {
                    // We're looking for 'OK 200' here, anything else is failure
                    Trace.WriteLine($"server returned: ({response.StatusCode}).");
                    if (response.StatusCode == HttpStatusCode.OK)
                        return true;

                    int statusCode = (int)response.StatusCode;

                    if (statusCode >= 202 && statusCode < 500)
                        return false;

                    Trace.WriteLine($"error appears to be a server connection issue, ignoring failure.");
                    return true;
                }
            }
            catch (Exception exception)
            {
                Trace.WriteLine($"!error: '{exception.Message}'.");
                System.Diagnostics.Debug.WriteLine(exception.ToString());
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
            if (token.Type == TokenType.PersonalAccess)
                return await ValidateCredentials(targetUri, (Credential)token);

            try
            {
                // Create an request to the VSTS deployment data end-point.
                Trace.WriteLine($"validating token against '{targetUri.ToString(false, true, true)}'.");

                var options = NetworkRequestOptions.Default;
                options.Authentication = token;

                // Send the request and wait for the response.
                using (HttpResponseMessage response = await Network.HttpHeadAsync(targetUri, options))
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

        internal static TargetUri GetConnectionDataUri(TargetUri targetUri)
        {
            const string VstsValidationUrlFormat = "{0}://{1}/_apis/connectiondata";

            if (targetUri is null)
                throw new ArgumentNullException(nameof(targetUri));

            // Create a URL to the connection data end-point, it's deployment level and "always on".
            string validationUrl = string.Format(System.Globalization.CultureInfo.InvariantCulture, VstsValidationUrlFormat, targetUri.Scheme, targetUri.DnsSafeHost);

            // Start building the request, only supports GET.
            return new TargetUri(validationUrl, targetUri.ProxyUri?.ToString());
        }

        internal async Task<Uri> GetIdentityServiceUri(TargetUri targetUri, Token accessToken)
        {
            const string LocationServiceUrlFormat = "https://{0}/_apis/ServiceDefinitions/LocationService2/951917AC-A960-4999-8464-E3F0AA25B381?api-version=1.0";

            if (targetUri is null)
                throw new ArgumentNullException(nameof(targetUri));

            Uri idenitityServiceUri = null;

            var locationServiceUrl = string.Format(System.Globalization.CultureInfo.InvariantCulture, LocationServiceUrlFormat, targetUri.Host);
            var locationServiceUri = new TargetUri(locationServiceUrl, targetUri.ProxyUri?.ToString());
            var options = new NetworkRequestOptions(true)
            {
                Authentication = accessToken,
            };

            using (var response = await Network.HttpGetAsync(locationServiceUri, options))
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

        private async Task<TargetUri> CreatePersonalAccessTokenRequestUri(TargetUri targetUri, Token accessToken, bool requireCompactToken)
        {
            const string SessionTokenUrl = "_apis/token/sessiontokens?api-version=1.0";
            const string CompactTokenUrl = SessionTokenUrl + "&tokentype=compact";

            BaseSecureStore.ValidateTargetUri(targetUri);

            Uri idenityServiceUri = await GetIdentityServiceUri(targetUri, accessToken);

            if (idenityServiceUri == null)
                throw new VstsLocationServiceException($"Failed to find Identity Service for {targetUri}");

            string url = idenityServiceUri.ToString();

            url += requireCompactToken
                ? CompactTokenUrl
                : SessionTokenUrl;

            return new TargetUri(url, targetUri.ProxyUri?.ToString());
        }
    }
}
