using NUnit.Framework;
using TestUtils;

namespace UnitTests
{
    class BaseOutputProcessorTests
    {
        protected const string TestRootPath = @"c:\TestSource";
        protected SubstituteFactory SubstituteFactory { get; private set; }

        [TestFixtureSetUp]
        public void TestFixtureSetup()
        {
            SubstituteFactory = new SubstituteFactory();
        }
    }
}
