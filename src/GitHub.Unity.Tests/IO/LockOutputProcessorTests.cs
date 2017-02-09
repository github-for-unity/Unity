using System.Collections.Generic;
using NUnit.Framework;

namespace GitHub.Unity.Tests
{
    [TestFixture]
    public class LockOutputProcessorTests : BaseOutputProcessorTests
    {
        private void AssertProcessOutput(IEnumerable<string> lines, GitLock[] expected)
        {
            var gitStatusEntryFactory = CreateGitStatusEntryFactory();

            var results = new List<GitLock>();
            var outputProcessor = new LockOutputProcessor(gitStatusEntryFactory);
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
