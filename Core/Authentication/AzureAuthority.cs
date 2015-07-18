using System;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace Microsoft.TeamFoundation.Git.Helpers.Authentication
{
    /// <summary>
    /// Interfaces with Azure to perform authentication and identity services.
    /// </summary>
    internal class AzureAuthority : IAzureAuthority
    {
        /// <summary>
        /// The base URL for logon services in Azure.
        /// </summary>
        public const string AuthorityHostUrlBase = "https://login.microsoftonline.com";
        /// <summary>
        /// The common Url for logon services in Azure.
        /// </summary>
        public const string DefaultAuthorityHostUrl = AuthorityHostUrlBase + "/common";

        /// <summary>
        /// Creates a new <see cref="AzureAuthority"/> with an optional authority host url.
        /// </summary>
        /// <param name="authorityHostUrl">Optional: sets a non-default authority host url.</param>
        public AzureAuthority(string authorityHostUrl = DefaultAuthorityHostUrl)
        {
            Debug.Assert(Uri.IsWellFormedUriString(authorityHostUrl, UriKind.Absolute), "The authorityHostUrl parameter is invalid.");

            AuthorityHostUrl = authorityHostUrl;
            _adalTokenCache = new VsoAdalTokenCache();
        }

        private readonly VsoAdalTokenCache _adalTokenCache;

        /// <summary>
        /// The URL use to interact with the Azure identity service.
        /// </summary>
        public string AuthorityHostUrl { get; protected set; }

        /// <summary>
        /// Aquires a <see cref="TokenPair"/> from the authority via an interactive user logon 
        /// prompt.
        /// </summary>
        /// <param name="targetUri">
        /// The uniform resource indicator of the resource access tokens are being requested for.
        /// </param>
        /// <param name="clientId">Identifier of the client requesting the token.</param>
        /// <param name="resource">
        /// Identifier of the target resource that is the recipient of the requested token.
        /// </param>
        /// <param name="redirectUri">
        /// Address to return to upon receiving a response from the authority.
        /// </param>
        /// <param name="queryParameters">
        /// Optional: appended as is to the query string in the HTTP authentication request to the 
        /// authority.
        /// </param>
        /// <returns>If successful a <see cref="TokenPair"/>; otherwise `null`.</returns>
        public TokenPair AcquireToken(Uri targetUri, string clientId, string resource, Uri redirectUri, string queryParameters = null)
        {
            Debug.Assert(targetUri != null && targetUri.IsAbsoluteUri, "The targetUri parameter is null or invalid");
            Debug.Assert(!String.IsNullOrWhiteSpace(clientId), "The clientId parameter is null or empty");
            Debug.Assert(!String.IsNullOrWhiteSpace(resource), "The resource parameter is null or empty");
            Debug.Assert(redirectUri != null, "The redirectUri parameter is null");
            Debug.Assert(redirectUri.IsAbsoluteUri, "The redirectUri parameter is not an absolute Uri");

            Trace.WriteLine("AzureAuthority::AquireToken");

            TokenPair tokens = null;
            queryParameters = queryParameters ?? String.Empty;

            try
            {
                AuthenticationContext authCtx = new AuthenticationContext(AuthorityHostUrl, _adalTokenCache);
                AuthenticationResult authResult = authCtx.AcquireToken(resource, clientId, redirectUri, PromptBehavior.Always, UserIdentifier.AnyUser, queryParameters);
                tokens = new TokenPair(authResult);

                Trace.WriteLine("    token acquisition succeeded.");
            }
            catch (AdalException exception)
            {
                Trace.WriteLine("    token acquisition failed.");
                Debug.Write(exception);
            }

            return tokens;
        }

        /// <summary>
        /// Aquires a <see cref="TokenPair"/> from the authority using optionally provided 
        /// credentials or via the current identity.
        /// </summary>
        /// <param name="targetUri">
        /// The uniform resource indicator of the resource access tokens are being requested for.
        /// </param>
        /// <param name="clientId">Identifier of the client requesting the token.</param>
        /// <param name="resource">
        /// Identifier of the target resource that is the recipient of the requested token.
        /// </param>
        /// <param name="credentials">Optional: user credential to use for token acquisition.</param>
        /// <returns>If successful a <see cref="TokenPair"/>; otherwise `null`.</returns>
        public async Task<TokenPair> AcquireTokenAsync(Uri targetUri, string clientId, string resource, Credential credentials = null)
        {
            Debug.Assert(targetUri != null && targetUri.IsAbsoluteUri, "The targetUri parameter is null or invalid");
            Debug.Assert(!String.IsNullOrWhiteSpace(clientId), "The clientId parameter is null or empty");
            Debug.Assert(!String.IsNullOrWhiteSpace(resource), "The resource parameter is null or empty");

            Trace.WriteLine("AzureAuthority::AcquireTokenAsync");

            TokenPair tokens = null;

            try
            {
                UserCredential userCredential = credentials == null ? new UserCredential() : new UserCredential(credentials.Username, credentials.Password);
                AuthenticationContext authCtx = new AuthenticationContext(AuthorityHostUrl, _adalTokenCache);
                AuthenticationResult authResult = await authCtx.AcquireTokenAsync(resource, clientId, userCredential);
                tokens = new TokenPair(authResult);

                Trace.WriteLine("    token acquisition succeeded.");
            }
            catch (AdalException exception)
            {
                Trace.WriteLine("    token acquisition failed.");
                Debug.WriteLine(exception);
            }

            return tokens;
        }

        /// <summary>
        /// Acquires an access token from the authority using a previously aquired refresh token.
        /// </summary>
        /// <param name="targetUri">
        /// The uniform resource indicator of the resource access tokens are being requested for.
        /// </param>
        /// <param name="clientId">Identifier of the client requesting the token.</param>
        /// <param name="resource">
        /// Identifier of the target resource that is the recipient of the requested token.
        /// </param>
        /// <param name="refreshToken">The <see cref="Token"/> of type <see cref="TokenType.Refresh"/>
        /// to be used to aquire the access token.</param>
        /// <returns>If successful a <see cref="TokenPair"/>; otherwise `null`.</returns>
        public async Task<TokenPair> AcquireTokenByRefreshTokenAsync(Uri targetUri, string clientId, string resource, Token refreshToken)
        {
            Debug.Assert(targetUri != null && targetUri.IsAbsoluteUri, "The targetUri parameter is null or invalid");
            Debug.Assert(!String.IsNullOrWhiteSpace(clientId), "The clientId parameter is null or empty");
            Debug.Assert(!String.IsNullOrWhiteSpace(resource), "The resource parameter is null or empty");
            Debug.Assert(refreshToken != null, "The refreshToken parameter is null");
            Debug.Assert(refreshToken.Type == TokenType.Refresh, "The value of refreshToken parameter is not a refresh token");
            Debug.Assert(!String.IsNullOrWhiteSpace(refreshToken.Value), "The value of refreshToken parameter is null or empty");

            TokenPair tokens = null;

            try
            {
                AuthenticationContext authCtx = new AuthenticationContext(AuthorityHostUrl, _adalTokenCache);
                AuthenticationResult authResult = await authCtx.AcquireTokenByRefreshTokenAsync(refreshToken.Value, clientId, resource);
                tokens = new TokenPair(authResult);

                Trace.WriteLine("    token acquisition succeeded.");
            }
            catch (AdalException exception)
            {
                Trace.WriteLine("    token acquisition failed.");
                Debug.WriteLine(exception);
            }

            return tokens;
        }
    }
}
