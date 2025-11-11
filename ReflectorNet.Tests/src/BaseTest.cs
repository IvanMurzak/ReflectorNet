using Xunit.Abstractions;

namespace com.IvanMurzak.ReflectorNet.Tests
{
    public class BaseTest
    {
        protected readonly ITestOutputHelper _output;

        public BaseTest(ITestOutputHelper output)
        {
            _output = output;
        }
    }
}
