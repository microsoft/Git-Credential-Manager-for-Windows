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
                ?? throw new ArgumentNullException(nameof(tokenScope));
            PersonalAccessTokenStore = personalAccessTokenStore
                ?? throw new ArgumentNullException(nameof(personalAccessTokenStore));
            AcquireCredentialsCallback = acquireCredentialsCallback
                ?? throw new ArgumentNullException(nameof(acquireCredentialsCallback));
            AcquireAuthenticationCodeCallback = acquireAuthenticationCodeCallback
                ?? throw new ArgumentNullException(nameof(acquireAuthenticationCodeCallback));

            Authority = new Authority(context, (TargetUri)NormalizeUri(targetUri));
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
            if (PersonalAccessTokenStore.ReadCredentials(normalizedTargetUri) != null)
            {
                result = await PersonalAccessTokenStore.DeleteCredentials(normalizedTargetUri);
                Trace.WriteLine($"credentials for '{normalizedTargetUri}' deleted");
            }

            return result;
        }

        /// <summary>
        /// Gets a configured authentication object for 'github.com'.
        /// <para/>
        /// Returns a new instance of `<see cref="Authentication"/>` if successful; otherwise `<see langword="null"/>`.
        /// </summary>
        /// <param name="targetUri">
        /// The uniform resource indicator of the resource which requires authentication.
        /// </param>
        /// <param name="tokenScope">The desired scope of any personal access tokens acquired.</param>
        /// <param name="personalAccessTokenStore">
        /// A secure secret store for any personal access tokens acquired.
        /// </param>
        public static BaseAuthentication GetAuthentication(
            RuntimeContext context,
            TargetUri targetUri,
            TokenScope tokenScope,
            ICredentialStore personalAccessTokenStore,
            AcquireCredentialsDelegate acquireCredentialsCallback,
            AcquireAuthenticationCodeDelegate acquireAuthenticationCodeCallback,
            AuthenticationResultDelegate authenticationResultCallback)
        {
            if (context is null)
                throw new ArgumentNullException(nameof(context));

            BaseAuthentication authentication = null;

            BaseSecureStore.ValidateTargetUri(targetUri);
            if (personalAccessTokenStore == null)
                throw new ArgumentNullException("personalAccessTokenStore", "The `personalAccessTokenStore` is null or invalid.");

            if (targetUri.DnsSafeHost.EndsWith(GitHubBaseUrlHost, StringComparison.OrdinalIgnoreCase))
            {
                var normalizedTargetUri = NormalizeUri(targetUri);
                authentication = new Authentication(context, (TargetUri)normalizedTargetUri, tokenScope, personalAccessTokenStore, acquireCredentialsCallback, acquireAuthenticationCodeCallback, authenticationResultCallback);
                context.Trace.WriteLine($"created GitHub authentication for '{normalizedTargetUri}'.");
            }
            else
            {
                authentication = null;
                context.Trace.WriteLine($"not github.com, authentication creation aborted.");
            }

            return authentication;
        }

        /// <summary>
        /// Gets a <see cref="Credential"/> from the storage used by the authentication object.
        /// <para/>
        /// Returns acquired `<see cref="Credential"/>` if successful; otherwise `<see langword="null"/>`.
        /// </summary>
        /// <param name="targetUri">The uniform resource indicator used to uniquely identify the credentials.</param>
        public override async Task<Credential> GetCredentials(TargetUri targetUri)
        {
            BaseSecureStore.ValidateTargetUri(targetUri);

            Credential credentials = null;

            var normalizedTargetUri = NormalizeUri(targetUri);
            if ((credentials = await PersonalAccessTokenStore.ReadCredentials(normalizedTargetUri)) != null)
            {
                Trace.WriteLine($"credentials for '{normalizedTargetUri}' found.");
            }

            return credentials;
        }

        /// <summary>
        /// Tokens acquired are stored in the secure secret store provided during initialization.
        /// <para/>
        /// Returns acquired `<see cref="Credential"/>` if successful; otherwise `<see langword="null"/>`.
        /// </summary>
        /// <param name="targetUri">
        /// The unique identifier for the resource for which access is to be acquired.
        /// </param>
        public async Task<Credential> InteractiveLogon(TargetUri targetUri)
        {
            if (targetUri is null)
                throw new ArgumentNullException(nameof(targetUri));
            if (AcquireCredentialsCallback is null)
            {
                var inner = new NullReferenceException(nameof(AcquireCredentialsCallback));
                throw new InvalidOperationException(inner.Message, inner);
            }

            TargetUri normalizedTargetUri = NormalizeUri(targetUri);
            Credential credentials = await AcquireCredentialsCallback(normalizedTargetUri);

            if (credentials != null)
            {
                AuthenticationResult result;

                if (result = await Authority.AcquireToken(normalizedTargetUri, credentials, null, TokenScope))
                {
                    Trace.WriteLine($"token acquisition for '{normalizedTargetUri}' succeeded");

                    credentials = (Credential)result.Token;
                    await PersonalAccessTokenStore.WriteCredentials(normalizedTargetUri, credentials);

                    // If a result callback was registered, call it.
                    if (AuthenticationResultCallback != null)
                    {
                        await AuthenticationResultCallback(normalizedTargetUri, result);
                    }

                    return credentials;
                }
                else if (result == GitHubAuthenticationResultType.TwoFactorApp
                        || result == GitHubAuthenticationResultType.TwoFactorSms)
                {
                    string authenticationCode = await AcquireAuthenticationCodeCallback(normalizedTargetUri, result);
                    if (authenticationCode != null)
                    {
                        if (result = await Authority.AcquireToken(normalizedTargetUri, credentials, authenticationCode, TokenScope))
                        {
                            Trace.WriteLine($"token acquisition for '{normalizedTargetUri}' succeeded.");

                            credentials = (Credential)result.Token;
                            await PersonalAccessTokenStore.WriteCredentials(normalizedTargetUri, credentials);

                            // If a result callback was registered, call it.
                            if (AuthenticationResultCallback != null)
                            {
                                await AuthenticationResultCallback(normalizedTargetUri, result);
                            }

                            return credentials;
                        }
                    }
                }

                // If a result callback was registered, call it.
                if (AuthenticationResultCallback != null)
                {
                    await AuthenticationResultCallback(normalizedTargetUri, result);
                }
            }

            Trace.WriteLine($"interactive logon for '{normalizedTargetUri}' failed.");
            return credentials;
        }

        /// <summary>
        /// Tokens acquired are stored in the secure secret store provided during initialization.
        /// <para/>
        /// Returns acquired `<see cref="Credential"/>` if successful; otherwise `<see langword="null"/>`.
        /// </summary>
        /// <param name="targetUri">
        /// The unique identifier for the resource for which access is to be acquired.
        /// </param>
        /// <param name="username">The username of the account for which access is to be acquired.</param>
        /// <param name="password">The password of the account for which access is to be acquired.</param>
        /// <param name="authenticationCode">
        /// The two-factor authentication code for use in access acquisition.
        /// </param>
        public async Task<Credential> NoninteractiveLogonWithCredentials(TargetUri targetUri, Credential credentials, string authenticationCode)
        {
            BaseSecureStore.ValidateTargetUri(targetUri);
            BaseSecureStore.ValidateCredential(credentials);

            TargetUri normalizedTargetUri = NormalizeUri(targetUri);
            AuthenticationResult result;

            if (result = await Authority.AcquireToken(normalizedTargetUri, credentials, authenticationCode, TokenScope))
            {
                Trace.WriteLine($"token acquisition for '{normalizedTargetUri}' succeeded.");

                credentials = (Credential)result.Token;
                await PersonalAccessTokenStore.WriteCredentials(normalizedTargetUri, credentials);

                return credentials;
            }

            Trace.WriteLine($"non-interactive logon for '{normalizedTargetUri}' failed.");
            return null;
        }

        public Task<Credential> NoninteractiveLogonWithCredentials(TargetUri targetUri, Credential credentials)
            => NoninteractiveLogonWithCredentials(targetUri, credentials, null);

        /// <summary>
        /// Sets a `<see cref="Credential"/>` in the storage used by the authentication object.
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
        public Task<bool> ValidateCredentials(TargetUri targetUri, Credential credentials)
        {
            BaseSecureStore.ValidateTargetUri(targetUri);
            BaseSecureStore.ValidateCredential(credentials);

            return Authority.ValidateCredentials(targetUri, credentials);
        }

        /// <summary>
        /// Delegate for credential acquisition from the UX.
        /// <para/>
        /// Returns acquired `<see cref="Credential"/>` if successful; otherwise `<see langword="null"/>`.
        /// </summary>
        /// <param name="targetUri">
        /// The uniform resource indicator used to uniquely identify the credentials.
        /// </param>
        public delegate Task<Credential> AcquireCredentialsDelegate(TargetUri targetUri);

        /// <summary>
        /// Delegate for authentication code acquisition from the UX.
        /// <para/>
        /// Returns the authentication code provided by the user if successful; otherwise `<see langword="null"/>`.
        /// </summary>
        /// <param name="targetUri">
        /// The uniform resource indicator used to uniquely identify the credentials.
        /// </param>
        /// <param name="resultType">
        /// The result of initial logon attempt, using the results of <see cref="AcquireCredentialsDelegate"/>.
        /// <para/>
        /// Should be either `<see cref="GitHubAuthenticationResultType.TwoFactorApp"/>` or `<see cref="GitHubAuthenticationResultType.TwoFactorSms"/>`.
        /// </param>
        public delegate Task<string> AcquireAuthenticationCodeDelegate(TargetUri targetUri, GitHubAuthenticationResultType resultType);

        /// <summary>
        /// Delegate for reporting the success or failure, of an authentication attempt.
        /// </summary>
        /// <param name="targetUri">The uniform resource indicator used to uniquely identify the credentials.</param>
        /// <param name="result">The result of the interactive authentication attempt.</param>
        public delegate Task AuthenticationResultDelegate(TargetUri targetUri, GitHubAuthenticationResultType result);

        static TargetUri NormalizeUri(TargetUri targetUri)
        {
            Uri queryUri = targetUri.QueryUri;

            // Special case for gist.github.com which are git backed repositories under the hood.
            // Credentials for these repositories are the same as the one stored with "github.com"
            if (queryUri.DnsSafeHost.Equals(GistBaseUrlHost, StringComparison.OrdinalIgnoreCase))
                return new TargetUri(GitHubBaseUri, targetUri.ProxyUri);

            return targetUri;
        }
    }
}
