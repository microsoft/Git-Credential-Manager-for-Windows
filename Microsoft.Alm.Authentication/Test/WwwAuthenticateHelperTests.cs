using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Microsoft.Alm.Authentication.Test
{
    public class WwwAuthenticateHelperTests : UnitTestBase
    {
        public WwwAuthenticateHelperTests(Xunit.Abstractions.ITestOutputHelper output)
            : base(XunitHelper.Convert(output))
        { }

        public static object[][] GetHeaderValuesData
        {
            get
            {
                return new object[][]
                {
                    new object[] { 1, "https://microsoft.visualstudio.com", 1, },
                    new object[] { 2, "https://github.com", 0, },
                };
            }
        }

        [Theory, MemberData(nameof(GetHeaderValuesData))]
        public async Task GetHeaderValues(int iteration, string queryUrl, int expectedCount)
        {
            InitializeTest(iteration);

            var targetUri = new TargetUri(queryUrl);

            var authenticateHeaders = await WwwAuthenticateHelper.GetHeaderValues(Context, targetUri);

            Assert.NotNull(authenticateHeaders);
            Assert.Equal(expectedCount, authenticateHeaders.Count());
        }
    }
}
