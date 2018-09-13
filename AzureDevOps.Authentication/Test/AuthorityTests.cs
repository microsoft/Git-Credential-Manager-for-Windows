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
using System.Threading.Tasks;
using Microsoft.Alm.Authentication;
using Microsoft.Alm.Authentication.Test;
using Xunit;

namespace AzureDevOps.Authentication.Test
{
    public class AuthorityTests : UnitTestBase
    {
        private const string OnceValidPersonalAccessToken = "u73gjiqqr5wyixbextwup2a5sx3veuklvbfmc54c5yuisnew2uwq";
        private const string MicrosoftGitToolsVstsAccount = "http://microsoft-git-tools.visualstudio.com/";

        public AuthorityTests(Xunit.Abstractions.ITestOutputHelper outputHelper)
            : base(XunitHelper.Convert(outputHelper))
        { }

        public static object[][] GetSecretKeyData
        {
            get
            {
                const string Org = "organization";
                const string Path = "project/_git/repository";
                const string Username = "user";
                const string VstsUrl = "visualstudio.com";
                const string CodexUrl = "codex.azure.com";

                var boolean = new[] { false, true };
                var hosts = new[] { CodexUrl, VstsUrl, };

                var data = new List<object[]>();

                foreach (string host in hosts)
                {
                    foreach (bool defaultPort in boolean)
                    {
                        foreach (bool hasUsername in boolean)
                        {
                            foreach (bool hasRemoteUrl in boolean)
                            {
                                foreach (bool hasFullPath in boolean)
                                {
                                    var targetBuilder = new UriBuilder()
                                    {
                                        // VSTS uses {organization}.{host} where as Codex does not.
                                        Host = host.Equals(VstsUrl) ? Org + '.' + host : host,
                                        Scheme = Uri.UriSchemeHttps,
                                    };
                                    var expectedBuilder = new UriBuilder()
                                    {
                                        // VSTS uses {organization}.{host} where as Codex does not.
                                        Host = host.Equals(VstsUrl) ? Org + '.' + host : host,
                                        Scheme = Uri.UriSchemeHttps,
                                    };
                                    var remoteUrlBuilder = new UriBuilder()
                                    {
                                        // VSTS uses {organization}.{host} where as Codex does not.
                                        Host = host.Equals(VstsUrl) ? Org + '.' + host : host,
                                        // Codex places {organization} first on path, VSTS does not.
                                        Path = host.Equals(CodexUrl) ? Org + '/' : "/",
                                        Scheme = Uri.UriSchemeHttps,
                                    };

                                    // If the URL has a non-default port, set it.
                                    if (!defaultPort)
                                    {
                                        targetBuilder.Port = 8080;
                                        expectedBuilder.Port = 8080;
                                        remoteUrlBuilder.Port = 8080;
                                    }

                                    // If the URL includes a username, include it.
                                    if (hasUsername)
                                    {
                                        // Append username to target
                                        targetBuilder.UserName = Username;

                                        // Append username to expected
                                        expectedBuilder.UserName = Username;

                                        // Handle Codex oddities
                                        if (host.Equals(CodexUrl))
                                        {
                                            // If there's not a remote URL, then the username becomes the path of the expected and remote values.
                                            if (!hasRemoteUrl)
                                            {
                                                expectedBuilder.Path = Username + '/';
                                                remoteUrlBuilder.Path = Username + '/';
                                            }
                                        }

                                        // The remote URL is the superset, so append the username
                                        remoteUrlBuilder.UserName = Username;
                                    }
                                    // Handle Codex oddities...
                                    else if (host.Equals(CodexUrl))
                                    {
                                        // If there's no remote URL, then Org needs to be the target's user-info;
                                        // and the expected's URL as well.
                                        if (!hasRemoteUrl)
                                        {
                                            targetBuilder.UserName = Org;
                                            expectedBuilder.UserName = Org;
                                            expectedBuilder.Path = Org + '/';
                                        }
                                    }

                                    // Codex places the organization info in the path.
                                    if (hasRemoteUrl && host.Equals(CodexUrl))
                                    {
                                        expectedBuilder.Path = Org + '/';
                                    }

                                    // Append the full path to both target and expected
                                    if (hasFullPath)
                                    {
                                        targetBuilder.Path = Org + '/' + Path;
                                        expectedBuilder.Path = Org + '/' + Path;
                                    }

                                    // The remote URL is the superset, so append the path.
                                    remoteUrlBuilder.Path += Path;

                                    // If the test doesn't contain a remote URL, set it to null.
                                    remoteUrlBuilder = hasRemoteUrl
                                        ? remoteUrlBuilder
                                        : null;

                                    data.Add(new object[]
                                    {
                                        targetBuilder.ToString(),
                                        remoteUrlBuilder?.ToString(),
                                        "git:" + expectedBuilder.ToString().Trim('/', '\\'),
                                    });
                                }
                            }
                        }
                    }
                }

                return data.ToArray();
            }
        }

