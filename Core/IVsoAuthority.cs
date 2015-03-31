using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.TeamFoundation.Git.Helpers.Authentication
{
    internal interface IVsoAuthority
    {
        Task<Credential> GeneratePersonalAccessToken(Uri targetUri, Token accessToken);
        Task<bool> ValidateCredentials(Credential credentials);
    }
}
