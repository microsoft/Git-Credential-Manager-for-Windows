using System;

namespace Microsoft.TeamFoundation.Git.Helpers.Authentication
{
    public interface ITokenStore
    {
        void DeleteToken(Uri targetUri);
        bool ReadToken(Uri targetUri, out Token token);
        void WriteToken(Uri targetUri, Token token);
    }
}
