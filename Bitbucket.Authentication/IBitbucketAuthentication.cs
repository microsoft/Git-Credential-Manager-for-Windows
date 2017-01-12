using Microsoft.Alm.Authentication;
using System.Threading.Tasks;

namespace Bitbucket.Authentication
{
    // TODO share
    public interface IBitbucketAuthentication
    {
        /// <summary>
        /// <para></para>
        /// <para>Tokens acquired are stored in the secure secret store provided during
        /// initialization.</para>
        /// </summary>
        /// <param name="targetUri">The unique identifier for the resource for which access is to
        /// be acquired.</param>
        /// /// <returns>Acquired <see cref="Credential"/> if successful; otherwise <see langword="null"/>.</returns>
        Task<Credential> InteractiveLogon(TargetUri targetUri);


        Task<Credential> ValidateCredentials(TargetUri targetUri, string username, Credential credentials);
    }
}