/**** Git Credential Manager for Windows ****
 *
 * Copyright (c) GitHub Corporation
 * All rights reserved.
 *
 * MIT License
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the """"Software""""), to deal
 * in the Software without restriction, including without limitation the rights to
 * use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
 * the Software, and to permit persons to whom the Software is furnished to do so,
 * subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED *AS IS*, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
 * FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
 * COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN
 * AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
 * WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE."
**/

using System;
using System.Diagnostics;
using Microsoft.Alm.Authentication;

namespace GitHub.Authentication
{
    [DebuggerDisplay("{Type}")]
    public struct AuthenticationResult : IEquatable<AuthenticationResult>
    {
        public AuthenticationResult(GitHubAuthenticationResultType type)
        {
            Type = type;
            Token = null;
        }

        public AuthenticationResult(GitHubAuthenticationResultType type, Token token)
        {
            Type = type;
            Token = token;
        }

        public readonly GitHubAuthenticationResultType Type;
        public Token Token { get; internal set; }

        public override Boolean Equals(object obj)
        {
            return (obj is AuthenticationResult
                    || obj is GitHubAuthenticationResultType)
                && Equals((AuthenticationResult)obj);
        }

        public bool Equals(AuthenticationResult other)
        {
            return Type == other.Type
                && Token == other.Token;
        }

        public static AuthenticationResult FromResultType(GitHubAuthenticationResultType type)
        {
            return new AuthenticationResult(type);
        }

        public override int GetHashCode()
        {
            return Token.GetHashCode();
        }

        public GitHubAuthenticationResultType ToResultType()
        {
            return Type;
        }

        public static bool operator ==(AuthenticationResult left, AuthenticationResult right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(AuthenticationResult left, AuthenticationResult right)
            => !(left == right);

        public static implicit operator bool(AuthenticationResult result)
        {
            return result.Type == GitHubAuthenticationResultType.Success;
        }

        public static implicit operator GitHubAuthenticationResultType(AuthenticationResult result)
        {
            return result.Type;
        }

        public static implicit operator AuthenticationResult(GitHubAuthenticationResultType type)
        {
            return new AuthenticationResult(type);
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
