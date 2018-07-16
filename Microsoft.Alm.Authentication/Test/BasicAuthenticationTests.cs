using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Alm.Authentication.Test
{
    public class BasicAuthenticationTests : UnitTestBase
    {
        public BasicAuthenticationTests(Xunit.Abstractions.ITestOutputHelper output)
            : base(XunitHelper.Convert(output))
        { }

        //[Fact]
        //public async Task AcquireCredentials()
        //{
        //    InitializeTest(nameof(AcquireCredentials), false);

        //    var credStore = new SecretCache(Context, "test");
        //    var authentication = new BasicAuthentication(Context, credStore);
        //    var targetUri = new TargetUri("https://fake.com");

        //    var credentials = await authentication.AcquireCredentials(targetUri);

        //    Assert.Null(credentials);
        //}
    }
}
