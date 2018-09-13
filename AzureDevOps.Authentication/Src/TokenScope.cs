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
using System.Linq;
using System.Runtime.CompilerServices;
using ScopeSet = System.Collections.Generic.HashSet<string>;

namespace AzureDevOps.Authentication
{
    public class TokenScope : Microsoft.Alm.Authentication.TokenScope
    {
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly TokenScope None = new TokenScope(string.Empty);

        /// <summary>
        /// Grants the ability to access build artifacts, including build results, definitions, and
        /// requests, and the ability to receive notifications about build events via service hooks.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly TokenScope BuildAccess = new TokenScope("vso.build");

        /// <summary>
        /// Grants the ability to access build artifacts, including build results, definitions, and
        /// requests, and the ability to queue a build, update build properties, and the ability to
        /// receive notifications about build events via service hooks.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly TokenScope BuildExecute = new TokenScope("vso.build_execute");

        /// <summary>
        /// Grants the ability to access rooms and view, post, and update messages. Also grants the
        /// ability to manage rooms and users and to receive notifications about new messages via
        /// service hooks.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly TokenScope ChatManage = new TokenScope("vso.chat_manage");

        /// <summary>
        /// Grants the ability to access rooms and view, post, and update messages. Also grants the
        /// ability to receive notifications about new messages via service hooks.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly TokenScope ChatWrite = new TokenScope("vso.chat_write");

        /// <summary>
        /// Grants the ability to read, update, and delete source code, access metadata about
        /// commits, change-sets, branches, and other version control artifacts. Also grants the
        /// ability to create and manage code repositories, create and manage pull requests and code
        /// reviews, and to receive notifications about version control events via service hooks.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly TokenScope CodeManage = new TokenScope("vso.code_manage");

        /// <summary>
        /// Grants the ability to read source code and metadata about commits, change-sets, branches,
        /// and other version control artifacts. Also grants the ability to get notified about
        /// version control events via service hooks.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly TokenScope CodeRead = new TokenScope("vso.code");

        /// <summary>
        /// Grants the ability to read, update, and delete source code, access metadata about
        /// commits, change-sets, branches, and other version control artifacts. Also grants the
        /// ability to create and manage pull requests and code reviews and to receive notifications
        /// about version control events via service hooks.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly TokenScope CodeWrite = new TokenScope("vso.code_write");

        /// <summary>
        /// Grants the ability to read, write, and delete feeds and packages.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly TokenScope PackagingManage = new TokenScope("vso.packaging_manage");

        /// <summary>
        /// Grants the ability to list feeds and read packages in those feeds.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly TokenScope PackagingRead = new TokenScope("vso.packaging");

        /// <summary>
        /// Grants the ability to list feeds and read, write, and delete packages in those feeds.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly TokenScope PackagingWrite = new TokenScope("vso.packaging_write");

        /// <summary>
        /// Grants the ability to read your profile, accounts, collections, projects, teams, and
        /// other top-level organizational artifacts.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly TokenScope ProfileRead = new TokenScope("vso.profile");

        /// <summary>
        /// Grants the ability to read release artifacts, including releases, release definitions
        /// and release environment.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly TokenScope ReleaseAccess = new TokenScope("vso.release");

        /// <summary>
        /// Grants the ability to read and update release artifacts, including releases, release
        /// definitions and release envrionment, and the ability to queue a new release.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly TokenScope ReleaseExecute = new TokenScope("vso.release_execute");

        /// <summary>
        /// Grants the ability to read, update and delete release artifacts, including releases,
        /// release definitions and release envrionment, and the ability to queue and approve a new release.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly TokenScope ReleaseManage = new TokenScope("vso.release_manage");

        /// <summary>
        /// Grants the ability to read service hook subscriptions and metadata, including supported
        /// events, consumers, and actions.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly TokenScope ServiceHookRead = new TokenScope("vso.hooks");

        /// <summary>
        /// Grants the ability to create and update service hook subscriptions and read metadata,
        /// including supported events, consumers, and actions."
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly TokenScope ServiceHookWrite = new TokenScope("vso.hooks_write");

