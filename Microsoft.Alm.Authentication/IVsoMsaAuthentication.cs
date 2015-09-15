using System;

namespace Microsoft.Alm.Authentication
{
    public interface IVsoMsaAuthentication
    {
        bool InteractiveLogon(Uri targetUri, bool requestCompactToken);
    }
}
