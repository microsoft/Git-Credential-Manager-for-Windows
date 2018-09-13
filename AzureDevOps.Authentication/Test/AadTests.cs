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

using System.Threading.Tasks;
using Microsoft.Alm.Authentication;
using Xunit;

namespace AzureDevOps.Authentication.Test
{
    public class AadTests : AuthenticationTests
    {
        public AadTests()
            : base()
        { }

        [Fact]
        public async Task VstsAadDeleteCredentialsTest()
        {
            TargetUri targetUri = DefaultTargetUri;
            AadAuthentication aadAuthentication = GetDevOpsAadAuthentication(RuntimeContext.Default, "aad-delete");

            if (aadAuthentication.Authority is AuthorityFake fake)
            {
                fake.CredentialsAreValid = false;
            }

            await aadAuthentication.PersonalAccessTokenStore.WriteCredentials(targetUri, DefaultPersonalAccessToken);

            await aadAuthentication.DeleteCredentials(targetUri);
            Assert.Null(await aadAuthentication.PersonalAccessTokenStore.ReadCredentials(targetUri));

            await aadAuthentication.DeleteCredentials(targetUri);
            Assert.Null(await aadAuthentication.PersonalAccessTokenStore.ReadCredentials(targetUri));
        }

        [Fact]
        public async Task VstsAadGetCredentialsTest()
        {
            TargetUri targetUri = DefaultTargetUri;
            AadAuthentication aadAuthentication = GetDevOpsAadAuthentication(RuntimeContext.Default, "aad-get");

            Assert.Null(await aadAuthentication.GetCredentials(targetUri));

            await aadAuthentication.PersonalAccessTokenStore.WriteCredentials(targetUri, DefaultPersonalAccessToken);

            Assert.NotNull(await aadAuthentication.GetCredentials(targetUri));
        }

        [Fact]
        public async Task VstsAadInteractiveLogonTest()
        {
            TargetUri targetUri = DefaultTargetUri;
            AadAuthentication aadAuthentication = GetDevOpsAadAuthentication(RuntimeContext.Default, "aad-logon");

            Assert.Null(await aadAuthentication.PersonalAccessTokenStore.ReadCredentials(targetUri));

            Assert.NotNull(await aadAuthentication.InteractiveLogon(targetUri, new PersonalAccessTokenOptions { RequireCompactToken = false }));

            Assert.NotNull(await aadAuthentication.PersonalAccessTokenStore.ReadCredentials(targetUri));
        }

        [Fact]
        public async Task VstsAadNoninteractiveLogonTest()
        {
            TargetUri targetUri = DefaultTargetUri;
            AadAuthentication aadAuthentication = GetDevOpsAadAuthentication(RuntimeContext.Default, "aad-noninteractive");

            Assert.NotNull(await aadAuthentication.NoninteractiveLogon(targetUri, new PersonalAccessTokenOptions { RequireCompactToken = false }));

            Assert.NotNull(await aadAuthentication.PersonalAccessTokenStore.ReadCredentials(targetUri));
        }

        [Fact]
        public async Task VstsAadSetCredentialsTest()
        {
            TargetUri targetUri = DefaultTargetUri;
            AadAuthentication aadAuthentication = GetDevOpsAadAuthentication(RuntimeContext.Default, "aad-set");
            Credential credentials = DefaultCredentials;

            await aadAuthentication.SetCredentials(targetUri, credentials);

            Assert.Null(await aadAuthentication.PersonalAccessTokenStore.ReadCredentials(targetUri));
            Assert.Null(credentials = await aadAuthentication.GetCredentials(targetUri));
        }

        [Fact]
        public async Task VstsAadValidateCredentialsTest()
        {
            AadAuthentication aadAuthentication = GetDevOpsAadAuthentication(RuntimeContext.Default, "aad-validate");
            Credential credentials = null;

            Assert.False(await aadAuthentication.ValidateCredentials(DefaultTargetUri, credentials), "Credential validation unexpectedly failed.");

            credentials = DefaultCredentials;

            Assert.True(await aadAuthentication.ValidateCredentials(DefaultTargetUri, credentials), "Credential validation unexpectedly failed.");
        }

        private static AadAuthentication GetDevOpsAadAuthentication(RuntimeContext context, string @namespace)
        {
            string expectedQueryParameters = null;

            ICredentialStore tokenStore1 = new SecretCache(context, @namespace + 1, Secret.UriToIdentityUrl);
            ITokenStore tokenStore2 = new SecretCache(context, @namespace + 2, Secret.UriToIdentityUrl);
            IAuthority devopsAuthority = new AuthorityFake(expectedQueryParameters);
            return new AadAuthentication(context, tokenStore1, tokenStore2, devopsAuthority);
        }
    }
}
