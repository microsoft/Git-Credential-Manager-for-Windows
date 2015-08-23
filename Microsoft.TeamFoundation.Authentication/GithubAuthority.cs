using System;
using System.Diagnostics;
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
        public const string GitHubApiAcceptsHeaderValue = "application/vnd.github.v3+json";
        /// <summary>
        /// The maximum wait time for a network request before timing out
        /// </summary>
        public const int RequestTimeout = 15 * 1000; // 15 second limit

        public GithubAuthority(string authorityUrl = null)
        {
            _authorityUrl = authorityUrl ?? DefaultAuthorityUrl;
        }

        private readonly string _authorityUrl;

        public async Task<GithubAuthenticationResult> AcquireToken(
            Uri targetUri,
            string username,
            string password,
            string authenticationCode,
            GithubTokenScope scope)
        {
            const string GithubOptHeader = "X-GitHub-OTP";

            Trace.WriteLine("GithubAuthority::AcquireToken");

            Token token = null;

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
                httpClient.DefaultRequestHeaders.Add("Accept", GitHubApiAcceptsHeaderValue);

                string basicAuthValue = String.Format("{0}:{1}", username, password);
                byte[] authBytes = Encoding.UTF8.GetBytes(basicAuthValue);
                basicAuthValue = Convert.ToBase64String(authBytes);

                httpClient.DefaultRequestHeaders.Add("Authorization", "Basic " + basicAuthValue);

                if (!String.IsNullOrWhiteSpace(authenticationCode))
                {
                    httpClient.DefaultRequestHeaders.Add(GithubOptHeader, authenticationCode);
                }

                const string HttpJsonContentType = "application/x-www-form-urlencoded";
                const string JsonContentFormat = @"{{ ""scopes"": [""{0}""], ""note"": ""git: {1} on {2}"" }}";

                StringBuilder scopes = new StringBuilder();
                

                string jsonContent = String.Format(JsonContentFormat, targetUri, Environment.MachineName);

                using (StringContent content = new StringContent(jsonContent, Encoding.UTF8, HttpJsonContentType))
                using (HttpResponseMessage response = await httpClient.PostAsync(_authorityUrl, content))
                {
                    Trace.WriteLine("   server responded with " + response.StatusCode);

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
                                    token = new Token(tokenText, TokenType.Personal);
                                }

                                if (token == null)
                                {
                                    Trace.WriteLine("   authentication failure");
                                    return new GithubAuthenticationResult(GithubAuthenticationResultType.Failure)
                                }
                                else
                                {
                                    Trace.WriteLine("   authentication success: new personal acces token created.");
                                    return new GithubAuthenticationResult(GithubAuthenticationResultType.Success, token);
                                }
                            }

                        case HttpStatusCode.Unauthorized:
                            {
                                if (String.IsNullOrWhiteSpace(authenticationCode)
                                    && response.Headers.Any(x => String.Equals(GithubOptHeader, x.Key, StringComparison.OrdinalIgnoreCase)))
                                {
                                    var mfakvp = response.Headers.First(x => String.Equals(GithubOptHeader, x.Key, StringComparison.OrdinalIgnoreCase) && x.Value != null && x.Value.Count() > 0);

                                    if (mfakvp.Value.First().Contains("app"))
                                    {
                                        Trace.WriteLine("   two-factor app authentication code required");
                                        return new GithubAuthenticationResult(GithubAuthenticationResultType.TwoFactorApp);
                                    }
                                    else
                                    {
                                        Trace.WriteLine("   two-factor sms authentication code required");
                                        return new GithubAuthenticationResult(GithubAuthenticationResultType.TwoFactorSms);
                                    }
                                }
                                else
                                {
                                    Trace.WriteLine("   authentication failed");
                                    return new GithubAuthenticationResult(GithubAuthenticationResultType.Failure);
                                }
                            }

                        default:
                            Trace.WriteLine("   authentication failed");
                            return new GithubAuthenticationResult(GithubAuthenticationResultType.Failure);
                    }

                }
            }
        }

        public async Task<bool> ValidateCredentials(Uri targetUri, Credential credentials)
        {
            Debug.Assert(targetUri != null && targetUri.IsAbsoluteUri, "The `targetUri` parameter is null or invalid.");
            Debug.Assert(credentials != null, "The `targetUri` parameter is null or invalid.");

            Trace.WriteLine("   GithubAuthority::ValidateCredentials");

            string authstring = String.Format("{0}:{1}", credentials.Username, credentials.Password);
            byte[] authbytes = Encoding.UTF8.GetBytes(authstring);
            string authencode = Convert.ToBase64String(authbytes);

            HttpWebRequest request = WebRequest.CreateHttp(_authorityUrl);
            request.Headers.Add(HttpRequestHeader.Accept, GitHubApiAcceptsHeaderValue);
            request.Headers.Add(HttpRequestHeader.Authorization, "Basic " + authencode);
            request.Headers.Add(HttpRequestHeader.UserAgent, Global.GetUserAgent());

            try
            {
                using (HttpWebResponse response = await request.GetResponseAsync() as HttpWebResponse)
                {
                    // we're looking for 'OK 200' here, anything else is failure
                    Trace.WriteLine("   server returned: " + response.StatusCode);
                    return response.StatusCode == HttpStatusCode.OK;
                }
            }
            catch (WebException webException)
            {
                Trace.WriteLine("   server returned: " + webException.Message);
            }
            catch
            {
                Trace.WriteLine("   unexpected error");
            }

            Trace.WriteLine("   credential validation failed");
            return false;
        }
    }
}
