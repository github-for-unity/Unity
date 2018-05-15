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
            yield return new TestCaseData(
                $"git version 2.11.1.windows.1{Environment.NewLine}",
                TheVersion.Parse("2.11.1.windows.1")).SetName("Windows 2.11.1");

            yield return new TestCaseData(
                $"git version 2.12.2{Environment.NewLine}",
                TheVersion.Parse("2.12.2")).SetName("Mac 2.12.2");
        }

        [TestCaseSource(nameof(ShouldParseVersionOutputs_TestCases))]
        public void ShouldParseVersionOutputs(string line, TheVersion expected)
        {
            TheVersion version = TheVersion.Default;

            var outputProcessor = new VersionOutputProcessor();
            outputProcessor.OnEntry += output => { version = output; };
            outputProcessor.LineReceived(line);

            version.Should().Be(expected);
        }
    }
}