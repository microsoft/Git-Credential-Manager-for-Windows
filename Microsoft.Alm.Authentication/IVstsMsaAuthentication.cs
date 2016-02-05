using System;

namespace Microsoft.Alm.Authentication
{
    public interface IVstsMsaAuthentication
    {
        bool InteractiveLogon(Uri targetUri, bool requestCompactToken);
    }
}
