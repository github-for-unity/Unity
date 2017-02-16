using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using GitHub.Unity;

namespace GitHub.Unity.Tests
{
    [TestFixture]
    class ProcessManagerIntegrationTests : BaseIntegrationTest
    {
        [Test]
        public async void BranchListTest()
        {
            var filesystem = new FileSystem();
            NPathFileSystemProvider.Current = filesystem;
            var environment = new DefaultEnvironment();
            environment.UnityProjectPath = TestGitRepoPath;
            var platform = new Platform(environment, filesystem);
            var gitEnvironment = platform.GitEnvironment;
            var processManager = new ProcessManager(environment, gitEnvironment);
            environment.GitExecutablePath = await gitEnvironment.FindGitInstallationPath(processManager);

            var gitBranches = processManager.GetGitBranches(TestGitRepoPath);

            gitBranches.Should().BeEquivalentTo(
                new GitBranch("master", string.Empty, true),
                new GitBranch("feature/document", string.Empty, false));
        }

        [Test]
        public async void LogEntriesTest()
        {
            var filesystem = new FileSystem();
            NPathFileSystemProvider.Current = filesystem;
            var environment = new DefaultEnvironment();
            environment.UnityProjectPath = TestGitRepoPath;
            var platform = new Platform(environment, filesystem);
            var gitEnvironment = platform.GitEnvironment;
            var processManager = new ProcessManager(environment, gitEnvironment);
            environment.GitExecutablePath = await gitEnvironment.FindGitInstallationPath(processManager);

            var logEntries =
                processManager.GetGitLogEntries(TestGitRepoPath, environment, filesystem, gitEnvironment, 2)
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
                            TestGitRepoPath + "Assets/TestDocument.txt".ToNPath(), null,
                            GitFileStatus.Renamed, "TestDocument.txt"),
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
                        new GitStatusEntry("TestDocument.txt",
                            TestGitRepoPath + "TestDocument.txt", null,
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
        public async void RemoteListTest()
        {
            var filesystem = new FileSystem();
            NPathFileSystemProvider.Current = filesystem;
            var environment = new DefaultEnvironment();
            environment.UnityProjectPath = TestGitRepoPath;
            var platform = new Platform(environment, filesystem);
            var gitEnvironment = platform.GitEnvironment;
            var processManager = new ProcessManager(environment, gitEnvironment);
            environment.GitExecutablePath = await gitEnvironment.FindGitInstallationPath(processManager);

            var gitRemotes = processManager.GetGitRemoteEntries(TestGitRepoPath);

            gitRemotes.Should().BeEquivalentTo(new GitRemote()
            {
                Name = "origin",
                Url = "https://github.com/EvilStanleyGoldman/IOTestsRepo.git",
                Host = "github.com",
                Function = GitRemoteFunction.Both
            });
        }
    
        [Test]
        public async void StatusTest()
        {
            var testRepo = TestGitRepoPath.ToNPath();
            var filesystem = new FileSystem(TestGitRepoPath);
            NPathFileSystemProvider.Current = filesystem;
            var environment = new DefaultEnvironment();
            environment.UnityProjectPath = TestGitRepoPath;
            
            var platform = new Platform(environment, filesystem);
            var gitEnvironment = platform.GitEnvironment;
            var processManager = new ProcessManager(environment, gitEnvironment);
            var gitClient = new GitClient(TestGitRepoPath);
            environment.Repository = gitClient.GetRepository();

            environment.GitExecutablePath = await gitEnvironment.FindGitInstallationPath(processManager);

            var gitStatus = processManager.GetGitStatus(TestGitRepoPath, environment, filesystem, gitEnvironment);

            gitStatus.AssertEqual(new GitStatus()
            {
                LocalBranch = "master",
                Entries = new List<GitStatusEntry>
                {
                    new GitStatusEntry("Assets/Added Document.txt".ToNPath(),
                        testRepo.Combine("Assets/Added Document.txt"),
                        "Assets/Added Document.txt".ToNPath(),
                        GitFileStatus.Added, staged: true),

                    new GitStatusEntry("Assets/Renamed TestDocument.txt".ToNPath(),
                        testRepo.Combine("Assets/Renamed TestDocument.txt"),
                        "Assets/Renamed TestDocument.txt".ToNPath(),
                        GitFileStatus.Renamed, "Assets/TestDocument.txt".ToNPath(), true),

                    new GitStatusEntry("Assets/Untracked Document.txt".ToNPath(),
                        testRepo.Combine("Assets/Untracked Document.txt"),
                        "Assets/Untracked Document.txt".ToNPath(),
                        GitFileStatus.Untracked),
                }
            });
        }

        //[Test]
        public async void CredentialHelperGetTest()
        {
            var filesystem = new FileSystem();
            NPathFileSystemProvider.Current = filesystem;
            var environment = new DefaultEnvironment();
            environment.UnityProjectPath = TestGitRepoPath;
            var platform = new Platform(environment, filesystem);
            var gitEnvironment = platform.GitEnvironment;
            var processManager = new ProcessManager(environment, gitEnvironment);
            environment.GitExecutablePath = await gitEnvironment.FindGitInstallationPath(processManager);

            var s = processManager.GetGitCreds(TestGitRepoPath, environment, filesystem, gitEnvironment);
        }
    }
}
