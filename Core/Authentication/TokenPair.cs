using System;
using System.Diagnostics;

namespace Microsoft.TeamFoundation.Git.Helpers.Authentication
{
    /// <summary>
    /// Azure token pair: access and refresh.
    /// </summary>
    internal sealed class TokenPair
    {
        /// <summary>
        /// Creates a new <see cref="TokenPair"/> from raw access and refresh token data.
        /// </summary>
        /// <param name="accessToken">The base64 encoded value of the access token's raw data</param>
        /// <param name="refreshToken">The base64 encoded value of the refresh token's raw data</param>
        public TokenPair(string accessToken, string refreshToken)
        {
            Debug.Assert(!String.IsNullOrWhiteSpace(accessToken), "The accessToken parameter is null or invalid.");
            Debug.Assert(!String.IsNullOrWhiteSpace(refreshToken), "The refreshToken parameter is null or invalid.");

            this.AccessToken = new Token(accessToken, TokenType.Access);
            this.RefeshToken = new Token(refreshToken, TokenType.Refresh);
        }
        /// <summary>
        /// Creates a new <see cref="TokenPair"/> from an ADAL <see cref="IdentityModel.Clients.ActiveDirectory.AuthenticationResult"/>.
        /// </summary>
        /// <param name="authResult">
        /// A successful <see cref="IdentityModel.Clients.ActiveDirectory.AuthenticationResult"/>
        /// which contains both access and refresh token data.
        /// </param>
        public TokenPair(IdentityModel.Clients.ActiveDirectory.AuthenticationResult authResult)
        {
            Debug.Assert(authResult != null, "The authResult parameter is null.");
            Debug.Assert(!String.IsNullOrWhiteSpace(authResult.AccessToken), "The authResult.AccessToken parameter is null or invalid.");
            Debug.Assert(!String.IsNullOrWhiteSpace(authResult.RefreshToken), "The authResult.RefreshToken parameter is null or invalid.");
            Debug.Assert(authResult.ExpiresOn > DateTimeOffset.UtcNow, "The authResult is expired and invalid.");

            this.AccessToken = new Token(authResult, TokenType.Access);
            this.RefeshToken = new Token(authResult, TokenType.Refresh);
        }

        /// <summary>
        /// Access token, used to grant access to resources.
        /// </summary>
        public readonly Token AccessToken;
        /// <summary>
        /// Refresh token, used to grant new access tokens.
        /// </summary>
        public readonly Token RefeshToken;
    }
}
