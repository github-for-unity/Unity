using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using NUnit.Framework;
using GitHub.Unity;
using TestUtils;

namespace IntegrationTests
{
    [TestFixture]
    class ProcessManagerIntegrationTests : BaseGitEnvironmentTest
    {
        [Test]
        public void BranchListTest()
        {
            InitializeEnvironment(TestRepoMasterClean);

            var gitBranches = ProcessManager.GetGitBranches(TestRepoMasterDirty);

            gitBranches.Should().BeEquivalentTo(
                new GitBranch("master", "origin/master: behind 1", true),
                new GitBranch("feature/document", "origin/feature/document", false));
        }

        [Test]
        public void LogEntriesTest()
        {
            InitializeEnvironment(TestRepoMasterClean);

            var logEntries =
                ProcessManager.GetGitLogEntries(TestRepoMasterClean, Environment, FileSystem, GitEnvironment, 2)
                    .ToArray();

            logEntries.AssertEqual(new[]
            {
                new GitLogEntry
                {
                    AuthorEmail = "author@example.com",
                    CommitEmail = "author@example.com",
                    AuthorName = "Author Person",
                    CommitName = "Author Person",
                    Changes = new List<GitStatusEntry>
                    {
                        new GitStatusEntry("Assets/TestDocument.txt".ToNPath(),
                            TestRepoMasterClean + "/Assets/TestDocument.txt".ToNPath(), "Assets/TestDocument.txt".ToNPath(),
                            GitFileStatus.Renamed, "TestDocument.txt")
                    },
                    CommitID = "018997938335742f8be694240a7c2b352ec0835f",
                    Description = "Moving project files where they should be kept",
                    Summary = "Moving project files where they should be kept",
                    Time = new DateTimeOffset(2017, 1, 27, 17, 19, 32, TimeSpan.FromHours(-5)),
                    CommitTime = new DateTimeOffset(2017, 1, 27, 17, 19, 32, TimeSpan.FromHours(-5)),
                },
                new GitLogEntry
                {
                    AuthorEmail = "author@example.com",
                    CommitEmail = "author@example.com",
                    AuthorName = "Author Person",
                    CommitName = "Author Person",
                    Changes = new List<GitStatusEntry>
                    {
                        new GitStatusEntry("TestDocument.txt".ToNPath(),
                            TestRepoMasterClean + "/TestDocument.txt".ToNPath(), "TestDocument.txt".ToNPath(),
                            GitFileStatus.Added),
                    },
                    CommitID = "03939ffb3eb8486dba0259b43db00842bbe6eca1",
                    Description = "Initial Commit",
                    Summary = "Initial Commit",
                    Time = new DateTimeOffset(2017, 1, 17, 11, 46, 16, TimeSpan.FromHours(-8)),
                    CommitTime = new DateTimeOffset(2017, 1, 17, 11, 46, 16, TimeSpan.FromHours(-8)),
                },
            });
        }

        [Test]
        public void RemoteListTest()
        {
            InitializeEnvironment(TestRepoMasterClean);

            var gitRemotes = ProcessManager.GetGitRemoteEntries(TestRepoMasterDirty);

            gitRemotes.Should().BeEquivalentTo(new GitRemote()
            {
                Name = "origin",
                Url = "https://github.com/EvilStanleyGoldman/IOTestsRepo.git",
                Host = "github.com",
                Function = GitRemoteFunction.Both
            });
        }

        [Test]
        public void StatusTest()
        {
            InitializeEnvironment(TestRepoMasterDirty);

            var gitStatus = ProcessManager.GetGitStatus(TestRepoMasterDirty, Environment, FileSystem, GitEnvironment);

            gitStatus.AssertEqual(new GitStatus()
            {
                LocalBranch = "master",
                RemoteBranch = "origin/master",
                Behind = 1,
                Entries = new List<GitStatusEntry>
                {
                    new GitStatusEntry("Assets/Added Document.txt".ToNPath(),
                        TestRepoMasterDirty.Combine("Assets/Added Document.txt"),
                        "Assets/Added Document.txt".ToNPath(),
                        GitFileStatus.Added, staged: true),

                    new GitStatusEntry("Assets/Renamed TestDocument.txt".ToNPath(),
                        TestRepoMasterDirty.Combine("Assets/Renamed TestDocument.txt"),
                        "Assets/Renamed TestDocument.txt".ToNPath(),
                        GitFileStatus.Renamed, "Assets/TestDocument.txt".ToNPath(), true),

                    new GitStatusEntry("Assets/Untracked Document.txt".ToNPath(),
                        TestRepoMasterDirty.Combine("Assets/Untracked Document.txt"),
                        "Assets/Untracked Document.txt".ToNPath(),
                        GitFileStatus.Untracked),
                }
            });
        }

        [Test]
        public void CredentialHelperGetTest()
        {
            InitializeEnvironment(TestRepoMasterClean);

            var s = ProcessManager.GetGitCreds(TestRepoMasterDirty, Environment, FileSystem, GitEnvironment);
            s.Should().NotBeNull();
        }
    }
}
