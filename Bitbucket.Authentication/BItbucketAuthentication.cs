using System;
using System.Diagnostics;
using Microsoft.Alm.Authentication;

namespace Bitbucket.Authentication
{
    /// <summary>
    ///     Extension of <see cref="BaseAuthentication" /> implementating <see cref="IBitbucketAuthentication" /> and providing
    ///     functionality to manage credentials for Bitbucket hosting service.
    /// </summary>
    public class BitbucketAuthentication : BaseAuthentication, IBitbucketAuthentication
    {
        public const string BitbucketBaseUrlHost = "bitbucket.org";

        public BitbucketAuthentication(ICredentialStore personalAccessTokenStore)
        {
            Console.WriteLine("BitbucketAuthentication");

            if (personalAccessTokenStore == null)
                throw new ArgumentNullException("personalAccessTokenStore",
                    "The parameter `personalAccessTokenStore` is null or invalid.");

            PersonalAccessTokenStore = personalAccessTokenStore;
        }

        internal ICredentialStore PersonalAccessTokenStore { get; set; }

        /// <inheritdoc />
        public override void DeleteCredentials(TargetUri targetUri)
        {
            Console.WriteLine("DeleteCredentials");
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public override Credential GetCredentials(TargetUri targetUri)
        {
            BaseSecureStore.ValidateTargetUri(targetUri);

            Trace.WriteLine("BitbucketAuthentication::GetCredentials");

            Credential credentials = null;

            if ((credentials = this.PersonalAccessTokenStore.ReadCredentials(targetUri)) != null)
            {
                Trace.WriteLine("   successfully retrieved stored credentials, updating credential cache");
            }

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
            ICredentialStore personalAccessTokenStore)
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
                authentication = new BitbucketAuthentication( /*tokenScope,*/ personalAccessTokenStore);
                    //, acquireCredentialsCallback, acquireAuthenticationCodeCallback, authenticationResultCallback);
                Trace.WriteLine("   authentication for Bitbucket created");
            }
            else
            {
                authentication = null;
                Trace.WriteLine("   not bitbucket.org, authentication creation aborted");
            }

            return authentication;
        }
    }
}