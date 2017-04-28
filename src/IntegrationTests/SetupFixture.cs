using System.IO;
using GitHub.Unity;
using NUnit.Framework;

namespace IntegrationTests
{
    [SetUpFixture]
    public class SetupFixture
    {
        [SetUp]
        public void Setup()
        {
            Logging.TracingEnabled = true;

            var tempFileName = Path.GetTempFileName();
            var fileLog = tempFileName.Substring(0, tempFileName.Length - 4) + "_integrationtest.log";

            Logging.LoggerFactory = context => new MultipleLogAdapter(() => new ConsoleLogAdapter(context), 
                () => new FileLogAdapter(fileLog, context));

            Logging.Trace("Logging to \"{0}\"", fileLog);
        }
    }
}
