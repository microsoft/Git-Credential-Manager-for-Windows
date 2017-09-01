/**** Git Credential Manager for Windows ****
 *
 * Copyright (c) Atlassian
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

using Trace = Microsoft.Alm.Git.Trace;

namespace Atlassian.Bitbucket.Authentication
{
    /// <summary>
    /// Extension of <see cref="BaseAuthentication"/> implementing Bitbucket's
    /// <see cref="IAuthentication"/> and providing functionality to manage credentials for Bitbucket
    /// hosting service.
    /// </summary>
    public class Authentication : BaseAuthentication, IAuthentication
    {
        public const string BitbucketBaseUrlHost = "bitbucket.org";

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="personalAccessTokenStore">where to store validated credentials</param>
        /// <param name="acquireCredentialsCallback">
        /// what to call to promot the user for Basic Auth credentials
        /// </param>
        /// <param name="acquireAuthenticationOAuthCallback">
        /// what to call to prompt the user to run the OAuth process
        /// </param>
        public Authentication(ICredentialStore personalAccessTokenStore, AcquireCredentialsDelegate acquireCredentialsCallback, AcquireAuthenticationOAuthDelegate acquireAuthenticationOAuthCallback)
        {
            if (personalAccessTokenStore == null)
                throw new ArgumentNullException(nameof(personalAccessTokenStore), $"The parameter `{nameof(personalAccessTokenStore)}` is null or invalid.");

            PersonalAccessTokenStore = personalAccessTokenStore;

            BitbucketAuthority = new Authority();
            TokenScope = TokenScope.SnippetWrite | TokenScope.RepositoryWrite;

            AcquireCredentialsCallback = acquireCredentialsCallback;
            AcquireAuthenticationOAuthCallback = acquireAuthenticationOAuthCallback;
        }

        /// <summary>
        /// The desired scope of the authentication token to be requested.
        /// </summary>
        public readonly TokenScope TokenScope;

        public ICredentialStore PersonalAccessTokenStore { get; }
        internal AcquireCredentialsDelegate AcquireCredentialsCallback { get; set; }
        internal AcquireAuthenticationOAuthDelegate AcquireAuthenticationOAuthCallback { get; set; }
        internal AuthenticationResultDelegate AuthenticationResultCallback { get; set; }

        private const string refreshTokenSuffix = "/refresh_token";

        /// <inheritdoc/>
        public override void DeleteCredentials(TargetUri targetUri)
        {
            DeleteCredentials(targetUri, null);
        }

        /// <inheritdoc/>
        public override void DeleteCredentials(TargetUri targetUri, string username)
        {
            BaseSecureStore.ValidateTargetUri(targetUri);

            Trace.WriteLine($"Deleting Bitbucket Credentials for {targetUri.ActualUri}");

            Credential credentials = null;
            if ((credentials = PersonalAccessTokenStore.ReadCredentials(targetUri)) != null)
            {
                // try to delete the credentials for the explicit target uri first
                PersonalAccessTokenStore.DeleteCredentials(targetUri);
                Trace.WriteLine($"host credentials deleted for {targetUri.ActualUri}");
            }

            // tidy up and delete any related refresh tokens
            var refreshTargetUri = GetRefreshTokenTargetUri(targetUri);
            if ((credentials = PersonalAccessTokenStore.ReadCredentials(refreshTargetUri)) != null)
            {
                // try to delete the credentials for the explicit target uri first
                PersonalAccessTokenStore.DeleteCredentials(refreshTargetUri);
                Trace.WriteLine($"host refresh credentials deleted for {refreshTargetUri.ActualUri}");
            }

            // if we deleted per user then we shoudl try and delete the host level credentials too if
            // they match the username
            if (targetUri.TargetUriContainsUsername)
            {
                var hostTargetUri = targetUri.GetHostTargetUri();
                var hostCredentials = GetCredentials(hostTargetUri);
                var encodedUsername = Uri.EscapeDataString(targetUri.TargetUriUsername);
                if (encodedUsername != username)
                {
                    Trace.WriteLine($"username {username} != targetUri userInfo {encodedUsername}");
                }

                if (hostCredentials != null && hostCredentials.Username.Equals(encodedUsername))
                {
                    DeleteCredentials(targetUri.GetHostTargetUri(), username);
                }
            }
        }

        /// <summary>
        /// Generate a new <see cref="TargetUri"/> to be used as the key when storing the
        /// refresh_tokne alongside the access_token.
        /// </summary>
        /// <param name="targetUri">contains Authority URL etc used for storing the sibling access_token</param>
        /// <returns></returns>
        private static TargetUri GetRefreshTokenTargetUri(TargetUri targetUri)
        {
            var uri = new Uri(targetUri.ActualUri, refreshTokenSuffix);
            return new TargetUri(uri);
        }

        /// <inheritdoc/>
        public Credential GetCredentials(TargetUri targetUri, string username)
        {
            if (string.IsNullOrWhiteSpace(username) || targetUri.TargetUriContainsUsername)
            {
                return GetCredentials(targetUri);
            }

            return GetCredentials(targetUri.GetPerUserTargetUri(username));
        }

        /// <inheritdoc/>
        public override Credential GetCredentials(TargetUri targetUri)
        {
            BaseSecureStore.ValidateTargetUri(targetUri);
            Credential credentials = null;

            if ((credentials = PersonalAccessTokenStore.ReadCredentials(targetUri)) != null)
            {
                Trace.WriteLine("successfully retrieved stored credentials, updating credential cache");
                return credentials;
            }

            // try for a refresh token
            var refreshCredentials = PersonalAccessTokenStore.ReadCredentials(GetRefreshTokenTargetUri(targetUri));
            if (refreshCredentials == null)
            {
                // no refresh token return null
                return credentials;
            }

            Credential refreshedCredentials = Task.Run(() => RefreshCredentials(targetUri, refreshCredentials.Password, null)).Result;
            if (refreshedCredentials == null)
            {
                // refresh failed return null
                return credentials;
            }
            else
            {
                credentials = refreshedCredentials;
            }

            return credentials;
        }

        /// <inheritdoc/>
        public override void SetCredentials(TargetUri targetUri, Credential credentials)
        {
            // this is only called from the store() method so only applies to default host entries
            // calling this from elsewhere may have unintended consequences, use
            // SetCredentials(targetUri, credentials, username) instead

            // only store the credentials as received if they match the uri and user of the existing
            // default entry
            var currentCredentials = GetCredentials(targetUri);
            if (currentCredentials != null && currentCredentials.Username != null && !currentCredentials.Username.Equals(credentials.Username))
            {
                // do nothing as the default is for another username and we don't want to overwrite it
                Trace.WriteLine($"skipping for {targetUri.ActualUri} new username {currentCredentials.Username} != {credentials.Username}");
                return;
            }

            SetCredentials(targetUri, credentials, null);

            // Store() will not call with a username url
            if (targetUri.TargetUriContainsUsername)
                return;

            // see if there is a matching personal refresh token
            var username = credentials.Username;
            var userSpecificTargetUri = targetUri.GetPerUserTargetUri(username);
            var userCredentials = GetCredentials(userSpecificTargetUri, username);

            if (userCredentials != null && userCredentials.Password.Equals(credentials.Password))
            {
                var userRefreshCredentials = GetCredentials(GetRefreshTokenTargetUri(userSpecificTargetUri), username);
                if (userRefreshCredentials != null)
                {
                    Trace.WriteLine("OAuth RefreshToken");
                    var hostRefreshCredentials = new Credential(credentials.Username, userRefreshCredentials.Password);
                    SetCredentials(GetRefreshTokenTargetUri(targetUri), hostRefreshCredentials, null);
                }
            }
        }

        /// <inheritdoc/>
        public void SetCredentials(TargetUri targetUri, Credential credentials, string username)
        {
            BaseSecureStore.ValidateTargetUri(targetUri);
            BaseSecureStore.ValidateCredential(credentials);

            Trace.WriteLine($"{credentials.Username} at {targetUri.ActualUri.AbsoluteUri}");

            // if the url doesn't contain a username then save with an explicit username.
            if (!targetUri.TargetUriContainsUsername && (!string.IsNullOrWhiteSpace(username)
                || !string.IsNullOrWhiteSpace(credentials.Username)))
            {
                var realUsername = GetRealUsername(credentials, username);
                Credential tempCredentials = new Credential(realUsername, credentials.Password);
                SetCredentials(targetUri.GetPerUserTargetUri(realUsername), tempCredentials, null);
            }

            PersonalAccessTokenStore.WriteCredentials(targetUri, credentials);
        }

        private static string GetRealUsername(Credential credentials, string username)
        {
            return GetRealUsername(credentials.Username, username);
        }

        private static string GetRealUsername(string remoteUsername, string username)
        {
            // if there is no credentials username, use the provided one
            if (string.IsNullOrWhiteSpace(remoteUsername))
            {
                return username;
            }

            // otherwise
            return remoteUsername;
        }

        /// <summary>
        /// Identify the Hosting service from the the targetUri.
        /// </summary>
        /// <param name="targetUri"></param>
        /// <returns>
        /// A <see cref="BaseAuthentication"/> instance if the targetUri represents Bitbucket, null otherwise.
        /// </returns>
        public static BaseAuthentication GetAuthentication(TargetUri targetUri, ICredentialStore personalAccessTokenStore, AcquireCredentialsDelegate acquireCredentialsCallback, AcquireAuthenticationOAuthDelegate acquireAuthenticationOAuthCallback)
        {
            BaseAuthentication authentication = null;

            BaseSecureStore.ValidateTargetUri(targetUri);

            if (personalAccessTokenStore == null)
                throw new ArgumentNullException(nameof(personalAccessTokenStore), $"The `{nameof(personalAccessTokenStore)}` is null or invalid.");

            if (targetUri.ActualUri.DnsSafeHost.EndsWith(BitbucketBaseUrlHost, StringComparison.OrdinalIgnoreCase))
            {
                authentication = new Authentication(personalAccessTokenStore, acquireCredentialsCallback, acquireAuthenticationOAuthCallback);
                Trace.WriteLine("authentication for Bitbucket created");
            }
            else
            {
                authentication = null;
            }

            return authentication;
        }

        /// <summary>
        /// Prompt the user for authentication credentials.
        /// </summary>
        /// <param name="targetUri"></param>
        /// <param name="username"></param>
        /// <returns>a valid instance of <see cref="Credential"/> or null</returns>
        public async Task<Credential> InteractiveLogon(TargetUri targetUri, string username)
        {
            if (string.IsNullOrWhiteSpace(username) || targetUri.TargetUriContainsUsername)
            {
                return await InteractiveLogon(targetUri);
            }

            return await InteractiveLogon(targetUri.GetPerUserTargetUri(username));
        }

        /// <inheritdoc/>
        public async Task<Credential> InteractiveLogon(TargetUri targetUri)
        {
            Credential credentials = null;
            string username;
            string password;

            // Ask the user for Basic Auth credentials
            if (AcquireCredentialsCallback("Please enter your Bitbucket credentials for ", targetUri, out username, out password))
            {
                AuthenticationResult result;

                if (result = await BitbucketAuthority.AcquireToken(targetUri, username, password, AuthenticationResultType.None, TokenScope))
                {
                    Trace.WriteLine("token acquisition succeeded");

                    credentials = GenerateCredentials(targetUri, username, ref result);
                    SetCredentials(targetUri, credentials, username);

                    // if a result callback was registered, call it
                    if (AuthenticationResultCallback != null)
                    {
                        AuthenticationResultCallback(targetUri, result);
                    }

                    return credentials;
                }
                else if (result == AuthenticationResultType.TwoFactor)
                {
                    // Basic Auth attempt returned a result indicating the user has 2FA on so prompt
                    // the user to run the OAuth dance.
                    if (AcquireAuthenticationOAuthCallback("", targetUri, result, username))
                    {
                        if (result = await BitbucketAuthority.AcquireToken(targetUri, username, password, AuthenticationResultType.TwoFactor, TokenScope))
                        {
                            Trace.WriteLine("token acquisition succeeded");

                            credentials = GenerateCredentials(targetUri, username, ref result);
                            SetCredentials(targetUri, credentials, username);
                            SetCredentials(GetRefreshTokenTargetUri(targetUri), new Credential(result.RefreshToken.Type.ToString(), result.RefreshToken.Value), username);

                            // if a result callback was registered, call it
                            if (AuthenticationResultCallback != null)
                            {
                                AuthenticationResultCallback(targetUri, result);
                            }

                            return credentials;
                        }
                    }
                }
            }

            Trace.WriteLine("interactive logon failed");
            return credentials;
        }

        /// <summary>
        /// Generate the final credentials for storing.
        /// <para>
        /// Bitbucket always wants the username as well as the password/token so if the username
        /// isn't explicit in the remote URL then we need to ensure the credentials are stored with a
        /// real username rather than 'Personal Access Token' etc
        /// </para>
        /// </summary>
        /// <param name="targetUri"></param>
        /// <param name="username"></param>
        /// <param name="result"></param>
        /// <returns>the final <see cref="Credential"/> instance.</returns>
        private Credential GenerateCredentials(TargetUri targetUri, string username,
            ref AuthenticationResult result)
        {
            Credential credentials = (Credential)result.Token;

            var realUsername = GetRealUsername(result.RemoteUsername, username);

            if (!targetUri.TargetUriContainsUsername)
            {
                // no user info in uri so personalize the credentials
                credentials = new Credential(realUsername, credentials.Password);
            }

            return credentials;
        }

        /// <summary>
        /// Generate the final refresh token credentials for storing.
        /// <para>
        /// Bitbucket always wants the username as well as the password/token so if the username
        /// isn't explicit in the remote URL then we need to ensure the credentials are stored with a
        /// real username rather than 'Personal Access Token' etc. This applies to the refesh token
        /// as well.
        /// </para>
        /// <para></para>
        /// </summary>
        /// <param name="targetUri"></param>
        /// <param name="username"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        private Credential GenerateRefreshCredentials(TargetUri targetUri, string username,
            ref AuthenticationResult result)
        {
            Credential credentials = (Credential)result.Token;

            if (!targetUri.TargetUriContainsUsername)
            {
                // no user info in uri so personalize the credentials
                credentials = new Credential(username, result.RefreshToken.Value);
            }
            else
            {
                credentials = new Credential(credentials.Username, result.RefreshToken.Value);
            }

            return credentials;
        }

        /// <inheritdoc/>
        public async Task<Credential> ValidateCredentials(TargetUri targetUri, string username, Credential credentials)
        {
            BaseSecureStore.ValidateTargetUri(targetUri);
            BaseSecureStore.ValidateCredential(credentials);

            TargetUri userSpecificTargetUri;
            if (targetUri.TargetUriContainsUsername)
            {
                userSpecificTargetUri = targetUri;
            }
            else
            {
                userSpecificTargetUri = targetUri.GetPerUserTargetUri(username);
            }

            if (await BitbucketAuthority.ValidateCredentials(userSpecificTargetUri, username, credentials))
            {
                return credentials;
            }

            var userSpecificRefreshCredentials = GetCredentials(GetRefreshTokenTargetUri(userSpecificTargetUri), username);
            // if there are refresh credentials it suggests it might be OAuth so we can try and
            // refresh the access_token and try again.
            if (userSpecificRefreshCredentials == null)
            {
                return null;
            }

            Credential refreshedCredentials;
            if ((refreshedCredentials = await RefreshCredentials(userSpecificTargetUri, userSpecificRefreshCredentials.Password, username ?? credentials.Username)) != null)
            {
                return refreshedCredentials;
            }

            return null;
        }

        /// <summary>
        /// Use locally stored refresh_token to attempt to retrieve a new access_token.
        /// </summary>
        /// <param name="targetUri"></param>
        /// <param name="refreshToken"></param>
        /// <param name="username"></param>
        /// <returns>
        /// A <see cref="Credential"/> containing the new access_token if successful, null otherwise
        /// </returns>
        private async Task<Credential> RefreshCredentials(TargetUri targetUri, string refreshToken, string username)
        {
            Credential credentials = null;
            AuthenticationResult result;
            if ((result = await BitbucketAuthority.RefreshToken(targetUri, refreshToken)) == true)
            {
                Trace.WriteLine("token refresh succeeded");

                var tempCredentials = GenerateCredentials(targetUri, username, ref result);
                if (!await BitbucketAuthority.ValidateCredentials(targetUri, username, tempCredentials))
                {
                    // oddly our new access_token failed to work, maybe we've been revoked in the
                    // last millisecond?
                    return credentials;
                }

                // the new access_token is good, so store it and store the refresh_token used to get it.
                SetCredentials(targetUri, tempCredentials, null);
                var newRefreshCredentials = GenerateRefreshCredentials(targetUri, username, ref result);
                SetCredentials(GetRefreshTokenTargetUri(targetUri), newRefreshCredentials, username);

                credentials = tempCredentials;
            }

            return credentials;
        }

        private IAuthority BitbucketAuthority { get; }

        /// <summary>
        /// Delegate for Basic Auth credential acquisition from the UX.
        /// </summary>
        /// <param name="titleMessage">the title to display to the user.</param>
        /// <param name="targetUri">
        /// The uniform resource indicator used to uniquely identify the credentials.
        /// </param>
        /// <param name="username">The username supplied by the user.</param>
        /// <param name="password">The password supplied by the user.</param>
        /// <returns>True if successful; otherwise false.</returns>
        public delegate bool AcquireCredentialsDelegate(string titleMessage, TargetUri targetUri, out string username, out string password);

        /// <summary>
        /// Delegate for OAuth token acquisition from the UX.
        /// </summary>
        /// <param name="targetUri">
        /// The uniform resource indicator used to uniquely identify the credentials.
        /// </param>
        /// <param name="resultType">
        /// <para>The result of initial logon attempt, using the results of <see cref="AcquireCredentialsDelegate"/>.</para>
        /// <para>Should be <see cref="AuthenticationResultType.OAuth"/>.</para>
        /// </param>
        /// <param name="authenticationCode">The authentication code provided by the user.</param>
        /// <returns>True if successful; otherwise false.</returns>
        public delegate bool AcquireAuthenticationOAuthDelegate(string title, TargetUri targetUri, AuthenticationResultType resultType, string username);

        /// <summary>
        /// Delegate for reporting the success, or not, of an authentication attempt.
        /// </summary>
        /// <param name="targetUri">
        /// The uniform resource indicator used to uniquely identify the credentials.
        /// </param>
        /// <param name="result">The result of the interactive authentication attempt.</param>
        public delegate void AuthenticationResultDelegate(TargetUri targetUri, AuthenticationResultType result);
    }
}
