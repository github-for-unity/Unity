using TestUtils;
using System.Collections.Generic;
using NUnit.Framework;
using GitHub.Unity;

namespace UnitTests
{
    [TestFixture]
    class GitStatusOutputProcessorTests : BaseOutputProcessorTests
    {

        [Test]
        public void ShouldParseDirtyWorkingTreeUntracked()
        {
            var output = new[]
            {
                "## master",
                " M GitHubVS.sln",
                "R  README.md -> README2.md",
                " D deploy.cmd",
                @"A  ""something added.txt""",
                "?? something.txt",
                null
            };

            AssertProcessOutput(output, new GitStatus
            {
                LocalBranch = "master",
                Entries = new List<GitStatusEntry>
                {
                    new GitStatusEntry("deploy.cmd", TestRootPath + @"\deploy.cmd", null, GitFileStatus.Deleted),
                    new GitStatusEntry("GitHubVS.sln", TestRootPath + @"\GitHubVS.sln", null, GitFileStatus.Modified),
                    new GitStatusEntry("README2.md", TestRootPath + @"\README2.md", null, GitFileStatus.Renamed, "README.md", true),
                    new GitStatusEntry("something added.txt", TestRootPath + @"\something added.txt", null, GitFileStatus.Added, staged: true),
                    new GitStatusEntry("something.txt", TestRootPath + @"\something.txt", null, GitFileStatus.Untracked),
                }
            });
        }

        [Test]
        public void ShouldParseDirtyWorkingTreeTrackedAhead1Behind1()
        {
            var output = new[]
            {
                "## master...origin/master [ahead 1, behind 1]",
                " M GitHubVS.sln",
                "R  README.md -> README2.md",
                " D deploy.cmd",
                @"A  ""something added.txt""",
                "?? something.txt",
                null
            };

            AssertProcessOutput(output, new GitStatus
            {
                LocalBranch = "master",
                RemoteBranch = "origin/master",
                Ahead = 1,
                Behind = 1,
                Entries = new List<GitStatusEntry>
                {
                    new GitStatusEntry("deploy.cmd", TestRootPath + @"\deploy.cmd", null, GitFileStatus.Deleted),
                    new GitStatusEntry("GitHubVS.sln", TestRootPath + @"\GitHubVS.sln", null, GitFileStatus.Modified),
                    new GitStatusEntry("README2.md", TestRootPath + @"\README2.md", null, GitFileStatus.Renamed, "README.md", true),
                    new GitStatusEntry("something added.txt", TestRootPath + @"\something added.txt", null, GitFileStatus.Added, staged: true),
                    new GitStatusEntry("something.txt", TestRootPath + @"\something.txt", null, GitFileStatus.Untracked),
                }
            });
        }

        [Test]
        public void ShouldParseDirtyWorkingTreeTrackedAhead1()
        {
            var output = new[]
            {
                "## master...origin/master [ahead 1]",
                " M GitHubVS.sln",
                "R  README.md -> README2.md",
                " D deploy.cmd",
                @"A  ""something added.txt""",
                "?? something.txt",
                null
            };

            AssertProcessOutput(output, new GitStatus
            {
                LocalBranch = "master",
                RemoteBranch = "origin/master",
                Ahead = 1,
                Entries = new List<GitStatusEntry>
                {
                    new GitStatusEntry("deploy.cmd", TestRootPath + @"\deploy.cmd", null, GitFileStatus.Deleted),
                    new GitStatusEntry("GitHubVS.sln", TestRootPath + @"\GitHubVS.sln", null, GitFileStatus.Modified),
                    new GitStatusEntry("README2.md", TestRootPath + @"\README2.md", null, GitFileStatus.Renamed, "README.md", true),
                    new GitStatusEntry("something added.txt", TestRootPath + @"\something added.txt", null, GitFileStatus.Added, staged: true),
                    new GitStatusEntry("something.txt", TestRootPath + @"\something.txt", null, GitFileStatus.Untracked),
                }
            });
        }

        [Test]
        public void ShouldParseDirtyWorkingTreeTrackedBehind1()
        {
            var output = new[]
            {
                "## master...origin/master [behind 1]",
                " M GitHubVS.sln",
                "R  README.md -> README2.md",
                " D deploy.cmd",
                @"A  ""something added.txt""",
                "?? something.txt",
                null
            };

            AssertProcessOutput(output, new GitStatus
            {
                LocalBranch = "master",
                RemoteBranch = "origin/master",
                Behind = 1,
                Entries = new List<GitStatusEntry>
                {
                    new GitStatusEntry("deploy.cmd", TestRootPath + @"\deploy.cmd", null, GitFileStatus.Deleted),
                    new GitStatusEntry("GitHubVS.sln", TestRootPath + @"\GitHubVS.sln", null, GitFileStatus.Modified),
                    new GitStatusEntry("README2.md", TestRootPath + @"\README2.md", null, GitFileStatus.Renamed, "README.md", true),
                    new GitStatusEntry("something added.txt", TestRootPath + @"\something added.txt", null, GitFileStatus.Added, staged: true),
                    new GitStatusEntry("something.txt", TestRootPath + @"\something.txt", null, GitFileStatus.Untracked),
                }
            });
        }

