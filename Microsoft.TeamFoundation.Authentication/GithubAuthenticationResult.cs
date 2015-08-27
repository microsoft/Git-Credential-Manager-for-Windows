using System;
using System.Diagnostics;

namespace Microsoft.TeamFoundation.Authentication
{
    [DebuggerDisplay("{Type}")]
    public struct GithubAuthenticationResult
    {
        public GithubAuthenticationResult(GithubAuthenticationResultType type)
        {
            Type = type;
            Token = null;
        }

        public GithubAuthenticationResult(GithubAuthenticationResultType type, Token token)
        {
            Type = type;
            Token = token;
        }

        public readonly GithubAuthenticationResultType Type;
        public Token Token { get; internal set; }

        public static implicit operator Boolean(GithubAuthenticationResult result)
        {
            return result.Type == GithubAuthenticationResultType.Success;
        }

        public static implicit operator GithubAuthenticationResultType(GithubAuthenticationResult result)
        {
            return result.Type;
        }

        public static implicit operator GithubAuthenticationResult(GithubAuthenticationResultType type)
        {
            return new GithubAuthenticationResult(type);
        }
    }

    public enum GithubAuthenticationResultType
    {
        Success,
        Failure,
        TwoFactorApp,
        TwoFactorSms,
    }
}
