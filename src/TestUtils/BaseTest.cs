using GitHub.Unity;
using NUnit.Framework;

namespace TestUtils
{
    abstract class BaseTest
    {
        protected ILogging Logger { get; private set; }
        protected TestUtils.SubstituteFactory Factory { get; set; }

        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            OnTestFixtureSetup();
        }

        protected virtual void OnTestFixtureSetup()
        {
            GitHub.Unity.Guard.InUnitTestRunner = true;
            Logger = Logging.GetLogger(GetType());
            Factory = new TestUtils.SubstituteFactory();
        }

        [TestFixtureTearDown]
        public void TestFixtureTearDown()
        {
            OnTestFixtureTearDown();
        }

        protected virtual void OnTestFixtureTearDown()
        {
        }

        [SetUp]
        public void SetUp()
        {
            OnSetup();
        }

        protected virtual void OnSetup()
        {

        }

        [TearDown]
        public void TearDown()
        {
            OnTearDown();
        }

        protected virtual void OnTearDown()
        {

        }
    }
}