using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Alm.Authentication;
using System.Threading;
using System.Windows;
using System.Text.RegularExpressions;

using Trace = Microsoft.Alm.Git.Trace;

namespace Bitbucket.Authentication
{
    /// <summary>
    ///     Extension of <see cref="BaseAuthentication" /> implementating <see cref="IBitbucketAuthentication" /> and providing
    ///     functionality to manage credentials for Bitbucket hosting service.
    /// </summary>
    public class BitbucketAuthentication : BaseAuthentication, IBitbucketAuthentication
    {
        public const string BitbucketBaseUrlHost = "bitbucket.org";

        public BitbucketAuthentication(ICredentialStore personalAccessTokenStore,
            AcquireCredentialsDelegate acquireCredentialsCallback,
            AcquireAuthenticationOAuthDelegate acquireAuthenticationOAuthCallback)
        {
            if (personalAccessTokenStore == null)
                throw new ArgumentNullException("personalAccessTokenStore",
                    "The parameter `personalAccessTokenStore` is null or invalid.");

            PersonalAccessTokenStore = personalAccessTokenStore;

            BitbucketAuthority = new BitbucketAuthority();
            TokenScope = BitbucketTokenScope.SnippetWrite | BitbucketTokenScope.RepositoryWrite;
            ;
            AcquireCredentialsCallback = acquireCredentialsCallback;
            AcquireAuthenticationOAuthCallback = acquireAuthenticationOAuthCallback;
        }

        /// <summary>
        /// The desired scope of the authentication token to be requested.
        /// </summary>
        public readonly BitbucketTokenScope TokenScope;

        public ICredentialStore PersonalAccessTokenStore { get; }
        internal AcquireCredentialsDelegate AcquireCredentialsCallback { get; set; }
        internal AcquireAuthenticationOAuthDelegate AcquireAuthenticationOAuthCallback { get; set; }
        internal AuthenticationResultDelegate AuthenticationResultCallback { get; set; }

        private const string refreshTokenSuffix = "/refresh_token";

        /// <inheritdoc />
        public override void DeleteCredentials(TargetUri targetUri)
        {
            DeleteCredentials(targetUri, null);
        }

        public void DeleteCredentials(TargetUri targetUri, string username)
        {
            BaseSecureStore.ValidateTargetUri(targetUri);

            Trace.WriteLine("BitbucketAuthentication::DeleteCredentials");

            Credential credentials = null;

            var userTargetUri = GetRefreshTokenTargetUri(GetPerUserTargetUri(targetUri, username));
            if ((credentials = PersonalAccessTokenStore.ReadCredentials(targetUri)) != null)
            {
                // try to delete the credentials for the explicit target uri first
                PersonalAccessTokenStore.DeleteCredentials(targetUri);
                Trace.WriteLine("   host credentials deleted");
            }
            else if ((credentials = PersonalAccessTokenStore.ReadCredentials(userTargetUri)) != null)
            {
                PersonalAccessTokenStore.DeleteCredentials(userTargetUri);
                Trace.WriteLine("   user credentials deleted");
            }

            // tidy up and refresh tokens
            var hostRefreshTargetUri = GetRefreshTokenTargetUri(targetUri);
            var userRefreshTargetUri = GetRefreshTokenTargetUri(GetPerUserTargetUri(targetUri, username));
            if ((credentials = PersonalAccessTokenStore.ReadCredentials(hostRefreshTargetUri)) != null)
            {
                // try to delete the credentials for the explicit target uri first
                PersonalAccessTokenStore.DeleteCredentials(hostRefreshTargetUri);
                Trace.WriteLine("   host refresh credentials deleted");
            }
            else if ((credentials = PersonalAccessTokenStore.ReadCredentials(userRefreshTargetUri)) != null)
            {
                PersonalAccessTokenStore.DeleteCredentials(userRefreshTargetUri);
                Trace.WriteLine("   user refresh credentials deleted");
            }
        }

        private static TargetUri GetRefreshTokenTargetUri(TargetUri targetUri)
        {
            // TODO make more resiliant
            return
                new TargetUri(targetUri.ActualUri.AbsoluteUri.Substring(0, targetUri.ActualUri.AbsoluteUri.Length - 1) +
                              refreshTokenSuffix);
        }

