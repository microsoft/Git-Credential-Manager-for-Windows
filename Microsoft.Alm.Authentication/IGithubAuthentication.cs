using System;
using System.Threading.Tasks;

namespace Microsoft.Alm.Authentication
{
    public interface IGithubAuthentication : IAuthentication
    {
        bool InteractiveLogon(Uri targetUri, out Credential credentials);
        Task<bool> NoninteractiveLogonWithCredentials(Uri targetUri, string username, string password, string authenticationCode = null);
        Task<bool> ValidateCredentials(Uri targetUri, Credential credentials);
    }
}
