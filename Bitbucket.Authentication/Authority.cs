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
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Atlassian.Bitbucket.Authentication.BasicAuth;
using Atlassian.Bitbucket.Authentication.Rest;
using Microsoft.Alm.Authentication;

namespace Atlassian.Bitbucket.Authentication
{
    /// <summary>
    /// Implementation of <see cref="IAuthority"/> representing the Bitbucket APIs as the authority
    /// that can provide and validate credentials for Bitbucket.
    /// </summary>
    internal class Authority : Base, IAuthority
    {
        /// <summary>
        /// The root URL for Bitbucket REST API calls.
        /// </summary>
        public const string DefaultRestRoot = "https://api.bitbucket.org/";

        /// <summary>
        /// The maximum wait time for a network request before timing out
        /// </summary>
        public const int RequestTimeout = 15 * 1000; // 15 second limit

        /// <summary>
        /// Default constructor of the <see cref="Authority"/>. Allows the default Bitbucket REST URL
        /// to be overridden.
        /// </summary>
        /// <param name="restRootUrl">overriding root URL for REST API call.</param>
        public Authority(RuntimeContext context, string restRootUrl = null)
            : base(context)
        {
            _restRootUrl = restRootUrl ?? DefaultRestRoot;
        }

        private readonly string _restRootUrl;

        /// <inheritdoc/>
        public async Task<AuthenticationResult> AcquireToken(TargetUri targetUri, string username, string password, AuthenticationResultType resultType, TokenScope scope)
        {
            if (resultType == AuthenticationResultType.TwoFactor)
            {
                // a previous attempt to aquire a token failed in a way that suggests the user has
                // Bitbucket 2FA turned on. so attempt to run the OAuth dance...
                OAuth.OAuthAuthenticator oauth = new OAuth.OAuthAuthenticator(Context);
                try
                {
                    var result = await oauth.GetAuthAsync(targetUri, scope, CancellationToken.None);

                    if (!result.IsSuccess)
                    {
                        Trace.WriteLine($"oauth authentication failed");
                        return new AuthenticationResult(AuthenticationResultType.Failure);
                    }

                    // we got a toke but lets check to see the usernames match
                    var restRootUri = new Uri(_restRootUrl);
                    var authHeader = GetBearerHeaderAuthHeader(result.Token.Value);
                    var userResult = await (new RestClient(Context)).TryGetUser(targetUri, RequestTimeout, restRootUri, authHeader);

                    if (!userResult.IsSuccess)
                    {
                        Trace.WriteLine($"oauth user check failed");
                        return new AuthenticationResult(AuthenticationResultType.Failure);
                    }

                    if (!string.IsNullOrWhiteSpace(userResult.RemoteUsername) && !username.Equals(userResult.RemoteUsername))
                    {
                        Trace.WriteLine($"Remote username [{userResult.RemoteUsername}] != [{username}] supplied username");
                        // make sure the 'real' username is returned
                        return new AuthenticationResult(AuthenticationResultType.Success, result.Token, result.RefreshToken, userResult.RemoteUsername);
                    }

                    // everything is hunky dory
                    return result;
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"oauth authentication failed [{ex.Message}]");
                    return new AuthenticationResult(AuthenticationResultType.Failure);
                }
            }
            else
            {
                BasicAuthAuthenticator basicauth = new BasicAuthAuthenticator(Context);
                try
                {
                    var restRootUri = new Uri(_restRootUrl);
                    return await basicauth.GetAuthAsync(targetUri, scope, RequestTimeout, restRootUri, username, password);
                }
                catch (Exception ex)
                {
                    Trace.WriteLine($"basic auth authentication failed [{ex.Message}]");
                    return new AuthenticationResult(AuthenticationResultType.Failure);
                }
            }
        }

        /// <inheritdoc/>
        public async Task<AuthenticationResult> RefreshToken(TargetUri targetUri, string refreshToken)
        {
            // Refreshing is only an OAuth concept so use the OAuth tools
            OAuth.OAuthAuthenticator oauth = new OAuth.OAuthAuthenticator(Context);
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

            // We don't know when the credentials arrive here if they are using OAuth or Basic Auth,
            // so we try both.

            // Try the simplest Basic Auth first
            var authEncode = GetEncodedCredentials(username, credentials);
            if (await ValidateCredentials(targetUri, GetBasicAuthHeader(authEncode)))
            {
                return true;
            }

            // if the Basic Auth test failed then try again as OAuth
            if (await ValidateCredentials(targetUri, GetBearerHeaderAuthHeader(credentials.Password)))
            {
                return true;
            }

            return false;
        }

        private static string GetBasicAuthHeader(string secret)
        {
            return "Basic " + secret;
        }

        private static string GetBearerHeaderAuthHeader(string secret)
        {
            return "Bearer " + secret;
        }

        /// <summary>
        /// Get the HTTP encoded version of the Credentials secret
        /// </summary>
        private static string GetEncodedCredentials(string username, Credential credentials)
        {
            var user = string.IsNullOrWhiteSpace(username) ? credentials.Username : username;
            var password = credentials.Password;
            return GetEncodedCredentials(user, password);
        }

        /// <summary>
        /// Get the HTTP encoded version of the Credentials secret
        /// </summary>
        private static string GetEncodedCredentials(string user, string password)
        {
            string authString = string.Format("{0}:{1}", user, password);
            byte[] authBytes = Encoding.UTF8.GetBytes(authString);
            string authEncode = Convert.ToBase64String(authBytes);
            return authEncode;
        }

        /// <summary>
        /// Validate the provided credentials, made up of the username and the contents if the
        /// authHeader, by making a request to a known Bitbucket REST API resource. A 200/Success
        /// response indicates the credentials are valid. Any other response indicates they are not.
        /// </summary>
        /// <param name="targetUri">
        /// Contains the <see cref="HttpClientHandler"/> used when making the REST API request
        /// </param>
        /// <param name="authHeader">
        /// the HTTP auth header containing the password/access_token to validate
        /// </param>
        /// <returns>true if the credentials are valid, false otherwise.</returns>
        private async Task<bool> ValidateCredentials(TargetUri targetUri, string authHeader)
        {
            BaseSecureStore.ValidateTargetUri(targetUri);

            Trace.WriteLine($"Auth Type = {authHeader.Substring(0, 5)}");

            var restRootUrl = new Uri(_restRootUrl);
            var result = await (new RestClient(Context)).TryGetUser(targetUri, RequestTimeout, restRootUrl, authHeader);

            if (result.Type.Equals(AuthenticationResultType.Success))
            {
                Trace.WriteLine("credential validation succeeded");
                return true;
            }

            Trace.WriteLine("credential validation failed");
            return false;
        }
    }
}
