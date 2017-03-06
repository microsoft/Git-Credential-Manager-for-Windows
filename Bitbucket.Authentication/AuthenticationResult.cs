using Microsoft.Alm.Authentication;
using System;
using System.Diagnostics;

namespace Atlassian.Bitbucket.Authentication
{
    [DebuggerDisplay("{Type}")]
    public struct AuthenticationResult
    {
        public AuthenticationResult(AuthenticationResultType type)
        {
            Type = type;
            Token = null;
            RefreshToken = null;
        }

        public AuthenticationResult(AuthenticationResultType type, Token token)
        {
            Type = type;
            Token = token;
            RefreshToken = null;
        }

        public AuthenticationResult(AuthenticationResultType type, Token accessToken,
            Token refreshToken)
        {
            Type = type;
            Token = accessToken;
            RefreshToken = refreshToken;
        }

        public readonly AuthenticationResultType Type;
        public Token Token { get; internal set; }
        public Token RefreshToken { get; internal set; }

        public static implicit operator Boolean(AuthenticationResult result)
        {
            return result.Type == AuthenticationResultType.Success;
        }

        public static implicit operator AuthenticationResultType(AuthenticationResult result)
        {
            return result.Type;
        }

        public static implicit operator AuthenticationResult(AuthenticationResultType type)
        {
            return new AuthenticationResult(type);
        }
    }

    public enum AuthenticationResultType
    {
        Success,
        Failure,
        TwoFactor,
        None,
    }
}