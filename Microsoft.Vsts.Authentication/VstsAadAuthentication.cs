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
using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace Microsoft.Alm.Authentication
{
    /// <summary>
    /// Facilitates Azure Directory authentication.
    /// </summary>
    public sealed class VstsAadAuthentication : BaseVstsAuthentication, IVstsAadAuthentication
    {
        /// <summary>
        /// Creates a new instance of `<see cref="VstsAadAuthentication"/>`.
        /// </summary>
        /// <param name="tenantId">
        /// URI of the responsible Azure tenant.
        /// <para/>
        /// Use `<see cref="BaseVstsAuthentication.GetAuthentication"/>` to detect the tenant identity and create the authentication object.
        /// </param>
        /// <param name="tokenScope">The scope of all access tokens acquired by the authority.</param>
        /// <param name="personalAccessTokenStore">The secure secret store for storing any personal access tokens acquired.</param>
        /// <param name="adaRefreshTokenStore">The secure secret store for storing any Azure tokens acquired.</param>
        public VstsAadAuthentication(
            RuntimeContext context,
            Guid tenantId,
            VstsTokenScope tokenScope,
            ICredentialStore personalAccessTokenStore)
            : base(context, tokenScope, personalAccessTokenStore)
        {
            if (tenantId == Guid.Empty)
            {
                VstsAuthority = new VstsAzureAuthority(context, AzureAuthority.DefaultAuthorityHostUrl);
            }
            else
            {
                // Create an authority host URL in the format of https://login.microsoft.com/12345678-9ABC-DEF0-1234-56789ABCDEF0.
                string authorityHost = AzureAuthority.GetAuthorityUrl(tenantId);
                VstsAuthority = new VstsAzureAuthority(context, authorityHost);
            }
        }

        /// <summary>
        /// Test constructor which allows for using fake credential stores.
        /// </summary>
        internal VstsAadAuthentication(
            RuntimeContext context,
            ICredentialStore personalAccessTokenStore,
            ITokenStore vstsIdeTokenCache,
            IVstsAuthority vstsAuthority)
            : base(context,
                   personalAccessTokenStore,
                   vstsIdeTokenCache,
                   vstsAuthority)
        { }

        /// <summary>
        /// Creates an interactive logon session, using ADAL secure browser GUI, which enables users to authenticate with the Azure tenant and acquire the necessary access tokens to exchange for a VSTS personal access token.
        /// <para/>
        /// Tokens acquired are stored in the secure secret stores provided during initialization.
        /// <para/>
        /// Return a `<see cref="Credential"/>` for resource access if successful; otherwise `<see langword="null"/>`.
        /// </summary>
        /// <param name="targetUri">The URI of the VSTS resource.</param>
        public async Task<Credential> InteractiveLogon(TargetUri targetUri, PersonalAccessTokenOptions options)
        {
            BaseSecureStore.ValidateTargetUri(targetUri);

            try
            {
                Token token;
                if ((token = await VstsAuthority.InteractiveAcquireToken(targetUri, ClientId, Resource, new Uri(RedirectUrl), null)) != null)
                {
                    Trace.WriteLine($"token acquisition for '{targetUri}' succeeded.");

                    return await GeneratePersonalAccessToken(targetUri, token, options);
                }
            }
            catch (AdalException)
            {
                Trace.WriteLine($"token acquisition for '{targetUri}' failed.");
            }

            Trace.WriteLine($"interactive logon for '{targetUri}' failed");
            return null;
        }

        /// <summary>
        /// Creates an interactive logon session, using ADAL secure browser GUI, which enables users to authenticate with the Azure tenant and acquire the necessary access tokens to exchange for a VSTS personal access token.
        /// <para/>
        /// Tokens acquired are stored in the secure secret stores provided during initialization.
        /// <para/>
        /// Return a `<see cref="Credential"/>` for resource access if successful; otherwise `<see langword="null"/>`.
        /// </summary>
        /// <param name="targetUri">The URI of the VSTS resource.</param>
        /// <param name="requestCompactToken">
        /// Requests a compact format personal access token if `<see langword="true"/>`; otherwise requests a standard format personal access token.
        /// <para/>
        /// Compact tokens are necessary for clients which have restrictions on the size of the basic authentication header which they can create (example: Git).
        /// </param>
        [Obsolete("Please use Task<Credential> InteractiveLogon(TargetUri targetUri, PersonalAccessTokenOptions options) instead.", false)]
        public async Task<Credential> InteractiveLogon(TargetUri targetUri, bool requestCompactToken)
        {
            BaseSecureStore.ValidateTargetUri(targetUri);

            try
            {
                Token token;
                if ((token = await VstsAuthority.InteractiveAcquireToken(targetUri, ClientId, Resource, new Uri(RedirectUrl), null)) != null)
                {
                    Trace.WriteLine($"token acquisition for '{targetUri}' succeeded.");

                    return await GeneratePersonalAccessToken(targetUri, token, requestCompactToken);
                }
            }
            catch (AdalException)
            {
                Trace.WriteLine($"token acquisition for '{targetUri}' failed.");
            }

            Trace.WriteLine($"interactive logon for '{targetUri}' failed");
            return null;
        }

        /// <summary>
        /// Uses Active Directory Federation Services to authenticate with the Azure tenant non-interactively and acquire the necessary access tokens to exchange for a VSTS personal access token.
        /// <para/>
        /// Tokens acquired are stored in the secure secret stores provided during initialization.
        /// <para/>
        /// Return a `<see cref="Credential"/>` for resource access if successful; otherwise `<see langword="null"/>`.
        /// </summary>
        /// <param name="targetUri">The URL of the VSTS resource.</param>
        /// <param name="options">Options related to VSTS personal access creation.</param>
        public async Task<Credential> NoninteractiveLogon(TargetUri targetUri, PersonalAccessTokenOptions options)
        {
            BaseSecureStore.ValidateTargetUri(targetUri);

            try
            {
                Token token;
                if ((token = await VstsAuthority.NoninteractiveAcquireToken(targetUri, ClientId, Resource, new Uri(RedirectUrl))) != null)
                {
                    Trace.WriteLine($"token acquisition for '{targetUri}' succeeded");

                    return await GeneratePersonalAccessToken(targetUri, token, options);
                }
            }
            catch (AdalException)
            {
                Trace.WriteLine($"failed to acquire for '{targetUri}' token from VstsAuthority.");
            }

            Trace.WriteLine($"non-interactive logon for '{targetUri}' failed");
            return null;
        }

        /// <summary>
        /// Uses Active Directory Federation Services to authenticate with the Azure tenant non-interactively and acquire the necessary access tokens to exchange for a VSTS personal access token.
        /// <para/>
        /// Tokens acquired are stored in the secure secret stores provided during initialization.
        /// <para/>
        /// Return a `<see cref="Credential"/>` for resource access if successful; otherwise `<see langword="null"/>`.
        /// </summary>
        /// <param name="targetUri">The URL of the VSTS resource.</param>
        /// <param name="requestCompactToken">
        /// Requests a compact format personal access token if `<see langword="true"/>`; otherwise requests a standard format personal access token.
        /// <para/>
        /// Compact tokens are necessary for clients which have restrictions on the size of the basic authentication header which they can create (example: Git).
        /// </param>
        [Obsolete("Please use Task<Credential> NoninteractiveLogon(TargetUri targetUri, PersonalAccessTokenOptions options) instead.", false)]
        public async Task<Credential> NoninteractiveLogon(TargetUri targetUri, bool requestCompactToken)
        {
            BaseSecureStore.ValidateTargetUri(targetUri);

            try
            {
                Token token;
                if ((token = await VstsAuthority.NoninteractiveAcquireToken(targetUri, ClientId, Resource, new Uri(RedirectUrl))) != null)
                {
                    Trace.WriteLine($"token acquisition for '{targetUri}' succeeded");

                    return await GeneratePersonalAccessToken(targetUri, token, requestCompactToken);
                }
            }
            catch (AdalException)
            {
                Trace.WriteLine($"failed to acquire for '{targetUri}' token from VstsAuthority.");
            }

            Trace.WriteLine($"non-interactive logon for '{targetUri}' failed");
            return null;
        }

        /// <summary>
        /// Sets credentials for future use with this authentication object.
        /// </summary>
        /// <remarks>Not supported.</remarks>
        /// <param name="targetUri">
        /// The uniform resource indicator of the resource access tokens are being set for.
        /// </param>
        /// <param name="credentials">The credentials being set.</param>
        public override Task<bool> SetCredentials(TargetUri targetUri, Credential credentials)
        {
            BaseSecureStore.ValidateTargetUri(targetUri);
            BaseSecureStore.ValidateCredential(credentials);

            return Task.FromResult(false);
        }
    }
}
