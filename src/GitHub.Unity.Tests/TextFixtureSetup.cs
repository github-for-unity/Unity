using GitHub.Unity.Logging;
using GitHub.Unity.Tests.Logging;
using NUnit.Framework;

namespace GitHub.Unity.Tests
{
    [SetUpFixture]
    public class TextFixtureSetup
    {
        [SetUp]
        public void SetUp()
        {
            //Changing the Logger Instance to avoid calling Unity application libraries from nunit
            //Failure to do so will result in the following exception
            //System.Security.SecurityException : ECall methods must be packaged into a system module.
            Logger.LoggerFactory = s => new TestLogAdapter(s);
        }
    }
}