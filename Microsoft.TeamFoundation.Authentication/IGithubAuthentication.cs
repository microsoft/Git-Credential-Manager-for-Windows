using System;
using System.Threading.Tasks;

namespace Microsoft.TeamFoundation.Authentication
{
    public interface IGithubAuthentication : IAuthentication
    {
        bool InteractiveLogon(Uri targetUri, out Credential credentials);
    }
}
