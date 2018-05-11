using TestUtils;
using System.Collections.Generic;
using NUnit.Framework;
using GitHub.Unity;
using System;
using System.Globalization;
using FluentAssertions;

namespace UnitTests
{
    [TestFixture]
    class LockOutputProcessorTests : BaseOutputProcessorTests
    {
        private void AssertProcessOutput(IEnumerable<string> lines, GitLock[] expected)
        {
            var results = new List<GitLock>();
            var outputProcessor = new LocksOutputProcessor();
            outputProcessor.OnEntry += gitLock => { results.Add(gitLock); };

            foreach (var line in lines)
            {
                outputProcessor.LineReceived(line);
            }
            CollectionAssert.AreEqual(expected, results);
        }

        [Test]
        public void ShouldParseZeroLocksFormat1()
        {
            var output = new[] {
                null,
                "0 lock(s) matched query."
            };

            var expected = new GitLock[0];

            AssertProcessOutput(output, expected);
        }

        [Test]
        public void ShouldParseZeroLocksFormat2()
        {
            var output = new[] {
                null,
                "0 lock (s) matched query."
            };

            var expected = new GitLock[0];

            AssertProcessOutput(output, expected);
        }

        [Test]
        public void ShouldParseTwoLocksFormat1()
        {
            var now = DateTimeOffset.ParseExact(DateTimeOffset.UtcNow.ToString(Constants.Iso8601FormatZ), Constants.Iso8601Formats, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);
            var output = new[]
            {
                @"[{""id"":""12"", ""path"":""folder/somefile.png"", ""owner"":{""name"":""GitHub User""}, ""locked_at"":""" + now.ToString(Constants.Iso8601FormatZ) + "\"}" +
                @" ,{""id"":""21"", ""path"":""somezip.zip"", ""owner"":{""name"":""GitHub User""}, ""locked_at"":""" + now.ToString(Constants.Iso8601FormatZ) + "\"}]",
                string.Empty,
                "2 lock(s) matched query.",
                null
            };

            var expected = new[] {
                new GitLock(12, "folder/somefile.png".ToNPath(), new GitUser("GitHub User", ""), now),
                new GitLock(21, "somezip.zip".ToNPath(), new GitUser("GitHub User", ""), now)
            };


         AssertProcessOutput(output, expected);
        }
    }
}
