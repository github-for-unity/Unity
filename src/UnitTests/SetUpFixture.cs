using System.IO;
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

            var tempFileName = Path.GetTempFileName();
            var fileLog = tempFileName.Substring(0, tempFileName.Length - 4) + "_integrationtest.log";

            Logging.LogAdapter = new MultipleLogAdapter(new ConsoleLogAdapter(), new FileLogAdapter(fileLog));
            Logging.Trace("Logging to \"{0}\"", fileLog);
        }
    }
}