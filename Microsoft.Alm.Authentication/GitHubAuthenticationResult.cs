using System;
using System.Diagnostics;

namespace Microsoft.Alm.Authentication
{
    [DebuggerDisplay("{Type}")]
    public struct GitHubAuthenticationResult
    {
        public GitHubAuthenticationResult(GitHubAuthenticationResultType type)
        {
            Type = type;
            Token = null;
        }

        public GitHubAuthenticationResult(GitHubAuthenticationResultType type, Token token)
        {
            Type = type;
            Token = token;
        }

        public readonly GitHubAuthenticationResultType Type;
        public Token Token { get; internal set; }

        public static implicit operator Boolean(GitHubAuthenticationResult result)
        {
            return result.Type == GitHubAuthenticationResultType.Success;
        }

        public static implicit operator GitHubAuthenticationResultType(GitHubAuthenticationResult result)
        {
            return result.Type;
        }

        public static implicit operator GitHubAuthenticationResult(GitHubAuthenticationResultType type)
        {
            return new GitHubAuthenticationResult(type);
        }
    }

    public enum GitHubAuthenticationResultType
    {
        Success,
        Failure,
        TwoFactorApp,
        TwoFactorSms,
    }
}
