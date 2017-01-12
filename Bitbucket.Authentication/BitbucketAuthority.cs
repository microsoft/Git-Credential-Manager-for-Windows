using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Alm.Authentication;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Net;
using System.Text.RegularExpressions;

using Trace = Microsoft.Alm.Git.Trace;

namespace Bitbucket.Authentication
{
    internal class BitbucketAuthority : IBitbucketAuthority
    {
        /// <summary>
        /// The GitHub authorizations URL
        /// </summary>
        public const string DefaultRestRoot = "https://api.bitbucket.org/";

        /// <summary>
        /// The maximum wait time for a network request before timing out
        /// </summary>
        public const int RequestTimeout = 15*1000; // 15 second limit

        public BitbucketAuthority(string restRootUrl = null)
        {
            _restRootUrl = restRootUrl ?? DefaultRestRoot;
        }

        private readonly string _restRootUrl;

        public string UserUrl
        {
            get { return "/2.0/user"; }
        }

        public async Task<BitbucketAuthenticationResult> AcquireToken(TargetUri targetUri, string username,
            string password, BitbucketAuthenticationResultType resultType, BitbucketTokenScope scope)
        {
            if (resultType == BitbucketAuthenticationResultType.TwoFactor)
            {
                OAuth.OAuthAuthenticator oauth = new OAuth.OAuthAuthenticator();
                try
                {
                    return await oauth.GetAuthAsync(targetUri, scope, CancellationToken.None);
                }
                catch (Exception ex)
                {
                    Trace.WriteLine("   oauth authentication failed");
                    return new BitbucketAuthenticationResult(BitbucketAuthenticationResultType.Failure);
                }
            }
            else
            {
                // TOSO refactor into BasicAuthAuthenticator.GetAuthAsync();
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
                            Trace.WriteLine("   server responded with " + response.StatusCode);

                            switch (response.StatusCode)
                            {
                                case HttpStatusCode.OK:
                                case HttpStatusCode.Created:
                                {
                                    // Success with username/passord indicates 2FA is not on so the 'token' is actually the password
                                    // if we had a successful call then the password is good.
                                    token = new Token(password, TokenType.Personal);

                                    Trace.WriteLine("   authentication success: new password token created.");
                                    return new BitbucketAuthenticationResult(BitbucketAuthenticationResultType.Success,
                                        token);
                                }

                                case HttpStatusCode.Forbidden:
                                {
                                    Trace.WriteLine("   two-factor app authentication code required");
                                    return new BitbucketAuthenticationResult(BitbucketAuthenticationResultType.TwoFactor);
                                }
                                case HttpStatusCode.Unauthorized:
                                {
                                    Trace.WriteLine("   authentication failed");
                                    return new BitbucketAuthenticationResult(BitbucketAuthenticationResultType.Failure);
                                }

                                default:
                                    Trace.WriteLine("   authentication failed");
                                    return new BitbucketAuthenticationResult(BitbucketAuthenticationResultType.Failure);
                            }
                        }
                    }
                }
            }
        }

        public async Task<BitbucketAuthenticationResult> RefreshToken(TargetUri targetUri, string refreshToken)
        {
            OAuth.OAuthAuthenticator oauth = new OAuth.OAuthAuthenticator();
            try
            {
                return await oauth.RefreshAuthAsync(targetUri, refreshToken, CancellationToken.None);
            }
            catch (Exception ex)
            {
                Trace.WriteLine("   oauth refresh failed");
                return new BitbucketAuthenticationResult(BitbucketAuthenticationResultType.Failure);
            }
        }

        public async Task<bool> ValidateCredentials(TargetUri targetUri, string username, Credential credentials)
        {
            BaseSecureStore.ValidateTargetUri(targetUri);
            BaseSecureStore.ValidateCredential(credentials);

            // Try the simplest Basic Auth first
            var user = string.IsNullOrWhiteSpace(username) ? credentials.Username : username;
            string authString = String.Format("{0}:{1}", user, credentials.Password);
            byte[] authBytes = Encoding.UTF8.GetBytes(authString);
            string authEncode = Convert.ToBase64String(authBytes);

            if (await ValidateCredentials(targetUri, username, "Basic " + authEncode))
            {
                return true;
            }

            if (await ValidateCredentials(targetUri, username, "Bearer " + credentials.Password))
            {
                return true;
            }

            return false;
        }

        public async Task<bool> ValidateCredentials(TargetUri targetUri, string username, string authHeader)
        {
            const string ValidationUrl = "https://api.bitbucket.org/2.0/user";

            BaseSecureStore.ValidateTargetUri(targetUri);

            Trace.WriteLine("   BitbucketAuthority::ValidateCredentials");
            Trace.WriteLine($"        Auth Type = {authHeader.Substring(0,5)}");

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
                            Trace.WriteLine("   credential validation succeeded");
                            return true;
                        }
                        case HttpStatusCode.Forbidden:
                        {
                            Trace.WriteLine("   credential validation failed: Forbidden");
                            return false;
                        }
                        case HttpStatusCode.Unauthorized:
                        {
                            Trace.WriteLine("   credential validation failed: Unauthorized");
                            return false;
                        }
                        default:
                        {
                            Trace.WriteLine("   credential validation failed");
                            return false;
                        }
                    }
                }
            }
        }
    }
}