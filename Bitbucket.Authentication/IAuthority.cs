using Microsoft.Alm.Authentication;
using System.Threading.Tasks;

namespace Atlassian.Bitbucket.Authentication
{
    /// <summary>
    ///     Defines the interactions with the Authority capable of providing and validating Bitbucket credentials.
    /// </summary>
    internal interface IAuthority
    {
        /// <summary>
        ///     Use the provided credentials, username and password, to request an access token from the Authority.
        ///     <para>For Bitbucket an access token can be a password, if the user and their account is configured to allow Basic Auth requests.</para>
        /// </summary>
        /// <param name="targetUri">defines the Authority to call</param>
        /// <param name="username">the username to use when requesting the token</param>
        /// <param name="password">the password to use when requesting the token</param>
        /// <param name="resultType">Optional parameter. Used to pass in the results of a previous token request, e.g. first attempt used Basic Auth now try OAuth</param>
        /// <param name="scope">the access scopes to request</param>
        /// <returns></returns>
        Task<AuthenticationResult> AcquireToken(
            TargetUri targetUri,
            string username,
            string password,
            AuthenticationResultType resultType,
            TokenScope scope);

        /// <summary>
        ///     Use an existing refresh token to request a new access token from the specified Authority
        /// </summary>
        /// <param name="targetUri">defines the Authority to call</param>
        /// <param name="refreshToken">the existing refresh_token to use</param>
        /// <returns></returns>
        Task<AuthenticationResult> RefreshToken(
            TargetUri targetUri,
            string refreshToken);

        /// <summary>
        ///     Confirm that an existing set of credentials are still valid for accessing the Authority
        /// </summary>
        /// <param name="targetUri"></param>
        /// <param name="username"></param>
        /// <param name="credentials"></param>
        /// <returns></returns>
        Task<bool> ValidateCredentials(TargetUri targetUri, string username, Credential credentials);
    }
}