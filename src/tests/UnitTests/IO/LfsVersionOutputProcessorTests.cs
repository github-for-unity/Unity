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
            TestCaseData testCase;

            testCase = new TestCaseData(
                $"git-lfs/2.2.0 (GitHub; windows amd64; go 1.8.3; git a99f4b21){Environment.NewLine}", 
                new SoftwareVersion(2, 2, 0));

            testCase.SetName("Windows GitLFS 2.2.0");
            yield return testCase;
        }

        [TestCaseSource(nameof(ShouldParseVersionOutputs_TestCases))]
        public void ShouldParseVersionOutputs(string line, SoftwareVersion expected)
        {
            SoftwareVersion? version = null;

            var outputProcessor = new LfsVersionOutputProcessor();
            outputProcessor.OnEntry += output => { version = output; };
            outputProcessor.LineReceived(line);

            version.HasValue.Should().BeTrue();
            version.Value.Should().Be(expected);
        }
    }
}