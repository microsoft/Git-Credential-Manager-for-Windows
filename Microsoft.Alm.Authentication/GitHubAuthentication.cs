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
using System.Threading.Tasks;

namespace Microsoft.Alm.Authentication
{
    /// <summary>
    /// Facilitates GitHub simple and two-factor authentication
    /// </summary>
    public class GitHubAuthentication : BaseAuthentication, IGitHubAuthentication
    {
        /// <summary>
        ///
        /// </summary>
        /// <param name="tokenScope"></param>
        /// <param name="personalAccessTokenStore"></param>
        public GitHubAuthentication(
            GitHubTokenScope tokenScope,
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
            GitHubAuthority = new GitHubAuthority();

            AcquireCredentialsCallback = acquireCredentialsCallback;
            AcquireAuthenticationCodeCallback = acquireAuthenticationCodeCallback;
            AuthenticationResultCallback = authenticationResultCallback;
        }

        /// <summary>
        /// The desired scope of the authentication token to be requested.
        /// </summary>
        public readonly GitHubTokenScope TokenScope;

        internal IGitHubAuthority GitHubAuthority { get; set; }
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

            Trace.WriteLine("GitHubAuthentication::DeleteCredentials");

            Credential credentials = null;
            if (this.PersonalAccessTokenStore.ReadCredentials(targetUri, out credentials))
            {
                this.PersonalAccessTokenStore.DeleteCredentials(targetUri);
                Trace.WriteLine("   credentials deleted");
            }
        }

        /// <summary>
        /// Gets a configured authentication object for 'github.com'.
        /// </summary>
        /// <param name="targetUri">The uniform resource indicator of the resource which requires
        /// authentication.</param>
        /// <param name="tokenScope">The desired scope of any personal access tokens acquired.</param>
        /// <param name="personalAccessTokenStore">A secure secret store for any personal access
        /// tokens acquired.</param>
        /// <param name="authentication">(out) The authentication object if successful.</param>
        /// <returns>True if success; otherwise false.</returns>
        public static bool GetAuthentication(
            TargetUri targetUri,
            GitHubTokenScope tokenScope,
            ICredentialStore personalAccessTokenStore,
            AcquireCredentialsDelegate acquireCredentialsCallback,
            AcquireAuthenticationCodeDelegate acquireAuthenticationCodeCallback,
            AuthenticationResultDelegate authenticationResultCallback,
            out BaseAuthentication authentication)
        {
            const string GitHubBaseUrlHost = "github.com";

            BaseSecureStore.ValidateTargetUri(targetUri);
            if (personalAccessTokenStore == null)
                throw new ArgumentNullException("personalAccessTokenStore", "The `personalAccessTokenStore` is null or invalid.");

            Trace.WriteLine("GitHubAuthentication::GetAuthentication");

            if (targetUri.ActualUri.DnsSafeHost.EndsWith(GitHubBaseUrlHost, StringComparison.OrdinalIgnoreCase))
            {
                authentication = new GitHubAuthentication(tokenScope, personalAccessTokenStore, acquireCredentialsCallback, acquireAuthenticationCodeCallback, authenticationResultCallback);
                Trace.WriteLine("   authentication for GitHub created");
            }
            else
            {
                authentication = null;
                Trace.WriteLine("   not github.com, authentication creation aborted");
            }

            return authentication != null;
        }

        /// <summary>
        /// Gets a <see cref="Credential"/> from the storage used by the authentication object.
        /// </summary>
        /// <param name="targetUri">
        /// The uniform resource indicator used to uniquely identity the credentials.
        /// </param>
        /// <param name="credentials">
        /// (out) A <see cref="Credential"/> object from the authentication object,
        /// authority or storage; otherwise `null`, if successful.
        /// </param>
        /// <returns>True if successful; otherwise false.</returns>
        public override bool GetCredentials(TargetUri targetUri, out Credential credentials)
        {
            BaseSecureStore.ValidateTargetUri(targetUri);

            Trace.WriteLine("GitHubAuthentication::GetCredentials");

            if (this.PersonalAccessTokenStore.ReadCredentials(targetUri, out credentials))
            {
                Trace.WriteLine("   successfully retrieved stored credentials, updating credential cache");
            }

            return credentials != null;
        }

