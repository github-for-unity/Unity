using TestUtils;
using System.Collections.Generic;
using NUnit.Framework;
using GitHub.Unity;

namespace UnitTests
{
    [TestFixture]
    class GitCountObjectProcessorTests : BaseOutputProcessorTests
    {

        [Test]
        public void ShouldParseGitCountOutput()
        {
            var output = new[]
            {
                "2488 objects, 4237 kilobytes",
                null
            };

            AssertProcessOutput(output, new GitCountObjects(2488, 4237));
        }

        private void AssertProcessOutput(IEnumerable<string> lines, GitCountObjects expected)
        {
            GitCountObjects? result = null;
            var outputProcessor = new GitCountObjectsProcessor();
            outputProcessor.OnEntry += status => { result = status; };

            foreach (var line in lines)
            {
                outputProcessor.LineReceived(line);
            }

            Assert.IsTrue(result.HasValue);
            result.Value.AssertEqual(expected);
        }
    }
}