using NUnit.Framework;

namespace GitHub.Unity.Tests
{
    [SetUpFixture]
    public class SetupFixture
    {
        [SetUp]
        public void Setup()
        {
            Logging.LoggerFactory = s => new ConsoleLogAdapter(s);
        }
    }
}