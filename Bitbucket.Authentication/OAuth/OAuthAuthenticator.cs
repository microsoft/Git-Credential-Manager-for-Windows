﻿using Microsoft.Alm.Authentication;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using Trace = Microsoft.Alm.Git.Trace;

namespace Atlassian.Bitbucket.Authentication.OAuth
{
    

    /// <summary>
    /// </summary>
    public class OAuthAuthenticator
    {
        /// <summary>
        /// The maximum wait time for a network request before timing out
        /// </summary>
        public const int RequestTimeout = 15 * 1000; // 15 second limit

        public OAuthAuthenticator()
        {
        }

        public string AuthorizeUri { get { return "/site/oauth2/authorize"; } }
        public string TokenUri { get { return "/site/oauth2/access_token"; } }

        public string ConsumerKey { get { return "HJdmKXV87DsmC9zSWB"; } }
        public string ConsumerSecret { get { return "wwWw47VB9ZHwMsD4Q4rAveHkbxNrMp3n"; } }
        public string CallbackUri { get { return "http://localhost:34106/"; } }

        /// <summary>
        ///     Gets the OAuth access token
        /// </summary>
        /// <returns>The access token</returns>
        /// <exception cref="SourceTree.Exceptions.OAuthException">Thrown when OAuth fails for whatever reason</exception>
        public async Task<AuthenticationResult> GetAuthAsync(TargetUri targetUri, TokenScope scope, CancellationToken cancellationToken)
        {

            var authToken = await Authorize(targetUri, scope, cancellationToken);

            return await GetAccessToken(targetUri, authToken);
        }

        public async Task<AuthenticationResult> RefreshAuthAsync(TargetUri targetUri, string refreshToken, CancellationToken cancellationToken)
        {
            return await RefreshAccessToken(targetUri, refreshToken);
        }

