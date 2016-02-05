using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using ScopeSet = System.Collections.Generic.HashSet<string>;

namespace Microsoft.Alm.Authentication
{
    public class VstsTokenScope : TokenScope
    {
        public static readonly VstsTokenScope None = new VstsTokenScope(String.Empty);
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
        /// ability to create and manage code repositories, create and manage pull requests and 
        /// code reviews, and to receive notifications about version control events via service 
        /// hooks.
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
        /// ability to create and manage pull requests and code reviews and to receive 
        /// notifications about version control events via service hooks.
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
        /// Grants the ability to read test plans, cases, results and other test management related 
        /// artifacts.
        /// </summary>
        public static readonly VstsTokenScope TestRead = new VstsTokenScope("vso.test");
        /// <summary>
        /// Grants the ability to read, create, and update test plans, cases, results and other 
        /// test management related artifacts.
        /// </summary>
        public static readonly VstsTokenScope TestWrite = new VstsTokenScope("vso.test_write");
        /// <summary>
        /// Grants the ability to read work items, queries, boards, area and iterations paths, and 
        /// other work item tracking related metadata. Also grants the ability to execute queries 
        /// and to receive notifications about work item events via service hooks.
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

        private VstsTokenScope(string[] values)
            : base(values)
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static VstsTokenScope operator +(VstsTokenScope scope1, VstsTokenScope scope2)
        {
            ScopeSet set = new ScopeSet();
            set.UnionWith(scope1._scopes);
            set.UnionWith(scope2._scopes);

            return new VstsTokenScope(set);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static VstsTokenScope operator -(VstsTokenScope scope1, VstsTokenScope scope2)
        {
            ScopeSet set = new ScopeSet();
            set.UnionWith(scope1._scopes);
            set.ExceptWith(scope2._scopes);

            return new VstsTokenScope(set);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static VstsTokenScope operator |(VstsTokenScope scope1, VstsTokenScope scope2)
        {
            ScopeSet set = new ScopeSet();
            set.UnionWith(scope1._scopes);
            set.UnionWith(scope2._scopes);

            return new VstsTokenScope(set);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static VstsTokenScope operator &(VstsTokenScope scope1, VstsTokenScope scope2)
        {
            ScopeSet set = new ScopeSet();
            set.UnionWith(scope1._scopes);
            set.IntersectWith(scope2._scopes);

            return new VstsTokenScope(set);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static VstsTokenScope operator ^(VstsTokenScope scope1, VstsTokenScope scope2)
        {
            ScopeSet set = new ScopeSet();
            set.UnionWith(scope1._scopes);
            set.SymmetricExceptWith(scope2._scopes);

            return new VstsTokenScope(set);
        }
    }
}
