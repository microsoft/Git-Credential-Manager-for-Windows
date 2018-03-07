using System;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Alm.Authentication.Test
{
    internal class AuthorityFake : IVstsAuthority
    {
        public AuthorityFake(string expectedQueryParameters)
        {
            ExpectedQueryParameters = expectedQueryParameters;

            CredentialsAreValid = true;
        }

        internal readonly string ExpectedQueryParameters;

        public bool CredentialsAreValid { get; set; }

        /// <summary>
        /// Generates a personal access token for use with Visual Studio Team Services.
        /// <para/>
        /// Returns the acquired token if successful; otherwise <see langword="null"/>;
        /// </summary>
        /// <param name="targetUri">The uniform resource indicator of the resource access tokens are being requested for.</param>
        /// <param name="accessToken">Access token granted by the identity authority (Azure).</param>
        /// <param name="tokenScope">The requested access scopes to be granted to the token.</param>
        /// <param name="requireCompactToken">`<see langword="true"/>` if requesting a compact format token; otherwise `<see langword="false"/>`.</param>
        /// <param name="tokenDuration">
        /// The requested lifetime of the requested token.
        /// <para/>
        /// The authority granting the token decides the actual lifetime of any token granted, regardless of the duration requested.
        /// </param>
        public async Task<Token> GeneratePersonalAccessToken(TargetUri targetUri, Token accessToken, VstsTokenScope tokenScope, bool requireCompactToken, TimeSpan? tokenDuration)
        {
            return await Task.FromResult(new Token("personal-access-token", TokenType.Personal));
        }

        /// <summary>
        /// Acquires a <see cref="Token"/> from the authority via an interactive user logon prompt.
        /// <para/>
        /// Returns a `<see cref="Token"/>` is successful; otherwise <see langword="null"/>.
        /// </summary>
        /// <param name="targetUri">Uniform resource indicator of the resource access tokens are being requested for.</param>
        /// <param name="clientId">Identifier of the client requesting the token.</param>
        /// <param name="resource">Identifier of the target resource that is the recipient of the requested token.</param>
        /// <param name="redirectUri">Address to return to upon receiving a response from the authority.</param>
        /// <param name="queryParameters">optional value, appended as-is to the query string in the HTTP authentication request to the authority.</param>
        public async Task<Token> InteractiveAcquireToken(TargetUri targetUri, string clientId, string resource, Uri redirectUri, string queryParameters = null)
        {
            Assert.Equal(ExpectedQueryParameters, queryParameters);

            return await Task.FromResult(new Token("token-access", TokenType.AzureAccess));
        }

        /// <summary>
        /// Acquires a `<see cref="Token"/>` from the authority via an non-interactive user logon.
        /// <para/>
        /// Returns the acquired `<see cref="Token"/>` if successful; otherwise `<see langword="null"/>`.
        /// </summary>
        /// <param name="targetUri">Uniform resource indicator of the resource access tokens are being requested for.</param>
        /// <param name="clientId">Identifier of the client requesting the token.</param>
        /// <param name="resource">Identifier of the target resource that is the recipient of the requested token.</param>
        /// <param name="redirectUri">Address to return to upon receiving a response from the authority.</param>
        public async Task<Token> NoninteractiveAcquireToken(TargetUri targetUri, string clientId, string resource, Uri redirectUri)
        {
            return await Task.FromResult(new Token("token-access", TokenType.AzureAccess));
        }

        /// <summary>
        /// Validates that a `<see cref="Credential"/>` is valid to grant access to the VSTS resource referenced by `<paramref name="targetUri"/>`.
        /// <para/>
        /// Returns `<see langword="true"/>` if successful; otherwise `<see langword="false"/>`.
        /// </summary>
        /// <param name="targetUri">URI of the VSTS resource.</param>
        /// <param name="credentials">`<see cref="Credential"/>` expected to grant access to the VSTS service.</param>
        public async Task<bool> ValidateCredentials(TargetUri targetUri, Credential credentials)
        {
            return await Task.Run(() =>
            {
                try
                {
                    BaseSecureStore.ValidateCredential(credentials);
                    return CredentialsAreValid;
                }
                catch { }
                return false;
            });
        }

        /// <summary>
        /// Validates that a `<see cref="Token"/>` is valid to grant access to the VSTS resource referenced by `<paramref name="targetUri"/>`.
        /// <para/>
        /// Returns `<see langword="true"/>` if successful; otherwise `<see langword="false"/>`.
        /// </summary>
        /// <param name="targetUri">URI of the VSTS resource.</param>
        /// <param name="token">`<see cref="Token"/>` expected to grant access to the VSTS resource.</param>
        public async Task<bool> ValidateToken(TargetUri targetUri, Token token)
        {
            return await Task.Run(() =>
            {
                try
                {
                    Token.Validate(token);
                    return true;
                }
                catch { }
                return false;
            });
        }
    }
}
