using System.Collections.Generic;
using GitHub.Unity;
using NUnit.Framework;

namespace UnitTests
{
    [TestFixture]
    class LinuxDiskUsageOutputProcessorTests : BaseOutputProcessorTests
    {
        [Test]
        public void ParseOutput()
        {
            var output = new[]
            {
                "4       .git/lfs/objects/f7/0a",
                "4       .git/lfs/objects/f7",
                "44      .git/lfs/objects/f8/0c",
                "1       .git/lfs/objects/f8/1e",
                "45      .git/lfs/objects/f8",
                "176     .git/lfs/objects/fa/7c",
                "176     .git/lfs/objects/fa",
                "7716    .git/lfs/objects/fb/8d",
                "188     .git/lfs/objects/fb/ac",
                "7904    .git/lfs/objects/fb",
                "168     .git/lfs/objects/fd/39",
                "168     .git/lfs/objects/fd",
                "284     .git/lfs/objects/fe/50",
                "284     .git/lfs/objects/fe",
                "2556    .git/lfs/objects/ff/48",
                "1       .git/lfs/objects/ff/c6",
                "2557    .git/lfs/objects/ff",
                "4       .git/lfs/objects/incomplete",
                "0       .git/lfs/objects/logs",
                "628417  .git/lfs/objects",
                "0       .git/lfs/tmp/objects",
                "64      .git/lfs/tmp",
                "628481  .git/lfs",
                null
            };

            AssertProcessOutput(output, 628481);
        }

        private void AssertProcessOutput(IEnumerable<string> lines, int expected)
        {
            int? result = null;
            var outputProcessor = new LinuxDiskUsageOutputProcessor();
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