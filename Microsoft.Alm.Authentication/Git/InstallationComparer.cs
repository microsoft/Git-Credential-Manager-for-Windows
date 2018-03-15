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

namespace Microsoft.Alm.Authentication.Git
{
    /// <summary>
    /// Defines methods to support the comparison of `<see cref="Installation"/>` for equality.
    /// </summary>
    public class InstallationComparer : IEqualityComparer<Installation>
    {
        public static readonly StringComparer PathComparer = StringComparer.OrdinalIgnoreCase;

        /// <summary>
        /// Determines whether the specified objects are equal.
        /// <para/>
        /// Returns `<see langword="true"/>` if equal; otherwise `<see langword="false"/>`.
        /// </summary>
        /// <param name="lhs">The first `<see cref="Installation"/>` to compare.</param>
        /// <param name="rhs">The second `<see cref="Installation"/>` to compare.</param>
        public bool Equals(Installation lhs, Installation rhs)
        {
            if (lhs is null && rhs is null)
                return true;
            if (lhs is null || rhs is null)
                return false;

            return lhs.Version == rhs.Version
                && StringComparer.OrdinalIgnoreCase.Equals(lhs.Path, rhs.Path);
        }

        /// <summary>
        /// Returns a hash code for the specified object.
        /// </summary>
        /// <param name="value">The `<see cref="Installation"/>` for which a hash code is to be returned.</param>
        public int GetHashCode(Installation value)
        {
            if (value is null)
                return 0;

            return (((int)value.Version) << 24) 
                 | (StringComparer.OrdinalIgnoreCase.GetHashCode(value.Path) >> 24);
        }            
    }
}
