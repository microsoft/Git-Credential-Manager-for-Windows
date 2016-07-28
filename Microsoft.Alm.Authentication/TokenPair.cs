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
    /// <summary>
    /// Azure token pair: access and refresh.
    /// </summary>
    internal sealed class TokenPair : IEquatable<TokenPair>
    {
        /// <summary>
        /// Creates a new <see cref="TokenPair"/> from raw access and refresh token data.
        /// </summary>
        /// <param name="accessToken">The base64 encoded value of the access token's raw data</param>
        /// <param name="refreshToken">The base64 encoded value of the refresh token's raw data</param>
        public TokenPair(string accessToken, string refreshToken)
        {
            Debug.Assert(!String.IsNullOrWhiteSpace(accessToken), "The accessToken parameter is null or invalid.");
            Debug.Assert(!String.IsNullOrWhiteSpace(refreshToken), "The refreshToken parameter is null or invalid.");

            this.AccessToken = new Token(accessToken, TokenType.Access);
            this.RefeshToken = new Token(refreshToken, TokenType.Refresh);
        }
        /// <summary>
        /// Creates a new <see cref="TokenPair"/> from an ADAL <see cref="IdentityModel.Clients.ActiveDirectory.AuthenticationResult"/>.
        /// </summary>
        /// <param name="authResult">
        /// A successful <see cref="IdentityModel.Clients.ActiveDirectory.AuthenticationResult"/>
        /// which contains both access and refresh token data.
        /// </param>
        public TokenPair(IdentityModel.Clients.ActiveDirectory.AuthenticationResult authResult)
        {
            Debug.Assert(authResult != null, "The authResult parameter is null.");
            Debug.Assert(!String.IsNullOrWhiteSpace(authResult.AccessToken), "The authResult.AccessToken parameter is null or invalid.");
            Debug.Assert(!String.IsNullOrWhiteSpace(authResult.RefreshToken), "The authResult.RefreshToken parameter is null or invalid.");
            Debug.Assert(authResult.ExpiresOn > DateTimeOffset.UtcNow, "The authResult is expired and invalid.");

            this.AccessToken = new Token(authResult, TokenType.Access);
            this.RefeshToken = new Token(authResult, TokenType.Refresh);
        }

        /// <summary>
        /// Access token, used to grant access to resources.
        /// </summary>
        public readonly Token AccessToken;
        /// <summary>
        /// Refresh token, used to grant new access tokens.
        /// </summary>
        public readonly Token RefeshToken;

        /// <summary>
        /// Compares an object to this.
        /// </summary>
        /// <param name="obj">The object to compare.</param>
        /// <returns>True if equal; false otherwise.</returns>
        public override bool Equals(Object obj)
        {
            return this.Equals(obj as TokenPair);
        }
        /// <summary>
        /// Compares a <see cref="TokenPair"/> to this this.
        /// </summary>
        /// <param name="other">The <see cref="TokenPair"/> to compare.</param>
        /// <returns>True if equal; otherwise false.</returns>
        public bool Equals(TokenPair other)
        {
            return this == other;
        }
        /// <summary>
        /// Gets a hash code based on the contents of the <see cref="TokenPair"/>.
        /// </summary>
        /// <returns>32-bit hash code.</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                return AccessToken.GetHashCode() * RefeshToken.GetHashCode();
            }
        }

        /// <summary>
        /// Compares two <see cref="TokenPair"/> for equality.
        /// </summary>
        /// <param name="pair1"><see cref="TokenPair"/> to compare.</param>
        /// <param name="pair2"><see cref="TokenPair"/> to compare.</param>
        /// <returns>True if equal; false otherwise.</returns>
        public static bool operator ==(TokenPair pair1, TokenPair pair2)
        {
            if (ReferenceEquals(pair1, pair2))
                return true;
            if (ReferenceEquals(pair1, null) || ReferenceEquals(null, pair2))
                return false;

            return pair1.AccessToken == pair2.AccessToken
                && pair1.RefeshToken == pair2.RefeshToken;
        }
        /// <summary>
        /// Compares two <see cref="TokenPair"/> for inequality.
        /// </summary>
        /// <param name="pair1"><see cref="TokenPair"/> to compare.</param>
        /// <param name="pair2"><see cref="TokenPair"/> to compare.</param>
        /// <returns>False if equal; true otherwise.</returns>
        public static bool operator !=(TokenPair pair1, TokenPair pair2)
        {
            return !(pair1 == pair2);
        }
    }
}