        [Test]
        public void ShouldParseDirtyWorkingTreeTracked()
        {
            var output = new[]
            {
                "## master...origin/master",
                " M GitHubVS.sln",
                "R  README.md -> README2.md",
                " D deploy.cmd",
                @"A  ""something added.txt""",
                "?? something.txt",
                null
            };

            AssertProcessOutput(output, new GitStatus
            {
                LocalBranch = "master",
                RemoteBranch = "origin/master",
                Entries = new List<GitStatusEntry>
                {
                    new GitStatusEntry("deploy.cmd", TestRootPath + @"\deploy.cmd", null, GitFileStatus.Deleted),
                    new GitStatusEntry("GitHubVS.sln", TestRootPath + @"\GitHubVS.sln", null, GitFileStatus.Modified),
                    new GitStatusEntry("README2.md", TestRootPath + @"\README2.md", null, GitFileStatus.Renamed, "README.md", true),
                    new GitStatusEntry("something added.txt", TestRootPath + @"\something added.txt", null, GitFileStatus.Added, staged: true),
                    new GitStatusEntry("something.txt", TestRootPath + @"\something.txt", null, GitFileStatus.Untracked),
                }
            });
        }

        [Test]
        public void ShouldParseCleanWorkingTreeUntracked()
        {
            var output = new[]
            {
                "## something",
                null
            };

            AssertProcessOutput(output, new GitStatus
            {
                LocalBranch = "something",
                Entries = new List<GitStatusEntry>()
            });
        }

        [Test]
        public void ShouldParseCleanWorkingTreeTrackedAhead1Behind1()
        {
            var output = new[]
            {
                "## master...origin/master [ahead 1, behind 1]",
                null
            };

            AssertProcessOutput(output, new GitStatus
            {
                LocalBranch = "master",
                RemoteBranch = "origin/master",
                Ahead = 1,
                Behind = 1,
                Entries = new List<GitStatusEntry>()
            });
        }

        [Test]
        public void ShouldParseCleanWorkingTreeTrackedAhead1()
        {
            var output = new[]
            {
                "## master...origin/master [ahead 1]",
                null
            };

            AssertProcessOutput(output, new GitStatus
            {
                LocalBranch = "master",
                RemoteBranch = "origin/master",
                Ahead = 1,
                Entries = new List<GitStatusEntry>()
            });
        }

        [Test]
        public void ShouldParseCleanWorkingTreeTrackedBehind1()
        {
            var output = new[]
            {
                "## master...origin/master [behind 1]",
                null
            };

            AssertProcessOutput(output, new GitStatus
            {
                LocalBranch = "master",
                RemoteBranch = "origin/master",
                Behind = 1,
                Entries = new List<GitStatusEntry>()
            });
        }

        [Test]
        public void ShouldParseCleanWorkingTreeTracked()
        {
            var output = new[]
            {
                "## master...origin/master",
                null
            };

            AssertProcessOutput(output, new GitStatus
            {
                LocalBranch = "master",
                RemoteBranch = "origin/master",
                Entries = new List<GitStatusEntry>()
            });
        }

        [Test]
        public void ShouldSortOutputCorrectly()
        {
            var output = new[]
            {
                "## master",
                "?? GitHub.Unity.dll",
                "?? GitHub.Unity.dll.mdb",
                "?? GitHub.Unity.dll.mdb.meta",
                "?? GitHub.Unity.dll.meta",
                null
            };

            AssertProcessOutput(output, new GitStatus
            {
                LocalBranch = "master",
                Entries = new List<GitStatusEntry>
                {
                    new GitStatusEntry(@"GitHub.Unity.dll", TestRootPath + @"\GitHub.Unity.dll", null, GitFileStatus.Untracked),
                    new GitStatusEntry(@"GitHub.Unity.dll.meta", TestRootPath + @"\GitHub.Unity.dll.meta", null, GitFileStatus.Untracked),
                    new GitStatusEntry(@"GitHub.Unity.dll.mdb", TestRootPath + @"\GitHub.Unity.dll.mdb", null, GitFileStatus.Untracked),
                    new GitStatusEntry(@"GitHub.Unity.dll.mdb.meta", TestRootPath + @"\GitHub.Unity.dll.mdb.meta", null, GitFileStatus.Untracked),
                }
            });
        }

