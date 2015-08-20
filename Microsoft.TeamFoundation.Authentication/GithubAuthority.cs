using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Microsoft.TeamFoundation.Authentication
{
    internal class GithubAuthority : IGithubAuthority
    {
        public const string DefaultAuthorityUrl = "https://api.github.com/authorizations";
        /// <summary>
        /// The maximum wait time for a network request before timing out
        /// </summary>
        public const int RequestTimeout = 15 * 1000; // 15 second limit

        public GithubAuthority(string authorityUrl = null)
        {
            _authorityUrl = authorityUrl ?? DefaultAuthorityUrl;
        }

        private readonly string _authorityUrl;

        public GithubAuthenticationResult AcquireToken(
            Uri targetUri,
            string username,
            string password,
            string authenticationCode,
            GithubTokenScope scope,
            out Token token)
        {
            const string GithubOptHeader = "X-GitHub-OTP";

            using (HttpClientHandler handler = new HttpClientHandler()
            {
                MaxAutomaticRedirections = 2,
                UseDefaultCredentials = true
            })
            using (HttpClient httpClient = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromMilliseconds(RequestTimeout)
            })
            {
                httpClient.DefaultRequestHeaders.Add("User-Agent", Global.GetUserAgent());
                httpClient.DefaultRequestHeaders.Add("Accept", "application/vnd.github.v3+json");

                string basicAuthValue = String.Format("{0}:{1}", username, password);
                byte[] authBytes = Encoding.UTF8.GetBytes(basicAuthValue);
                basicAuthValue = Convert.ToBase64String(authBytes);

                httpClient.DefaultRequestHeaders.Add("Authorization", "Basic " + basicAuthValue);

                if (!String.IsNullOrWhiteSpace(authenticationCode))
                {
                    httpClient.DefaultRequestHeaders.Add(GithubOptHeader, authenticationCode);
                }

                Token local = null;
                GithubAuthenticationResult result = Task.Run(async () =>
                {
                    const string HttpJsonContentType = "application/x-www-form-urlencoded";
                    const string JsonContentFormat = @"{{ ""scopes"": [""public_repo""], ""note"": ""admin script"", ""fingerprint"": ""{0}"" }}";

                    string jsonContent = String.Format(JsonContentFormat, targetUri);

                    using (StringContent content = new StringContent(jsonContent, Encoding.UTF8, HttpJsonContentType))
                    using (HttpResponseMessage response = await httpClient.PostAsync(_authorityUrl, content))
                    {
                        switch (response.StatusCode)
                        {
                            case HttpStatusCode.OK:
                            case HttpStatusCode.Created:
                                {
                                    string responseText = await response.Content.ReadAsStringAsync();

                                    Match tokenMatch;
                                    if ((tokenMatch = Regex.Match(responseText, @"\s*""token""\s*:\s*""([^""]+)""\s*", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase)).Success
                                        && tokenMatch.Groups.Count > 1)
                                    {
                                        string tokenText = tokenMatch.Groups[1].Value;
                                        local = new Token(tokenText, TokenType.Personal);
                                    }

                                    return (local == null)
                                        ? new GithubAuthenticationResult(GithubAuthenticationResultType.Failure)
                                        : new GithubAuthenticationResult(GithubAuthenticationResultType.Success);
                                }

                            case HttpStatusCode.Unauthorized:
                                {
                                    if (String.IsNullOrWhiteSpace(authenticationCode)
                                        && response.Headers.Any(x => String.Equals(GithubOptHeader, x.Key, StringComparison.OrdinalIgnoreCase)))
                                    {
                                        var mfakvp = response.Headers.First(x => String.Equals(GithubOptHeader, x.Key, StringComparison.OrdinalIgnoreCase) && x.Value != null && x.Value.Count() > 0);

                                        if (mfakvp.Value.First().Contains("app"))
                                            return new GithubAuthenticationResult(GithubAuthenticationResultType.TwoFactorApp);
                                        else
                                            return new GithubAuthenticationResult(GithubAuthenticationResultType.TwoFactorSms);
                                    }
                                    else
                                        return new GithubAuthenticationResult(GithubAuthenticationResultType.Failure);
                                }

                            default:
                                return new GithubAuthenticationResult(GithubAuthenticationResultType.Failure);
                        }

                    }
                }).Result;

                token = local;
                return result;
            }
        }
    }
}
