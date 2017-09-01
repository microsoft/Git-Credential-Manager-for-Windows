/**** Git Credential Manager for Windows ****
 *
 * Copyright (c) Microsoft Corporation
 * Copyright (c) Atlassian
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

namespace Atlassian.Bitbucket.Authentication
{
    /// <summary>
    /// Defines the available scopes associated with OAuth tokens in Bitbucket.
    /// </summary>
    public sealed class TokenScope : Microsoft.Alm.Authentication.TokenScope
    {
        public static readonly TokenScope None = new TokenScope(string.Empty);

        /// <summary>
        /// Access accounts
        /// </summary>
        public static readonly TokenScope Account = new TokenScope("account");

        /// <summary>
        /// Modify accounts
        /// </summary>
        public static readonly TokenScope AccountWrite = new TokenScope("account:write");

        /// <summary>
        /// Access teams
        /// </summary>
        public static readonly TokenScope Team = new TokenScope("team");

        /// <summary>
        /// Modify Teams
        /// </summary>
        public static readonly TokenScope TeamWrite = new TokenScope("team:write");

        /// <summary>
        /// Access repositories
        /// </summary>
        public static readonly TokenScope Repository = new TokenScope("repository");

        /// <summary>
        /// Modify repositories
        /// </summary>
        public static readonly TokenScope RepositoryWrite = new TokenScope("repository:write");

        /// <summary>
        /// Manage repositories
        /// </summary>
        public static readonly TokenScope RepositoryAdmin = new TokenScope("repository:admin");

        /// <summary>
        /// Access pullrequests
        /// </summary>
        public static readonly TokenScope PullRequest = new TokenScope("pullrequest");

        /// <summary>
        /// Modify pullrequests
        /// </summary>
        public static readonly TokenScope PullRequestWrite = new TokenScope("pullrequest:write");

        /// <summary>
        /// Access snippets
        /// </summary>
        public static readonly TokenScope Snippet = new TokenScope("snippet");

        /// <summary>
        /// Modify snippets
        /// </summary>
        public static readonly TokenScope SnippetWrite = new TokenScope("snippet:write");

        /// <summary>
        /// Access issues
        /// </summary>
        public static readonly TokenScope Issue = new TokenScope("issue");

        /// <summary>
        /// Modify issues
        /// </summary>
        public static readonly TokenScope IssueWrite = new TokenScope("issue:write");

        /// <summary>
        /// Access wiki
        /// </summary>
        public static readonly TokenScope Wiki = new TokenScope("wiki");

        /// <summary>
        /// Access email
        /// </summary>
        public static readonly TokenScope Email = new TokenScope("email");

        /// <summary>
        /// Access webhooks
        /// </summary>
        public static readonly TokenScope Webhook = new TokenScope("webhook");

        /// <summary>
        /// Access projects
        /// </summary>
        public static readonly TokenScope Project = new TokenScope("project");

        /// <summary>
        /// Modify projects
        /// </summary>
        public static readonly TokenScope ProjectWrite = new TokenScope("project:write");

        private TokenScope(string value) : base(value)
        { }

        private TokenScope(string[] values) : base(values)
        { }

        private TokenScope(ScopeSet set) : base(set)
        { }

        public static IEnumerable<TokenScope> EnumerateValues()
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
        public static TokenScope operator +(TokenScope scope1, TokenScope scope2)
        {
            ScopeSet set = new ScopeSet();
            set.UnionWith(scope1._scopes);
            set.UnionWith(scope2._scopes);

            return new TokenScope(set);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TokenScope operator -(TokenScope scope1, TokenScope scope2)
        {
            ScopeSet set = new ScopeSet();
            set.UnionWith(scope1._scopes);
            set.ExceptWith(scope2._scopes);

            return new TokenScope(set);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TokenScope operator |(TokenScope scope1, TokenScope scope2)
        {
            ScopeSet set = new ScopeSet();
            set.UnionWith(scope1._scopes);
            set.UnionWith(scope2._scopes);

            return new TokenScope(set);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TokenScope operator &(TokenScope scope1, TokenScope scope2)
        {
            ScopeSet set = new ScopeSet();
            set.UnionWith(scope1._scopes);
            set.IntersectWith(scope2._scopes);

            return new TokenScope(set);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TokenScope operator ^(TokenScope scope1, TokenScope scope2)
        {
            ScopeSet set = new ScopeSet();
            set.UnionWith(scope1._scopes);
            set.SymmetricExceptWith(scope2._scopes);

            return new TokenScope(set);
        }
    }
}
