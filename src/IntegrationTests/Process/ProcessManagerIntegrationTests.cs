using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using FluentAssertions;
using NUnit.Framework;
using GitHub.Unity;

namespace IntegrationTests
{
    [TestFixture]
    class ProcessManagerIntegrationTests : BaseGitIntegrationTest
    {
        public ProcessManager ProcessManager { get; set; }

        public IProcessEnvironment GitEnvironment { get; set; }

        protected override void OnSetup()
        {
            base.OnSetup();

            var platform = new Platform(Environment, FileSystem, new TestUIDispatcher());
            GitEnvironment = platform.GitEnvironment;
            ProcessManager = new ProcessManager(Environment, GitEnvironment);

            Environment.UnityProjectPath = TestBasePath;
            Environment.GitExecutablePath = GitEnvironment.FindGitInstallationPath(ProcessManager).Result;

            NPath repositoryPath = TestBasePath;
            var repositoryManager = new RepositoryManager(new RepositoryPathConfiguration(repositoryPath), platform, CancellationToken.None);
            Environment.Repository = repositoryManager.Repository;
        }

        [Test]
        public void BranchListTest()
        {
            var gitBranches = ProcessManager.GetGitBranches(TestBasePath);

            gitBranches.Should().BeEquivalentTo(
                new GitBranch("master", string.Empty, true),
                new GitBranch("feature/document", string.Empty, false));
        }

        [Test]
        public void LogEntriesTest()
        {
            var logEntries =
                ProcessManager.GetGitLogEntries(TestBasePath, Environment, FileSystem, GitEnvironment, 2)
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
                            TestBasePath + "/Assets/TestDocument.txt".ToNPath(), "Assets/TestDocument.txt".ToNPath(),
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
                            TestBasePath + "/TestDocument.txt".ToNPath(), "TestDocument.txt".ToNPath(),
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
            var gitRemotes = ProcessManager.GetGitRemoteEntries(TestBasePath);

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
            var gitStatus = ProcessManager.GetGitStatus(TestBasePath, Environment, FileSystem, GitEnvironment);

            gitStatus.AssertEqual(new GitStatus()
            {
                LocalBranch = "master",
                Entries = new List<GitStatusEntry>
                {
                    new GitStatusEntry("Assets/Added Document.txt".ToNPath(),
                        TestBasePath.Combine("Assets/Added Document.txt"),
                        "Assets/Added Document.txt".ToNPath(),
                        GitFileStatus.Added, staged: true),

                    new GitStatusEntry("Assets/Renamed TestDocument.txt".ToNPath(),
                        TestBasePath.Combine("Assets/Renamed TestDocument.txt"),
                        "Assets/Renamed TestDocument.txt".ToNPath(),
                        GitFileStatus.Renamed, "Assets/TestDocument.txt".ToNPath(), true),

                    new GitStatusEntry("Assets/Untracked Document.txt".ToNPath(),
                        TestBasePath.Combine("Assets/Untracked Document.txt"),
                        "Assets/Untracked Document.txt".ToNPath(),
                        GitFileStatus.Untracked),
                }
            });
        }

        [Test]
        public void CredentialHelperGetTest()
        {
            var s = ProcessManager.GetGitCreds(TestBasePath, Environment, FileSystem, GitEnvironment);
            s.Should().NotBeNull();
        }
    }
}
