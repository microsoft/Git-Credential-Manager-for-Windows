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
using Git = Microsoft.Alm.Git;

namespace GitHub.Authentication
{
    /// <summary>
    /// Facilitates GitHub simple and two-factor authentication
    /// </summary>
    public class Authentication: BaseAuthentication, IAuthentication
    {
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
            TargetUri targetUri,
            TokenScope tokenScope,
            ICredentialStore personalAccessTokenStore,
            AcquireCredentialsDelegate acquireCredentialsCallback,
            AcquireAuthenticationCodeDelegate acquireAuthenticationCodeCallback,
            AuthenticationResultDelegate authenticationResultCallback)
        {
            if (tokenScope == null)
                throw new ArgumentNullException("tokenScope", "The parameter `tokenScope` is null or invalid.");
            if (personalAccessTokenStore == null)
                throw new ArgumentNullException("personalAccessTokenStore", "The parameter `personalAccessTokenStore` is null or invalid.");
            if (acquireCredentialsCallback == null)
                throw new ArgumentNullException("acquireCredentialsCallback", "The parameter `acquireCredentialsCallback` is null or invalid.");
            if (acquireAuthenticationCodeCallback == null)
                throw new ArgumentNullException("acquireAuthenticationCodeCallback", "The parameter `acquireAuthenticationCodeCallback` is null or invalid.");

            TokenScope = tokenScope;

            PersonalAccessTokenStore = personalAccessTokenStore;
            Authority = new Authority(targetUri);

            AcquireCredentialsCallback = acquireCredentialsCallback;
            AcquireAuthenticationCodeCallback = acquireAuthenticationCodeCallback;
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
        /// Deletes a <see cref="Credential"/> from the storage used by the authentication object.
        /// </summary>
        /// <param name="targetUri">
        /// The uniform resource indicator used to uniquely identify the credentials.
        /// </param>
        public override void DeleteCredentials(TargetUri targetUri)
        {
            BaseSecureStore.ValidateTargetUri(targetUri);

            if (this.PersonalAccessTokenStore.ReadCredentials(targetUri) != null)
            {
                this.PersonalAccessTokenStore.DeleteCredentials(targetUri);
                Git.Trace.WriteLine($"credentials for '{targetUri}' deleted");
            }
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
            TargetUri targetUri,
            TokenScope tokenScope,
            ICredentialStore personalAccessTokenStore,
            AcquireCredentialsDelegate acquireCredentialsCallback,
            AcquireAuthenticationCodeDelegate acquireAuthenticationCodeCallback,
            AuthenticationResultDelegate authenticationResultCallback)
        {
            const string GitHubBaseUrlHost = "github.com";

            BaseAuthentication authentication = null;

            BaseSecureStore.ValidateTargetUri(targetUri);
            if (personalAccessTokenStore == null)
                throw new ArgumentNullException("personalAccessTokenStore", "The `personalAccessTokenStore` is null or invalid.");

            if (targetUri.DnsSafeHost.EndsWith(GitHubBaseUrlHost, StringComparison.OrdinalIgnoreCase))
            {
                authentication = new Authentication(targetUri, tokenScope, personalAccessTokenStore, acquireCredentialsCallback, acquireAuthenticationCodeCallback, authenticationResultCallback);
                Git.Trace.WriteLine($"created GitHub authentication for '{targetUri}'.");
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
        /// </summary>
        /// <param name="targetUri">
        /// The uniform resource indicator used to uniquely identity the credentials.
        /// </param>
        /// <returns>
        /// <see cref="Credential"/> object from the authentication object, authority or storage;
        /// otherwise `null`, if successful.
        /// </returns>
        public override Credential GetCredentials(TargetUri targetUri)
        {
            BaseSecureStore.ValidateTargetUri(targetUri);

            Credential credentials = null;

            if ((credentials = this.PersonalAccessTokenStore.ReadCredentials(targetUri)) != null)
            {
                Git.Trace.WriteLine($"credentials for '{targetUri}' found.");
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

            if (AcquireCredentialsCallback(targetUri, out username, out password))
            {
                AuthenticationResult result;

                if (result = await Authority.AcquireToken(targetUri, username, password, null, this.TokenScope))
                {
                    Git.Trace.WriteLine($"token acquisition for '{targetUri}' succeeded");

                    credentials = (Credential)result.Token;
                    this.PersonalAccessTokenStore.WriteCredentials(targetUri, credentials);

                    // if a result callback was registered, call it
                    AuthenticationResultCallback?.Invoke(targetUri, result);

                    return credentials;
                }
                else if (result == GitHubAuthenticationResultType.TwoFactorApp
                        || result == GitHubAuthenticationResultType.TwoFactorSms)
                {
                    string authenticationCode;
                    if (AcquireAuthenticationCodeCallback(targetUri, result, username, out authenticationCode))
                    {
                        if (result = await Authority.AcquireToken(targetUri, username, password, authenticationCode, this.TokenScope))
                        {
                            Git.Trace.WriteLine($"token acquisition for '{targetUri}' succeeded.");

                            credentials = (Credential)result.Token;
                            this.PersonalAccessTokenStore.WriteCredentials(targetUri, credentials);

                            // if a result callback was registered, call it
                            AuthenticationResultCallback?.Invoke(targetUri, result);

                            return credentials;
                        }
                    }
                }

                // if a result callback was registered, call it
                if (AuthenticationResultCallback != null)
                {
                    AuthenticationResultCallback(targetUri, result);
                }
            }

            Git.Trace.WriteLine($"interactive logon for '{targetUri}' failed.");
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
            if (String.IsNullOrWhiteSpace(username))
                throw new ArgumentNullException("username", "The `username` parameter is null or invalid.");
            if (String.IsNullOrWhiteSpace(password))
                throw new ArgumentNullException("username", "The `password` parameter is null or invalid.");

            Credential credentials = null;

            AuthenticationResult result;
            if (result = await Authority.AcquireToken(targetUri, username, password, authenticationCode, this.TokenScope))
            {
                Git.Trace.WriteLine($"token acquisition for '{targetUri}' succeeded.");

                credentials = (Credential)result.Token;
                PersonalAccessTokenStore.WriteCredentials(targetUri, credentials);

                return credentials;
            }

            Git.Trace.WriteLine($"non-interactive logon for '{targetUri}' failed.");
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
        public override void SetCredentials(TargetUri targetUri, Credential credentials)
        {
            BaseSecureStore.ValidateTargetUri(targetUri);
            BaseSecureStore.ValidateCredential(credentials);

            PersonalAccessTokenStore.WriteCredentials(targetUri, credentials);
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
    }
}
