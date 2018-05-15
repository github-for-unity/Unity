using System;
using System.Collections.Generic;
using FluentAssertions;
using GitHub.Unity;
using NUnit.Framework;

namespace UnitTests
{
    [TestFixture]
    class LfsVersionOutputProcessorTests : BaseOutputProcessorTests
    {
        public static IEnumerable<TestCaseData> ShouldParseVersionOutputs_TestCases()
        {
            yield return new TestCaseData(
                $"git-lfs/2.2.0 (GitHub; windows amd64; go 1.8.3; git a99f4b21){Environment.NewLine}", 
                TheVersion.Parse("2.2")).SetName("Windows GitLFS 2.2.0");

            yield return new TestCaseData(
                $"git-lfs/2.2.0 (GitHub; darwin amd64; go 1.8.3){Environment.NewLine}",
                TheVersion.Parse("2.2")).SetName("Mac GitLFS 2.2.0");
        }

        [TestCaseSource(nameof(ShouldParseVersionOutputs_TestCases))]
        public void ShouldParseVersionOutputs(string line, TheVersion expected)
        {
            TheVersion version = TheVersion.Default;

            var outputProcessor = new LfsVersionOutputProcessor();
            outputProcessor.OnEntry += output => { version = output; };
            outputProcessor.LineReceived(line);

            version.Should().Be(expected);
        }
    }
}