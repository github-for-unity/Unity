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
            outputProcessor.OnEntry += gitLock => { results.Add(gitLock); };

            foreach (var line in lines)
            {
                outputProcessor.LineReceived(line);
            }

            results.AssertEqual(expected);
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
            var output = new[]
            {
                "folder/somefile.png\tGitHub User\tID:12",
                "somezip.zip\tGitHub User\tID:21",
                string.Empty,
                "2 lock(s) matched query.",
                null
            };

            var expected = new[] {
                new GitLock {
                    Path = "folder/somefile.png",
                    FullPath = TestRootPath + @"\folder/somefile.png",
                    User = "GitHub User",
                    ID = 12
                },
                new GitLock {
                    Path = "somezip.zip",
                    FullPath = TestRootPath + @"\somezip.zip",
                    User = "GitHub User",
                    ID = 21
                }
            };

            AssertProcessOutput(output, expected);
        }

        [Test]
        public void ShouldParseTwoLocksFormat2()
        {
            var output = new[]
            {
                "folder/somefile.png\tGitHub User\tID:12",
                "somezip.zip\tGitHub User\tID:21",
                null
            };

            var expected = new[] {
                new GitLock {
                    Path = "folder/somefile.png",
                    FullPath = TestRootPath + @"\folder/somefile.png",
                    User = "GitHub User",
                    ID = 12
                },
                new GitLock {
                    Path = "somezip.zip",
                    FullPath = TestRootPath + @"\somezip.zip",
                    User = "GitHub User",
                    ID = 21
                }
            };

            AssertProcessOutput(output, expected);
        }

        [Test]
        public void ShouldParseLocksOnFileWithNumericFirstLetter()
        {
            var output = new[]
            {
                "2_TurtleDoves.jpg\tTree\tID:100",
            };

            var expected = new[] {
                new GitLock {
                    Path = "2_TurtleDoves.jpg",
                    FullPath = TestRootPath + @"\2_TurtleDoves.jpg",
                    User = "Tree",
                    ID = 100
                }
            };

            AssertProcessOutput(output, expected);
        }
    }
}
