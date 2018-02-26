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
using System.Collections.Generic;

namespace Microsoft.Alm.Authentication
{
    public class SecretComparer : IEqualityComparer<Secret>
    {
        /// <summary>
        /// Determines whether the specified objects are equal.
        /// <para/>
        /// Returns `<see langword="true"/>` if the values are equal; otherwise `<see langword="false"/>`.
        /// </summary>
        public bool Equals(Secret lhs, Secret rhs)
        {
            if (lhs is null && rhs is null)
                return true;
            if (lhs is null || rhs is null)
                return false;

            return StringComparer.Ordinal.Equals(lhs.Value, rhs.Value);
        }

        /// <summary>
        /// Returns a hash code for the specified value.
        /// </summary>
        public int GetHashCode(Secret value)
        {
            if (value is null || value.Value is null)
                return 0;

            return StringComparer.Ordinal.GetHashCode(value.Value);
        }
    }

    public class CredentialComparer : SecretComparer, IEqualityComparer<Credential>
    {
        /// <summary>
        /// Determines whether the specified objects are equal.
        /// <para/>
        /// Returns `<see langword="true"/>` if the values are equal; otherwise `<see langword="false"/>`.
        /// </summary>
        public bool Equals(Credential lhs, Credential rhs)
        {
            if (lhs is null && rhs is null)
                return true;
            if (lhs is null || rhs is null)
                return false;

            return StringComparer.Ordinal.Equals(lhs.Password, rhs.Password)
                && StringComparer.OrdinalIgnoreCase.Equals(lhs.Username, rhs.Username);
        }

        /// <summary>
        /// Returns a hash code for the specified value.
        /// </summary>
        public int GetHashCode(Credential value)
        {
            if (value is null)
                return 0;

            int hash = 0;

            if (value.Username != null)
            {
                hash |= (int)(StringComparer.OrdinalIgnoreCase.GetHashCode(value.Username) & 0xFFFF0000);
            }

            if (value.Password != null)
            {
                hash |= (StringComparer.Ordinal.GetHashCode(value.Password) & 0x0000FFFF);
            }

            return hash;
        }
    }

    public class TokenComparer : SecretComparer, IEqualityComparer<Token>
    {
        /// <summary>
        /// Determines whether the specified objects are equal.
        /// <para/>
        /// Returns `<see langword="true"/>` if the values are equal; otherwise `<see langword="false"/>`.
        /// </summary>
        public bool Equals(Token lhs, Token rhs)
        {
            if (lhs is null && rhs is null)
                return true;
            if (lhs is null || rhs is null)
                return false;

            return lhs.Type == rhs.Type
                && StringComparer.Ordinal.Equals(lhs.Value, rhs.Value);
        }

        /// <summary>
        /// Returns a hash code for the specified value.
        /// </summary>
        public int GetHashCode(Token value)
        {
            if (value is null)
                return 0;

            int hash = ((int)value.Type & 0x000000FF);

            if (value.Value != null)
            {
                hash |= (int)(StringComparer.Ordinal.GetHashCode(value.Value) & 0xFFFFFF00);
            }

            return hash;
        }
    }
}
