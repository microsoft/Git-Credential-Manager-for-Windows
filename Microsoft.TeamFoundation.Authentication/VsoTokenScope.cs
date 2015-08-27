using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using ScopeSet = System.Collections.Generic.HashSet<string>;

namespace Microsoft.TeamFoundation.Authentication
{
    public class VsoTokenScope : TokenScope
    {
        public static readonly VsoTokenScope None = new VsoTokenScope(String.Empty);
        /// <summary>
        /// Grants the ability to access build artifacts, including build results, definitions, and 
        /// requests, and the ability to receive notifications about build events via service hooks.
        /// </summary>
        public static readonly VsoTokenScope BuildAccess = new VsoTokenScope("vso.build");
        /// <summary>
        /// Grants the ability to access build artifacts, including build results, definitions, and 
        /// requests, and the ability to queue a build, update build properties, and the ability to 
        /// receive notifications about build events via service hooks.
        /// </summary>
        public static readonly VsoTokenScope BuildExecute = new VsoTokenScope("vso.build_execute");
        /// <summary>
        /// Grants the ability to access rooms and view, post, and update messages. Also grants the 
        /// ability to manage rooms and users and to receive notifications about new messages via 
        /// service hooks.
        /// </summary>
        public static readonly VsoTokenScope ChatManage = new VsoTokenScope("vso.chat_manage");
        /// <summary>
        /// Grants the ability to access rooms and view, post, and update messages. Also grants the 
        /// ability to receive notifications about new messages via service hooks.
        /// </summary>
        public static readonly VsoTokenScope ChatWrite = new VsoTokenScope("vso.chat_write");
        /// <summary>
        /// Grants the ability to read, update, and delete source code, access metadata about 
        /// commits, changesets, branches, and other version control artifacts. Also grants the 
        /// ability to create and manage code repositories, create and manage pull requests and 
        /// code reviews, and to receive notifications about version control events via service 
        /// hooks.
        /// </summary>
        public static readonly VsoTokenScope CodeManage = new VsoTokenScope("vso.code_manage");
        /// <summary>
        /// Grants the ability to read source code and metadata about commits, changesets, branches, 
        /// and other version control artifacts. Also grants the ability to get notified about 
        /// version control events via service hooks.
        /// </summary>
        public static readonly VsoTokenScope CodeRead = new VsoTokenScope("vso.code");
        /// <summary>
        /// Grants the ability to read, update, and delete source code, access metadata about 
        /// commits, changesets, branches, and other version control artifacts. Also grants the 
        /// ability to create and manage pull requests and code reviews and to receive 
        /// notifications about version control events via service hooks.
        /// </summary>
        public static readonly VsoTokenScope CodeWrite = new VsoTokenScope("vso.code_write");
        /// <summary>
        /// Grants the ability to read, write, and delete feeds and packages.
        /// </summary>
        public static readonly VsoTokenScope PackagingManage = new VsoTokenScope("vso.packaging_manage");
        /// <summary>
        /// Grants the ability to list feeds and read packages in those feeds.
        /// </summary>
        public static readonly VsoTokenScope PackagingRead = new VsoTokenScope("vso.packaging");
        /// <summary>
        /// Grants the ability to list feeds and read, write, and delete packages in those feeds.
        /// </summary>
        public static readonly VsoTokenScope PackagingWrite = new VsoTokenScope("vso.packaging_write");
        /// <summary>
        /// Grants the ability to read your profile, accounts, collections, projects, teams, and 
        /// other top-level organizational artifacts.
        /// </summary>
        public static readonly VsoTokenScope ProfileRead = new VsoTokenScope("vso.profile");
        /// <summary>
        /// Grants the ability to read service hook subscriptions and metadata, including supported
        /// events, consumers, and actions.
        /// </summary>
        public static readonly VsoTokenScope ServiceHookRead = new VsoTokenScope("vso.hooks");
        /// <summary>
        /// Grants the ability to create and update service hook subscriptions and read metadata, 
        /// including supported events, consumers, and actions."
        /// </summary>
        public static readonly VsoTokenScope ServiceHookWrite = new VsoTokenScope("vso.hooks_write");
        /// <summary>
        /// Grants the ability to read test plans, cases, results and other test management related 
        /// artifacts.
        /// </summary>
        public static readonly VsoTokenScope TestRead = new VsoTokenScope("vso.test");
        /// <summary>
        /// Grants the ability to read, create, and update test plans, cases, results and other 
        /// test management related artifacts.
        /// </summary>
        public static readonly VsoTokenScope TestWrite = new VsoTokenScope("vso.test_write");
        /// <summary>
        /// Grants the ability to read work items, queries, boards, area and iterations paths, and 
        /// other work item tracking related metadata. Also grants the ability to execute queries 
        /// and to receive notifications about work item events via service hooks.
        /// </summary>
        public static readonly VsoTokenScope WorkRead = new VsoTokenScope("vso.work");
        /// <summary>
        /// Grants the ability to read, create, and update work items and queries, update board 
        /// metadata, read area and iterations paths other work item tracking related metadata, 
        /// execute queries, and to receive notifications about work item events via service hooks.
        /// </summary>
        public static readonly VsoTokenScope WorkWrite = new VsoTokenScope("vso.work_write");

        private VsoTokenScope(string value)
            : base(value)
        { }

        private VsoTokenScope(string[] values)
            : base(values)
        { }

        private VsoTokenScope(ScopeSet set)
            : base(set)
        { }

        public static IEnumerable<VsoTokenScope> EnumerateValues()
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
        public static VsoTokenScope operator +(VsoTokenScope scope1, VsoTokenScope scope2)
        {
            ScopeSet set = new ScopeSet();
            set.UnionWith(scope1._scopes);
            set.UnionWith(scope2._scopes);

            return new VsoTokenScope(set);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static VsoTokenScope operator -(VsoTokenScope scope1, VsoTokenScope scope2)
        {
            ScopeSet set = new ScopeSet();
            set.UnionWith(scope1._scopes);
            set.ExceptWith(scope2._scopes);

            return new VsoTokenScope(set);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static VsoTokenScope operator |(VsoTokenScope scope1, VsoTokenScope scope2)
        {
            ScopeSet set = new ScopeSet();
            set.UnionWith(scope1._scopes);
            set.UnionWith(scope2._scopes);

            return new VsoTokenScope(set);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static VsoTokenScope operator &(VsoTokenScope scope1, VsoTokenScope scope2)
        {
            ScopeSet set = new ScopeSet();
            set.UnionWith(scope1._scopes);
            set.IntersectWith(scope2._scopes);

            return new VsoTokenScope(set);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static VsoTokenScope operator ^(VsoTokenScope scope1, VsoTokenScope scope2)
        {
            ScopeSet set = new ScopeSet();
            set.UnionWith(scope1._scopes);
            set.SymmetricExceptWith(scope2._scopes);

            return new VsoTokenScope(set);
        }
    }
}
