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

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using ScopeSet = System.Collections.Generic.HashSet<string>;

namespace Microsoft.Alm.Authentication
{
    public class VstsTokenScope : TokenScope
    {
        public static readonly VstsTokenScope None = new VstsTokenScope(string.Empty);

        /// <summary>
        /// Grants the ability to access build artifacts, including build results, definitions, and
        /// requests, and the ability to receive notifications about build events via service hooks.
        /// </summary>
        public static readonly VstsTokenScope BuildAccess = new VstsTokenScope("vso.build");

        /// <summary>
        /// Grants the ability to access build artifacts, including build results, definitions, and
        /// requests, and the ability to queue a build, update build properties, and the ability to
        /// receive notifications about build events via service hooks.
        /// </summary>
        public static readonly VstsTokenScope BuildExecute = new VstsTokenScope("vso.build_execute");

        /// <summary>
        /// Grants the ability to access rooms and view, post, and update messages. Also grants the
        /// ability to manage rooms and users and to receive notifications about new messages via
        /// service hooks.
        /// </summary>
        public static readonly VstsTokenScope ChatManage = new VstsTokenScope("vso.chat_manage");

        /// <summary>
        /// Grants the ability to access rooms and view, post, and update messages. Also grants the
        /// ability to receive notifications about new messages via service hooks.
        /// </summary>
        public static readonly VstsTokenScope ChatWrite = new VstsTokenScope("vso.chat_write");

        /// <summary>
        /// Grants the ability to read, update, and delete source code, access metadata about
        /// commits, changesets, branches, and other version control artifacts. Also grants the
        /// ability to create and manage code repositories, create and manage pull requests and code
        /// reviews, and to receive notifications about version control events via service hooks.
        /// </summary>
        public static readonly VstsTokenScope CodeManage = new VstsTokenScope("vso.code_manage");

        /// <summary>
        /// Grants the ability to read source code and metadata about commits, changesets, branches,
        /// and other version control artifacts. Also grants the ability to get notified about
        /// version control events via service hooks.
        /// </summary>
        public static readonly VstsTokenScope CodeRead = new VstsTokenScope("vso.code");

        /// <summary>
        /// Grants the ability to read, update, and delete source code, access metadata about
        /// commits, changesets, branches, and other version control artifacts. Also grants the
        /// ability to create and manage pull requests and code reviews and to receive notifications
        /// about version control events via service hooks.
        /// </summary>
        public static readonly VstsTokenScope CodeWrite = new VstsTokenScope("vso.code_write");

        /// <summary>
        /// Grants the ability to read, write, and delete feeds and packages.
        /// </summary>
        public static readonly VstsTokenScope PackagingManage = new VstsTokenScope("vso.packaging_manage");

        /// <summary>
        /// Grants the ability to list feeds and read packages in those feeds.
        /// </summary>
        public static readonly VstsTokenScope PackagingRead = new VstsTokenScope("vso.packaging");

        /// <summary>
        /// Grants the ability to list feeds and read, write, and delete packages in those feeds.
        /// </summary>
        public static readonly VstsTokenScope PackagingWrite = new VstsTokenScope("vso.packaging_write");

        /// <summary>
        /// Grants the ability to read your profile, accounts, collections, projects, teams, and
        /// other top-level organizational artifacts.
        /// </summary>
        public static readonly VstsTokenScope ProfileRead = new VstsTokenScope("vso.profile");

        /// <summary>
        /// Grants the ability to read service hook subscriptions and metadata, including supported
        /// events, consumers, and actions.
        /// </summary>
        public static readonly VstsTokenScope ServiceHookRead = new VstsTokenScope("vso.hooks");

        /// <summary>
        /// Grants the ability to create and update service hook subscriptions and read metadata,
        /// including supported events, consumers, and actions."
        /// </summary>
        public static readonly VstsTokenScope ServiceHookWrite = new VstsTokenScope("vso.hooks_write");

        /// <summary>
        /// Grants the ability to read test plans, cases, results and other test management related artifacts.
        /// </summary>
        public static readonly VstsTokenScope TestRead = new VstsTokenScope("vso.test");

        /// <summary>
        /// Grants the ability to read, create, and update test plans, cases, results and other test
        /// management related artifacts.
        /// </summary>
        public static readonly VstsTokenScope TestWrite = new VstsTokenScope("vso.test_write");

        /// <summary>
        /// Grants the ability to read work items, queries, boards, area and iterations paths, and
        /// other work item tracking related metadata. Also grants the ability to execute queries and
        /// to receive notifications about work item events via service hooks.
        /// </summary>
        public static readonly VstsTokenScope WorkRead = new VstsTokenScope("vso.work");

        /// <summary>
        /// Grants the ability to read, create, and update work items and queries, update board
        /// metadata, read area and iterations paths other work item tracking related metadata,
        /// execute queries, and to receive notifications about work item events via service hooks.
        /// </summary>
        public static readonly VstsTokenScope WorkWrite = new VstsTokenScope("vso.work_write");

        private VstsTokenScope(string value)
            : base(value)
        { }

        private VstsTokenScope(ScopeSet set)
            : base(set)
        { }

        public static IEnumerable<VstsTokenScope> EnumerateValues()
        {
            yield return BuildAccess;
            yield return BuildExecute;
            yield return ChatManage;
            yield return ChatWrite;
            yield return CodeManage;
            yield return CodeRead;
            yield return CodeWrite;
            yield return PackagingManage;
            yield return PackagingRead;
            yield return PackagingWrite;
            yield return ProfileRead;
            yield return ServiceHookRead;
            yield return ServiceHookWrite;
            yield return TestRead;
            yield return TestWrite;
            yield return WorkRead;
            yield return WorkWrite;
        }

        public override bool Equals(object obj)
            => TokenScope.Equals(this as TokenScope, obj);

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
        public bool Equals(VstsTokenScope other)
            => TokenScope.Equals(this as TokenScope, other as TokenScope);

        public override int GetHashCode()
            => TokenScope.GetHashCode(this as TokenScope);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(VstsTokenScope left, VstsTokenScope right)
            => TokenScope.Equals(left as TokenScope, right as TokenScope);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(VstsTokenScope left, VstsTokenScope right)
            => !TokenScope.Equals(left as TokenScope, right as TokenScope);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static VstsTokenScope operator +(VstsTokenScope left, VstsTokenScope right)
        {
            var set = TokenScope.UnionWith(left as TokenScope, right as TokenScope);
            return new VstsTokenScope(set);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static VstsTokenScope operator -(VstsTokenScope left, VstsTokenScope right)
        {
            var set = TokenScope.ExceptWith(left as TokenScope, right as TokenScope);
            return new VstsTokenScope(set);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static VstsTokenScope operator |(VstsTokenScope left, VstsTokenScope right)
        {
            var set = TokenScope.UnionWith(left as TokenScope, right as TokenScope);
            return new VstsTokenScope(set);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static VstsTokenScope operator &(VstsTokenScope left, VstsTokenScope right)
        {
            var set = TokenScope.IntersectWith(left as TokenScope, right as TokenScope);
            return new VstsTokenScope(set);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static VstsTokenScope operator ^(VstsTokenScope left, VstsTokenScope right)
        {
            var set = TokenScope.SymmetricExceptWith(left as TokenScope, right as TokenScope);
            return new VstsTokenScope(set);
        }
    }
}
