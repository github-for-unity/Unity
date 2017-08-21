using System;
using System.Collections.Generic;
using FluentAssertions;
using GitHub.Unity.Helpers;
using NUnit.Framework;

namespace UnitTests
{
    [TestFixture]
    public class NewlineSplitStringBuilderTests
    {
        public static TestCaseData[] GetNewlineSplitTestData()
        {
            return new[] {
                new TestCaseData(new string[] {null}, new string[0]).SetName("Null returns nothing"),
                new TestCaseData(new string[] { $"ASDF{Environment.NewLine}Hello", null}, new[] {"ASDF", "Hello"}).SetName("Can split whole string"),
                new TestCaseData(new string[] { $"ASDF", $"{Environment.NewLine}Hello", null}, new[] {"ASDF", "Hello"}).SetName("Can split string with second beginning newline"),
                new TestCaseData(new string[] { $"ASDF{Environment.NewLine}", "Hello", null}, new[] {"ASDF", "Hello"}).SetName("Can split string with first ending newline"),
                new TestCaseData(new string[] { $"AS", $"DF{Environment.NewLine}Hello", null}, new[] {"ASDF", "Hello"}).SetName("Can split string with newline contained in second"),
            };
        }

        [TestCaseSource("GetNewlineSplitTestData")]
        public void NewlineSplitStringBuilderTest(string[] expectedInputs, string[] expectedOutputs)
        {
            var results = new List<string>();
            var newlineSplitStringBuilder = new NewlineSplitStringBuilder();
            foreach (var expectedInput in expectedInputs)
            {
                var output = newlineSplitStringBuilder.Append(expectedInput);
                if (output != null)
                {
                    results.AddRange(output);
                }
            }

            results.ShouldAllBeEquivalentTo(expectedOutputs);
        }
    }
}