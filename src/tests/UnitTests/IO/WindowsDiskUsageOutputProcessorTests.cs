using System.Collections.Generic;
using GitHub.Unity;
using NUnit.Framework;

namespace UnitTests
{
    [TestFixture]
    class WindowsDiskUsageOutputProcessorTests : BaseOutputProcessorTests
    {
        [Test]
        public void WindowsDiskUsageOutput()
        {
            var output = new[]
            {
                "01/10/2018  09:11 AM                 0 862394886",
                "05 / 10 / 2018  01:17 PM                 0 880468387",
                "05 / 10 / 2018  02:12 PM                 0 882539090",
                "05 / 08 / 2018  10:56 AM                 0 890139785",
                "05 / 10 / 2018  02:12 PM                 0 907039131",
                "01 / 10 / 2018  09:11 AM                 0 909343522",
                "01 / 10 / 2018  09:11 AM                 0 914279800",
                "03 / 08 / 2018  12:50 PM                 0 935882723",
                "02 / 20 / 2018  10:40 AM                 0 953135163",
                "01 / 10 / 2018  09:11 AM                 0 956375995",
                "01 / 10 / 2018  09:11 AM                 0 957028503",
                "01 / 10 / 2018  09:11 AM                 0 957454540",
                "05 / 08 / 2018  10:56 AM                 0 961987973",
                "01 / 10 / 2018  09:11 AM                 0 972291688",
                "01 / 10 / 2018  09:11 AM                 0 986253768",
                "03 / 07 / 2018  06:33 PM                 0 991012201",
                "04 / 02 / 2018  02:39 PM<DIR> objects",
                "148 File(s)              0 bytes",
                "",
                @" Directory of C:\Users\Spade\Projects\GitHub\Unity\.git\lfs\tmp\objects",
                "",
                "04 / 02 / 2018  02:39 PM<DIR>.",
                "04 / 02 / 2018  02:39 PM<DIR>..",
                "                   0 File(s)              0 bytes",
                "",
                "         Total Files Listed:",
                "                 409 File(s)    643,058,481 bytes",
                "                1325 Dir(s)  151,921,385,472 bytes free",
                null
            };

            AssertProcessOutput(output, 627986);
        }

        private void AssertProcessOutput(IEnumerable<string> lines, int expected)
        {
            long? result = null;
            var outputProcessor = new WindowsDiskUsageOutputProcessor();
            outputProcessor.OnEntry += status => { result = status; };

            foreach (var line in lines)
            {
                outputProcessor.LineReceived(line);
            }

            Assert.IsTrue(result.HasValue);
            Assert.AreEqual(expected, result.Value);
        }
    }
}
