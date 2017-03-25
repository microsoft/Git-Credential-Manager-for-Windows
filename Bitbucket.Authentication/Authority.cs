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

using Microsoft.Alm.Authentication;
using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Trace = Microsoft.Alm.Git.Trace;

namespace Atlassian.Bitbucket.Authentication
{
    /// <summary>
    ///     Implementation of <see cref="IAuthority"/> representing the Bitbucket APIs as the authority that can provide and validate credentials for Bitbucket.
    /// </summary>
    internal class Authority : IAuthority
    {
        /// <summary>
        /// The root URL for Bitbucket REST API calls.
        /// </summary>
        public const string DefaultRestRoot = "https://api.bitbucket.org/";

        /// <summary>
        /// The maximum wait time for a network request before timing out
        /// </summary>
        public const int RequestTimeout = 15*1000; // 15 second limit

        /// <summary>
        ///     Default constructor of the <see cref="Authority"/>. Allows the default Bitbucket REST URL to be overridden.
        /// </summary>
        /// <param name="restRootUrl">overriding root URL for REST API call.</param>
        public Authority(string restRootUrl = null)
        {
            _restRootUrl = restRootUrl ?? DefaultRestRoot;
        }

        private readonly string _restRootUrl;

        public string UserUrl
        {
            get { return "/2.0/user"; }
        }

        /// <inheritdoc/>
        public async Task<AuthenticationResult> AcquireToken(TargetUri targetUri, string username,
            string password, AuthenticationResultType resultType, TokenScope scope)
        {
            if (resultType == AuthenticationResultType.TwoFactor)
            {
                // a previous attempt to aquire a token failed in a way that suggests the user has Bitbucket 2FA turned on.
                // so attempt to run the OAuth dance...
                OAuth.OAuthAuthenticator oauth = new OAuth.OAuthAuthenticator();
                try
                {
                    return await oauth.GetAuthAsync(targetUri, scope, CancellationToken.None);
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"oauth authentication failed [{ex.Message}]");
                    return new AuthenticationResult(AuthenticationResultType.Failure);
                }
            }
            else
            {
                // use the provided username and password and attempt a Basic Auth request to a known REST API resource.
                Token token = null;
                using (HttpClientHandler handler = targetUri.HttpClientHandler)
                {
                    using (HttpClient httpClient = new HttpClient(handler)
                    {
                        Timeout = TimeSpan.FromMilliseconds(RequestTimeout)
                    })
                    {
                        string basicAuthValue = String.Format("{0}:{1}", username, password);
                        byte[] authBytes = Encoding.UTF8.GetBytes(basicAuthValue);
                        basicAuthValue = Convert.ToBase64String(authBytes);
                        httpClient.DefaultRequestHeaders.Add("Authorization", "Basic " + basicAuthValue);

                        var url = new Uri(new Uri(_restRootUrl), UserUrl).AbsoluteUri;
                        using (HttpResponseMessage response = await httpClient.GetAsync(url))
                        {
                            Trace.WriteLine($"server responded with {response.StatusCode}.");

                            switch (response.StatusCode)
                            {
                                case HttpStatusCode.OK:
                                case HttpStatusCode.Created:
                                {
                                    // Success with username/passord indicates 2FA is not on so the 'token' is actually the password
                                    // if we had a successful call then the password is good.
                                    token = new Token(password, TokenType.Personal);

                                    Trace.WriteLine("authentication success: new password token created.");
                                    return new AuthenticationResult(AuthenticationResultType.Success,
                                        token);
                                }

                                case HttpStatusCode.Forbidden:
                                {
                                    // A 403/Forbidden response indicates the username/password are recognized and good but 2FA is on
                                    // in which case we want to indicate that with the TwoFactor result
                                    Trace.WriteLine("two-factor app authentication code required");
                                    return new AuthenticationResult(AuthenticationResultType.TwoFactor);
                                }
                                case HttpStatusCode.Unauthorized:
                                {
                                    // username or password are wrong.
                                    Trace.WriteLine("authentication failed");
                                    return new AuthenticationResult(AuthenticationResultType.Failure);
                                }

                                default:
                                    // any unexpected result can be treated as a failure.
                                    Trace.WriteLine("authentication failed");
                                    return new AuthenticationResult(AuthenticationResultType.Failure);
                            }
                        }
                    }
                }
            }
        }

