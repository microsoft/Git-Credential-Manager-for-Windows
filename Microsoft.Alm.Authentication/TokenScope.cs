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
using System.Runtime.CompilerServices;
using ScopeSet = System.Collections.Generic.HashSet<string>;

namespace Microsoft.Alm.Authentication
{
    public abstract class TokenScope : IEquatable<TokenScope>
    {
        protected TokenScope(string value)
        {
            if (String.IsNullOrWhiteSpace(value))
            {
                _scopes = new string[0];
            }
            else
            {
                _scopes = new string[1];
                _scopes[0] = value;
            }
        }

        protected TokenScope(string[] values)
        {
            _scopes = values;
        }

        protected TokenScope(ScopeSet set)
        {
            string[] result = new string[set.Count];
            set.CopyTo(result);

            _scopes = result;
        }

        public string Value { get { return String.Join(" ", _scopes); } }

        protected readonly string[] _scopes;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object obj)
        {
            return this == obj as TokenScope;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(TokenScope other)
        {
            return this == other;
        }

        public override int GetHashCode()
        {
            // largest 31-bit prime (https://msdn.microsoft.com/en-us/library/Ee621251.aspx)
            int hash = 2147483647;

            for (int i = 0; i < _scopes.Length; i++)
            {
                unchecked
                {
                    hash ^= _scopes[i].GetHashCode();
                }
            }

            return hash;
        }

        public override String ToString()
        {
            return Value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(TokenScope scope1, TokenScope scope2)
        {
            if (ReferenceEquals(scope1, scope2))
                return true;
            if (ReferenceEquals(scope1, null) || ReferenceEquals(null, scope2))
                return false;

            ScopeSet set = new ScopeSet();
            set.UnionWith(scope1._scopes);
            return set.SetEquals(scope2._scopes);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(TokenScope scope1, TokenScope scope2)
        {
            return !(scope1 == scope2);
        }
    }
}
