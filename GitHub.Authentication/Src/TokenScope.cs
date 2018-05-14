/**** Git Credential Manager for Windows ****
 *
 * Copyright (c) Microsoft Corporation
 * Copyright (c) GitHub Corporation
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

namespace GitHub.Authentication
{
    public sealed class TokenScope : Microsoft.Alm.Authentication.TokenScope, IEquatable<TokenScope>
    {
        public static readonly TokenScope None = new TokenScope(string.Empty);

        /// <summary>
        /// Create gists
        /// </summary>
        public static readonly TokenScope Gist = new TokenScope("gist");

        /// <summary>
        /// Access notifications
        /// </summary>
        public static readonly TokenScope Notifications = new TokenScope("notifications");

        /// <summary>
        /// Full control of orgs and teams
        /// </summary>
        public static readonly TokenScope OrgAdmin = new TokenScope("admin:org");

        /// <summary>
        /// Read org and team membership
        /// </summary>
        public static readonly TokenScope OrgRead = new TokenScope("read:org");

        /// <summary>
        /// Read and write org and team membership
        /// </summary>
        public static readonly TokenScope OrgWrite = new TokenScope("write:org");

        /// <summary>
        /// Full control of organization hooks
        /// </summary>
        public static readonly TokenScope OrgHookAdmin = new TokenScope("admin:org_hook");

        /// <summary>
        /// Full control of user's public keys
        /// </summary>
        public static readonly TokenScope PublicKeyAdmin = new TokenScope("admin:public_key");

        /// <summary>
        /// Read user's public keys
        /// </summary>
        public static readonly TokenScope PublicKeyRead = new TokenScope("read:public_key");

        /// <summary>
        /// Write user's public keys
        /// </summary>
        public static readonly TokenScope PublicKeyWrite = new TokenScope("write:public_key");

        /// <summary>
        /// Access private repositories
        /// </summary>
        public static readonly TokenScope Repo = new TokenScope("repo");

        /// <summary>
        /// Delete repositories
        /// </summary>
        public static readonly TokenScope RepoDelete = new TokenScope("delete_repo");

        /// <summary>
        /// Access deployment status
        /// </summary>
        public static readonly TokenScope RepoDeployment = new TokenScope("repo_deployment");

        /// <summary>
        /// Access public repositories
        /// </summary>
        public static readonly TokenScope RepoPublic = new TokenScope("public_repo");

        /// <summary>
        /// Access commit status
        /// </summary>
        public static readonly TokenScope RepoStatus = new TokenScope("repo:status");

        /// <summary>
        /// Full control of repository hooks
        /// </summary>
        public static readonly TokenScope RepoHookAdmin = new TokenScope("admin:repo_hook");

        /// <summary>
        /// Read repository hooks
        /// </summary>
        public static readonly TokenScope RepoHookRead = new TokenScope("read:repo_hook");

        /// <summary>
        /// Write repository hooks
        /// </summary>
        public static readonly TokenScope RepoHookWrite = new TokenScope("write:repo_hook");

        /// <summary>
        /// Update all user information
        /// </summary>
        public static readonly TokenScope User = new TokenScope("user");

        /// <summary>
        /// Access user email address (read-only)
        /// </summary>
        public static readonly TokenScope UserEmail = new TokenScope("user:email");

        /// <summary>
        /// Follow and unfollow users
        /// </summary>
        public static readonly TokenScope UserFollow = new TokenScope("user:follow");

        private TokenScope(string value)
            : base(value)
        { }

        private TokenScope(ScopeSet set)
            : base(set)
        { }

        public static IEnumerable<TokenScope> EnumerateValues()
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

        public override bool Equals(object obj)
            => Equals(this as Microsoft.Alm.Authentication.TokenScope, obj);

        public bool Equals(TokenScope other)
            => Equals(this as Microsoft.Alm.Authentication.TokenScope, other as Microsoft.Alm.Authentication.TokenScope);

        public override int GetHashCode()
            => GetHashCode(this as Microsoft.Alm.Authentication.TokenScope);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(TokenScope left, TokenScope right)
            => Equals(left as Microsoft.Alm.Authentication.TokenScope, right as Microsoft.Alm.Authentication.TokenScope);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(TokenScope left, TokenScope right)
            => !Equals(left as Microsoft.Alm.Authentication.TokenScope, right as Microsoft.Alm.Authentication.TokenScope);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TokenScope operator +(TokenScope left, TokenScope right)
        {
            var set = UnionWith(left as Microsoft.Alm.Authentication.TokenScope, right as Microsoft.Alm.Authentication.TokenScope);
            return new TokenScope(set);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TokenScope operator -(TokenScope left, TokenScope right)
        {
            var set = ExceptWith(left as Microsoft.Alm.Authentication.TokenScope, right as Microsoft.Alm.Authentication.TokenScope);
            return new TokenScope(set);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TokenScope operator |(TokenScope left, TokenScope right)
        {
            var set = UnionWith(left as Microsoft.Alm.Authentication.TokenScope, right as Microsoft.Alm.Authentication.TokenScope);
            return new TokenScope(set);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TokenScope operator &(TokenScope left, TokenScope right)
        {
            var set = IntersectWith(left as Microsoft.Alm.Authentication.TokenScope, right as Microsoft.Alm.Authentication.TokenScope);
            return new TokenScope(set);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TokenScope operator ^(TokenScope left, TokenScope right)
        {
            var set = SymmetricExceptWith(left as Microsoft.Alm.Authentication.TokenScope, right as Microsoft.Alm.Authentication.TokenScope);
            return new TokenScope(set);
        }
    }
}
