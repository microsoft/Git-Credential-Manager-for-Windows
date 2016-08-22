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
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace Microsoft.Alm.Authentication
{
    /// <summary>
    /// Base functionality for performing authentication operations against Visual Studio Online.
    /// </summary>
    public abstract class BaseVstsAuthentication : BaseAuthentication
    {
        public const string DefaultResource = "499b84ac-1321-427f-aa17-267ca6975798";
        public const string DefaultClientId = "872cd9fa-d31f-45e0-9eab-6e460a02d1f1";
        public const string RedirectUrl = "urn:ietf:wg:oauth:2.0:oob";

        protected const string AdalRefreshPrefix = "ada";

        protected BaseVstsAuthentication(VstsTokenScope tokenScope, ICredentialStore personalAccessTokenStore)
        {
            if (ReferenceEquals(tokenScope, null))
                throw new ArgumentNullException(nameof(TokenScope));
            if (ReferenceEquals(personalAccessTokenStore, null))
                throw new ArgumentNullException(nameof(personalAccessTokenStore));

            this.ClientId = DefaultClientId;
            this.Resource = DefaultResource;
            this.TokenScope = tokenScope;
            this.PersonalAccessTokenStore = personalAccessTokenStore;
            this.VstsAuthority = new VstsAzureAuthority();
        }
        internal BaseVstsAuthentication(
            ICredentialStore personalAccessTokenStore,
            ITokenStore vstsIdeTokenCache,
            IVstsAuthority vstsAuthority)
            : this(VstsTokenScope.ProfileRead, personalAccessTokenStore)
        {
            if (ReferenceEquals(vstsIdeTokenCache, null))
                throw new ArgumentNullException(nameof(vstsIdeTokenCache));
            if (ReferenceEquals(vstsAuthority, null))
                throw new ArgumentNullException(nameof(vstsAuthority));

            this.VstsIdeTokenCache = vstsIdeTokenCache;
            this.VstsAuthority = vstsAuthority;
            this.VstsAdalTokenCache = TokenCache.DefaultShared;
        }

        /// <summary>
        /// The application client identity by which access will be requested.
        /// </summary>
        public readonly string ClientId;
        /// <summary>
        /// The Azure resource for which access will be requested.
        /// </summary>
        public readonly string Resource;
        /// <summary>
        /// The desired scope of the authentication token to be requested.
        /// </summary>
        public readonly VstsTokenScope TokenScope;

        internal readonly TokenCache VstsAdalTokenCache;
        internal readonly ITokenStore VstsIdeTokenCache;

        internal ICredentialStore PersonalAccessTokenStore { get; set; }
        internal IVstsAuthority VstsAuthority { get; set; }
        internal Guid TenantId { get; set; }

        /// <summary>
        /// Deletes a set of stored credentials by their target resource.
        /// </summary>
        /// <param name="targetUri">The 'key' by which to identify credentials.</param>
        public override void DeleteCredentials(TargetUri targetUri)
        {
            BaseSecureStore.ValidateTargetUri(targetUri);

            Trace.WriteLine("BaseVstsAuthentication::DeleteCredentials");

            Credential credentials = null;

            if ((credentials = this.PersonalAccessTokenStore.ReadCredentials(targetUri)) != null)
            {
                this.PersonalAccessTokenStore.DeleteCredentials(targetUri);
            }
        }

        /// <summary>
        /// Attempts to get a set of credentials from storage by their target resource.
        /// </summary>
        /// <param name="targetUri">The 'key' by which to identify credentials.</param>
        /// <param name="credentials">Credentials associated with the URI if successful;
        /// <see langword="null"/> otherwise.</param>
        /// <returns><see langword="true"/> if successful; <see langword="false" /> otherwise.</returns>
        public override Credential GetCredentials(TargetUri targetUri)
        {
            BaseSecureStore.ValidateTargetUri(targetUri);

            Trace.WriteLine("BaseVstsAuthentication::GetCredentials");

            Credential credentials = null;

            if ((credentials = this.PersonalAccessTokenStore.ReadCredentials(targetUri)) != null)
            {
                Trace.WriteLine("   successfully retrieved stored credentials, updating credential cache");
            }

            return credentials;
        }

        /// <summary>
        /// Validates that a set of credentials grants access to the target resource.
        /// </summary>
        /// <param name="targetUri">The target resource to validate against.</param>
        /// <param name="credentials">The credentials to validate.</param>
        /// <returns><see langword="true"/> if successful; <see langword="false"/> otherwise.</returns>
        public async Task<bool> ValidateCredentials(TargetUri targetUri, Credential credentials)
        {
            Trace.WriteLine("BaseVstsAuthentication::ValidateCredentials");

            return await this.VstsAuthority.ValidateCredentials(targetUri, credentials);
        }

        /// <summary>
        /// Generates a "personal access token" or service specific, usage resticted access token.
        /// </summary>
        /// <param name="targetUri">The target resource for which to acquire the personal access
        /// token for.</param>
        /// <param name="accessToken">Azure Directory access token with privileges to grant access
        /// to the target resource.</param>
        /// <param name="requestCompactToken">Generates a compact token if <see langword="true"/>;
        /// generates a self describing token if <see langword="false"/>.</param>
        /// <returns><see langword="true"/> if successful; <see langword="false"/> otherwise.</returns>
        protected async Task<Credential> GeneratePersonalAccessToken(
            TargetUri targetUri,
            Token accessToken,
            bool requestCompactToken)
        {
            BaseSecureStore.ValidateTargetUri(targetUri);

            if (ReferenceEquals(accessToken, null))
                throw new ArgumentNullException(nameof(accessToken));

            Trace.WriteLine("BaseVstsAuthentication::GeneratePersonalAccessToken");

            Credential credential = null;

            Token personalAccessToken;
            if ((personalAccessToken = await this.VstsAuthority.GeneratePersonalAccessToken(targetUri, accessToken, TokenScope, requestCompactToken)) != null)
            {
                credential = (Credential)personalAccessToken;

                this.PersonalAccessTokenStore.WriteCredentials(targetUri, credential);
            }

            return credential;
        }

        /// <summary>
        /// Detects the backing authority of the end-point.
        /// </summary>
        /// <param name="targetUri">The resource which the authority protects.</param>
        /// <param name="tenantId">The identity of the authority tenant; <see cref="Guid.Empty"/> otherwise.</param>
        /// <returns><see langword="true"/> if the authority is Visual Studio Online; <see langword="false"/> otherwise</returns>
        public static bool DetectAuthority(TargetUri targetUri, out Guid tenantId)
        {
            const string VstsBaseUrlHost = "visualstudio.com";
            const string VstsResourceTenantHeader = "X-VSS-ResourceTenant";

            Trace.WriteLine("BaseAuthentication::DetectTenant");

            tenantId = Guid.Empty;

            if (targetUri.ActualUri.Host.EndsWith(VstsBaseUrlHost, StringComparison.OrdinalIgnoreCase))
            {
                Trace.WriteLine("   detected visualstudio.com, checking AAD vs MSA");

                string tenant = null;
                WebResponse response;

                if (StringComparer.OrdinalIgnoreCase.Equals(targetUri.Scheme, "http")
                    || StringComparer.OrdinalIgnoreCase.Equals(targetUri.Scheme, "https"))
                {
                    try
                    {
                        // build a request that we expect to fail, do not allow redirect to sign in url
                        var request = WebRequest.CreateHttp(targetUri);
                        request.UserAgent = Global.UserAgent;
                        request.Method = "HEAD";
                        request.AllowAutoRedirect = false;
                        // get the response from the server
                        response = request.GetResponse();
                    }
                    catch (WebException exception)
                    {
                        response = exception.Response;
                    }

                    // if the response exists and we have headers, parse them
                    if (response != null && response.SupportsHeaders)
                    {
                        Trace.WriteLine("   server has responded");

                        // find the VSTS resource tenant entry
                        tenant = response.Headers[VstsResourceTenantHeader];

                        return !String.IsNullOrWhiteSpace(tenant)
                            && Guid.TryParse(tenant, out tenantId);
                    }
                }
                else
                {
                    Trace.Write("   detected non-https based protocol: " + targetUri.Scheme);
                }
            }

            Trace.WriteLine("   failed detection");

            // if all else fails, fallback to basic authentication
            return false;
        }

        /// <summary>
        /// Creates a new authentication broker based for the specified resource.
        /// </summary>
        /// <param name="targetUri">The resource for which authentication is being requested.</param>
        /// <param name="scope">The scope of the access being requested.</param>
        /// <param name="personalAccessTokenStore">Storage container for personal access token secrets.</param>
        /// <param name="adaRefreshTokenStore">Storage container for Azure access token secrets.</param>
        /// <param name="authentication">
        /// An implementation of <see cref="BaseAuthentication"/> if one was detected;
        /// <see langword="null"/> otherwise.
        /// </param>
        /// <returns>
        /// <see langword="true"/> if an authority could be determined; <see langword="false"/> otherwise.
        /// </returns>
        public static BaseAuthentication GetAuthentication(
            TargetUri targetUri,
            VstsTokenScope scope,
            ICredentialStore personalAccessTokenStore)
        {
            BaseSecureStore.ValidateTargetUri(targetUri);
            if (ReferenceEquals(scope, null))
                throw new ArgumentNullException(nameof(scope));
            if (ReferenceEquals(personalAccessTokenStore, null))
                throw new ArgumentNullException(nameof(personalAccessTokenStore));

            Trace.WriteLine("BaseVstsAuthentication::DetectAuthority");

            BaseAuthentication authentication = null;

            Guid tenantId;
            if (DetectAuthority(targetUri, out tenantId))
            {
                // empty Guid is MSA, anything else is AAD
                if (tenantId == Guid.Empty)
                {
                    Trace.WriteLine("   MSA authority detected");
                    authentication = new VstsMsaAuthentication(scope, personalAccessTokenStore);
                }
                else
                {
                    Trace.WriteLine("   AAD authority for tenant '" + tenantId + "' detected");
                    authentication = new VstsAadAuthentication(tenantId, scope, personalAccessTokenStore);
                    (authentication as VstsAadAuthentication).TenantId = tenantId;
                }
            }

            return authentication;
        }
    }
}
