
using Microsoft.Alm.Authentication;
using System;
using System.Diagnostics;

namespace Bitbucket.Authentication
{
    [DebuggerDisplay("{Type}")]
    public struct BitbucketAuthenticationResult
    {
        public BitbucketAuthenticationResult(BitbucketAuthenticationResultType type)
        {
            Type = type;
            Token = null;
            RefreshToken = null;
        }

        public BitbucketAuthenticationResult(BitbucketAuthenticationResultType type, Token token)
        {
            Type = type;
            Token = token;
            RefreshToken = null;
        }

        public BitbucketAuthenticationResult(BitbucketAuthenticationResultType type, Token accessToken, Token refreshToken)
        {
            Type = type;
            Token = accessToken;
            RefreshToken = refreshToken;
        }

        public readonly BitbucketAuthenticationResultType Type;
        public Token Token { get; internal set; }
        public Token RefreshToken { get; internal set; }

        public static implicit operator Boolean(BitbucketAuthenticationResult result)
        {
            return result.Type == BitbucketAuthenticationResultType.Success;
        }

        public static implicit operator BitbucketAuthenticationResultType(BitbucketAuthenticationResult result)
        {
            return result.Type;
        }

        public static implicit operator BitbucketAuthenticationResult(BitbucketAuthenticationResultType type)
        {
            return new BitbucketAuthenticationResult(type);
        }
    }

    public enum BitbucketAuthenticationResultType
    {
        Success,
        Failure,
        TwoFactor,
        None,
    }
}