        [Test]
        public void ShouldSortOutputCorrectly2()
        {
            var output = new[]
            {
                "## master",
                "?? Assets/Assets.Test.dll",
                "?? Assets/Assets.Test.dll.meta",
                "?? Plugins/GitHub.Unity.dll",
                "?? Plugins/GitHub.Unity.dll.mdb",
                "?? Plugins/GitHub.Unity.dll.mdb.meta",
                "?? Plugins/GitHub.Unity.dll.meta",
                "?? blah.txt",
                null
            };

            AssertProcessOutput(output, new GitStatus
            {
                LocalBranch = "master",
                Entries = new List<GitStatusEntry>
                {
                    new GitStatusEntry(@"Assets/Assets.Test.dll", TestRootPath + @"\Assets/Assets.Test.dll", null, GitFileStatus.Untracked),
                    new GitStatusEntry(@"Assets/Assets.Test.dll.meta", TestRootPath + @"\Assets/Assets.Test.dll.meta", null, GitFileStatus.Untracked),
                    new GitStatusEntry(@"blah.txt", TestRootPath + @"\blah.txt", null, GitFileStatus.Untracked),
                    new GitStatusEntry(@"Plugins/GitHub.Unity.dll", TestRootPath + @"\Plugins/GitHub.Unity.dll", null, GitFileStatus.Untracked),
                    new GitStatusEntry(@"Plugins/GitHub.Unity.dll.meta", TestRootPath + @"\Plugins/GitHub.Unity.dll.meta", null, GitFileStatus.Untracked),
                    new GitStatusEntry(@"Plugins/GitHub.Unity.dll.mdb", TestRootPath + @"\Plugins/GitHub.Unity.dll.mdb", null, GitFileStatus.Untracked),
                    new GitStatusEntry(@"Plugins/GitHub.Unity.dll.mdb.meta", TestRootPath + @"\Plugins/GitHub.Unity.dll.mdb.meta", null, GitFileStatus.Untracked),
                }
            });
        }

        [Test]
        public void ShouldSortOutputCorrectly3()
        {
            var output = new[]
            {
                "## master",
                "?? Assets/Assets.Test.dll",
                "?? Assets/Assets.Test.dll.meta",
                "?? Plugins/GitHub.Unity.dll",
                "?? Plugins/GitHub.Unity.dll.mdb",
                "?? Plugins/GitHub.Unity.dll.mdb.meta",
                "?? Plugins/GitHub.Unity2.dll",
                "?? Plugins/GitHub.Unity2.dll.mdb",
                "?? Plugins/GitHub.Unity2.dll.mdb.meta",
                "?? Plugins/GitHub.Unity2.dll.meta",
                "?? Plugins/GitHub.Unity.dll.meta",
                "?? blah.txt",
                null
            };

            AssertProcessOutput(output, new GitStatus
            {
                LocalBranch = "master",
                Entries = new List<GitStatusEntry>
                {
                    new GitStatusEntry(@"Assets/Assets.Test.dll", TestRootPath + @"\Assets/Assets.Test.dll", null, GitFileStatus.Untracked),
                    new GitStatusEntry(@"Assets/Assets.Test.dll.meta", TestRootPath + @"\Assets/Assets.Test.dll.meta", null, GitFileStatus.Untracked),
                    new GitStatusEntry(@"blah.txt", TestRootPath + @"\blah.txt", null, GitFileStatus.Untracked),
                    new GitStatusEntry(@"Plugins/GitHub.Unity.dll", TestRootPath + @"\Plugins/GitHub.Unity.dll", null, GitFileStatus.Untracked),
                    new GitStatusEntry(@"Plugins/GitHub.Unity.dll.meta", TestRootPath + @"\Plugins/GitHub.Unity.dll.meta", null, GitFileStatus.Untracked),
                    new GitStatusEntry(@"Plugins/GitHub.Unity.dll.mdb", TestRootPath + @"\Plugins/GitHub.Unity.dll.mdb", null, GitFileStatus.Untracked),
                    new GitStatusEntry(@"Plugins/GitHub.Unity.dll.mdb.meta", TestRootPath + @"\Plugins/GitHub.Unity.dll.mdb.meta", null, GitFileStatus.Untracked),
                    new GitStatusEntry(@"Plugins/GitHub.Unity2.dll", TestRootPath + @"\Plugins/GitHub.Unity2.dll", null, GitFileStatus.Untracked),
                    new GitStatusEntry(@"Plugins/GitHub.Unity2.dll.meta", TestRootPath + @"\Plugins/GitHub.Unity2.dll.meta", null, GitFileStatus.Untracked),
                    new GitStatusEntry(@"Plugins/GitHub.Unity2.dll.mdb", TestRootPath + @"\Plugins/GitHub.Unity2.dll.mdb", null, GitFileStatus.Untracked),
                    new GitStatusEntry(@"Plugins/GitHub.Unity2.dll.mdb.meta", TestRootPath + @"\Plugins/GitHub.Unity2.dll.mdb.meta", null, GitFileStatus.Untracked),
                }
            });
        }

        private void AssertProcessOutput(IEnumerable<string> lines, GitStatus expected)
        {
            var gitObjectFactory = SubstituteFactory.CreateGitObjectFactory(TestRootPath);

            GitStatus? result = null;
            var outputProcessor = new GitStatusOutputProcessor(gitObjectFactory);
            outputProcessor.OnEntry += status => { result = status; };

            foreach (var line in lines)
            {
                outputProcessor.LineReceived(line);
            }

            Assert.IsTrue(result.HasValue);
            result.Value.AssertEqual(expected);
        }
    }
}
