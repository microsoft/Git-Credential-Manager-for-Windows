using System.Threading.Tasks;

namespace Microsoft.Alm.Authentication
{
    public interface IGithubAuthentication : IAuthentication
    {
        bool InteractiveLogon(TargetUri targetUri, out Credential credentials);
        Task<bool> NoninteractiveLogonWithCredentials(TargetUri targetUri, string username, string password, string authenticationCode = null);
        Task<bool> ValidateCredentials(TargetUri targetUri, Credential credentials);
    }
}
