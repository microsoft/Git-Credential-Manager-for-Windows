using System;
using System.Threading.Tasks;

namespace Microsoft.TeamFoundation.Git.Helpers.Authentication
{
    public interface IVsoAadAuthentication
    {
        bool InteractiveLogon(Uri targetUri);
        Task<bool> NoninteractiveLogonWithCredentials(Uri targetUri, Credential credentials);
        Task<bool> NoninteractiveLogon(Uri targetUri);
    }
}
