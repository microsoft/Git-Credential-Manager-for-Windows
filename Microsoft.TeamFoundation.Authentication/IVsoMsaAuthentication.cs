using System;

namespace Microsoft.TeamFoundation.Authentication
{
    public interface IVsoMsaAuthentication
    {
        bool InteractiveLogon(Uri targetUri, bool requestCompactToken);
    }
}
