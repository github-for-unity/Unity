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
            InitializeEnvironment(TestRepoMasterCleanUnsynchronized);

            var gitBranches = ProcessManager.GetGitBranches(TestRepoMasterCleanUnsynchronized).Result;

            gitBranches.Should().BeEquivalentTo(
                new GitBranch("master", "origin/master: behind 1", true),
                new GitBranch("feature/document", "origin/feature/document", false));
        }

        [Test]
        public void LogEntriesTest()
        {
            InitializeEnvironment(TestRepoMasterCleanUnsynchronized);

            var logEntries =
                ProcessManager
                    .GetGitLogEntries(TestRepoMasterCleanUnsynchronized, Environment, FileSystem, GitEnvironment, 2)
                    .Result
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
                            TestRepoMasterCleanUnsynchronized + "/Assets/TestDocument.txt".ToNPath(), "Assets/TestDocument.txt".ToNPath(),
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
                            TestRepoMasterCleanUnsynchronized + "/TestDocument.txt".ToNPath(), "TestDocument.txt".ToNPath(),
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

        [Test, Category("DoNotRunOnAppVeyor")]
        public void RussianLogEntriesTest()
        {
            InitializeEnvironment(TestRepoMasterCleanUnsynchronizedRussianLanguage);

            var logEntries =
                ProcessManager
                    .GetGitLogEntries(TestRepoMasterCleanUnsynchronizedRussianLanguage, Environment, FileSystem, GitEnvironment, 1)
                    .Result
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
                        new GitStatusEntry(@"Assets\A new file.txt".ToNPath(),
                            TestRepoMasterCleanUnsynchronizedRussianLanguage + "/Assets/A new file.txt".ToNPath(), "Assets/A new file.txt".ToNPath(),
                            GitFileStatus.Added),
                    },
                    CommitID = "06d6451d351626894a30e9134f551db12c74254b",
                    Description = "Я люблю github",
                    Summary = "Я люблю github",
                    Time = new DateTimeOffset(2017, 4, 20, 11, 47, 18, TimeSpan.FromHours(-4)),
                    CommitTime = new DateTimeOffset(2017, 4, 20, 11, 47, 18, TimeSpan.FromHours(-4)),
                }
            });
        }

        [Test]
        public void RemoteListTest()
        {
            InitializeEnvironment(TestRepoMasterCleanSynchronized);

            var gitRemotes = ProcessManager.GetGitRemoteEntries(TestRepoMasterCleanSynchronized).Result;

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
            InitializeEnvironment(TestRepoMasterDirtyUnsynchronized);

            var gitStatus = ProcessManager
                .GetGitStatus(TestRepoMasterDirtyUnsynchronized, Environment, FileSystem, GitEnvironment)
                .Result;

            gitStatus.AssertEqual(new GitStatus()
            {
                LocalBranch = "master",
                RemoteBranch = "origin/master",
                Behind = 1,
                Entries = new List<GitStatusEntry>
                {
                    new GitStatusEntry("Assets/Added Document.txt".ToNPath(),
                        TestRepoMasterDirtyUnsynchronized.Combine("Assets/Added Document.txt"),
                        "Assets/Added Document.txt".ToNPath(),
                        GitFileStatus.Added, staged: true),

                    new GitStatusEntry("Assets/Renamed TestDocument.txt".ToNPath(),
                        TestRepoMasterDirtyUnsynchronized.Combine("Assets/Renamed TestDocument.txt"),
                        "Assets/Renamed TestDocument.txt".ToNPath(),
                        GitFileStatus.Renamed, "Assets/TestDocument.txt".ToNPath(), true),

                    new GitStatusEntry("Assets/Untracked Document.txt".ToNPath(),
                        TestRepoMasterDirtyUnsynchronized.Combine("Assets/Untracked Document.txt"),
                        "Assets/Untracked Document.txt".ToNPath(),
                        GitFileStatus.Untracked),
                }
            });
        }

        [Test]
        public void CredentialHelperGetTest()
        {
            InitializeEnvironment(TestRepoMasterCleanSynchronized);

            var s = ProcessManager.GetGitCreds(TestRepoMasterCleanSynchronized, Environment, FileSystem, GitEnvironment);
            s.Should().NotBeNull();
        }
    }
}
