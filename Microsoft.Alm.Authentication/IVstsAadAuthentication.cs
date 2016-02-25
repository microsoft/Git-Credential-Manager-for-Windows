using System;
using System.Threading.Tasks;

namespace Microsoft.Alm.Authentication
{
    public interface IVstsAadAuthentication
    {
        bool InteractiveLogon(TargetUri targetUri, bool requestCompactToken);
        Task<bool> NoninteractiveLogonWithCredentials(TargetUri targetUri, Credential credentials, bool requestCompactToken);
        Task<bool> NoninteractiveLogon(TargetUri targetUri, bool requestCompactToken);
    }
}
