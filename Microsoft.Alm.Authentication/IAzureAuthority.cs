using System;
using System.Threading.Tasks;

namespace Microsoft.Alm.Authentication
{
    internal interface IAzureAuthority
    {
        TokenPair AcquireToken(Uri targetUri, string clientId, string resource, Uri redirectUri, string queryParameters = null);
        Task<TokenPair> AcquireTokenAsync(Uri targetUri, string clientId, string resource, Credential credentials = null);
        Task<TokenPair> AcquireTokenByRefreshTokenAsync(Uri targetUri, string clientId, string resource, Token refreshToken);
    }
}
