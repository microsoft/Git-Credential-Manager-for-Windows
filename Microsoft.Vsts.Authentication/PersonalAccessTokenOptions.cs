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
    [DebuggerDisplay("{DebuggerDisplay, nq}")]
    public struct PersonalAccessTokenOptions
    {
        private bool _compact;
        private TimeSpan? _duration;
        private VstsTokenScope _scope;

        /// <summary>
        /// <para>
        /// Requests a compact format personal access token; otherwise requests a standard personal
        /// access token.
        /// </para>
        /// <para>
        /// Compact tokens are necessary for clients which have restrictions on the size of the basic
        /// authentication header which they can create (example: Git).
        /// </para>
        /// </summary>
        public bool RequireCompactToken
        {
            get { return _compact; }
            set
            {
                this = new PersonalAccessTokenOptions()
                {
                    _duration = _duration,
                    _compact = value,
                    _scope = _scope
                };
            }
        }

        /// <summary>
        /// <para>
        /// Requests a limited duration personal access token when specified; otherwise the default
        /// duration is requested.
        /// </para>
        /// <para>Cannot be less than one hour; values less than one hour (1hr) are ignored.</para>
        /// </summary>
        public TimeSpan? TokenDuration
        {
            get { return _duration; }
            set
            {
                this = new PersonalAccessTokenOptions()
                {
                    _duration = value,
                    _compact = _compact,
                    _scope = _scope
                };
            }
        }

        /// <summary>
        /// <para>
        /// Requests a limited scope personal access token; otherwise the default scope is requested.
        /// </para>
        /// </summary>
        public VstsTokenScope TokenScope
        {
            get { return _scope; }
            set
            {
                this = new PersonalAccessTokenOptions()
                {
                    _duration = _duration,
                    _compact = _compact,
                    _scope = value
                };
            }
        }

        private string DebuggerDisplay
        {
            get { return $"{(_compact ? "Compact" : "Normal")} {((_scope == null) ? "Default" : _scope.Value)} [{(_duration.HasValue ? _duration.Value.ToString("u") : "Default")}]"; }
        }

        public override bool Equals(object obj)
        {
            return false;
        }

        public override int GetHashCode()
        {
            return StringComparer.Ordinal.GetHashCode(_scope.Value);
        }

        public override string ToString()
        {
            return typeof(PersonalAccessTokenOptions).Name;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "right")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "left")]
        public static bool operator ==(PersonalAccessTokenOptions left, PersonalAccessTokenOptions right)
            => false;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "right")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA1801:ReviewUnusedParameters", MessageId = "left")]
        public static bool operator !=(PersonalAccessTokenOptions left, PersonalAccessTokenOptions right)
            => false;
    }
}
