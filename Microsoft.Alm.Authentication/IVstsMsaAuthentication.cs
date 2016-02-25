using System;

namespace Microsoft.Alm.Authentication
{
    public interface IVstsMsaAuthentication
    {
        bool InteractiveLogon(TargetUri targetUri, bool requestCompactToken);
    }
}
