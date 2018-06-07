using Atlassian.Bitbucket.Authentication;
using GitHub.Authentication;
using Microsoft.Alm.Authentication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Bitbucket.Authentication.Test
{
    public class AuthorityTest
    {
        [Fact]
        public async void VerifyAcquireTokenAcceptsValidAuthenticationResultTypes()
        {
            var context = RuntimeContext.Default;
            var authority = new Authority(context);
            var targetUri = new TargetUri("https://bitbucket.org");
            var credentials = new Credential("a", "b");
            var resultType = AuthenticationResultType.None;
            var tokenScope = Atlassian.Bitbucket.Authentication.TokenScope.None;

            var values = Enum.GetValues(typeof(AuthenticationResultType)).Cast<AuthenticationResultType>();
            values.ToList().ForEach(async _ =>
            {
                var token = await authority.AcquireToken(targetUri, credentials, resultType, tokenScope);

                Assert.NotNull(token);

            });
        }
    }
}
