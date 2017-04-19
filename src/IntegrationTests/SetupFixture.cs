using GitHub.Unity;
using NUnit.Framework;

namespace IntegrationTests
{
    [SetUpFixture]
    public class SetupFixture
    {
        [SetUp]
        public void Setup()
        {
            Logging.TracingEnabled = true;
            Logging.LoggerFactory = s => new ConsoleLogAdapter(s);
        }
    }
}