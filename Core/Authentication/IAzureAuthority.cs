using System;
using System.Threading.Tasks;

namespace Microsoft.TeamFoundation.Git.Helpers.Authentication
{
    internal interface IAzureAuthority
    {
        Tokens AcquireToken(string clientId, string resource, Uri redirectUri, string queryParameters = null);
        Task<Tokens> AcquireTokenAsync(string clientId, string resource, Credential credentials = null);
        Task<Tokens> AcquireTokenByRefreshTokenAsync(string clientId, string resource, Token refreshToken);
    }
}
