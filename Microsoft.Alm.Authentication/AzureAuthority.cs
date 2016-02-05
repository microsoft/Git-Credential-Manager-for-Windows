using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace Microsoft.Alm.Authentication
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
            _adalTokenCache = new VstsAdalTokenCache();
        }

        private readonly VstsAdalTokenCache _adalTokenCache;

        /// <summary>
        /// The URL used to interact with the Azure identity service.
        /// </summary>
        public string AuthorityHostUrl { get; protected set; }

        /// <summary>
        /// acquires a <see cref="TokenPair"/> from the authority via an interactive user logon 
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
        /// Optional: appended as-is to the query string in the HTTP authentication request to the
        /// authority.
        /// </param>
        /// <returns>If successful a <see cref="TokenPair"/>; otherwise <see langword="null"/>.</returns>
        public TokenPair AcquireToken(Uri targetUri, string clientId, string resource, Uri redirectUri, string queryParameters = null)
        {
            Debug.Assert(targetUri != null && targetUri.IsAbsoluteUri, "The targetUri parameter is null or invalid");
            Debug.Assert(!String.IsNullOrWhiteSpace(clientId), "The clientId parameter is null or empty");
            Debug.Assert(!String.IsNullOrWhiteSpace(resource), "The resource parameter is null or empty");
            Debug.Assert(redirectUri != null, "The redirectUri parameter is null");
            Debug.Assert(redirectUri.IsAbsoluteUri, "The redirectUri parameter is not an absolute Uri");

            Trace.WriteLine("AzureAuthority::AcquireToken");

            TokenPair tokens = null;
            queryParameters = queryParameters ?? String.Empty;

            try
            {
                Trace.WriteLine(String.Format("   authority host url = '{0}'.", AuthorityHostUrl));

                AuthenticationContext authCtx = new AuthenticationContext(AuthorityHostUrl, _adalTokenCache);
                AuthenticationResult authResult = authCtx.AcquireToken(resource, clientId, redirectUri, PromptBehavior.Always, UserIdentifier.AnyUser, queryParameters);
                tokens = new TokenPair(authResult);

                Trace.WriteLine("   token acquisition succeeded.");
            }
            catch (AdalException)
            {
                Trace.WriteLine("   token acquisition failed.");
            }

            return tokens;
        }

        /// <summary>
        /// acquires a <see cref="TokenPair"/> from the authority using optionally provided 
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
        /// <returns>If successful a <see cref="TokenPair"/>; otherwise <see langword="null"/>.</returns>
        public async Task<TokenPair> AcquireTokenAsync(Uri targetUri, string clientId, string resource, Credential credentials = null)
        {
            Debug.Assert(targetUri != null && targetUri.IsAbsoluteUri, "The targetUri parameter is null or invalid");
            Debug.Assert(!String.IsNullOrWhiteSpace(clientId), "The clientId parameter is null or empty");
            Debug.Assert(!String.IsNullOrWhiteSpace(resource), "The resource parameter is null or empty");

            Trace.WriteLine("AzureAuthority::AcquireTokenAsync");

            TokenPair tokens = null;

            try
            {
                Trace.WriteLine(String.Format("   authority host url = '{0}'.", AuthorityHostUrl));

                UserCredential userCredential = credentials == null ? new UserCredential() : new UserCredential(credentials.Username, credentials.Password);
                AuthenticationContext authCtx = new AuthenticationContext(AuthorityHostUrl, _adalTokenCache);
                AuthenticationResult authResult = await authCtx.AcquireTokenAsync(resource, clientId, userCredential);
                tokens = new TokenPair(authResult);

                Trace.WriteLine("   token acquisition succeeded.");
            }
            catch (AdalException)
            {
                Trace.WriteLine("   token acquisition failed.");
            }

            return tokens;
        }

        /// <summary>
        /// Acquires an access token from the authority using a previously acquired refresh token.
        /// </summary>
        /// <param name="targetUri">
        /// The uniform resource indicator of the resource access tokens are being requested for.
        /// </param>
        /// <param name="clientId">Identifier of the client requesting the token.</param>
        /// <param name="resource">
        /// Identifier of the target resource that is the recipient of the requested token.
        /// </param>
        /// <param name="refreshToken">The <see cref="Token"/> of type <see cref="TokenType.Refresh"/>
        /// to be used to acquire the access token.</param>
        /// <returns>If successful a <see cref="TokenPair"/>; otherwise <see langword="null"/>.</returns>
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
                string authorityHostUrl = AuthorityHostUrl;

                if (refreshToken.TargetIdentity != Guid.Empty)
                {
                    authorityHostUrl = GetAuthorityUrl(refreshToken.TargetIdentity);

                    Trace.WriteLine("   authority host url set by refresh token.");
                }

                Trace.WriteLine(String.Format("   authority host url = '{0}'.", authorityHostUrl));

                AuthenticationContext authCtx = new AuthenticationContext(authorityHostUrl, _adalTokenCache);
                AuthenticationResult authResult = await authCtx.AcquireTokenByRefreshTokenAsync(refreshToken.Value, clientId, resource);
                tokens = new TokenPair(authResult);

                Trace.WriteLine("   token acquisition succeeded.");
            }
            catch (AdalException)
            {
                Trace.WriteLine("   token acquisition failed.");
            }

            return tokens;
        }

        public static string GetAuthorityUrl(Guid tenantId)
        {
            return String.Format("{0}/{1:D}", AzureAuthority.AuthorityHostUrlBase, tenantId);
        }
    }
}
