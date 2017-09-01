/**** Git Credential Manager for Windows ****
 *
 * Copyright (c) Atlassian
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

namespace Atlassian.Bitbucket.Authentication
{
    /// <summary>
    /// Defines the properties of interest of the results of an Authentication attempt.
    /// </summary>
    [DebuggerDisplay("{Type}")]
    public struct AuthenticationResult
    {
        public AuthenticationResult(AuthenticationResultType type)
        {
            Type = type;
            Token = null;
            RefreshToken = null;
            RemoteUsername = null;
        }

        public AuthenticationResult(AuthenticationResultType type, string username)
        {
            Type = type;
            Token = null;
            RefreshToken = null;
            RemoteUsername = username;
        }

        public AuthenticationResult(AuthenticationResultType type, Token token)
        {
            Type = type;
            Token = token;
            RefreshToken = null;
            RemoteUsername = null;
        }

        public AuthenticationResult(AuthenticationResultType type, Token token, string username)
        {
            Type = type;
            Token = token;
            RefreshToken = null;
            RemoteUsername = username;
        }

        public AuthenticationResult(AuthenticationResultType type, Token accessToken,
            Token refreshToken)
        {
            Type = type;
            Token = accessToken;
            RefreshToken = refreshToken;
            RemoteUsername = null;
        }

        public AuthenticationResult(AuthenticationResultType type, Token accessToken,
            Token refreshToken, string remoteUsername)
        {
            Type = type;
            Token = accessToken;
            RefreshToken = refreshToken;
            RemoteUsername = remoteUsername;
        }

        public readonly AuthenticationResultType Type;
        public Token Token { get; internal set; }
        public Token RefreshToken { get; internal set; }
        public string RemoteUsername { get; internal set; }

        /// <summary>
        /// Flag indicating if the results is a success
        /// </summary>
        public bool IsSuccess { get { return Type.Equals(AuthenticationResultType.Success); } }

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

    /// <summary>
    /// Defines the types of Authentication results we care about.
    /// </summary>
    public enum AuthenticationResultType
    {
        Success,
        Failure,
        TwoFactor,
        None,
    }
}
