using Microsoft.Alm.Authentication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Atlassian.Bitbucket.Authentication
{
    // TODO share
    internal interface IAuthority
    {
        Task<AuthenticationResult> AcquireToken(
            TargetUri targetUri,
            string username,
            string password,
            AuthenticationResultType resultType,
            TokenScope scope);

        Task<AuthenticationResult> RefreshToken(
            TargetUri targetUri,
            string refreshToken);

        Task<bool> ValidateCredentials(TargetUri targetUri, string username, Credential credentials);
    }
}