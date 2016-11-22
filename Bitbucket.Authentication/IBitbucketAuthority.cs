using Microsoft.Alm.Authentication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bitbucket.Authentication
{
    // TODO share
    internal interface IBitbucketAuthority
    {
        Task<BitbucketAuthenticationResult> AcquireToken(
    TargetUri targetUri,
    string username,
    string password,
    BitbucketAuthenticationResultType resultType,
    BitbucketTokenScope scope);

        Task<BitbucketAuthenticationResult> RefreshToken(
TargetUri targetUri,
string refreshToken);

        Task<bool> ValidateCredentials(TargetUri targetUri, string username, Credential credentials);

    }
}
