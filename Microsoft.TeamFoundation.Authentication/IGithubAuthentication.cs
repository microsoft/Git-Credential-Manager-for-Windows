using System;
using System.Threading.Tasks;

namespace Microsoft.TeamFoundation.Authentication
{
    public interface IGithubAuthentication : IAuthentication
    {
        bool InteractiveLogon(Uri targetUri, GithubTokenScope scope, out Credential credentials);
    }
}
