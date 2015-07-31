using System;
using System.Runtime.CompilerServices;
using ScopeSet = System.Collections.Generic.HashSet<string>;

namespace Microsoft.TeamFoundation.Authentication
{
    public class VsoTokenScope : IEquatable<VsoTokenScope>
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

        private VsoTokenScope(string[] values)
        {
            _scopes = values;
        }

        private VsoTokenScope(ScopeSet set)
        {
            string[] result = new string[set.Count];
            set.CopyTo(result);

            _scopes = result;
        }

        public string Value { get { return String.Join(" ", _scopes); } }

        private readonly string[] _scopes;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object obj)
        {
            return this == obj as VsoTokenScope;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(VsoTokenScope other)
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(VsoTokenScope scope1, VsoTokenScope scope2)
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
        public static bool operator !=(VsoTokenScope scope1, VsoTokenScope scope2)
        {
            return !(scope1 == scope2);
        }
    }
}
