using System;
using System.Collections.Generic;
using System.Globalization;
using FluentAssertions;
using NUnit.Framework;
using GitHub.Unity;
using TestUtils;
using System.Threading.Tasks;

namespace IntegrationTests
{
    [TestFixture]
    class ProcessManagerIntegrationTests : BaseGitEnvironmentTest
    {
        [Test]
        [Category("DoNotRunOnAppVeyor")]
        public async Task BranchListTest()
        {
            InitializePlatformAndEnvironment(TestRepoMasterCleanUnsynchronized);

            IEnumerable<GitBranch> gitBranches = null;
            gitBranches = await ProcessManager
                .GetGitBranches(TestRepoMasterCleanUnsynchronized)
                .StartAsAsync();

            gitBranches.Should().BeEquivalentTo(
                new GitBranch("master", "origin/master"),
                new GitBranch("feature/document", "origin/feature/document"));
        }

        [Test]
        public async Task LogEntriesTest()
        {
            InitializePlatformAndEnvironment(TestRepoMasterCleanUnsynchronized);

            List<GitLogEntry> logEntries = null;
            logEntries = await ProcessManager
                .GetGitLogEntries(TestRepoMasterCleanUnsynchronized, Environment, 2)
                .StartAsAsync();

            var firstCommitTime = new DateTimeOffset(2017, 1, 27, 17, 19, 32, TimeSpan.FromHours(-5));
            var secondCommitTime = new DateTimeOffset(2017, 1, 17, 11, 46, 16, TimeSpan.FromHours(-8));

            logEntries.AssertEqual(new[]
            {
                new GitLogEntry("018997938335742f8be694240a7c2b352ec0835f", 
                    "Author Person", "author@example.com", "Author Person", 
                    "author@example.com", 
                    "Moving project files where they should be kept",
                    "",
                    firstCommitTime,
                    firstCommitTime, new List<GitStatusEntry>
                    {
                        new GitStatusEntry("Assets/TestDocument.txt".ToNPath(),
                            TestRepoMasterCleanUnsynchronized + "/Assets/TestDocument.txt".ToNPath(), "Assets/TestDocument.txt".ToNPath(),
                            GitFileStatus.Renamed, GitFileStatus.None, "TestDocument.txt")
                    }),

                new GitLogEntry("03939ffb3eb8486dba0259b43db00842bbe6eca1", 
                    "Author Person", "author@example.com", "Author Person",
                    "author@example.com",
                    "Initial Commit",
                    "",
                    secondCommitTime,
                    secondCommitTime, new List<GitStatusEntry>
                    {
                        new GitStatusEntry("TestDocument.txt".ToNPath(),
                            TestRepoMasterCleanUnsynchronized + "/TestDocument.txt".ToNPath(), "TestDocument.txt".ToNPath(),
                            GitFileStatus.Added, GitFileStatus.None),
                    }),
            });
        }

        [Test]
        public async Task RussianLogEntriesTest()
        {
            InitializePlatformAndEnvironment(TestRepoMasterCleanUnsynchronizedRussianLanguage);

            List<GitLogEntry> logEntries = null;
            logEntries = await ProcessManager
                .GetGitLogEntries(TestRepoMasterCleanUnsynchronizedRussianLanguage, Environment, 1)
                .StartAsAsync();

            var commitTime = new DateTimeOffset(2017, 4, 20, 11, 47, 18, TimeSpan.FromHours(-4));

            logEntries.AssertEqual(new[]
            {
                new GitLogEntry("06d6451d351626894a30e9134f551db12c74254b",
                    "Author Person", "author@example.com", "Author Person",
                    "author@example.com",
                    "Я люблю github",
                    "",
                    commitTime,
                    commitTime, new List<GitStatusEntry>
                    {
                        new GitStatusEntry(@"Assets\A new file.txt".ToNPath(),
                            TestRepoMasterCleanUnsynchronizedRussianLanguage + "/Assets/A new file.txt".ToNPath(), "Assets/A new file.txt".ToNPath(),
                            GitFileStatus.Added, GitFileStatus.None),
                    }),
            });
        }

        [Test]
        public async Task RemoteListTest()
        {
            InitializePlatformAndEnvironment(TestRepoMasterCleanSynchronized);

            List<GitRemote> gitRemotes = null;
            gitRemotes = await ProcessManager
                .GetGitRemoteEntries(TestRepoMasterCleanSynchronized)
                .StartAsAsync();

            gitRemotes.Should().BeEquivalentTo(new GitRemote("origin", "github.com", "https://github.com/EvilStanleyGoldman/IOTestsRepo.git", GitRemoteFunction.Both));
        }

        [Test]
        public async Task StatusTest()
        {
            InitializePlatformAndEnvironment(TestRepoMasterDirtyUnsynchronized);

            GitStatus? gitStatus = null;
            gitStatus = await ProcessManager
                .GetGitStatus(TestRepoMasterDirtyUnsynchronized, Environment)
                .StartAsAsync();

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
                        GitFileStatus.Added, GitFileStatus.None),

                    new GitStatusEntry("Assets/Renamed TestDocument.txt".ToNPath(),
                        TestRepoMasterDirtyUnsynchronized.Combine("Assets/Renamed TestDocument.txt"),
                        "Assets/Renamed TestDocument.txt".ToNPath(),
                        GitFileStatus.Renamed,  GitFileStatus.None, "Assets/TestDocument.txt".ToNPath()),

                    new GitStatusEntry("Assets/Untracked Document.txt".ToNPath(),
                        TestRepoMasterDirtyUnsynchronized.Combine("Assets/Untracked Document.txt"),
                        "Assets/Untracked Document.txt".ToNPath(),
                        GitFileStatus.Untracked, GitFileStatus.Untracked),
                }
            });
        }

        [Test]
        public async Task CredentialHelperGetTest()
        {
            InitializePlatformAndEnvironment(TestRepoMasterCleanSynchronized);

            await ProcessManager
                .GetGitCreds(TestRepoMasterCleanSynchronized)
                .StartAsAsync();
        }
    }
}
