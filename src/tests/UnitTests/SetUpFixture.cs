using System;
using GitHub.Unity;
using NUnit.Framework;
using GitHub.Logging;

namespace UnitTests
{
    [SetUpFixture]
    public class SetUpFixture
    {
        [SetUp]
        public void SetUp()
        {
            LogHelper.TracingEnabled = true;

            LogHelper.LogAdapter = new MultipleLogAdapter(
                new FileLogAdapter($"..\\{DateTime.UtcNow:yyyyMMddHHmmss}-unit-tests.log")
                //, new ConsoleLogAdapter()
            );
        }
    }
}