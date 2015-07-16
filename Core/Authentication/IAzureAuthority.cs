using System;
using System.Threading.Tasks;

namespace Microsoft.TeamFoundation.Git.Helpers.Authentication
{
    internal interface IAzureAuthority : IVsoAuthority
    {
        Task<Tokens> AcquireTokenAsync(Uri targetUri, string clientId, string resource, Credential credentials = null);
    }
}
