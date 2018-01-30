using System;
using GitHub.Unity;
using NUnit.Framework;
using GitHub.Unity.Logs;

namespace IntegrationTests
{
    [SetUpFixture]
    public class SetUpFixture
    {
        [SetUp]
        public void Setup()
        {
            Logging.TracingEnabled = true;

            Logging.LogAdapter = new MultipleLogAdapter(
                new FileLogAdapter($"..\\{DateTime.UtcNow.ToString("yyyyMMddHHmmss")}-integration-tests.log")
                //, new ConsoleLogAdapter()
            );
        }
    }
}
