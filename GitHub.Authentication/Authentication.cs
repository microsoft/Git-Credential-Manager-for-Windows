/**** Git Credential Manager for Windows ****
 *
 * Copyright (c) GitHub Corporation
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
using Microsoft.Alm.Authentication;
using Git = Microsoft.Alm.Authentication.Git;

namespace GitHub.Authentication
{
    /// <summary>
    /// Facilitates GitHub simple and two-factor authentication
    /// </summary>
    public class Authentication : BaseAuthentication, IAuthentication
    {
        const string GitHubBaseUrlHost = "github.com";
        const string GistBaseUrlHost = "gist." + GitHubBaseUrlHost;
        static readonly Uri GitHubBaseUri = new Uri("https://" + GitHubBaseUrlHost);

        /// <summary>
        /// Creates a new authentication
        /// </summary>
        /// <param name="targetUri">
        /// The uniform resource indicator of the resource which requires authentication.
        /// </param>
        /// <param name="tokenScope">The desired scope of any personal access tokens acquired.</param>
        /// <param name="personalAccessTokenStore">
        /// A secure secret store for any personal access tokens acquired.
        /// </param>
        public Authentication(
            RuntimeContext context,
            TargetUri targetUri,
            TokenScope tokenScope,
            ICredentialStore personalAccessTokenStore,
            AcquireCredentialsDelegate acquireCredentialsCallback,
            AcquireAuthenticationCodeDelegate acquireAuthenticationCodeCallback,
            AuthenticationResultDelegate authenticationResultCallback)
            : base(context)
        {
            TokenScope = tokenScope
                ?? throw new ArgumentNullException("tokenScope", "The parameter `tokenScope` is null or invalid.");
            PersonalAccessTokenStore = personalAccessTokenStore
                ?? throw new ArgumentNullException("personalAccessTokenStore", "The parameter `personalAccessTokenStore` is null or invalid.");
            AcquireCredentialsCallback = acquireCredentialsCallback
                ?? throw new ArgumentNullException("acquireCredentialsCallback", "The parameter `acquireCredentialsCallback` is null or invalid.");
            AcquireAuthenticationCodeCallback = acquireAuthenticationCodeCallback
                ?? throw new ArgumentNullException("acquireAuthenticationCodeCallback", "The parameter `acquireAuthenticationCodeCallback` is null or invalid.");

            Authority = new Authority(NormalizeUri(targetUri));
            AuthenticationResultCallback = authenticationResultCallback;
        }

        /// <summary>
        /// The desired scope of the authentication token to be requested.
        /// </summary>
        public readonly TokenScope TokenScope;

        internal IAuthority Authority { get; set; }

        internal ICredentialStore PersonalAccessTokenStore { get; set; }

        internal AcquireCredentialsDelegate AcquireCredentialsCallback { get; set; }

        internal AcquireAuthenticationCodeDelegate AcquireAuthenticationCodeCallback { get; set; }

        internal AuthenticationResultDelegate AuthenticationResultCallback { get; set; }

        /// <summary>
        /// Deletes a `<see cref="Credential"/>` from the storage used by the authentication object.
        /// </summary>
        /// <param name="targetUri">The uniform resource indicator used to uniquely identify the credentials.</param>
        public override async Task<bool> DeleteCredentials(TargetUri targetUri)
        {
            BaseSecureStore.ValidateTargetUri(targetUri);

            bool result = false;

            var normalizedTargetUri = NormalizeUri(targetUri);
            if (await PersonalAccessTokenStore.ReadCredentials(normalizedTargetUri) != null)
            {
                result = await PersonalAccessTokenStore.DeleteCredentials(normalizedTargetUri);
                Git.Trace.WriteLine($"credentials for '{normalizedTargetUri}' deleted");
            }

            return result;
        }

        /// <summary>
        /// Gets a configured authentication object for 'github.com'.
        /// </summary>
        /// <param name="targetUri">
        /// The uniform resource indicator of the resource which requires authentication.
        /// </param>
        /// <param name="tokenScope">The desired scope of any personal access tokens acquired.</param>
        /// <param name="personalAccessTokenStore">
        /// A secure secret store for any personal access tokens acquired.
        /// </param>
        /// <param name="authentication">(out) The authentication object if successful.</param>
        /// <returns>True if success; otherwise false.</returns>
        public static BaseAuthentication GetAuthentication(
            RuntimeContext context,
            TargetUri targetUri,
            TokenScope tokenScope,
            ICredentialStore personalAccessTokenStore,
            AcquireCredentialsDelegate acquireCredentialsCallback,
            AcquireAuthenticationCodeDelegate acquireAuthenticationCodeCallback,
            AuthenticationResultDelegate authenticationResultCallback)
        {
            BaseAuthentication authentication = null;

            BaseSecureStore.ValidateTargetUri(targetUri);
            if (personalAccessTokenStore == null)
                throw new ArgumentNullException("personalAccessTokenStore", "The `personalAccessTokenStore` is null or invalid.");

            if (targetUri.DnsSafeHost.EndsWith(GitHubBaseUrlHost, StringComparison.OrdinalIgnoreCase))
            {
                var normalizedTargetUri = NormalizeUri(targetUri);
                authentication = new Authentication(context,
                                                    normalizedTargetUri,
                                                    tokenScope,
                                                    personalAccessTokenStore,
                                                    acquireCredentialsCallback,
                                                    acquireAuthenticationCodeCallback,
                                                    authenticationResultCallback);
                Git.Trace.WriteLine($"created GitHub authentication for '{normalizedTargetUri}'.");
            }
            else
            {
                authentication = null;
                Git.Trace.WriteLine($"not github.com, authentication creation aborted.");
            }

            return authentication;
        }

        /// <summary>
        /// Gets a <see cref="Credential"/> from the storage used by the authentication object.
        /// <para/>
        /// Returns a `<see cref="Credential"/>` if successful; otherwise `<see langword="null"/>`.
        /// </summary>
        /// <param name="targetUri">The uniform resource indicator used to uniquely identify the credentials.</param>
        public override async Task<Credential> GetCredentials(TargetUri targetUri)
        {
            BaseSecureStore.ValidateTargetUri(targetUri);

            Credential credentials = null;

            var normalizedTargetUri = NormalizeUri(targetUri);
            if ((credentials = await PersonalAccessTokenStore.ReadCredentials(normalizedTargetUri)) != null)
            {
                Git.Trace.WriteLine($"credentials for '{normalizedTargetUri}' found.");
            }

            return credentials;
        }

        /// <summary>
        /// <para></para>
        /// <para>Tokens acquired are stored in the secure secret store provided during initialization.</para>
        /// </summary>
        /// <param name="targetUri">
        /// The unique identifier for the resource for which access is to be acquired.
        /// </param>
        /// ///
        /// <returns>Acquired <see cref="Credential"/> if successful; otherwise <see langword="null"/>.</returns>
        public async Task<Credential> InteractiveLogon(TargetUri targetUri)
        {
            Credential credentials = null;
            string username;
            string password;

            var normalizedTargetUri = NormalizeUri(targetUri);
            if (AcquireCredentialsCallback(normalizedTargetUri, out username, out password))
            {
                AuthenticationResult result;

                if (result = await Authority.AcquireToken(normalizedTargetUri, username, password, null, TokenScope))
                {
                    Git.Trace.WriteLine($"token acquisition for '{normalizedTargetUri}' succeeded");

                    credentials = (Credential)result.Token;
                    await PersonalAccessTokenStore.WriteCredentials(normalizedTargetUri, credentials);

                    // if a result callback was registered, call it
                    AuthenticationResultCallback?.Invoke(normalizedTargetUri, result);

                    return credentials;
                }
                else if (result == GitHubAuthenticationResultType.TwoFactorApp
                        || result == GitHubAuthenticationResultType.TwoFactorSms)
                {
                    string authenticationCode;
                    if (AcquireAuthenticationCodeCallback(normalizedTargetUri, result, username, out authenticationCode))
                    {
                        if (result = await Authority.AcquireToken(normalizedTargetUri, username, password, authenticationCode, TokenScope))
                        {
                            Git.Trace.WriteLine($"token acquisition for '{normalizedTargetUri}' succeeded.");

                            credentials = (Credential)result.Token;
                            await PersonalAccessTokenStore.WriteCredentials(normalizedTargetUri, credentials);

                            // if a result callback was registered, call it
                            AuthenticationResultCallback?.Invoke(normalizedTargetUri, result);

                            return credentials;
                        }
                    }
                }

                AuthenticationResultCallback?.Invoke(normalizedTargetUri, result);
            }

            Git.Trace.WriteLine($"interactive logon for '{normalizedTargetUri}' failed.");
            return credentials;
        }

        /// <summary>
        /// <para></para>
        /// <para>Tokens acquired are stored in the secure secret store provided during initialization.</para>
        /// </summary>
        /// <param name="targetUri">
        /// The unique identifier for the resource for which access is to be acquired.
        /// </param>
        /// <param name="username">The username of the account for which access is to be acquired.</param>
        /// <param name="password">The password of the account for which access is to be acquired.</param>
        /// <param name="authenticationCode">
        /// The two-factor authentication code for use in access acquisition.
        /// </param>
        /// <returns>Acquired <see cref="Credential"/> if successful; otherwise <see langword="null"/>.</returns>
        public async Task<Credential> NoninteractiveLogonWithCredentials(TargetUri targetUri, string username, string password, string authenticationCode)
        {
            BaseSecureStore.ValidateTargetUri(targetUri);
            if (string.IsNullOrWhiteSpace(username))
                throw new ArgumentNullException("username", "The `username` parameter is null or invalid.");
            if (string.IsNullOrWhiteSpace(password))
                throw new ArgumentNullException("username", "The `password` parameter is null or invalid.");

            Credential credentials = null;
            var normalizedTargetUri = NormalizeUri(targetUri);
            AuthenticationResult result;
            if (result = await Authority.AcquireToken(normalizedTargetUri, username, password, authenticationCode, TokenScope))
            {
                Git.Trace.WriteLine($"token acquisition for '{normalizedTargetUri}' succeeded.");

                credentials = (Credential)result.Token;
                await PersonalAccessTokenStore.WriteCredentials(normalizedTargetUri, credentials);

                return credentials;
            }

            Git.Trace.WriteLine($"non-interactive logon for '{normalizedTargetUri}' failed.");
            return credentials;
        }

        public Task<Credential> NoninteractiveLogonWithCredentials(TargetUri targetUri, string username, string password)
            => NoninteractiveLogonWithCredentials(targetUri, username, password, null);

        /// <summary>
        /// Sets a <see cref="Credential"/> in the storage used by the authentication object.
        /// </summary>
        /// <param name="targetUri">
        /// The uniform resource indicator used to uniquely identify the credentials.
        /// </param>
        /// <param name="credentials">The value to be stored.</param>
        public override Task<bool> SetCredentials(TargetUri targetUri, Credential credentials)
        {
            BaseSecureStore.ValidateTargetUri(targetUri);
            BaseSecureStore.ValidateCredential(credentials);

            return PersonalAccessTokenStore.WriteCredentials(NormalizeUri(targetUri), credentials);
        }

        /// <summary>
        /// Validates that a set of credentials grants access to the target resource.
        /// </summary>
        /// <param name="targetUri">
        /// The unique identifier for the resource for which credentials are being validated against.
        /// </param>
        /// <param name="credentials">The credentials to validate.</param>
        /// <returns>True is successful; otherwise false.</returns>
        public async Task<bool> ValidateCredentials(TargetUri targetUri, Credential credentials)
        {
            BaseSecureStore.ValidateTargetUri(targetUri);
            BaseSecureStore.ValidateCredential(credentials);

            return await Authority.ValidateCredentials(targetUri, credentials);
        }

        /// <summary>
        /// Delegate for credential acquisition from the UX.
        /// </summary>
        /// <param name="targetUri">
        /// The uniform resource indicator used to uniquely identify the credentials.
        /// </param>
        /// <param name="username">The username supplied by the user.</param>
        /// <param name="password">The password supplied by the user.</param>
        /// <returns>True if successful; otherwise false.</returns>
        public delegate bool AcquireCredentialsDelegate(TargetUri targetUri, out string username, out string password);

        /// <summary>
        /// Delegate for authentication code acquisition from the UX.
        /// </summary>
        /// <param name="targetUri">
        /// The uniform resource indicator used to uniquely identify the credentials.
        /// </param>
        /// <param name="resultType">
        /// <para>The result of initial logon attempt, using the results of <see cref="AcquireCredentialsDelegate"/>.</para>
        /// <para>
        /// Should be either <see cref="GitHubAuthenticationResultType.TwoFactorApp"/> or <see cref="GitHubAuthenticationResultType.TwoFactorSms"/>.
        /// </para>
        /// </param>
        /// <param name="authenticationCode">The authentication code provided by the user.</param>
        /// <returns>True if successful; otherwise false.</returns>
        public delegate bool AcquireAuthenticationCodeDelegate(TargetUri targetUri, GitHubAuthenticationResultType resultType, string username, out string authenticationCode);

        /// <summary>
        /// Delegate for reporting the success, or not, of an authentication attempt.
        /// </summary>
        /// <param name="targetUri">
        /// The uniform resource indicator used to uniquely identify the credentials.
        /// </param>
        /// <param name="result">The result of the interactive authentication attempt.</param>
        public delegate void AuthenticationResultDelegate(TargetUri targetUri, GitHubAuthenticationResultType result);

        static Uri NormalizeUri(Uri targetUri)
        {
            // Special case for gist.github.com which are git backed repositories under the hood.
            // Credentials for these repos are the same as the one stored with "github.com"
            if (targetUri.DnsSafeHost.Equals(GistBaseUrlHost, StringComparison.OrdinalIgnoreCase))
                return GitHubBaseUri;

            return targetUri;
        }
    }
}
