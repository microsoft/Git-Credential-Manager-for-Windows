using System;

namespace Microsoft.TeamFoundation.Git.Helpers.Authentication
{
    public interface IVsoMsaAuthentication
    {
        bool InteractiveLogon(Uri targetUri, bool requestCompactToken);
    }
}
