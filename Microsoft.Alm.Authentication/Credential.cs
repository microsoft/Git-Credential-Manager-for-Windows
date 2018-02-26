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
using System.Net;
using System.Text;

namespace Microsoft.Alm.Authentication
{
    /// <summary>
    /// Credentials for user authentication.
    /// </summary>
    public sealed class Credential : Secret, IEquatable<Credential>
    {
        private static readonly CredentialComparer Comparer = new CredentialComparer();

        /// <summary>
        /// Creates a credential object with a username and password pair.
        /// </summary>
        /// <param name="username">The username value of the `<see cref="Credential"/>`.</param>
        /// <param name="password">The password value of the `<see cref="Credential"/>`.</param>
        public Credential(string username, string password)
        {
            if (username is null)
                throw new ArgumentNullException(nameof(username));

            _username = username;
            _password = password ?? string.Empty;
        }

        /// <summary>
        /// Creates a credential object with only a username.
        /// </summary>
        /// <param name="username">The username value of the `<see cref="Credential"/>`.</param>
        public Credential(string username)
            : this(username, string.Empty)
        { }

        private string _username;
        private string _password;

        /// <summary>
        /// Secret related to the username.
        /// </summary>
        public string Password
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

        public override string Value
            => ToString();

        /// <summary>
        /// Compares a <see cref="Credential"/> to this <see cref="Credential"/> for equality.
        /// <para/>
        /// Returns `<see langword="true"/>` if equal; otherwise `<see langword="false"/>`.
        /// </summary>
        /// <param name="other">Credential to be compared.</param>
        public bool Equals(Credential other)
            => Comparer.Equals(this, other);

        public override bool Equals(object obj)
        {
            return (obj is Credential other
                    && Equals(other))
                || base.Equals(obj);
        }

        /// <summary>
        /// Returns a credentials associated with a specified URL, and authentication type.
        /// </summary>
        /// <param name="uri">The URI the credentials are for.</param>
        /// <param name="authType">The type of authentication the credentials are for.</param>
        public override NetworkCredential GetCredential(Uri uri, string authType)
        {
            if (StringComparer.OrdinalIgnoreCase.Equals(authType, "basic"))
                return new NetworkCredential(_username, _password);

            return null;
        }

        public override int GetHashCode()
            => Comparer.GetHashCode(this);

        /// <summary>
        /// Returns the credentials in a base64 encoded `"{username}:{password}" format.
        /// </summary>
        /// <returns></returns>
        public string ToBase64String()
        {
            // Get the simple {username}:{password} format.
            string basicAuthValue = ToString();

            // Convert the string into UTF-8 encoded bytes.
            byte[] authBytes = Encoding.UTF8.GetBytes(basicAuthValue);

            // Base64 encode the bytes (as another string).
            basicAuthValue = Convert.ToBase64String(authBytes);

            return basicAuthValue;
        }

        public override string ToString()
        {
            return string.Format(System.Globalization.CultureInfo.InvariantCulture, "{0}:{1}", _username, _password);
        }

        public static bool operator ==(Credential lhs, Credential rhs)
        {
            if (ReferenceEquals(lhs, rhs))
                return true;
            if (lhs is null || rhs is null)
                return false;

            return string.Equals(lhs.Username, rhs.Username, StringComparison.Ordinal)
                && string.Equals(lhs.Password, rhs.Password, StringComparison.Ordinal);
        }

        public static bool operator !=(Credential lhs, Credential rhs)
        {
            return !(lhs == rhs);
        }
    }
}
