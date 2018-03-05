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

namespace Microsoft.Alm.Authentication
{
    /// <summary>
    /// Credentials for user authentication.
    /// </summary>
    public sealed class Credential : Secret, IEquatable<Credential>
    {
        public static readonly Credential Empty = new Credential(string.Empty, string.Empty);

        /// <summary>
        /// Creates a credential object with a username and password pair.
        /// </summary>
        /// <param name="username">The username value of the <see cref="Credential"/>.</param>
        /// <param name="password">The password value of the <see cref="Credential"/>.</param>
        public Credential(string username, string password)
        {
            if (username is null)
                throw new ArgumentNullException(nameof(username));

            Username = username;
            Password = password ?? string.Empty;
        }

        /// <summary>
        /// Creates a credential object with only a username.
        /// </summary>
        /// <param name="username">The username value of the <see cref="Credential"/>.</param>
        public Credential(string username)
            : this(username, string.Empty)
        { }

        /// <summary>
        /// Secret related to the username.
        /// </summary>
        public readonly string Password;

        /// <summary>
        /// Unique identifier of the user.
        /// </summary>
        public readonly string Username;

        /// <summary>
        /// Compares an object to this <see cref="Credential"/> for equality.
        /// </summary>
        /// <param name="obj">The object to compare.</param>
        /// <returns><see langword="true"/> if equal; <see langword="false"/> otherwise.</returns>
        public override bool Equals(object obj)
        {
            return this == obj as Credential;
        }

        /// <summary>
        /// Compares a <see cref="Credential"/> to this <see cref="Credential"/> for equality.
        /// </summary>
        /// <param name="other">Credential to be compared.</param>
        /// <returns><see langword="true"/> if equal; <see langword="false"/> otherwise.</returns>
        public bool Equals(Credential other)
        {
            return this == other;
        }

        /// <summary>
        /// Gets a hash code based on the contents of the <see cref="Credential"/>.
        /// </summary>
        /// <returns>32-bit hash code.</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                return Username.GetHashCode() + 7 * Password.GetHashCode();
            }
        }

        /// <summary>
        /// Compares two credentials for equality.
        /// </summary>
        /// <param name="credential1">Credential to compare.</param>
        /// <param name="credential2">Credential to compare.</param>
        /// <returns><see langword="true"/> if equal; <see langword="false"/> otherwise.</returns>
        public static bool operator ==(Credential credential1, Credential credential2)
        {
            if (ReferenceEquals(credential1, credential2))
                return true;
            if (credential1 is null || credential2 is null)
                return false;

            return string.Equals(credential1.Username, credential2.Username, StringComparison.Ordinal)
                && string.Equals(credential1.Password, credential2.Password, StringComparison.Ordinal);
        }

        /// <summary>
        /// Compares two credentials for inequality.
        /// </summary>
        /// <param name="credential1">Credential to compare.</param>
        /// <param name="credential2">Credential to compare.</param>
        /// <returns><see langword="false"/> if equal; <see langword="true"/> otherwise.</returns>
        public static bool operator !=(Credential credential1, Credential credential2)
        {
            return !(credential1 == credential2);
        }
    }
}
