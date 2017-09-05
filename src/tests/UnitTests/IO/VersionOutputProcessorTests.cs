using System;
using System.Collections.Generic;
using System.Globalization;
using FluentAssertions;
using NUnit.Framework;
using TestUtils;
using GitHub.Unity;

namespace UnitTests
{
    [TestFixture]
    class VersionOutputProcessorTests : BaseOutputProcessorTests
    {
        public static IEnumerable<TestCaseData> ShouldParseVersionOutputs_TestCases()
        {
            TestCaseData testCase;

            testCase = new TestCaseData(
                $"git version 2.11.1.windows.1{Environment.NewLine}",
                new SoftwareVersion(2, 11, 1));

            testCase.SetName("Windows 2.11.1");
            yield return testCase;
        }

        [TestCaseSource(nameof(ShouldParseVersionOutputs_TestCases))]
        public void ShouldParseVersionOutputs(string line, SoftwareVersion expected)
        {
            SoftwareVersion? version = null;

            var outputProcessor = new VersionOutputProcessor();
            outputProcessor.OnEntry += output => { version = output; };
            outputProcessor.LineReceived(line);

            version.HasValue.Should().BeTrue();
            version.Value.Should().Be(expected);
        }
    }
}