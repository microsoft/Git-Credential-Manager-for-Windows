using System;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Alm.Authentication;
using Microsoft.Alm.Authentication.Git;

namespace Atlassian.Bitbucket.Authentication.Rest
{
    public class RestClient : Base
    {
        internal static readonly Regex UsernameRegex = new Regex(@"\s*""username""\s*:\s*""([^""]+)""\s*", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

        public RestClient(RuntimeContext context)
            : base(context)
        { }

        public static string UserUrl
        {
            get { return "/2.0/user"; }
        }

        public async Task<AuthenticationResult> TryGetUser(TargetUri targetUri, int requestTimeout, Uri restRootUrl, string authHeader)
        {
            using (HttpClientHandler handler = targetUri.HttpClientHandler)
            {
                using (HttpClient httpClient = new HttpClient(handler) { Timeout = TimeSpan.FromMilliseconds(requestTimeout) })
                {
                    httpClient.DefaultRequestHeaders.Add("Authorization", authHeader);

                    var url = new Uri(restRootUrl, UserUrl).AbsoluteUri;
                    using (HttpResponseMessage response = await httpClient.GetAsync(url))
                    {
                        Trace.WriteLine($"server responded with {response.StatusCode}.");

                        switch (response.StatusCode)
                        {
                            case HttpStatusCode.OK:
                            case HttpStatusCode.Created:
                                {
                                    Trace.WriteLine("authentication success: new password token created.");

                                    // Get useername to cross check against supplied one
                                    var responseText = await response.Content.ReadAsStringAsync();
                                    var username = FindUsername(responseText);
                                    return new AuthenticationResult(AuthenticationResultType.Success, username);
                                }

                            case HttpStatusCode.Forbidden:
                                {
                                    // A 403/Forbidden response indicates the username/password are
                                    // recognized and good but 2FA is on in which case we want to
                                    // indicate that with the TwoFactor result
                                    Trace.WriteLine("two-factor app authentication code required");
                                    return new AuthenticationResult(AuthenticationResultType.TwoFactor);
                                }
                            case HttpStatusCode.Unauthorized:
                                {
                                    // username or password are wrong.
                                    Trace.WriteLine("authentication unauthorised");
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

        private static string FindUsername(string responseText)
        {
            Match usernameMatch;
            if ((usernameMatch = UsernameRegex.Match(responseText)).Success
                && usernameMatch.Groups.Count > 1)
            {
                string usernameText = usernameMatch.Groups[1].Value;
                Trace.WriteLine($"Found username [{usernameText}]");
                return usernameText;
            }

            return null;
        }
    }
}
