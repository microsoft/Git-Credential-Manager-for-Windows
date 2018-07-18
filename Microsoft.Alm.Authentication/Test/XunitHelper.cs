using Xunit.Abstractions;

namespace Microsoft.Alm.Authentication.Test
{
    public class XunitHelper : IUnitTestTrace
    {
        private XunitHelper(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
        }

        private readonly ITestOutputHelper _outputHelper;

        public static IUnitTestTrace Convert(ITestOutputHelper outputHelper)
        {
            return new XunitHelper(outputHelper);
        }

        public void WriteLine(string message)
        {
            _outputHelper?.WriteLine(message);
        }
    }
}