        /// <summary>
        /// <para></para>
        /// <para>Tokens acquired are stored in the secure secret store provided during
        /// initialization.</para>
        /// </summary>
        /// <param name="targetUri">The unique identifier for the resource for which access is to
        /// be acquired.</param>
        /// <param name="credentials">(out) Credentials when acquisition is successful; null otherwise.</param>
        /// <returns>True if success; otherwise false.</returns>
        public bool InteractiveLogon(TargetUri targetUri, out Credential credentials)
        {
            string username;
            string password;
            if (AcquireCredentialsCallback(targetUri, out username, out password))
            {
                GitHubAuthenticationResult result;

                if (result = GitHubAuthority.AcquireToken(targetUri, username, password, null, this.TokenScope).Result)
                {
                    Trace.WriteLine("   token acquisition succeeded");

                    credentials = (Credential)result.Token;
                    this.PersonalAccessTokenStore.WriteCredentials(targetUri, credentials);

                    // if a result callback was registered, call it
                    if (AuthenticationResultCallback != null)
                    {
                        AuthenticationResultCallback(targetUri, result);
                    }

                    return true;
                }
                else if (result == GitHubAuthenticationResultType.TwoFactorApp
                        || result == GitHubAuthenticationResultType.TwoFactorSms)
                {
                    string authenticationCode;
                    if (AcquireAuthenticationCodeCallback(targetUri, result, username, out authenticationCode))
                    {
                        if (result = GitHubAuthority.AcquireToken(targetUri, username, password, authenticationCode, this.TokenScope).Result)
                        {
                            Trace.WriteLine("   token acquisition succeeded");

                            credentials = (Credential)result.Token;
                            this.PersonalAccessTokenStore.WriteCredentials(targetUri, credentials);

                            // if a result callback was registered, call it
                            if (AuthenticationResultCallback != null)
                            {
                                AuthenticationResultCallback(targetUri, result);
                            }

                            return true;
                        }
                    }
                }

                // if a result callback was registered, call it
                if (AuthenticationResultCallback != null)
                {
                    AuthenticationResultCallback(targetUri, result);
                }
            }

            Trace.WriteLine("   interactive logon failed");
            credentials = null;
            return false;
        }

        /// <summary>
        /// <para></para>
        /// <para>Tokens acquired are stored in the secure secret store provided during
        /// initialization.</para>
        /// </summary>
        /// <param name="targetUri">The unique identifier for the resource for which access is to
        /// be acquired.</param>
        /// <param name="username">The username of the account for which access is to be acquired.</param>
        /// <param name="password">The password of the account for which access is to be acquired.</param>
        /// <param name="authenticationCode">The two-factor authentication code for use in access acquisition.</param>
        /// <returns>True if success; otherwise false.</returns>
        public async Task<bool> NoninteractiveLogonWithCredentials(TargetUri targetUri, string username, string password, string authenticationCode = null)
        {
            BaseSecureStore.ValidateTargetUri(targetUri);
            if (String.IsNullOrWhiteSpace(username))
                throw new ArgumentNullException("username", "The `username` parameter is null or invalid.");
            if (String.IsNullOrWhiteSpace(password))
                throw new ArgumentNullException("username", "The `password` parameter is null or invalid.");

            Trace.WriteLine("GitHubAuthentication::NoninteractiveLogonWithCredentials");

            GitHubAuthenticationResult result;
            if (result = await GitHubAuthority.AcquireToken(targetUri, username, password, authenticationCode, this.TokenScope))
            {
                Trace.WriteLine("   token acquisition succeeded");

                PersonalAccessTokenStore.WriteCredentials(targetUri, (Credential)result.Token);

                return true;
            }

            Trace.WriteLine("   non-interactive logon failed");
            return false;
        }

        /// <summary>
        /// Sets a <see cref="Credential"/> in the storage used by the authentication object.
        /// </summary>
        /// <param name="targetUri">
        /// The uniform resource indicator used to uniquely identify the credentials.
        /// </param>
        /// <param name="credentials">The value to be stored.</param>
        /// <returns>True if successful; otherwise false.</returns>
        public override bool SetCredentials(TargetUri targetUri, Credential credentials)
        {
            BaseSecureStore.ValidateTargetUri(targetUri);
            Credential.Validate(credentials);

            Trace.WriteLine("GitHubAuthentication::SetCredentials");

            PersonalAccessTokenStore.WriteCredentials(targetUri, credentials);

            return true;
        }

        /// <summary>
        /// Validates that a set of credentials grants access to the target resource.
        /// </summary>
        /// <param name="targetUri">The unique identifier for the resource for which credentials
        /// are being validated against.</param>
        /// <param name="credentials">The credentials to validate.</param>
        /// <returns>True is successful; otherwise false.</returns>
        public async Task<bool> ValidateCredentials(TargetUri targetUri, Credential credentials)
        {
            BaseSecureStore.ValidateTargetUri(targetUri);
            Credential.Validate(credentials);

            Trace.WriteLine("GitHubAuthentication::ValidateCredentials");

            return await GitHubAuthority.ValidateCredentials(targetUri, credentials);
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
        /// <para>Should be either <see cref="GitHubAuthenticationResultType.TwoFactorApp"/> or <see cref="GitHubAuthenticationResultType.TwoFactorSms"/>.</para>
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
