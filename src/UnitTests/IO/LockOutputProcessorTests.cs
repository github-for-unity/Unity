using TestUtils;
using System.Collections.Generic;
using NUnit.Framework;
using GitHub.Unity;

namespace UnitTests
{
    [TestFixture]
    class LockOutputProcessorTests : BaseOutputProcessorTests
    {
        private void AssertProcessOutput(IEnumerable<string> lines, GitLock[] expected)
        {
            var gitObjectFactory = SubstituteFactory.CreateGitObjectFactory(TestRootPath);

            var results = new List<GitLock>();
            var outputProcessor = new LockOutputProcessor(gitObjectFactory);
            outputProcessor.OnGitLock += gitLock => { results.Add(gitLock); };

            foreach (var line in lines)
            {
                outputProcessor.LineReceived(line);
            }

            results.AssertEqual(expected);
        }

        [Test]
        public void ShouldParseZeroLocks()
        {
            var output = new[] {
                null,
                "0 lock(s) matched query."
            };

            var expected = new GitLock[0];

            AssertProcessOutput(output, expected);
        }

        [Test, Ignore]
        public void GitLFSLocksFormat1()
        {
            var output = new[]
            {
                "test/foobar.txt\tsomeone",
                "asdf.txt\tsomeoneElse",
                string.Empty,
                "2 lock (s) matched query.",
                null
            };

            AssertProcessOutput(output, null);
        }

        [Test, Ignore]
        public void GitLFSLocksFormat2()
        {
            var output = new[]
            {
                "test/foobar.txt\tsomeone\tID:320",
                "asdf.txt\tsomeoneElse\tID:321",
                null
            };

            AssertProcessOutput(output, null);
        }

        [Test]
        public void ShouldParseTwoLocks()
        {
            var output = new[] {
                "folder/somefile.png    GitHub User 12 <>",
                "somezip.zip GitHub User 21 <>",
                null,
                "2 lock(s) matched query."
            };

            var expected = new[] {
                new GitLock("folder/somefile.png", TestRootPath + @"\folder/somefile.png", "GitHub User 12"),
                new GitLock("somezip.zip", TestRootPath + @"\somezip.zip", "GitHub User 21")
            };

            AssertProcessOutput(output, expected);
        }
    }
}
