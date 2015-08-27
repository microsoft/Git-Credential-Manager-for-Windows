using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using ScopeSet = System.Collections.Generic.HashSet<string>;

namespace Microsoft.TeamFoundation.Authentication
{
    public sealed class GithubTokenScope : TokenScope
    {
        public static readonly GithubTokenScope None = new GithubTokenScope(String.Empty);
        /// <summary>
        /// Create gists
        /// </summary>
        public static readonly GithubTokenScope Gist = new GithubTokenScope("gist");
        /// <summary>
        /// Access notifications
        /// </summary>
        public static readonly GithubTokenScope Notifications = new GithubTokenScope("notifications");
        /// <summary>
        /// Full control of orgs and teams
        /// </summary>
        public static readonly GithubTokenScope OrgAdmin = new GithubTokenScope("admin:org");
        /// <summary>
        /// Read org and team membership
        /// </summary>
        public static readonly GithubTokenScope OrgRead = new GithubTokenScope("read:org");
        /// <summary>
        /// Read and write org and team membership
        /// </summary>
        public static readonly GithubTokenScope OrgWrite = new GithubTokenScope("write:org");
        /// <summary>
        /// Full control of organization hooks
        /// </summary>
        public static readonly GithubTokenScope OrgHookAdmin = new GithubTokenScope("admin:org_hook");
        /// <summary>
        /// Full control of user's public keys
        /// </summary>
        public static readonly GithubTokenScope PublicKeyAdmin = new GithubTokenScope("admin:public_key");
        /// <summary>
        /// Read user's public keys
        /// </summary>
        public static readonly GithubTokenScope PublicKeyRead = new GithubTokenScope("read:public_key");
        /// <summary>
        /// Write user's public keys
        /// </summary>
        public static readonly GithubTokenScope PublicKeyWrite = new GithubTokenScope("write:public_key");
        /// <summary>
        /// Access private repositories
        /// </summary>
        public static readonly GithubTokenScope Repo = new GithubTokenScope("repo");
        /// <summary>
        /// Delete repositories
        /// </summary>
        public static readonly GithubTokenScope RepoDelete = new GithubTokenScope("delete_repo");
        /// <summary>
        /// Access deployment status
        /// </summary>
        public static readonly GithubTokenScope RepoDeployment = new GithubTokenScope("repo_deployment");
        /// <summary>
        /// Access public repositories
        /// </summary>
        public static readonly GithubTokenScope RepoPublic = new GithubTokenScope("public_repo");
        /// <summary>
        /// Access commit status
        /// </summary>
        public static readonly GithubTokenScope RepoStatus = new GithubTokenScope("repo:status");
        /// <summary>
        /// Full control of repository hooks
        /// </summary>
        public static readonly GithubTokenScope RepoHookAdmin = new GithubTokenScope("admin:repo_hook");
        /// <summary>
        /// Read repository hooks
        /// </summary>
        public static readonly GithubTokenScope RepoHookRead = new GithubTokenScope("read:repo_hook");
        /// <summary>
        /// Write repository hooks
        /// </summary>
        public static readonly GithubTokenScope RepoHookWrite = new GithubTokenScope("write:repo_hook");
        /// <summary>
        /// Update all user information
        /// </summary>
        public static readonly GithubTokenScope User = new GithubTokenScope("user");
        /// <summary>
        /// Access user email address (read-only)
        /// </summary>
        public static readonly GithubTokenScope UserEmail = new GithubTokenScope("user:email");
        /// <summary>
        /// Follow and unfollow users
        /// </summary>
        public static readonly GithubTokenScope UserFollow = new GithubTokenScope("user:follow");

        private GithubTokenScope(string value)
            : base(value)
        { }

        private GithubTokenScope(string[] values)
            : base(values)
        { }

        private GithubTokenScope(ScopeSet set)
            : base(set)
        { }

        public static IEnumerable<GithubTokenScope> EnumerateValues()
        {
            yield return Gist;
            yield return Notifications;
            yield return OrgAdmin;
            yield return OrgRead;
            yield return OrgWrite;
            yield return OrgHookAdmin;
            yield return PublicKeyAdmin;
            yield return PublicKeyRead;
            yield return PublicKeyWrite;
            yield return Repo;
            yield return RepoDelete;
            yield return RepoDeployment;
            yield return RepoPublic;
            yield return RepoStatus;
            yield return RepoHookAdmin;
            yield return RepoHookRead;
            yield return RepoHookWrite;
            yield return User;
            yield return UserEmail;
            yield return UserFollow;
            yield break;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GithubTokenScope operator +(GithubTokenScope scope1, GithubTokenScope scope2)
        {
            ScopeSet set = new ScopeSet();
            set.UnionWith(scope1._scopes);
            set.UnionWith(scope2._scopes);

            return new GithubTokenScope(set);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GithubTokenScope operator -(GithubTokenScope scope1, GithubTokenScope scope2)
        {
            ScopeSet set = new ScopeSet();
            set.UnionWith(scope1._scopes);
            set.ExceptWith(scope2._scopes);

            return new GithubTokenScope(set);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GithubTokenScope operator |(GithubTokenScope scope1, GithubTokenScope scope2)
        {
            ScopeSet set = new ScopeSet();
            set.UnionWith(scope1._scopes);
            set.UnionWith(scope2._scopes);

            return new GithubTokenScope(set);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GithubTokenScope operator &(GithubTokenScope scope1, GithubTokenScope scope2)
        {
            ScopeSet set = new ScopeSet();
            set.UnionWith(scope1._scopes);
            set.IntersectWith(scope2._scopes);

            return new GithubTokenScope(set);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GithubTokenScope operator ^(GithubTokenScope scope1, GithubTokenScope scope2)
        {
            ScopeSet set = new ScopeSet();
            set.UnionWith(scope1._scopes);
            set.SymmetricExceptWith(scope2._scopes);

            return new GithubTokenScope(set);
        }
    }
}
