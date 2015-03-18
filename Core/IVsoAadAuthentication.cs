using System;
using System.Threading.Tasks;

namespace Microsoft.TeamFoundation.Git.Helpers.Authentication
{
    public interface IVsoAadAuthentication : IVsoAuthentication
    {
        Task<bool> NoninteractiveLogon(Uri targetUri);
        Task<bool> RefreshCredentials(Uri targetUri);
    }
}
