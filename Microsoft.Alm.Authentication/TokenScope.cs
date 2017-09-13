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
using System.Runtime.CompilerServices;
using ScopeSet = System.Collections.Generic.HashSet<string>;

namespace Microsoft.Alm.Authentication
{
    public abstract class TokenScope : IEquatable<TokenScope>
    {
        protected TokenScope(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                _scopes = new string[0];
            }
            else
            {
                var scopes = new string[1];
                scopes[0] = value;

                _scopes = scopes;
            }
        }

        protected TokenScope(string[] values)
        {
            _scopes = values;
        }

        protected TokenScope(ScopeSet set)
        {
            if (ReferenceEquals(set, null))
                throw new ArgumentNullException(nameof(set));

            string[] result = new string[set.Count];
            set.CopyTo(result);

            _scopes = result;
        }

        public string Value { get { return string.Join(" ", _scopes); } }

        protected readonly IReadOnlyList<string> _scopes;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static bool Equals(TokenScope left, TokenScope right)
        {
            if (ReferenceEquals(left, right))
                return true;
            if (ReferenceEquals(left, null) || ReferenceEquals(null, right))
                return false;

            ScopeSet set = new ScopeSet();
            set.UnionWith(left._scopes);
            return set.SetEquals(right._scopes);
        }

        protected static bool Equals(TokenScope left, object right)
            => Equals(left, right as TokenScope);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(TokenScope other)
            => TokenScope.Equals(this, other);

        public override bool Equals(object obj)
            => TokenScope.Equals(this, obj as TokenScope);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static ScopeSet ExceptWith(TokenScope left, TokenScope right)
        {
            if (ReferenceEquals(left, null))
                throw new ArgumentNullException(nameof(left));
            if (ReferenceEquals(right, null))
                throw new ArgumentNullException(nameof(right));

            ScopeSet set = new ScopeSet();
            set.UnionWith(left._scopes);
            set.ExceptWith(right._scopes);

            return set;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static int GetHashCode(TokenScope value)
        {
            if (ReferenceEquals(value, null))
                return 0;

            // largest 31-bit prime (https://msdn.microsoft.com/en-us/library/Ee621251.aspx)
            int hash = 2147483647;

            for (int i = 0; i < value._scopes.Count; i++)
            {
                unchecked
                {
                    hash ^= value._scopes[i].GetHashCode();
                }
            }

            return hash;
        }

        public override int GetHashCode()
            => GetHashCode(this);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static ScopeSet IntersectWith(TokenScope left, TokenScope right)
        {
            if (ReferenceEquals(left, null))
                throw new ArgumentNullException(nameof(left));
            if (ReferenceEquals(right, null))
                throw new ArgumentNullException(nameof(right));

            ScopeSet set = new ScopeSet();
            set.UnionWith(left._scopes);
            set.IntersectWith(right._scopes);

            return set;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static ScopeSet SymmetricExceptWith(TokenScope left, TokenScope right)
        {
            if (ReferenceEquals(left, null))
                throw new ArgumentNullException(nameof(left));
            if (ReferenceEquals(right, null))
                throw new ArgumentNullException(nameof(right));

            ScopeSet set = new ScopeSet();
            set.UnionWith(left._scopes);
            set.SymmetricExceptWith(right._scopes);

            return set;
        }

        public override string ToString()
        {
            return Value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected static ScopeSet UnionWith(TokenScope left, TokenScope right)
        {
            if (ReferenceEquals(left, null))
                throw new ArgumentNullException(nameof(left));
            if (ReferenceEquals(right, null))
                throw new ArgumentNullException(nameof(right));

            ScopeSet set = new ScopeSet();
            set.UnionWith(left._scopes);
            set.UnionWith(right._scopes);

            return set;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(TokenScope left, TokenScope right)
            => TokenScope.Equals(left, right);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(TokenScope left, TokenScope right)
            => !TokenScope.Equals(left, right);
    }
}
