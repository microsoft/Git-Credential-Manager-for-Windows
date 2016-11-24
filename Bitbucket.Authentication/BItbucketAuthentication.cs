using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Alm.Authentication;
using System.Threading;
using System.Windows;

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
            TokenScope = BitbucketTokenScope.SnippetWrite | BitbucketTokenScope.RepositoryWrite; ;
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

        private const string refreshTokenSuffix = "-token";

        /// <inheritdoc />
        public override void DeleteCredentials(TargetUri targetUri)
        {
            BaseSecureStore.ValidateTargetUri(targetUri);

            Trace.WriteLine("BitbucketAuthentication::DeleteCredentials");

            Credential credentials = null;

            if ((credentials = PersonalAccessTokenStore.ReadCredentials(targetUri)) != null)
            {
                PersonalAccessTokenStore.DeleteCredentials(targetUri);
                Trace.WriteLine("   credentials deleted");
            }

            // tidy up and refresh tokens
            if ((credentials = PersonalAccessTokenStore.ReadCredentials(GetRefreshTokenTargetUri(targetUri))) != null)
            {
                PersonalAccessTokenStore.DeleteCredentials(targetUri);
                Trace.WriteLine("   refresh credentials deleted");
            }
        }

        private static TargetUri GetRefreshTokenTargetUri(TargetUri targetUri)
        {
            // TODO make more resiliant
            return new TargetUri(targetUri.ActualUri.AbsoluteUri.Substring(0, targetUri.ActualUri.AbsoluteUri.Length - 1) + refreshTokenSuffix);
        }

        /// <inheritdoc />
        public override Credential GetCredentials(TargetUri targetUri)
        {
            BaseSecureStore.ValidateTargetUri(targetUri);

            Trace.WriteLine("BitbucketAuthentication::GetCredentials");

            Credential credentials = null;

            if ((credentials = PersonalAccessTokenStore.ReadCredentials(targetUri)) != null)
                Trace.WriteLine("   successfully retrieved stored credentials, updating credential cache");

            return credentials;
        }

        /// <inheritdoc />
        public override void SetCredentials(TargetUri targetUri, Credential credentials)
        {
            BaseSecureStore.ValidateTargetUri(targetUri);
            BaseSecureStore.ValidateCredential(credentials);

            Trace.WriteLine("BitbucketAuthentication::SetCredentials");

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

        public async Task<Credential> InteractiveLogon(TargetUri targetUri)
        {
            Trace.WriteLine("BitbucketAuthentication::InteractiveLogon");

            Credential credentials = null;
            string username;
            string password;

            if (AcquireCredentialsCallback("Please enter your Bitbucket credentials for ",targetUri, out username, out password))
            {
                BitbucketAuthenticationResult result;

                if (result = await BitbucketAuthority.AcquireToken(targetUri, username, password, BitbucketAuthenticationResultType.None, this.TokenScope))
                {
                    Trace.WriteLine("   token acquisition succeeded");

                    credentials = (Credential)result.Token;
                    this.PersonalAccessTokenStore.WriteCredentials(targetUri, credentials);

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
                        if (result = await BitbucketAuthority.AcquireToken(targetUri, username, password, BitbucketAuthenticationResultType.TwoFactor, this.TokenScope))
                        {
                            Trace.WriteLine("   token acquisition succeeded");

                            credentials = (Credential)result.Token;
                            this.PersonalAccessTokenStore.WriteCredentials(targetUri, credentials);
                            this.PersonalAccessTokenStore.WriteCredentials(GetRefreshTokenTargetUri(targetUri), new Credential(result.RefreshToken.Type.ToString(), result.RefreshToken.Value));

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

        public async Task<bool> ValidateCredentials(TargetUri targetUri, string username, Credential credentials)
        {
            BaseSecureStore.ValidateTargetUri(targetUri);
            BaseSecureStore.ValidateCredential(credentials);

            Trace.WriteLine("BitbucketAuthentication::ValidateCredentials");

            if(await BitbucketAuthority.ValidateCredentials(targetUri, username, credentials))
            {
                return true;
            }

            var refreshCredentials = this.PersonalAccessTokenStore.ReadCredentials(GetRefreshTokenTargetUri(targetUri));
            // if there are refresh credentials it suggests it might be OAuth so we can try and refresh the access_token and try again.
            if (refreshCredentials == null)
            {
                return false;
            }

            BitbucketAuthenticationResult result;
            if (result = await BitbucketAuthority.RefreshToken(targetUri, refreshCredentials.Password))
            {
                Trace.WriteLine("   token refresh succeeded");

                credentials = (Credential)result.Token;
                this.PersonalAccessTokenStore.WriteCredentials(targetUri, credentials);
                this.PersonalAccessTokenStore.WriteCredentials(GetRefreshTokenTargetUri(targetUri), new Credential(result.RefreshToken.Type.ToString(), result.RefreshToken.Value));

                if (await BitbucketAuthority.ValidateCredentials(targetUri, username, credentials))
                {
                    return true;
                }
            }

            return false;
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
        public delegate bool AcquireCredentialsDelegate(string titleMessage, TargetUri targetUri, out string username, out string password);

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
        public delegate bool AcquireAuthenticationOAuthDelegate(string title, TargetUri targetUri, BitbucketAuthenticationResultType resultType,
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