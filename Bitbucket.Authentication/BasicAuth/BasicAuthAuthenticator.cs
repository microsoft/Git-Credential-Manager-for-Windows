﻿using System;
using System.Text;
using System.Threading.Tasks;
using Atlassian.Bitbucket.Authentication.Rest;
using Microsoft.Alm.Authentication;

namespace Atlassian.Bitbucket.Authentication.BasicAuth
{
    internal class BasicAuthAuthenticator: BaseType
    {
        public BasicAuthAuthenticator(RuntimeContext context)
            : base(context)
        { }

        public async Task<AuthenticationResult> GetAuthAsync(TargetUri targetUri, TokenScope scope, int requestTimeout, Uri restRootUrl, Credential credentials)
        {
            // use the provided username and password and attempt a Basic Auth request to a known
            // REST API resource.
            var result = await new RestClient(Context).TryGetUser(targetUri, requestTimeout, restRootUrl, credentials);

            if (result.Type.Equals(AuthenticationResultType.Success))
            {
                // Success with username/password indicates 2FA is not on so the 'token' is actually
                // the password if we had a successful call then the password is good.
                var token = new Token(credentials.Password, TokenType.PersonalAccess);
                if (!string.IsNullOrWhiteSpace(result.RemoteUsername) && !credentials.Username.Equals(result.RemoteUsername))
                {
                    Trace.WriteLine($"Remote username [{result.RemoteUsername}] != [{credentials.Username}] supplied username");
                    return new AuthenticationResult(AuthenticationResultType.Success, token, result.RemoteUsername);
                }

                return new AuthenticationResult(AuthenticationResultType.Success, token);
            }

            Trace.WriteLine("authentication failed");
            return result;
        }
    }
}
