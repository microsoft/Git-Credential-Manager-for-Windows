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
using static System.StringComparer;
using static System.Text.Encoding;
using Convert = System.Convert;
using CultureInfo = System.Globalization.CultureInfo;

namespace Microsoft.Alm.Authentication
{
    /// <summary>
    /// Credentials for user authentication.
    /// </summary>
    public sealed class Credential : Secret, IEquatable<Credential>
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly Credential Empty = new Credential(string.Empty, string.Empty);

        /// <summary>
        /// Creates a credential object with a username and password pair.
        /// </summary>
        /// <param name="Username">The username value of the `<see cref="Credential"/>`.</param>
        /// <param name="Password">The password value of the `<see cref="Credential"/>`.</param>
        public Credential(string Username, string Password)
        {
            if (Username is null)
                throw new ArgumentNullException(nameof(Username));

            _password = Password ?? string.Empty;
            _username = Username;
        }

        private readonly string _password;
        private readonly string _username;

        /// <summary>
        /// Secret related to username.
        /// </summary>
        public  string Password
        {
            get { return _password; }
        }

        /// <summary>
        /// Unique identifier of the user.
        /// </summary>
        public string Username
        {
            get { return _username; }
        }

        /// <summary>
        /// Compares an object to this instance for equality.
        /// <para/>
        /// Returns `<see langword="true"/>` if equal; otherwise `<see langword="false"/>`.
        /// </summary>
        /// <param name="obj">The object to compare.</param>
        public override bool Equals(object obj)
        {
            return this == obj as Credential;
        }

        /// <summary>
        /// Compares a `<see cref="Credential"/>` to this instance for equality.
        /// <para/>
        /// Returns `<see langword="true"/>` if equal; otherwise `<see langword="false"/>`.
        /// </summary>
        /// <param name="other">Credential to be compared.</param>
        public bool Equals(Credential other)
        {
            return this == other;
        }

        /// <summary>
        /// Returns the hash code based on the contents of this `<see cref="Credential"/>`.
        /// </summary>
        public override int GetHashCode()
        {
            unchecked
            {
                // User the upper-16 bits of the username hash code combined
                // with the lower 16-bits of the password has code to compose
                // the credentials hash code.
                unchecked
                {
                    return (int)(Ordinal.GetHashCode(_username) & 0xFFFF0000)
                         | (int)(Ordinal.GetHashCode(_password) & 0x0000FFFF);
                }
            }
        }

        /// <summary>
        /// Returns the base-64 encoded, {username}:{password} formatted string of this `<see cref="Credential"/>.
        /// </summary>
        public string ToBase64String()
        {
            string basicAuthValue = string.Format(CultureInfo.InvariantCulture, "{0}:{1}", _username, _password);
            byte[] authBytes = UTF8.GetBytes(basicAuthValue);
            return Convert.ToBase64String(authBytes);
        }

        /// <summary>
        /// Compares two credentials for equality.
        /// <para/>
        /// Returns `<see langword="true"/>` if equal; otherwise `<see langword="false"/>`.
        /// </summary>
        /// <param name="lhs">Credential to compare.</param>
        /// <param name="rhs">Credential to compare.</param>
        public static bool operator ==(Credential lhs, Credential rhs)
        {
            if (ReferenceEquals(lhs, rhs))
                return true;
            if (lhs is null || rhs is null)
                return false;

            return Ordinal.Equals(lhs.Username, rhs.Username)
                && Ordinal.Equals(lhs.Password, rhs.Password);
        }

        /// <summary>
        /// Compares two credentials for inequality.
        /// <para/>
        /// Returns `<see langword="false"/>` if equal; otherwise `<see langword="true"/>`.
        /// </summary>
        /// <param name="lhs">Credential to compare.</param>
        /// <param name="rhs">Credential to compare.</param>
        public static bool operator !=(Credential lhs, Credential rhs)
        {
            return !(lhs == rhs);
        }
    }
}
