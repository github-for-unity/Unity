using System.Collections.Generic;
using GitHub.Unity;
using NUnit.Framework;

namespace UnitTests
{
    [TestFixture]
    class TrackedFilesOutputProcessorTests : BaseOutputProcessorTests
    {
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

            AssertProcessOutput(output, new GitStatus
            {
                LocalBranch = "master",
                Entries = new List<GitStatusEntry>
                {
                }
            });
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

            AssertProcessOutput(output, new GitStatus
            {
                LocalBranch = "master",
                Entries = new List<GitStatusEntry>
                {
                }
            });
        }

        private void AssertProcessOutput(IEnumerable<string> lines, GitStatus expected)
        {
            var gitObjectFactory = CreateGitObjectFactory();

            var result = new GitStatus();
            var outputProcessor = new StatusOutputProcessor(gitObjectFactory);
            outputProcessor.OnStatus += status => { result = status; };

            foreach (var line in lines)
            {
                outputProcessor.LineReceived(line);
            }

            result.AssertEqual(expected);
        }
    }
}