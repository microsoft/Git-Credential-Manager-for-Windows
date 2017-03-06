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

using Microsoft.Alm.Authentication;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using ScopeSet = System.Collections.Generic.HashSet<string>;

namespace Bitbucket.Authentication
{
    public sealed class BitbucketTokenScope : TokenScope
    {
        public static readonly BitbucketTokenScope None = new BitbucketTokenScope(String.Empty);

        /// <summary>
        /// Access accounts
        /// </summary>
        public static readonly BitbucketTokenScope Account = new BitbucketTokenScope("account");

        /// <summary>
        /// Modify accounts
        /// </summary>
        public static readonly BitbucketTokenScope AccountWrite = new BitbucketTokenScope("account:write");

        /// <summary>
        /// Access teams
        /// </summary>
        public static readonly BitbucketTokenScope Team = new BitbucketTokenScope("team");

        /// <summary>
        /// Modify Teams
        /// </summary>
        public static readonly BitbucketTokenScope TeamWrite = new BitbucketTokenScope("team:write");

        /// <summary>
        /// Access repositories
        /// </summary>
        public static readonly BitbucketTokenScope Repository = new BitbucketTokenScope("repository");

        /// <summary>
        /// Modify repositories
        /// </summary>
        public static readonly BitbucketTokenScope RepositoryWrite = new BitbucketTokenScope("repository:write");

        /// <summary>
        /// Manage repositories
        /// </summary>
        public static readonly BitbucketTokenScope RepositoryAdmin = new BitbucketTokenScope("repository:admin");

        /// <summary>
        /// Access pullrequests
        /// </summary>
        public static readonly BitbucketTokenScope PullRequest = new BitbucketTokenScope("pullrequest");

        /// <summary>
        /// Modify pullrequests
        /// </summary>
        public static readonly BitbucketTokenScope PullRequestWrite = new BitbucketTokenScope("pullrequest:write");

        /// <summary>
        /// Access snippets
        /// </summary>
        public static readonly BitbucketTokenScope Snippet = new BitbucketTokenScope("snippet");

        /// <summary>
        /// Modify snippets
        /// </summary>
        public static readonly BitbucketTokenScope SnippetWrite = new BitbucketTokenScope("snippet:write");

        /// <summary>
        /// Access issues
        /// </summary>
        public static readonly BitbucketTokenScope Issue = new BitbucketTokenScope("issue");

        /// <summary>
        /// Modify issues
        /// </summary>
        public static readonly BitbucketTokenScope IssueWrite = new BitbucketTokenScope("issue:write");

        /// <summary>
        /// Access wiki
        /// </summary>
        public static readonly BitbucketTokenScope Wiki = new BitbucketTokenScope("wiki");

        /// <summary>
        /// Access email
        /// </summary>
        public static readonly BitbucketTokenScope Email = new BitbucketTokenScope("email");

        /// <summary>
        /// Access webhooks
        /// </summary>
        public static readonly BitbucketTokenScope Webhook = new BitbucketTokenScope("webhook");

        /// <summary>
        /// Access projects
        /// </summary>
        public static readonly BitbucketTokenScope Project = new BitbucketTokenScope("project");

        /// <summary>
        /// Modify projects
        /// </summary>
        public static readonly BitbucketTokenScope ProjectWrite = new BitbucketTokenScope("project:write");

        private BitbucketTokenScope(string value)
            : base(value)
        {
        }

        private BitbucketTokenScope(string[] values)
            : base(values)
        {
        }

        private BitbucketTokenScope(ScopeSet set)
            : base(set)
        {
        }

        public static IEnumerable<BitbucketTokenScope> EnumerateValues()
        {
            yield return Account;
            yield return AccountWrite;
            yield return Team;
            yield return TeamWrite;
            yield return Repository;
            yield return RepositoryWrite;
            yield return RepositoryAdmin;
            yield return PullRequest;
            yield return PullRequestWrite;
            yield return Snippet;
            yield return SnippetWrite;
            yield return Issue;
            yield return IssueWrite;
            yield return Wiki;
            yield return Email;
            yield return Webhook;
            yield return Project;
            yield return ProjectWrite;
            yield break;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BitbucketTokenScope operator +(BitbucketTokenScope scope1, BitbucketTokenScope scope2)
        {
            ScopeSet set = new ScopeSet();
            set.UnionWith(scope1._scopes);
            set.UnionWith(scope2._scopes);

            return new BitbucketTokenScope(set);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BitbucketTokenScope operator -(BitbucketTokenScope scope1, BitbucketTokenScope scope2)
        {
            ScopeSet set = new ScopeSet();
            set.UnionWith(scope1._scopes);
            set.ExceptWith(scope2._scopes);

            return new BitbucketTokenScope(set);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BitbucketTokenScope operator |(BitbucketTokenScope scope1, BitbucketTokenScope scope2)
        {
            ScopeSet set = new ScopeSet();
            set.UnionWith(scope1._scopes);
            set.UnionWith(scope2._scopes);

            return new BitbucketTokenScope(set);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BitbucketTokenScope operator &(BitbucketTokenScope scope1, BitbucketTokenScope scope2)
        {
            ScopeSet set = new ScopeSet();
            set.UnionWith(scope1._scopes);
            set.IntersectWith(scope2._scopes);

            return new BitbucketTokenScope(set);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BitbucketTokenScope operator ^(BitbucketTokenScope scope1, BitbucketTokenScope scope2)
        {
            ScopeSet set = new ScopeSet();
            set.UnionWith(scope1._scopes);
            set.SymmetricExceptWith(scope2._scopes);

            return new BitbucketTokenScope(set);
        }
    }
}