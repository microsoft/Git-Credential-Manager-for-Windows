using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Alm.Authentication;
using Microsoft.Alm.Authentication.Test;
using Xunit;

namespace Atlassian.Bitbucket.Authentication.Test
{
    public class AuthorityTest : UnitTestBase
    {
        public AuthorityTest(Xunit.Abstractions.ITestOutputHelper output)
            : base(XunitHelper.Convert(output))
        { }

        [Fact]
        public void VerifyAcquireTokenAcceptsValidAuthenticationResultTypes()
        {
            InitializeTest();

            var authority = new Authority(Context);
            var targetUri = new TargetUri("https://bitbucket.org");
            var credentials = new Credential("a", "b");
            var resultType = AuthenticationResultType.None;
            var tokenScope = TokenScope.None;

            var values = Enum.GetValues(typeof(AuthenticationResultType))
                             .Cast<AuthenticationResultType>()
                             .ToList();
            int count = 0;

            values.ToList().ForEach(_ =>
            {
                Task.Run(async () =>
                {
                    Interlocked.Increment(ref count);

                    AuthenticationResult token = await authority.AcquireToken(targetUri, credentials, resultType, tokenScope);

                    Assert.NotNull(token);
                }).Wait();
            });

            Assert.Equal(values.Count, count);
        }
    }
}
