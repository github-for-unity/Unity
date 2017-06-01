using System;
using System.Collections.Generic;
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
            Initialize(TestRepoMasterCleanUnsynchronized);

            IEnumerable<GitBranch> gitBranches = null;
            Assert.DoesNotThrow(async () => gitBranches = await ProcessManager
                .GetGitBranches(TestRepoMasterCleanUnsynchronized)
                .Start().Task);

            gitBranches.Should().BeEquivalentTo(
                new GitBranch("master", "origin/master: behind 1", true),
                new GitBranch("feature/document", "origin/feature/document", false));
        }

        [Test]
        public void LogEntriesTest()
        {
            Initialize(TestRepoMasterCleanUnsynchronized);

            List<GitLogEntry> logEntries = null;
            Assert.DoesNotThrow(async () => logEntries = await ProcessManager
                .GetGitLogEntries(TestRepoMasterCleanUnsynchronized, Environment, GitEnvironment, 2)
                .Start().Task);

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
            Initialize(TestRepoMasterCleanUnsynchronizedRussianLanguage);

            List<GitLogEntry> logEntries = null;
            Assert.DoesNotThrow(async () => logEntries = await ProcessManager
                .GetGitLogEntries(TestRepoMasterCleanUnsynchronizedRussianLanguage, Environment, GitEnvironment, 1)
                .Start().Task);

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
            Initialize(TestRepoMasterCleanSynchronized);

            List<GitRemote> gitRemotes = null;
            Assert.DoesNotThrow(async () => gitRemotes = await ProcessManager
                .GetGitRemoteEntries(TestRepoMasterCleanSynchronized)
                .Start().Task);

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
            Initialize(TestRepoMasterDirtyUnsynchronized);

            GitStatus? gitStatus = null;
            Assert.DoesNotThrow(async () => gitStatus = await ProcessManager
                .GetGitStatus(TestRepoMasterDirtyUnsynchronized, Environment, GitEnvironment)
                .Start().Task);

            gitStatus.Value.AssertEqual(new GitStatus()
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
            Initialize(TestRepoMasterCleanSynchronized);

            string s = null;
            Assert.DoesNotThrow(async () => s = await ProcessManager
                .GetGitCreds(TestRepoMasterCleanSynchronized, Environment, GitEnvironment)
                .Start().Task);
            s.Should().NotBeNull();
        }
    }
}
