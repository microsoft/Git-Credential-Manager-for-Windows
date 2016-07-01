using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using ScopeSet = System.Collections.Generic.HashSet<string>;

namespace Microsoft.Alm.Authentication
{
    public sealed class GitHubTokenScope : TokenScope
    {
        public static readonly GitHubTokenScope None = new GitHubTokenScope(String.Empty);
        /// <summary>
        /// Create gists
        /// </summary>
        public static readonly GitHubTokenScope Gist = new GitHubTokenScope("gist");
        /// <summary>
        /// Access notifications
        /// </summary>
        public static readonly GitHubTokenScope Notifications = new GitHubTokenScope("notifications");
        /// <summary>
        /// Full control of orgs and teams
        /// </summary>
        public static readonly GitHubTokenScope OrgAdmin = new GitHubTokenScope("admin:org");
        /// <summary>
        /// Read org and team membership
        /// </summary>
        public static readonly GitHubTokenScope OrgRead = new GitHubTokenScope("read:org");
        /// <summary>
        /// Read and write org and team membership
        /// </summary>
        public static readonly GitHubTokenScope OrgWrite = new GitHubTokenScope("write:org");
        /// <summary>
        /// Full control of organization hooks
        /// </summary>
        public static readonly GitHubTokenScope OrgHookAdmin = new GitHubTokenScope("admin:org_hook");
        /// <summary>
        /// Full control of user's public keys
        /// </summary>
        public static readonly GitHubTokenScope PublicKeyAdmin = new GitHubTokenScope("admin:public_key");
        /// <summary>
        /// Read user's public keys
        /// </summary>
        public static readonly GitHubTokenScope PublicKeyRead = new GitHubTokenScope("read:public_key");
        /// <summary>
        /// Write user's public keys
        /// </summary>
        public static readonly GitHubTokenScope PublicKeyWrite = new GitHubTokenScope("write:public_key");
        /// <summary>
        /// Access private repositories
        /// </summary>
        public static readonly GitHubTokenScope Repo = new GitHubTokenScope("repo");
        /// <summary>
        /// Delete repositories
        /// </summary>
        public static readonly GitHubTokenScope RepoDelete = new GitHubTokenScope("delete_repo");
        /// <summary>
        /// Access deployment status
        /// </summary>
        public static readonly GitHubTokenScope RepoDeployment = new GitHubTokenScope("repo_deployment");
        /// <summary>
        /// Access public repositories
        /// </summary>
        public static readonly GitHubTokenScope RepoPublic = new GitHubTokenScope("public_repo");
        /// <summary>
        /// Access commit status
        /// </summary>
        public static readonly GitHubTokenScope RepoStatus = new GitHubTokenScope("repo:status");
        /// <summary>
        /// Full control of repository hooks
        /// </summary>
        public static readonly GitHubTokenScope RepoHookAdmin = new GitHubTokenScope("admin:repo_hook");
        /// <summary>
        /// Read repository hooks
        /// </summary>
        public static readonly GitHubTokenScope RepoHookRead = new GitHubTokenScope("read:repo_hook");
        /// <summary>
        /// Write repository hooks
        /// </summary>
        public static readonly GitHubTokenScope RepoHookWrite = new GitHubTokenScope("write:repo_hook");
        /// <summary>
        /// Update all user information
        /// </summary>
        public static readonly GitHubTokenScope User = new GitHubTokenScope("user");
        /// <summary>
        /// Access user email address (read-only)
        /// </summary>
        public static readonly GitHubTokenScope UserEmail = new GitHubTokenScope("user:email");
        /// <summary>
        /// Follow and unfollow users
        /// </summary>
        public static readonly GitHubTokenScope UserFollow = new GitHubTokenScope("user:follow");

        private GitHubTokenScope(string value)
            : base(value)
        { }

        private GitHubTokenScope(string[] values)
            : base(values)
        { }

        private GitHubTokenScope(ScopeSet set)
            : base(set)
        { }

        public static IEnumerable<GitHubTokenScope> EnumerateValues()
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
        public static GitHubTokenScope operator +(GitHubTokenScope scope1, GitHubTokenScope scope2)
        {
            ScopeSet set = new ScopeSet();
            set.UnionWith(scope1._scopes);
            set.UnionWith(scope2._scopes);

            return new GitHubTokenScope(set);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GitHubTokenScope operator -(GitHubTokenScope scope1, GitHubTokenScope scope2)
        {
            ScopeSet set = new ScopeSet();
            set.UnionWith(scope1._scopes);
            set.ExceptWith(scope2._scopes);

            return new GitHubTokenScope(set);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GitHubTokenScope operator |(GitHubTokenScope scope1, GitHubTokenScope scope2)
        {
            ScopeSet set = new ScopeSet();
            set.UnionWith(scope1._scopes);
            set.UnionWith(scope2._scopes);

            return new GitHubTokenScope(set);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GitHubTokenScope operator &(GitHubTokenScope scope1, GitHubTokenScope scope2)
        {
            ScopeSet set = new ScopeSet();
            set.UnionWith(scope1._scopes);
            set.IntersectWith(scope2._scopes);

            return new GitHubTokenScope(set);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static GitHubTokenScope operator ^(GitHubTokenScope scope1, GitHubTokenScope scope2)
        {
            ScopeSet set = new ScopeSet();
            set.UnionWith(scope1._scopes);
            set.SymmetricExceptWith(scope2._scopes);

            return new GitHubTokenScope(set);
        }
    }
}
