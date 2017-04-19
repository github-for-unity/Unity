using GitHub.Unity;
using NUnit.Framework;

namespace UnitTests
{
    [SetUpFixture]
    public class SetUpFixture
    {
        [SetUp]
        public void SetUp()
        {
            Logging.TracingEnabled = true;
            Logging.LoggerFactory = s => new ConsoleLogAdapter(s);
        }
    }
}