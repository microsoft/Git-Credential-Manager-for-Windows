/**** Git Credential Manager for Windows ****
 *
 * Copyright (c) Microsoft Corporation
 * All rights reserved.
 *
 * MIT License
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the """"Software""""), to deal
 * in the Software without restriction, including without limitation the rights to
 * use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
 * the Software, and to permit persons to whom the Software is furnished to do so,
 * subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED *AS IS*, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
 * FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
 * COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN
 * AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
 * WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE."
**/

using System;
using System.Threading.Tasks;

using Adal = Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace Microsoft.Alm.Authentication
{
    /// <summary>
    /// Interfaces with Azure to perform authentication and identity services.
    /// </summary>
    internal class AzureAuthority : BaseType, IAzureAuthority
    {
        /// <summary>
        /// The base URL for logon services in Azure.
        /// </summary>
        public const string AuthorityHostUrlBase = "https://login.microsoftonline.com";

        /// <summary>
        /// The common URL for logon services in Azure.
        /// </summary>
        public const string DefaultAuthorityHostUrl = AuthorityHostUrlBase + "/common";

        /// <summary>
        /// Creates a new instance of `<see cref="AzureAuthority"/>`.
        /// </summary>
        /// <param name="authorityHostUrl">A non-default authority host URL; otherwise defaults to `<see cref="DefaultAuthorityHostUrl"/>`.</param>
        public AzureAuthority(RuntimeContext context, string authorityHostUrl = DefaultAuthorityHostUrl)
            : base(context)
        {
            if (authorityHostUrl is null)
                throw new ArgumentNullException(nameof(authorityHostUrl));
            if (!Uri.IsWellFormedUriString(authorityHostUrl, UriKind.Absolute))
            {
                var inner = new UriFormatException("Authority URL must be absolute.");
                throw new ArgumentException(inner.Message, nameof(authorityHostUrl), inner);
            }

            _authorityHostUrl = authorityHostUrl;
            _adalTokenCache = new VstsAdalTokenCache(context);
        }

        private readonly VstsAdalTokenCache _adalTokenCache;
        private string _authorityHostUrl;

        /// <summary>
        /// The URL used to interact with the Azure identity service.
        /// </summary>
        public string AuthorityHostUrl
        {
            get { return _authorityHostUrl; }
            protected set
            {
                if (value is null)
                    throw new ArgumentNullException(nameof(AuthorityHostUrl));

                _authorityHostUrl = value;
            }
        }

        /// <summary>
        /// Acquires a `<see cref="Token"/>` from the authority via an interactive user logon prompt.
        /// <para/>
        /// Returns a `<see cref="Token"/>` is successful; otherwise `<see langword="null"/>`.
        /// </summary>
        /// <param name="targetUri">Uniform resource indicator of the resource access tokens are being requested for.</param>
        /// <param name="clientId">Identifier of the client requesting the token.</param>
        /// <param name="resource">Identifier of the target resource that is the recipient of the requested token.</param>
        /// <param name="redirectUri">Address to return to upon receiving a response from the authority.</param>
        /// <param name="queryParameters">optional value, appended as-is to the query string in the HTTP authentication request to the authority.</param>
        public async Task<Token> InteractiveAcquireToken(TargetUri targetUri, string clientId, string resource, Uri redirectUri, string queryParameters = null)
        {
            if (targetUri is null)
                throw new ArgumentNullException(nameof(targetUri));
            if (string.IsNullOrWhiteSpace(clientId))
                throw new ArgumentNullException(nameof(clientId));
            if (string.IsNullOrWhiteSpace(resource))
                throw new ArgumentNullException(nameof(resource));
            if (redirectUri is null)
                throw new ArgumentNullException(nameof(redirectUri));
            if (!redirectUri.IsAbsoluteUri)
                throw new ArgumentException(nameof(redirectUri));

            Token token = null;
            queryParameters = queryParameters ?? string.Empty;

            try
            {
                Adal.AuthenticationContext authCtx = new Adal.AuthenticationContext(AuthorityHostUrl, _adalTokenCache);
                Adal.AuthenticationResult authResult = await authCtx.AcquireTokenAsync(resource,
                                                                                       clientId,
                                                                                       redirectUri,
                                                                                       new Adal.PlatformParameters(Adal.PromptBehavior.Always),
                                                                                       Adal.UserIdentifier.AnyUser,
                                                                                       queryParameters);
                Guid tenantId;
                if (Guid.TryParse(authResult.TenantId, out tenantId))
                {
                    token = new Token(authResult.AccessToken, tenantId, TokenType.AzureAccess);
                }

                Trace.WriteLine($"authority host URL = '{AuthorityHostUrl}', token acquisition succeeded.");
            }
            catch (Adal.AdalException)
            {
                Trace.WriteLine($"authority host URL = '{AuthorityHostUrl}', token acquisition failed.");
            }

            return token;
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
            if (targetUri is null)
                throw new ArgumentNullException(nameof(targetUri));
            if (string.IsNullOrWhiteSpace(clientId))
                throw new ArgumentNullException(nameof(clientId));
            if (string.IsNullOrWhiteSpace(resource))
                throw new ArgumentNullException(nameof(resource));
            if (redirectUri is null)
                throw new ArgumentNullException(nameof(redirectUri));
            if (!redirectUri.IsAbsoluteUri)
                throw new ArgumentException(nameof(redirectUri));

            Token token = null;

            try
            {
                Adal.AuthenticationContext authCtx = new Adal.AuthenticationContext(AuthorityHostUrl, _adalTokenCache);
                Adal.AuthenticationResult authResult = await Adal.AuthenticationContextIntegratedAuthExtensions.AcquireTokenAsync(authCtx,
                                                                                                                                  resource,
                                                                                                                                  clientId,
                                                                                                                                  new Adal.UserCredential());



                Guid tentantId;
                if (Guid.TryParse(authResult.TenantId, out tentantId))
                {
                    token = new Token(authResult.AccessToken, tentantId, TokenType.AzureAccess);

                    Trace.WriteLine($"token acquisition for authority host URL = '{AuthorityHostUrl}' succeeded.");
                }
            }
            catch (Adal.AdalException)
            {
                Trace.WriteLine($"token acquisition for authority host URL = '{AuthorityHostUrl}' failed.");
            }

            return token;
        }

        /// <summary>
        /// Returns the properly formatted URL for the Azure authority given a tenant identity.
        /// </summary>
        /// <param name="tenantId">Identity of the tenant.</param>
        public static string GetAuthorityUrl(Guid tenantId)
        {
            return string.Format("{0}/{1:D}", AzureAuthority.AuthorityHostUrlBase, tenantId);
        }
    }
}
