using System;
using System.Threading.Tasks;

namespace Microsoft.TeamFoundation.Git.Helpers.Authentication
{
    interface ILiveAuthority
    {
        Tokens AcquireToken(string clientId, string resource, Uri redirectUri, string queryParameters = null);
        Task<Tokens> AcquireTokenByRefreshTokenAsync(string clientId, string resource, Token refreshToken);
    }
}