        public Credential GetCredentials(TargetUri targetUri, string username)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                return GetCredentials(targetUri);
            }

            return GetCredentials(GetPerUserTargetUri(targetUri, username));
        }

        private TargetUri GetPerUserTargetUri(TargetUri targetUri, string username)
        {
            return
                new TargetUri(targetUri.ActualUri.AbsoluteUri.Replace(targetUri.Host, username + "@" + targetUri.Host));
        }

        private bool TargetUriContainsUsername(TargetUri targetUri)
        {
            return targetUri.ActualUri.AbsoluteUri.Contains("@");
        }

        /// <inheritdoc />
        public override Credential GetCredentials(TargetUri targetUri)
        {
            BaseSecureStore.ValidateTargetUri(targetUri);

            Trace.WriteLine("BitbucketAuthentication::GetCredentials");

            Credential credentials = null;

            if ((credentials = PersonalAccessTokenStore.ReadCredentials(targetUri)) != null)
            {
                Trace.WriteLine("   successfully retrieved stored credentials, updating credential cache");
                return credentials;
            }

            // try for a refresh token
            var refreshCredentials = PersonalAccessTokenStore.ReadCredentials(GetRefreshTokenTargetUri(targetUri));
            if (refreshCredentials == null)
            {
                // no refresh token return null
                return credentials;
            }

            Credential refreshedCredentials =
                Task.Run(() => RefreshCredentials(targetUri, refreshCredentials.Password, null)).Result;
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

        /// <inheritdoc />
        public override void SetCredentials(TargetUri targetUri, Credential credentials)
        {
            SetCredentials(targetUri, credentials, null);
        }

        public void SetCredentials(TargetUri targetUri, Credential credentials, string username)
        {
            BaseSecureStore.ValidateTargetUri(targetUri);
            BaseSecureStore.ValidateCredential(credentials);

            Trace.WriteLine("BitbucketAuthentication::SetCredentials");

            Credential tempCredentials;
            // if the url doesn't contain a username then save with an explicit username.
            if (TargetUriContainsUsername(targetUri) || string.IsNullOrWhiteSpace(username))
            {
                tempCredentials = credentials;
            }
            else
            {
                tempCredentials = new Credential(username, credentials.Password);
            }

            PersonalAccessTokenStore.WriteCredentials(targetUri, credentials);
        }

        /// <summary>
        ///     Identify the Hosting service from the the targetUri.
        /// </summary>
        /// <param name="targetUri"></param>
        /// <returns>A <see cref="BaseAuthentication" /> instance if the targetUri represents Bitbucket, null otherwise.</returns>
        public static BaseAuthentication GetAuthentication(TargetUri targetUri,
            ICredentialStore personalAccessTokenStore,
            AcquireCredentialsDelegate acquireCredentialsCallback,
            AcquireAuthenticationOAuthDelegate acquireAuthenticationOAuthCallback)
        {
            BaseAuthentication authentication = null;

            BaseSecureStore.ValidateTargetUri(targetUri);

            if (personalAccessTokenStore == null)
                throw new ArgumentNullException("personalAccessTokenStore",
                    "The `personalAccessTokenStore` is null or invalid.");

            Trace.WriteLine("BitbucketAuthentication::GetAuthentication");

            if (targetUri.ActualUri.DnsSafeHost.EndsWith(BitbucketBaseUrlHost, StringComparison.OrdinalIgnoreCase))
            {
                // TODO
                authentication = new BitbucketAuthentication( /*tokenScope,*/ personalAccessTokenStore
                    , acquireCredentialsCallback, acquireAuthenticationOAuthCallback);
                //acquireAuthenticationCodeCallback, authenticationResultCallback);
                Trace.WriteLine("   authentication for Bitbucket created");
            }
            else
            {
                authentication = null;
                Trace.WriteLine("   not bitbucket.org, authentication creation aborted");
            }

            return authentication;
        }

        public async Task<Credential> InteractiveLogon(TargetUri targetUri, string username)
        {
            if (string.IsNullOrWhiteSpace(username))
            {
                return await InteractiveLogon(targetUri);
            }

            return await InteractiveLogon(GetPerUserTargetUri(targetUri, username));
        }

        public async Task<Credential> InteractiveLogon(TargetUri targetUri)
        {
            Trace.WriteLine("BitbucketAuthentication::InteractiveLogon");

            Credential credentials = null;
            string username;
            string password;

            if (AcquireCredentialsCallback("Please enter your Bitbucket credentials for ", targetUri, out username,
                out password))
            {
                BitbucketAuthenticationResult result;

                if (
                    result =
                        await
                            BitbucketAuthority.AcquireToken(targetUri, username, password,
                                BitbucketAuthenticationResultType.None, this.TokenScope))
                {
                    Trace.WriteLine("   token acquisition succeeded");

                    credentials = GenerateCredentials(targetUri, username, ref result);
                    SetCredentials(targetUri, credentials, username);
                    //this.PersonalAccessTokenStore.WriteCredentials(targetUri, credentials);

                    // if a result callback was registered, call it
                    if (AuthenticationResultCallback != null)
                    {
                        AuthenticationResultCallback(targetUri, result);
                    }

                    return credentials;
                }
                else if (result == BitbucketAuthenticationResultType.TwoFactor)
                {
                    if (AcquireAuthenticationOAuthCallback("", targetUri, result, username))
                    {
                        if (
                            result =
                                await
                                    BitbucketAuthority.AcquireToken(targetUri, username, password,
                                        BitbucketAuthenticationResultType.TwoFactor, this.TokenScope))
                        {
                            Trace.WriteLine("   token acquisition succeeded");

                            credentials = GenerateCredentials(targetUri, username, ref result);
                            SetCredentials(targetUri, credentials, username);
                            //this.PersonalAccessTokenStore.WriteCredentials(targetUri, credentials);
                            SetCredentials(GetRefreshTokenTargetUri(targetUri),
                                new Credential(result.RefreshToken.Type.ToString(), result.RefreshToken.Value), username);
                            //this.PersonalAccessTokenStore.WriteCredentials(GetRefreshTokenTargetUri(targetUri), new Credential(result.RefreshToken.Type.ToString(), result.RefreshToken.Value));

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

            Trace.WriteLine("   interactive logon failed");
            return credentials;
        }

        private Credential GenerateCredentials(TargetUri targetUri, string username,
            ref BitbucketAuthenticationResult result)
        {
            Credential credentials = (Credential) result.Token;
            if (!TargetUriContainsUsername(targetUri))
            {
                // no user info in uri so personalize the credentials
                credentials = new Credential(username, credentials.Password);
            }

            return credentials;
        }

        public Credential GenerateRefreshCredentials(TargetUri targetUri, string username,
            ref BitbucketAuthenticationResult result)
        {
            Credential credentials = (Credential) result.Token;
            if (!TargetUriContainsUsername(targetUri))
            {
                // no user info in uri so personalize the credentials
                credentials = new Credential(username, result.RefreshToken.Value);
            }

            return credentials;
        }

        public async Task<bool> ValidateCredentials(TargetUri targetUri, string username, Credential credentials)
        {
            BaseSecureStore.ValidateTargetUri(targetUri);
            BaseSecureStore.ValidateCredential(credentials);

            Trace.WriteLine("BitbucketAuthentication::ValidateCredentials");

            var userSpecificTargetUri = GetPerUserTargetUri(targetUri, username);

            if (await BitbucketAuthority.ValidateCredentials(userSpecificTargetUri, username, credentials))
            {
                return true;
            }

            var userSpecificRefreshCredentials = GetCredentials(GetRefreshTokenTargetUri(targetUri), username);
            // if there are refresh credentials it suggests it might be OAuth so we can try and refresh the access_token and try again.
            if (userSpecificRefreshCredentials == null)
            {
                return false;
            }

            Credential refreshedCredentials;
            if (
                (refreshedCredentials =
                    await RefreshCredentials(userSpecificTargetUri, userSpecificRefreshCredentials.Password, username)) !=
                null)
            {
                return true;
            }
            //BitbucketAuthenticationResult result;
            //if (result = await BitbucketAuthority.RefreshToken(userSpecificTargetUri, userSpecificRefreshCredentials.Password))
            //{
            //    Trace.WriteLine("   token refresh succeeded");

            //    credentials = GenerateCredentials(targetUri, username, ref result);
            //    SetCredentials(userSpecificTargetUri, credentials);
            //    //this.PersonalAccessTokenStore.WriteCredentials(userSpecificTargetUri, credentials);
            //    var newRefreshCredentials = GenerateRefreshCredentials(targetUri, username, ref result);
            //    SetCredentials(GetRefreshTokenTargetUri(userSpecificTargetUri), newRefreshCredentials, username);
            //    //this.PersonalAccessTokenStore.WriteCredentials(GetRefreshTokenTargetUri(userSpecificTargetUri), new Credential(result.RefreshToken.Type.ToString(), result.RefreshToken.Value));

            //    if (await BitbucketAuthority.ValidateCredentials(userSpecificTargetUri, username, credentials))
            //    {
            //        return true;
            //    }
            //}

            return false;
        }

        private async Task<Credential> RefreshCredentials(TargetUri targetUri, string refreshToken, string username)
        {
            Credential credentials = null;
            BitbucketAuthenticationResult result;
            if ((result = await BitbucketAuthority.RefreshToken(targetUri, refreshToken)) == true)
            {
                Trace.WriteLine("   token refresh succeeded");

                var tempCredentials = GenerateCredentials(targetUri, username, ref result);
                if (!await BitbucketAuthority.ValidateCredentials(targetUri, username, tempCredentials))
                {
                    return credentials;
                }

                SetCredentials(targetUri, tempCredentials);
                //this.PersonalAccessTokenStore.WriteCredentials(userSpecificTargetUri, credentials);
                var newRefreshCredentials = GenerateRefreshCredentials(targetUri, username, ref result);
                SetCredentials(GetRefreshTokenTargetUri(targetUri), newRefreshCredentials, username);
                //this.PersonalAccessTokenStore.WriteCredentials(GetRefreshTokenTargetUri(userSpecificTargetUri), new Credential(result.RefreshToken.Type.ToString(), result.RefreshToken.Value));

                credentials = tempCredentials;
            }

            return credentials;
        }

        private IBitbucketAuthority BitbucketAuthority { get; }

        /// <summary>
        /// Delegate for credential acquisition from the UX.
        /// </summary>
        /// <param name="titleMessage">the title to display to the user.</param>
        /// <param name="targetUri">
        /// The uniform resource indicator used to uniquely identify the credentials.
        /// </param>
        /// <param name="username">The username supplied by the user.</param>
        /// <param name="password">The password supplied by the user.</param>
        /// <returns>True if successful; otherwise false.</returns>
        public delegate bool AcquireCredentialsDelegate(
            string titleMessage, TargetUri targetUri, out string username, out string password);

        /// <summary>
        /// Delegate for authentication oauth acquisition from the UX.
        /// </summary>
        /// <param name="targetUri">
        /// The uniform resource indicator used to uniquely identify the credentials.
        /// </param>
        /// <param name="resultType">
        /// <para>The result of initial logon attempt, using the results of <see cref="AcquireCredentialsDelegate"/>.</para>
        /// <para>Should be <see cref="BitbucketAuthenticationResultType.OAuth"/>.</para>
        /// </param>
        /// <param name="authenticationCode">The authentication code provided by the user.</param>
        /// <returns>True if successful; otherwise false.</returns>
        public delegate bool AcquireAuthenticationOAuthDelegate(
            string title, TargetUri targetUri, BitbucketAuthenticationResultType resultType,
            string username);

        /// <summary>
        /// Delegate for reporting the success, or not, of an authentication attempt.
        /// </summary>
        /// <param name="targetUri">
        /// The uniform resource indicator used to uniquely identify the credentials.
        /// </param>
        /// <param name="result">The result of the interactive authentication attempt.</param>
        public delegate void AuthenticationResultDelegate(TargetUri targetUri, BitbucketAuthenticationResultType result);
    }
}