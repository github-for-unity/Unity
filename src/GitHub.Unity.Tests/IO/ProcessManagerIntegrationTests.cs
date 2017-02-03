using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;

namespace GitHub.Unity.Tests
{
    [TestFixture]
    class ProcessManagerIntegrationTests : BaseIntegrationTest
    {
        [Test]
        public void BranchListTest()
        {
            var fileSystem = new FileSystem();

            var environment = new DefaultEnvironment();
            var gitEnvironment = environment.IsWindows
                ? new WindowsGitEnvironment(fileSystem, environment)
                : (environment.IsLinux 
                    ? (IGitEnvironment)new LinuxBasedGitEnvironment(fileSystem, environment)
                    : new MacBasedGitEnvironment(fileSystem, environment));

            var processManager = new ProcessManager(environment, gitEnvironment, fileSystem);
            var gitBranches = processManager.GetGitBranches(TestGitRepoPath);

            gitBranches.Should().BeEquivalentTo(
                new GitBranch("master", string.Empty, true),
                new GitBranch("feature/document", string.Empty, false));
        }

        [Test]
        public void LogEntriesTest()
        {
            var fileSystem = new FileSystem();

            var defaultEnvironment = new DefaultEnvironment();

            var environment = Substitute.For<IEnvironment>();
            environment.UnityProjectPath.Returns(TestGitRepoPath);

            var gitEnvironment = defaultEnvironment.IsWindows
                ? (IGitEnvironment) new WindowsGitEnvironment(fileSystem, environment)
                : new LinuxBasedGitEnvironment(fileSystem, environment);

            var processManager = new ProcessManager(environment, gitEnvironment, fileSystem);
            var logEntries =
                processManager.GetGitLogEntries(TestGitRepoPath, environment, fileSystem, gitEnvironment, 2)
                    .ToArray();

            logEntries.AssertEqual(new[]
            {
                new GitLogEntry
                {
                    AuthorEmail = "Stanley.Goldman@gmail.com",
                    CommitEmail = "Stanley.Goldman@gmail.com",
                    AuthorName = "Stanley Goldman",
                    CommitName = "Stanley Goldman",
                    Changes = new List<GitStatusEntry>
                    {
                        new GitStatusEntry("Assets/TestDocument.txt",
                            TestGitRepoPath + "Assets/TestDocument.txt", null,
                            GitFileStatus.Renamed, "TestDocument.txt"),
                    },
                    CommitID = "1ec2dd47eb6b00a5ef31da9eb13de04de57cbe9f",
                    Description = "Moving project files where they should be kept",
                    Summary = "Moving project files where they should be kept",
                    Time = new DateTimeOffset(2017, 1, 27, 17, 19, 32, TimeSpan.FromHours(-5)),
                    CommitTime = new DateTimeOffset(2017, 1, 27, 17, 19, 32, TimeSpan.FromHours(-5)),
                },
                new GitLogEntry
                {
                    AuthorEmail = "Stanley.Goldman@gmail.com",
                    CommitEmail = "Stanley.Goldman@gmail.com",
                    AuthorName = "Stanley Goldman",
                    CommitName = "Stanley Goldman",
                    Changes = new List<GitStatusEntry>
                    {
                        new GitStatusEntry("TestDocument.txt",
                            TestGitRepoPath + "TestDocument.txt", null,
                            GitFileStatus.Added),
                    },
                    CommitID = "22712461191fde4b69fcc4f731715f57fcbb31bc",
                    Description = "Initial Commit",
                    Summary = "Initial Commit",
                    Time = new DateTimeOffset(2017, 1, 17, 11, 46, 16, TimeSpan.FromHours(-8)),
                    CommitTime = new DateTimeOffset(2017, 1, 17, 11, 46, 16, TimeSpan.FromHours(-8)),
                },
            });
        }

        [Test]
        public void StatusTest()
        {
            var fileSystem = new FileSystem();

            var environment = Substitute.For<IEnvironment>();
            environment.UnityProjectPath.Returns(TestGitRepoPath);

            var gitEnvironment = environment.IsWindows
                ? (IGitEnvironment) new WindowsGitEnvironment(fileSystem, environment)
                : new LinuxBasedGitEnvironment(fileSystem, environment);

            var processManager = new ProcessManager(environment, gitEnvironment, fileSystem);
            var gitStatus = processManager.GetGitStatus(TestGitRepoPath, environment, fileSystem, gitEnvironment);

            gitStatus.AssertEqual(new GitStatus()
            {
                LocalBranch = "master",
                Entries = new List<GitStatusEntry>
                {
                    new GitStatusEntry("Assets/Added Document.txt",
                        TestGitRepoPath + @"Assets/Added Document.txt", null,
                        GitFileStatus.Added),

                    new GitStatusEntry("Assets/Renamed TestDocument.txt",
                        TestGitRepoPath + @"Assets/Renamed TestDocument.txt", null,
                        GitFileStatus.Renamed, "Assets/TestDocument.txt"),

                    new GitStatusEntry("Assets/Untracked Document.txt",
                        TestGitRepoPath + @"Assets/Untracked Document.txt", null,
                        GitFileStatus.Untracked),
                }
            });
        }
    }
}