        /// <inheritdoc/>
        public async Task<AuthenticationResult> RefreshToken(TargetUri targetUri, string refreshToken)
        {
            // Refreshing is only an OAuth concept so use the OAuth tools
            OAuth.OAuthAuthenticator oauth = new OAuth.OAuthAuthenticator();
            try
            {
                return await oauth.RefreshAuthAsync(targetUri, refreshToken, CancellationToken.None);
            }
            catch (Exception ex)
            {
                Trace.WriteLine($"oauth refresh failed [{ex.Message}]");
                return new AuthenticationResult(AuthenticationResultType.Failure);
            }
        }

        /// <inheritdoc/>
        public async Task<bool> ValidateCredentials(TargetUri targetUri, string username, Credential credentials)
        {
            BaseSecureStore.ValidateTargetUri(targetUri);
            BaseSecureStore.ValidateCredential(credentials);

            
            var user = string.IsNullOrWhiteSpace(username) ? credentials.Username : username;
            string authString = String.Format("{0}:{1}", user, credentials.Password);
            byte[] authBytes = Encoding.UTF8.GetBytes(authString);
            string authEncode = Convert.ToBase64String(authBytes);

            // We don't know when the credentials arrive here if they are using OAuth or Basic Auth, so we try both.
            
            // Try the simplest Basic Auth first
            if (await ValidateCredentials(targetUri, username, "Basic " + authEncode))
            {
                return true;
            }

            // if the Basic Auth test failed then try again as OAuth
            if (await ValidateCredentials(targetUri, username, "Bearer " + credentials.Password))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        ///     Validate the provided credentials, made up of the username and the contents if the authHeader, by making a request to a known Bitbucket
        /// REST API resource. A 200/Success response indicates the credentials are valid. Any other response indicates they are not.
        /// </summary>
        /// <param name="targetUri">Contains the <see cref="HttpClientHandler"/> used when making the REST API request</param>
        /// <param name="username">the username to validate</param>
        /// <param name="authHeader">the HTTP auth header containing the password/access_token to validate</param>
        /// <returns>true if the credentials are valid, false otherwise.</returns>
        private async Task<bool> ValidateCredentials(TargetUri targetUri, string username, string authHeader)
        {
            const string ValidationUrl = "https://api.bitbucket.org/2.0/user";

            BaseSecureStore.ValidateTargetUri(targetUri);

            Trace.WriteLine($"Auth Type = {authHeader.Substring(0,5)}");

            // craft the request header for the Bitbucket v2 API w/ credentials
            using (HttpClientHandler handler = targetUri.HttpClientHandler)
            using (HttpClient httpClient = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromMilliseconds(RequestTimeout)
            })
            {
                httpClient.DefaultRequestHeaders.Add("User-Agent", Global.UserAgent);
                httpClient.DefaultRequestHeaders.Add("Authorization", authHeader);

                using (HttpResponseMessage response = await httpClient.GetAsync(ValidationUrl))
                {
                    switch (response.StatusCode)
                    {
                        case HttpStatusCode.OK:
                        case HttpStatusCode.Created:
                        {
                            Trace.WriteLine("credential validation succeeded");
                            return true;
                        }
                        case HttpStatusCode.Forbidden:
                        {
                            Trace.WriteLine("credential validation failed: Forbidden");
                            return false;
                        }
                        case HttpStatusCode.Unauthorized:
                        {
                            Trace.WriteLine("credential validation failed: Unauthorized");
                            return false;
                        }
                        default:
                        {
                            Trace.WriteLine("credential validation failed");
                            return false;
                        }
                    }
                }
            }
        }
    }
}