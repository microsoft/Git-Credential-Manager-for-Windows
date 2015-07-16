using System;
using System.Threading.Tasks;

namespace Microsoft.TeamFoundation.Git.Helpers.Authentication
{
    internal interface IAzureAuthority
    {
        Tokens AcquireToken(Uri targetUri, string clientId, string resource, Uri redirectUri, string queryParameters = null);
        Task<Tokens> AcquireTokenAsync(Uri targetUri, string clientId, string resource, Credential credentials = null);
        Task<Tokens> AcquireTokenByRefreshTokenAsync(Uri targetUri, string clientId, string resource, Token refreshToken);
    }
}
