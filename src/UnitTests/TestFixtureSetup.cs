using GitHub.Unity;
using NUnit.Framework;

namespace UnitTests
{
    [SetUpFixture]
    class TestFixtureSetup
    {
        [SetUp]
        public void SetUp()
        {
            //Changing the Logger Instance to avoid calling Unity application libraries from nunit
            //Failure to do so will result in the following exception
            //System.Security.SecurityException : ECall methods must be packaged into a system module.
            Logging.LoggerFactory = s => new ConsoleLogAdapter(s);
        }
    }
}

namespace GitHub.Unity.IntegrationTests
{
    [SetUpFixture]
    class TestFixtureSetup
    {
        [SetUp]
        public void SetUp()
        {
            //Changing the Logger Instance to avoid calling Unity application libraries from nunit
            //Failure to do so will result in the following exception
            //System.Security.SecurityException : ECall methods must be packaged into a system module.
            Logging.LoggerFactory = s => new ConsoleLogAdapter(s);
        }
    }
}