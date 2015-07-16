using System;

namespace Microsoft.TeamFoundation.Git.Helpers.Authentication
{
    public class AuthenticationTests
    {
        public static readonly Credential DefaultCredentials = new Credential("username", "password");
        public static readonly Token DefaultAzureAccessToken = new Token("azure-access-token", TokenType.Test);
        public static readonly Token DefaultAzureRefreshToken = new Token("azure-refresh-token", TokenType.Test);
        public static readonly Token DefaultPersonalAccessToken = new Token("personal-access-token", TokenType.Test);
        public static readonly Uri DefaultTargetUri = new Uri("https://unit-test.uri/git-credential");
    }
}
