using System;
using System.Threading.Tasks;

namespace Microsoft.Alm.Authentication
{
    internal interface IAzureAuthority
    {
        TokenPair AcquireToken(TargetUri targetUri, string clientId, string resource, Uri redirectUri, string queryParameters = null);
        Task<TokenPair> AcquireTokenAsync(TargetUri targetUri, string clientId, string resource, Credential credentials = null);
        Task<TokenPair> AcquireTokenByRefreshTokenAsync(TargetUri targetUri, string clientId, string resource, Token refreshToken);
    }
}