        /// <summary>
        /// Grants the ability to read test plans, cases, results and other test management related artifacts.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly TokenScope TestRead = new TokenScope("vso.test");

        /// <summary>
        /// Grants the ability to read, create, and update test plans, cases, results and other test
        /// management related artifacts.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly TokenScope TestWrite = new TokenScope("vso.test_write");

        /// <summary>
        /// Grants the ability to read work items, queries, boards, area and iterations paths, and
        /// other work item tracking related metadata. Also grants the ability to execute queries and
        /// to receive notifications about work item events via service hooks.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly TokenScope WorkRead = new TokenScope("vso.work");

        /// <summary>
        /// Grants the ability to read, create, and update work items and queries, update board
        /// metadata, read area and iterations paths other work item tracking related metadata,
        /// execute queries, and to receive notifications about work item events via service hooks.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        public static readonly TokenScope WorkWrite = new TokenScope("vso.work_write");

        private TokenScope(string value)
            : base(value)
        { }

        private TokenScope(ScopeSet set)
            : base(set)
        { }

        public static IEnumerable<TokenScope> EnumerateValues()
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
            => Microsoft.Alm.Authentication.TokenScope.Equals(this as Microsoft.Alm.Authentication.TokenScope, obj);

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1011:ConsiderPassingBaseTypesAsParameters")]
        public bool Equals(TokenScope other)
            => Microsoft.Alm.Authentication.TokenScope.Equals(this as Microsoft.Alm.Authentication.TokenScope, other as Microsoft.Alm.Authentication.TokenScope);

        public static bool Find(string value, out TokenScope devopsScope)
        {
            devopsScope = EnumerateValues().FirstOrDefault(v => StringComparer.OrdinalIgnoreCase.Equals(v.Value, value));

            return devopsScope != null;
        }

        public override int GetHashCode()
            => Microsoft.Alm.Authentication.TokenScope.GetHashCode(this as Microsoft.Alm.Authentication.TokenScope);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(TokenScope left, TokenScope right)
            => Microsoft.Alm.Authentication.TokenScope.Equals(left as Microsoft.Alm.Authentication.TokenScope, right as Microsoft.Alm.Authentication.TokenScope);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(TokenScope left, TokenScope right)
            => !Microsoft.Alm.Authentication.TokenScope.Equals(left as Microsoft.Alm.Authentication.TokenScope, right as Microsoft.Alm.Authentication.TokenScope);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TokenScope operator +(TokenScope left, TokenScope right)
        {
            var set = Microsoft.Alm.Authentication.TokenScope.UnionWith(left as Microsoft.Alm.Authentication.TokenScope, right as Microsoft.Alm.Authentication.TokenScope);
            return new TokenScope(set);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TokenScope operator -(TokenScope left, TokenScope right)
        {
            var set = Microsoft.Alm.Authentication.TokenScope.ExceptWith(left as Microsoft.Alm.Authentication.TokenScope, right as Microsoft.Alm.Authentication.TokenScope);
            return new TokenScope(set);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TokenScope operator |(TokenScope left, TokenScope right)
        {
            var set = Microsoft.Alm.Authentication.TokenScope.UnionWith(left as Microsoft.Alm.Authentication.TokenScope, right as Microsoft.Alm.Authentication.TokenScope);
            return new TokenScope(set);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TokenScope operator &(TokenScope left, TokenScope right)
        {
            var set = Microsoft.Alm.Authentication.TokenScope.IntersectWith(left as Microsoft.Alm.Authentication.TokenScope, right as Microsoft.Alm.Authentication.TokenScope);
            return new TokenScope(set);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TokenScope operator ^(TokenScope left, TokenScope right)
        {
            var set = Microsoft.Alm.Authentication.TokenScope.SymmetricExceptWith(left as Microsoft.Alm.Authentication.TokenScope, right as Microsoft.Alm.Authentication.TokenScope);
            return new TokenScope(set);
        }
    }
}