        public async Task<string> Authorize(TargetUri targetUri, TokenScope scope, CancellationToken cancellationToken)
        {
            var authorityUrl = string.Format(
            AuthorizeUri +
            "?response_type=code&client_id={0}&state=authenticated&scope={1}&redirect_uri={2}",
            ConsumerKey,
            scope.ToString(),
            CallbackUri);

            //Open the browser for auth
            var url = new Uri(new Uri("https://bitbucket.org"), authorityUrl).AbsoluteUri;
            Process.Start(url);

            string rawUrlData;
            try
            {
                //Fetch the url and await for the reply
                rawUrlData = await SimpleServer.WaitForURLAsync(CallbackUri, cancellationToken);
            }
            catch (Exception ex)
            {
                string message;
                string details;
                if (ex.InnerException != null && ex.InnerException.GetType().IsAssignableFrom(typeof(TimeoutException)))
                {
                    message = "Timeout awaiting response from Host service.";
                    details = "Please check the SourceTree's configuration for this Host." + Environment.NewLine +
                              "Confirm that if there are overrides to the OAuth Consumer Information that they are correct";
                }
                else
                {
                    message = "Unable to receive callback from OAuth service provider";
                    details = "Please try restarting SourceTree";
                }

                //throw new OAuthAuthenticationFlowException(message, details, ex);
                throw new Exception(message + details, ex);
            }


            //Parse and try to get the key
            Dictionary<string,string> qs =
          rawUrlData.Split('&')
               .ToDictionary(c => c.Split('=')[0],
                             c => Uri.UnescapeDataString(c.Split('=')[1]));
            var authCode = qs.Keys.Where(k => k.EndsWith("code", StringComparison.InvariantCultureIgnoreCase)).Select(k => qs[k]).FirstOrDefault();
            //var qs = new Dictionary<string, string>(); // HttpUtility.ParseQueryString(rawUrlData);
            //var authCode = qs["/?code"];
            //if (string.IsNullOrEmpty(authCode))
            //    authCode = qs["code"];

            if (string.IsNullOrWhiteSpace(authCode))
            {
                var error_desc = qs["error_description"];
                //throw new OAuthAuthenticationFlowException("Request for an OAuth request_token was denied", error_desc);
                throw new Exception("Request for an OAuth request_token was denied" + error_desc);
            }

            return authCode;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="refreshToken"></param>
        /// <returns></returns>
        //public async Task<AccessToken> RefreshAuthAsync(string refreshToken)
        //{
        //    var response = await RefreshAccessTokenAsync(refreshToken, GetClient());

        //    //Got it!
        //    return response.Data;
        //}

        private async Task<AuthenticationResult> GetAccessToken(TargetUri targetUri, string authCode)
        {
            Token token = null;
            Token refreshToken = null;

            using (HttpClientHandler handler = targetUri.HttpClientHandler)
            {
                using (HttpClient httpClient = new HttpClient(handler)
                {
                    Timeout = TimeSpan.FromMilliseconds(RequestTimeout)
                })
                {
                    var tokenUrl = string.Format(
                       TokenUri +
                       "?grant_type=authorization_code&code={0}&client_id={1}&client_secret={2}&state=authenticated",
                       authCode,
                       ConsumerKey,
                       ConsumerSecret);
                    var url = new Uri(new Uri(targetUri.ToString()), tokenUrl).AbsoluteUri;

                    var content = new MultipartFormDataContent();
                    content.Add(new StringContent("authorization_code"), "grant_type");
                    content.Add(new StringContent(authCode), "code");
                    content.Add(new StringContent(ConsumerKey), "client_id");
                    content.Add(new StringContent(ConsumerSecret), "client_secret");
                    content.Add(new StringContent("authenticated"), "state");
                    content.Add(new StringContent(CallbackUri), "redirect_uri");

                    using (HttpResponseMessage response = await httpClient.PostAsync(url, content))
                    {
                        Trace.WriteLine($"server responded with {response.StatusCode}.");

                        switch (response.StatusCode)
                        {
                            case HttpStatusCode.OK:
                            case HttpStatusCode.Created:
                                {
                                    string responseText = await response.Content.ReadAsStringAsync();

                                    Match tokenMatch;
                                    if ((tokenMatch = Regex.Match(responseText, @"\s*""access_token""\s*:\s*""([^""]+)""\s*", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)).Success
                                        && tokenMatch.Groups.Count > 1)
                                    {
                                        string tokenText = tokenMatch.Groups[1].Value;
                                        token = new Token(tokenText, TokenType.Personal);
                                    }

                                    Match refreshTokenMatch;
                                    if ((refreshTokenMatch = Regex.Match(responseText, @"\s*""refresh_token""\s*:\s*""([^""]+)""\s*", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)).Success
                                        && refreshTokenMatch.Groups.Count > 1)
                                    {
                                        string refreshTokenText = refreshTokenMatch.Groups[1].Value;
                                        refreshToken = new Token(refreshTokenText, TokenType.BitbucketRefresh);
                                    }

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
            }
        }

        public async Task<AuthenticationResult> RefreshAccessToken(TargetUri targetUri, string currentRefreshToken)
        {
            Token token = null;
            Token refreshToken = null;

            using (HttpClientHandler handler = targetUri.HttpClientHandler)
            {
                using (HttpClient httpClient = new HttpClient(handler)
                {
                    Timeout = TimeSpan.FromMilliseconds(RequestTimeout)
                })
                {
                    var url = new Uri(new Uri("https://bitbucket.org"), TokenUri).AbsoluteUri;

                    var content = new MultipartFormDataContent();
                    content.Add(new StringContent("refresh_token"), "grant_type");
                    content.Add(new StringContent(currentRefreshToken), "refresh_token");
                    content.Add(new StringContent(ConsumerKey), "client_id");
                    content.Add(new StringContent(ConsumerSecret), "client_secret");

                    using (HttpResponseMessage response = await httpClient.PostAsync(url, content))
                    {
                        Trace.WriteLine($"server responded with {response.StatusCode}.");

                        switch (response.StatusCode)
                        {
                            case HttpStatusCode.OK:
                            case HttpStatusCode.Created:
                                {
                                    string responseText = await response.Content.ReadAsStringAsync();

                                    Match tokenMatch;
                                    if ((tokenMatch = Regex.Match(responseText, @"\s*""access_token""\s*:\s*""([^""]+)""\s*", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)).Success
                                        && tokenMatch.Groups.Count > 1)
                                    {
                                        string tokenText = tokenMatch.Groups[1].Value;
                                        token = new Token(tokenText, TokenType.Personal);
                                    }

                                    Match refreshTokenMatch;
                                    if ((refreshTokenMatch = Regex.Match(responseText, @"\s*""refresh_token""\s*:\s*""([^""]+)""\s*", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)).Success
                                        && refreshTokenMatch.Groups.Count > 1)
                                    {
                                        string refreshTokenText = refreshTokenMatch.Groups[1].Value;
                                        refreshToken = new Token(refreshTokenText, TokenType.BitbucketRefresh);
                                    }

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
            }
        }       
    }
}
