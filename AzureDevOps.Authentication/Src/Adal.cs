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
using ActiveDirectory = Microsoft.IdentityModel.Clients.ActiveDirectory;
using AdalExtentions = Microsoft.IdentityModel.Clients.ActiveDirectory.AuthenticationContextIntegratedAuthExtensions;
using Alm = Microsoft.Alm.Authentication;

namespace AzureDevOps.Authentication
{
    public interface IAdal : Alm.IRuntimeService
    {
        /// <summary>
        /// Initiates an interactive authentication experience.
        /// <para/>
        /// Returns an authentication result which contains an access token and other relevant information.
        /// </summary>
        /// <param name="authorityHostUrl">Address of the authority to issue token.</param>
        /// <param name="resource">Identifier of the target resource that is the recipient of the requested token.</param>
        /// <param name="clientId">Identifier of the client requesting the token.</param>
        /// <param name="redirectUri">Address to return to upon receiving a response from the authority.</param>
        /// <param name="extraQueryParameters">
        /// This parameter will be appended as is to the query string in the HTTP authentication request to the authority.
        /// <para/>
        /// The parameter can be null.
        /// </param>
        Task<IAdalResult> AcquireTokenAsync(string authorityHostUrl, string resource, string clientId, Uri redirectUri, string extraQueryParameters);

        /// <summary>
        /// Initiates a non-interactive authentication experience.
        /// <para/>
        /// Returns an authentication result which contains an access token and other relevant information.
        /// </summary>
        /// <param name="authorityHostUrl">Address of the authority to issue token.</param>
        /// <param name="resource">Identifier of the target resource that is the recipient of the requested token.</param>
        /// <param name="clientId">Identifier of the client requesting the token.</param>
        Task<IAdalResult> AcquireTokenAsync(string authorityHostUrl, string resource, string client);
    }

    /// <summary>
    /// Contains the results of one token acquisition operation.
    /// </summary>
    public interface IAdalResult
    {
        /// <summary>
        /// Gets the Access Token requested.
        /// </summary>
        string AccessToken { get; }

        /// <summary>
        /// Gets the type of the Access Token returned.
        /// </summary>
        string AccessTokenType { get; }

        /// <summary>
        /// Gets the authority that has issued the token.
        /// </summary>
        string Authority { get; }

        /// <summary>
        /// Gets an identifier for the tenant the token was acquired from.
        /// <para/>
        /// This property will be null if tenant information is not returned by the service.
        /// </summary>
        string TenantId { get; }
    }

    internal class Adal : Alm.Base, IAdal
    {
        public Adal(Alm.RuntimeContext context)
            : base(context)
        {
            _cache = new AdalTokenCache(context);
        }

        private readonly ActiveDirectory.TokenCache _cache;

        public Type ServiceType
            => typeof(IAdal);

        public async Task<IAdalResult> AcquireTokenAsync(
            string authorityHostUrl,
            string resource,
            string clientId,
            Uri redirectUri,
            string extraQueryParameters)
        {
            if (authorityHostUrl is null)
                throw new ArgumentNullException(nameof(authorityHostUrl));
            if (resource is null)
                throw new ArgumentNullException(nameof(resource));
            if (clientId is null)
                throw new ArgumentNullException(nameof(clientId));
            if (redirectUri is null)
                throw new ArgumentNullException(nameof(redirectUri));
            if (extraQueryParameters is null)
                throw new ArgumentNullException(nameof(extraQueryParameters));

            try
            {
                var authenticationContext = new ActiveDirectory.AuthenticationContext(authorityHostUrl, _cache);
                var platformParameters = new ActiveDirectory.PlatformParameters(ActiveDirectory.PromptBehavior.SelectAccount);
                var userIdentifier = ActiveDirectory.UserIdentifier.AnyUser;

                ActiveDirectory.AuthenticationResult result = await authenticationContext.AcquireTokenAsync(resource,
                                                                                                            clientId,
                                                                                                            redirectUri,
                                                                                                            platformParameters,
                                                                                                            userIdentifier,
                                                                                                            extraQueryParameters);

                return new Result(result);
            }
            // We should just be able to catch AdalException here but due to an ADAL bug an HttpRequestException can be leaked:
            // https://github.com/AzureAD/azure-activedirectory-library-for-dotnet/issues/1285
            // Until we update to ADAL 4.x or MSAL, we should just workaround this problem.
            catch (Exception exception)
            {
                throw new AuthenticationException(exception);
            }
        }

        public async Task<IAdalResult> AcquireTokenAsync(
            string authorityHostUrl,
            string resource,
            string clientId)
        {
            if (authorityHostUrl is null)
                throw new ArgumentNullException(nameof(authorityHostUrl));
            if (resource is null)
                throw new ArgumentNullException(nameof(resource));
            if (clientId is null)
                throw new ArgumentNullException(nameof(clientId));

            try
            {
                var authenticationContext = new ActiveDirectory.AuthenticationContext(authorityHostUrl, _cache);
                var userCredential = new ActiveDirectory.UserCredential();

                ActiveDirectory.AuthenticationResult result = await AdalExtentions.AcquireTokenAsync(authenticationContext,
                                                                                                     resource,
                                                                                                     clientId,
                                                                                                     userCredential);

                return new Result(result);
            }
            // We should just be able to catch AdalException here but due to an ADAL bug an HttpRequestException can be leaked:
            // https://github.com/AzureAD/azure-activedirectory-library-for-dotnet/issues/1285
            // Until we update to ADAL 4.x or MSAL, we should just workaround this problem.
            catch (Exception exception)
            {
                throw new AuthenticationException(exception);
            }
        }

        [System.Diagnostics.DebuggerDisplay("{DebuggerDisplay, nq}")]
        internal class Result : IAdalResult
        {
            public Result(ActiveDirectory.AuthenticationResult result)
            {
                if (result is null)
                    throw new ArgumentNullException(nameof(result));

                _result = result;
                _resultAccessToken = result.AccessToken;
                _resultAuthority = result.Authority;
                _resultTenantId = result.TenantId;
                _resultTokenType = result.AccessTokenType;
            }

            public Result(string accessToken, string authority, string tenantId, string tokenType)
            {
                _resultAccessToken = accessToken;
                _resultAuthority = authority;
                _resultTenantId = tenantId;
                _resultTokenType = tokenType;
            }

            private readonly ActiveDirectory.AuthenticationResult _result;
            private readonly string _resultAccessToken;
            private readonly string _resultAuthority;
            private readonly string _resultTenantId;
            private readonly string _resultTokenType;

            public string AccessToken
            {
                get { return _resultAccessToken; }
            }

            public string AccessTokenType
            {
                get { return _resultTokenType; }
            }

            public string Authority
            {
                get { return _resultAuthority; }
            }

            public string TenantId
            {
                get { return _resultTenantId; }
            }

            internal ActiveDirectory.AuthenticationResult AuthenticationResult
                => _result;

            internal string DebuggerDisplay
            {
                get { return $"{nameof(Result)}: \"{AccessTokenType} @ {TenantId}\""; }
            }
        }
    }
}
