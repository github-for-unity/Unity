using System;
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

            Logging.LogAdapter = new MultipleLogAdapter(new FileLogAdapter($"..\\{DateTime.UtcNow.ToString("yyyyMMddHHmmss")}-unit-tests.log"));
        }
    }
}