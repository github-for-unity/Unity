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
    class LocksTests : BaseOutputProcessorTests
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
        public void ShouldParseTwoLocksFormat()
        {
            var now = DateTimeOffset.ParseExact(DateTimeOffset.UtcNow.ToString(Constants.Iso8601FormatZ), Constants.Iso8601Formats, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);
            var output = new[]
            {
                @"[{""id"":""12"", ""path"":""folder/somefile.png"", ""owner"":{""name"":""GitHub User""}, ""locked_at"":""" + now.ToString(Constants.Iso8601FormatZ) + "\"}" +
                @" ,{""id"":""2f9cfde9c159d50e235cc1402c3e534b0bf2198afb20760697a5f9b07bf04fb3"", ""path"":""somezip.zip"", ""owner"":{""name"":""GitHub User""}, ""locked_at"":""" + now.ToString(Constants.Iso8601FormatZ) + "\"}]",
                string.Empty,
                "2 lock(s) matched query.",
                null
            };

            var expected = new[] {
                new GitLock("12", "folder/somefile.png".ToNPath(), new GitUser("GitHub User", ""), now),
                new GitLock("2f9cfde9c159d50e235cc1402c3e534b0bf2198afb20760697a5f9b07bf04fb3", "somezip.zip".ToNPath(), new GitUser("GitHub User", ""), now)
            };

            AssertProcessOutput(output, expected);
        }

        [Test]
        public void ShouldParseVSTSLocksFormat()
        {
            var nowString = DateTimeOffset.UtcNow.ToString(Constants.Iso8601FormatPointZ);
            var now = DateTimeOffset.ParseExact(nowString, Constants.Iso8601Formats, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);
            var output = new[]
            {
                $@"[{{""id"":""7""   ,""path"":""Assets/Main.unity"",""owner"":{{""name"":""GitHub User""}},""locked_at"":""{nowString}""}}]",
                string.Empty,
                "1 lock(s) matched query.",
                null
            };

            var expected = new[] {
                new GitLock("7", "Assets/Main.unity".ToNPath(), new GitUser("GitHub User", ""), now),
            };

            AssertProcessOutput(output, expected);
        }

        [Test]
        public void GitLockComparisons()
        {
            GitLock lock1 = GitLock.Default;
            GitLock lock2 = GitLock.Default;
            Assert.IsTrue(lock1.Equals(lock2));
            Assert.IsTrue(lock1 == lock2);
            // these are the defaults
            lock1 = new GitLock(null, NPath.Default, GitUser.Default, DateTimeOffset.MinValue);
            lock2 = new GitLock();
            Assert.IsTrue(lock1.Equals(lock2));
            Assert.IsTrue(lock1 == lock2);
        }
    }
}
