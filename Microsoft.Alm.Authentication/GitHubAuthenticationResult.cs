/**** Git Credential Manager for Windows ****
 *
 * Copyright (c) Microsoft Corporation
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

namespace Microsoft.Alm.Authentication
{
    [DebuggerDisplay("{Type}")]
    public struct GitHubAuthenticationResult : IEquatable<GitHubAuthenticationResult>
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

        public override Boolean Equals(object obj)
        {
            return (obj is GitHubAuthenticationResult
                    || obj is GitHubAuthenticationResultType)
                && this.Equals((GitHubAuthenticationResult)obj);
        }

        public bool Equals(GitHubAuthenticationResult other)
        {
            return this.Type == other.Type
                && this.Token == other.Token;
        }

        public static GitHubAuthenticationResult FromResultType(GitHubAuthenticationResultType type)
        {
            return new GitHubAuthenticationResult(type);
        }

        public override int GetHashCode()
        {
            return Token.GetHashCode();
        }

        public GitHubAuthenticationResultType ToResultType()
        {
            return Type;
        }

        public static bool operator ==(GitHubAuthenticationResult left, GitHubAuthenticationResult right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(GitHubAuthenticationResult left, GitHubAuthenticationResult right)
            => !(left == right);

        public static implicit operator bool(GitHubAuthenticationResult result)
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
