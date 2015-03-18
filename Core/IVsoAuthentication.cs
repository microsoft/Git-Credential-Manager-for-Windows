using System;
using System.Threading.Tasks;

namespace Microsoft.TeamFoundation.Git.Helpers.Authentication
{
    public interface IVsoAuthentication:IAuthentication
    {
        Task<bool> InteractiveLogon(Uri targetUri, Credentials credentials);
    }
}