        [Theory, MemberData(nameof(GetSecretKeyData))]
        public void GetSecretKey(string targetUrl, string actualUrl, string expectedKey)
        {
            var targetUri = new TargetUri(targetUrl, null, actualUrl);
            string actualKey = Authentication.GetSecretKey(targetUri, "git");

            Assert.Equal(expectedKey, actualKey, StringComparer.Ordinal);
        }

        [Fact]
        public async Task ValidateCredentials_Failed()
        {
            InitializeTest();

            var authority = new Authority(Context);
            var credentials = new Credential("invalid-user", "fake+password");
            var targetUri = new TargetUri(MicrosoftGitToolsVstsAccount);

            Assert.False(await authority.ValidateCredentials(targetUri, credentials));
        }

        [Fact]
        public async Task ValidateCredentials_Null()
        {
            InitializeTest();

            var authority = new Authority(Context);
            var targetUri = new TargetUri(MicrosoftGitToolsVstsAccount);

            Assert.False(await authority.ValidateCredentials(targetUri, null));
        }

        [Fact]
        public async Task ValidateCredentials_Success()
        {
            InitializeTest();

            var authority = new Authority(Context);
            var credentials = (Credential)new Token(OnceValidPersonalAccessToken, TokenType.Personal);
            var targetUri = new TargetUri(MicrosoftGitToolsVstsAccount);

            Assert.True(await authority.ValidateCredentials(targetUri, credentials));
        }

        [Fact]
        public async Task ValidateToken_Access_Failed()
        {
            InitializeTest();

            var authority = new Authority(Context);
            var token = new Token("invalid+token+value", TokenType.AzureAccess);
            var targetUri = new TargetUri(MicrosoftGitToolsVstsAccount);

            Assert.False(await authority.ValidateToken(targetUri, token));
        }

        [Fact]
        public async Task ValidateToken_Federated_Failed()
        {
            InitializeTest();

            var authority = new Authority(Context);
            var token = new Token("invalid+token+value", TokenType.AzureFederated);
            var targetUri = new TargetUri(MicrosoftGitToolsVstsAccount);

            Assert.False(await authority.ValidateToken(targetUri, token));
        }

        [Fact]
        public async Task ValidateToken_Null()
        {
            InitializeTest();

            var authority = new Authority(Context);
            var targetUri = new TargetUri(MicrosoftGitToolsVstsAccount);

            Assert.False(await authority.ValidateToken(targetUri, null));
        }

        [Fact]
        public async Task ValidateToken_Success()
        {
            InitializeTest();

            var authority = new Authority(Context);
            var token = new Token(OnceValidPersonalAccessToken, TokenType.Personal);
            var targetUri = new TargetUri(MicrosoftGitToolsVstsAccount);

            Assert.True(await authority.ValidateToken(targetUri, token));
        }
    }
}
