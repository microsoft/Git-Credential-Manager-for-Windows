using System;
using System.Text;
using System.Threading.Tasks;
using Atlassian.Bitbucket.Authentication.Rest;
using Microsoft.Alm.Authentication;
using Microsoft.Alm.Authentication.Git;

namespace Atlassian.Bitbucket.Authentication.BasicAuth
{
    internal class BasicAuthAuthenticator : Base
    {
        public BasicAuthAuthenticator(RuntimeContext context)
            : base(context)
        { }

        public async Task<AuthenticationResult> GetAuthAsync(TargetUri targetUri, TokenScope scope, int requestTimeout, Uri restRootUrl, string username, string password)
        {
            // use the provided username and password and attempt a Basic Auth request to a known
            // REST API resource.

            string basicAuthValue = string.Format("{0}:{1}", username, password);
            byte[] authBytes = Encoding.UTF8.GetBytes(basicAuthValue);
            basicAuthValue = Convert.ToBase64String(authBytes);
            var authHeader = "Basic " + basicAuthValue;

            var result = await ( new RestClient(Context)).TryGetUser(targetUri, requestTimeout, restRootUrl, authHeader);

            if (result.Type.Equals(AuthenticationResultType.Success))
            {
                // Success with username/passord indicates 2FA is not on so the 'token' is actually
                // the password if we had a successful call then the password is good.
                var token = new Token(password, TokenType.Personal);
                if (!string.IsNullOrWhiteSpace(result.RemoteUsername) && !username.Equals(result.RemoteUsername))
                {
                    Trace.WriteLine($"Remote username [{result.RemoteUsername}] != [{username}] supplied username");
                    return new AuthenticationResult(AuthenticationResultType.Success, token, result.RemoteUsername);
                }

                return new AuthenticationResult(AuthenticationResultType.Success, token);
            }

            Trace.WriteLine("authentication failed");
            return result;
        }
    }
}
