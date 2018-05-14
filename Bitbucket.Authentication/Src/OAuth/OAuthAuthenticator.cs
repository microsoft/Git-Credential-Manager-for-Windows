/**** Git Credential Manager for Windows ****
 *
 * Copyright (c) Atlassian
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
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Alm.Authentication;

namespace Atlassian.Bitbucket.Authentication.OAuth
{
    /// <summary>
    /// </summary>
    public class OAuthAuthenticator : Base
    {
        /// <summary>
        /// The maximum wait time for a network request before timing out
        /// </summary>
        public const int RequestTimeout = 15 * 1000; // 15 second limit

        internal static readonly Regex RefreshTokenRegex = new Regex(@"\s*""refresh_token""\s*:\s*""([^""]+)""\s*", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);
        internal static readonly Regex AccessTokenTokenRegex = new Regex(@"\s*""access_token""\s*:\s*""([^""]+)""\s*", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

        public OAuthAuthenticator(RuntimeContext context)
            : base(context)
        { }

        public string AuthorizeUrlPath { get { return "/site/oauth2/authorize"; } }

        public string CallbackUri { get { return "http://localhost:34106/"; } }

        public string ConsumerKey { get { return "HJdmKXV87DsmC9zSWB"; } }

        public string ConsumerSecret { get { return "wwWw47VB9ZHwMsD4Q4rAveHkbxNrMp3n"; } }

        public string TokenUri { get { return "/site/oauth2/access_token"; } }

        /// <summary>
        /// Gets the OAuth access token
        /// </summary>
        /// <returns>The access token</returns>
        /// <exception cref="SourceTree.Exceptions.OAuthException">
        /// Thrown when OAuth fails for whatever reason
        /// </exception>
        public async Task<AuthenticationResult> GetAuthAsync(TargetUri targetUri, TokenScope scope, CancellationToken cancellationToken)
        {
            var authToken = await Authorize(targetUri, scope, cancellationToken);

            return await GetAccessToken(targetUri, authToken);
        }

        /// <summary>
        /// Uses a refresh_token to get a new access_token
        /// </summary>
        /// <param name="targetUri"></param>
        /// <param name="refreshToken"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task<AuthenticationResult> RefreshAuthAsync(TargetUri targetUri, string refreshToken, CancellationToken cancellationToken)
        {
            return await RefreshAccessToken(targetUri, refreshToken);
        }

        /// <summary>
        /// Run the OAuth dance to get a new request_token
        /// </summary>
        /// <param name="targetUri"></param>
        /// <param name="scope"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private async Task<string> Authorize(TargetUri targetUri, TokenScope scope, CancellationToken cancellationToken)
        {
            var authorizationUri = GetAuthorizationUri(scope);

            // Open the browser to prompt the user to authorize the token request
            Process.Start(authorizationUri.AbsoluteUri);

            string rawUrlData;
            try
            {
                //Start a temporary server to handle the callback request and await for the reply
                rawUrlData = await SimpleServer.WaitForURLAsync(CallbackUri, cancellationToken);
            }
            catch (Exception ex)
            {
                string message;
                if (ex.InnerException != null && ex.InnerException.GetType().IsAssignableFrom(typeof(TimeoutException)))
                {
                    message = "Timeout awaiting response from Host service.";
                }
                else
                {
                    message = "Unable to receive callback from OAuth service provider";
                }

                throw new Exception(message, ex);
            }

            //Parse the callback url
            Dictionary<string, string> qs = GetQueryParameters(rawUrlData);

            // look for a request_token code in the parameters
            string authCode = GetAuthenticationCode(qs);

            if (string.IsNullOrWhiteSpace(authCode))
            {
                var error_desc = GetErrorDescription(qs);
                throw new Exception("Request for an OAuth request_token was denied" + error_desc);
            }

            return authCode;
        }

        private string GetAuthenticationCode(Dictionary<string, string> qs)
        {
            if (qs == null)
            {
                return null;
            }

            return qs.Keys.Where(k => k.EndsWith("code", StringComparison.InvariantCultureIgnoreCase)).Select(k => qs[k]).FirstOrDefault();
        }

        private string GetErrorDescription(Dictionary<string, string> qs)
        {
            if (qs == null)
            {
                return null;
            }

            return qs["error_description"];
        }

        /// <summary>
        /// Use a request_token to get an access_token
        /// </summary>
        /// <param name="targetUri"></param>
        /// <param name="authCode"></param>
        /// <returns></returns>
        private async Task<AuthenticationResult> GetAccessToken(TargetUri targetUri, string authCode)
        {
            if (targetUri is null)
                throw new ArgumentNullException(nameof(targetUri));
            if (authCode is null)
                throw new ArgumentNullException(nameof(authCode));

            var options = new NetworkRequestOptions(true)
            {
                Timeout = TimeSpan.FromMilliseconds(RequestTimeout),
            };
            var grantUri = GetGrantUrl(targetUri, authCode);
            var requestUri = new TargetUri(grantUri, targetUri.ProxyUri);
            var content = GetGrantRequestContent(authCode);

            using (var response = await Network.HttpPostAsync(requestUri, content, options))
            {
                Trace.WriteLine($"server responded with {response.StatusCode}.");

                switch (response.StatusCode)
                {
                    case HttpStatusCode.OK:
                    case HttpStatusCode.Created:
                        {
                            // the request was successful, look for the tokens in the response
                            string responseText = await response.Content.ReadAsStringAsync();
                            var token = FindAccessToken(responseText);
                            var refreshToken = FindRefreshToken(responseText);
                            return GetAuthenticationResult(token, refreshToken);
                        }

                    case HttpStatusCode.Unauthorized:
                        {
                            // do something
                            return new AuthenticationResult(AuthenticationResultType.Failure);
                        }

                    default:
                        Trace.WriteLine("authentication failed");
                        var error = response.Content.ReadAsStringAsync();
                        return new AuthenticationResult(AuthenticationResultType.Failure);
                }
            }
        }

        /// <summary>
        /// Use a refresh_token to get a new access_token
        /// </summary>
        /// <param name="targetUri"></param>
        /// <param name="currentRefreshToken"></param>
        /// <returns></returns>
        private async Task<AuthenticationResult> RefreshAccessToken(TargetUri targetUri, string currentRefreshToken)
        {
            if (targetUri is null)
                throw new ArgumentNullException(nameof(targetUri));
            if (currentRefreshToken is null)
                throw new ArgumentNullException(nameof(currentRefreshToken));

            var refreshUri = GetRefreshUri();
            var requestUri = new TargetUri(refreshUri, targetUri.ProxyUri);
            var options = new NetworkRequestOptions(true)
            {
                Timeout = TimeSpan.FromMilliseconds(RequestTimeout),
            };
            var content = GetRefreshRequestContent(currentRefreshToken);

            using (var response = await Network.HttpPostAsync(requestUri, content, options))
            {
                Trace.WriteLine($"server responded with {response.StatusCode}.");

                switch (response.StatusCode)
                {
                    case HttpStatusCode.OK:
                    case HttpStatusCode.Created:
                        {
                            // the request was successful, look for the tokens in the response
                            string responseText = await response.Content.ReadAsStringAsync();
                            var token = FindAccessToken(responseText);
                            var refreshToken = FindRefreshToken(responseText);
                            return GetAuthenticationResult(token, refreshToken);
                        }

                    case HttpStatusCode.Unauthorized:
                        {
                            // do something
                            return new AuthenticationResult(AuthenticationResultType.Failure);
                        }

                    default:
                        Trace.WriteLine("authentication failed");
                        var error = response.Content.ReadAsStringAsync();
                        return new AuthenticationResult(AuthenticationResultType.Failure);
                }
            }
        }

        private Uri GetAuthorizationUri(TokenScope scope)
        {
            const string AuthorizationUrl = "{0}?response_type=code&client_id={1}&state=authenticated&scope={2}&redirect_uri={3}";

            var authorityUrl = string.Format(System.Globalization.CultureInfo.InvariantCulture,
                                             AuthorizationUrl,
                                             AuthorizeUrlPath,
                                             ConsumerKey,
                                             scope.ToString(),
                                             CallbackUri);

            return new Uri(new Uri("https://bitbucket.org"), authorityUrl);
        }

        private Uri GetRefreshUri()
        {
            return new Uri(new Uri("https://bitbucket.org"), TokenUri);
        }

        private Uri GetGrantUrl(TargetUri targetUri, string authCode)
        {
            var tokenUrl = $"{TokenUri}?grant_type=authorization_code&code={authCode}&client_id={ConsumerKey}&client_secret={ConsumerSecret}&state=authenticated";
            return new Uri(new Uri(targetUri.ToString()), tokenUrl);
        }

        private MultipartFormDataContent GetGrantRequestContent(string authCode)
        {
            var content = new MultipartFormDataContent
            {
                { new StringContent("authorization_code"), "grant_type" },
                { new StringContent(authCode), "code" },
                { new StringContent(ConsumerKey), "client_id" },
                { new StringContent(ConsumerSecret), "client_secret" },
                { new StringContent("authenticated"), "state" },
                { new StringContent(CallbackUri), "redirect_uri" }
            };
            return content;
        }

        private Dictionary<string, string> GetQueryParameters(string rawUrlData)
        {
            return rawUrlData.Split('&').ToDictionary(c => c.Split('=')[0], c => Uri.UnescapeDataString(c.Split('=')[1]));
        }

        private MultipartFormDataContent GetRefreshRequestContent(string currentRefreshToken)
        {
            var content = new MultipartFormDataContent
            {
                { new StringContent("refresh_token"), "grant_type" },
                { new StringContent(currentRefreshToken), "refresh_token" },
                { new StringContent(ConsumerKey), "client_id" },
                { new StringContent(ConsumerSecret), "client_secret" }
            };
            return content;
        }

        private Token FindAccessToken(string responseText)
        {
            Match tokenMatch;
            if ((tokenMatch = AccessTokenTokenRegex.Match(responseText)).Success
                && tokenMatch.Groups.Count > 1)
            {
                string tokenText = tokenMatch.Groups[1].Value;
                return new Token(tokenText, TokenType.Personal);
            }

            return null;
        }

        private Token FindRefreshToken(string responseText)
        {
            Match refreshTokenMatch;
            if ((refreshTokenMatch = RefreshTokenRegex.Match(responseText)).Success
                && refreshTokenMatch.Groups.Count > 1)
            {
                string refreshTokenText = refreshTokenMatch.Groups[1].Value;
                return new Token(refreshTokenText, TokenType.BitbucketRefresh);
            }

            return null;
        }

        private AuthenticationResult GetAuthenticationResult(Token token, Token refreshToken)
        {
            // Bitbucket should always return both
            if (token == null || refreshToken == null)
            {
                Trace.WriteLine("authentication failure");
                return new AuthenticationResult(AuthenticationResultType.Failure);
            }
            else
            {
                Trace.WriteLine("authentication success: new personal access token created.");
                return new AuthenticationResult(AuthenticationResultType.Success, token, refreshToken);
            }
        }
    }
}
