using System.Diagnostics;

namespace Microsoft.Alm.Authentication.Test
{
    public abstract class AuthenticationTests
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        protected static readonly Credential DefaultCredentials = new Credential("username", "password");

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        protected static readonly Token DefaultAzureAccessToken = new Token("azure-access-token", TokenType.Test);

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        protected static readonly Token DefaultAzureRefreshToken = new Token("azure-refresh-token", TokenType.Test);

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        protected static readonly Credential DefaultPersonalAccessToken = new Credential("personal-access-token", "personal-access-token");

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        protected static readonly TargetUri DefaultTargetUri = new TargetUri("https://unit-test.uri/git-credential");

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        protected static readonly TargetUri InvalidTargetUri = new TargetUri("https://invlaid-test.uri/git-credential");

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        protected static readonly VstsTokenScope DefaultTokenScope = VstsTokenScope.CodeWrite;

        protected AuthenticationTests()
        {
            if (Trace.Listeners.Count == 0)
            {
                Trace.Listeners.AddRange(Debug.Listeners);
            }
        }
    }
}
