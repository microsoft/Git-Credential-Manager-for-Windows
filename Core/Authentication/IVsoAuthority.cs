using System;
using System.Threading.Tasks;

namespace Microsoft.TeamFoundation.Git.Helpers.Authentication
{
    internal interface IVsoAuthority
    {
        Task<Token> GeneratePersonalAccessToken(Uri targetUri, Token accessToken);
        Task<bool> ValidateCredentials(Credential credentials);
    }
}
