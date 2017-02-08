using System.Collections.Generic;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace GitHub.Unity.Tests
{
    [TestFixture]
    public class StatusOutputProcessorTests : BaseOutputProcessorTests
    {
        //[Test]
        public void IntegrationTest()
        {
            var fs = new FileSystem();
            var env = new DefaultEnvironment();
            env.UnityProjectPath = @"D:\code\github\UnityInternal\src\UnityExtension";
            var genv = new WindowsGitEnvironment(fs, env);
            var fact = new GitStatusEntryFactory(env, fs, genv);
            var pm = new ProcessManager(env, genv, fs);
            var results = pm.GetGitStatus(@"D:\code\github\UnityInternal", env, fs, genv);
        }

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
                    new GitStatusEntry("GitHubVS.sln", TestRootPath + @"\GitHubVS.sln", null, GitFileStatus.Modified),
                    new GitStatusEntry("README2.md", TestRootPath + @"\README2.md", null, GitFileStatus.Renamed, "README.md", true),
                    new GitStatusEntry("deploy.cmd", TestRootPath + @"\deploy.cmd", null, GitFileStatus.Deleted),
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
                    new GitStatusEntry("GitHubVS.sln", TestRootPath + @"\GitHubVS.sln", null, GitFileStatus.Modified),
                    new GitStatusEntry("README2.md", TestRootPath + @"\README2.md", null, GitFileStatus.Renamed, "README.md", true),
                    new GitStatusEntry("deploy.cmd", TestRootPath + @"\deploy.cmd", null, GitFileStatus.Deleted),
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
                    new GitStatusEntry("GitHubVS.sln", TestRootPath + @"\GitHubVS.sln", null, GitFileStatus.Modified),
                    new GitStatusEntry("README2.md", TestRootPath + @"\README2.md", null, GitFileStatus.Renamed, "README.md", true),
                    new GitStatusEntry("deploy.cmd", TestRootPath + @"\deploy.cmd", null, GitFileStatus.Deleted),
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
                    new GitStatusEntry("GitHubVS.sln", TestRootPath + @"\GitHubVS.sln", null, GitFileStatus.Modified),
                    new GitStatusEntry("README2.md", TestRootPath + @"\README2.md", null, GitFileStatus.Renamed, "README.md", true),
                    new GitStatusEntry("deploy.cmd", TestRootPath + @"\deploy.cmd", null, GitFileStatus.Deleted),
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
                    new GitStatusEntry("GitHubVS.sln", TestRootPath + @"\GitHubVS.sln", null, GitFileStatus.Modified),
                    new GitStatusEntry("README2.md", TestRootPath + @"\README2.md", null, GitFileStatus.Renamed, "README.md", true),
                    new GitStatusEntry("deploy.cmd", TestRootPath + @"\deploy.cmd", null, GitFileStatus.Deleted),
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
                LocalBranch = "something"
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
                Behind = 1
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
                Ahead = 1
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
                Behind = 1
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
                RemoteBranch = "origin/master"
            });
        }

        private void AssertProcessOutput(IEnumerable<string> lines, GitStatus expected)
        {
            var gitStatusEntryFactory = CreateGitStatusEntryFactory();

            var result = new GitStatus();
            var outputProcessor = new StatusOutputProcessor(gitStatusEntryFactory);
            outputProcessor.OnStatus += status => { result = status; };

            foreach (var line in lines)
            {
                outputProcessor.LineReceived(line);
            }

            result.AssertEqual(expected);
        }
    }
}