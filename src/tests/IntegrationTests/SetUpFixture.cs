using System;
using GitHub.Unity;
using NUnit.Framework;
using GitHub.Logging;

namespace IntegrationTests
{
    [SetUpFixture]
    public class SetUpFixture
    {
        [SetUp]
        public void Setup()
        {
            LogHelper.TracingEnabled = true;

            LogHelper.LogAdapter = new MultipleLogAdapter(
                new FileLogAdapter($"..\\{DateTime.UtcNow.ToString("yyyyMMddHHmmss")}-integration-tests.log")
                //, new ConsoleLogAdapter()
            );
        }
    }
}
