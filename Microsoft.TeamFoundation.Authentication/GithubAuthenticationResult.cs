using System;

namespace Microsoft.TeamFoundation.Authentication
{
    public struct GithubAuthenticationResult
    {
        public GithubAuthenticationResult(GithubAuthenticationResultType type)
        {
            Type = type;
        }

        public readonly GithubAuthenticationResultType Type;

        public static implicit operator Boolean(GithubAuthenticationResult result)
        {
            return result.Type != GithubAuthenticationResultType.Failure;
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
