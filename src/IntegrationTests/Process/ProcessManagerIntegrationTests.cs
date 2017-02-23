using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using GitHub.Unity;
using System.Threading;

namespace IntegrationTests
{
    [TestFixture, Ignore]
    class ProcessManagerIntegrationTests : BaseGitIntegrationTest
    {
        [Test]
        public async void BranchListTest()
        {
            var filesystem = new FileSystem();
            NPathFileSystemProvider.Current = filesystem;
            var environment = new DefaultEnvironment();
            environment.UnityProjectPath = TestBasePath;
            var platform = new Platform(environment, filesystem, new TestUIDispatcher());
            var gitEnvironment = platform.GitEnvironment;
            var processManager = new ProcessManager(environment, gitEnvironment);
            environment.GitExecutablePath = await gitEnvironment.FindGitInstallationPath(processManager);

            var gitBranches = processManager.GetGitBranches(TestBasePath);

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
            environment.UnityProjectPath = TestBasePath;
            var platform = new Platform(environment, filesystem, new TestUIDispatcher());
            var gitEnvironment = platform.GitEnvironment;
            var processManager = new ProcessManager(environment, gitEnvironment);
            environment.GitExecutablePath = await gitEnvironment.FindGitInstallationPath(processManager);

            var logEntries =
                processManager.GetGitLogEntries(TestBasePath, environment, filesystem, gitEnvironment, 2)
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
                            TestBasePath + "Assets/TestDocument.txt".ToNPath(), null,
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
                            TestBasePath + "TestDocument.txt", null,
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
            environment.UnityProjectPath = TestBasePath;
            var platform = new Platform(environment, filesystem, new TestUIDispatcher());
            var gitEnvironment = platform.GitEnvironment;
            var processManager = new ProcessManager(environment, gitEnvironment);
            environment.GitExecutablePath = await gitEnvironment.FindGitInstallationPath(processManager);

            var gitRemotes = processManager.GetGitRemoteEntries(TestBasePath);

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
            var filesystem = new FileSystem(TestBasePath);
            NPathFileSystemProvider.Current = filesystem;
            var environment = new DefaultEnvironment();
            environment.UnityProjectPath = TestBasePath;
            
            var platform = new Platform(environment, filesystem, new TestUIDispatcher());
            var gitEnvironment = platform.GitEnvironment;
            var processManager = new ProcessManager(environment, gitEnvironment);
            var gitClient = new RepositoryLocator(TestBasePath);
            using (var repoManager = new RepositoryManager(TestBasePath, platform, CancellationToken.None))
            {

                environment.GitExecutablePath = await gitEnvironment.FindGitInstallationPath(processManager);

                var gitStatus = processManager.GetGitStatus(TestBasePath, environment, filesystem, gitEnvironment);

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
        }

        //[Test]
        public async void CredentialHelperGetTest()
        {
            var filesystem = new FileSystem();
            NPathFileSystemProvider.Current = filesystem;
            var environment = new DefaultEnvironment();
            environment.UnityProjectPath = TestBasePath;
            var platform = new Platform(environment, filesystem, new TestUIDispatcher());
            var gitEnvironment = platform.GitEnvironment;
            var processManager = new ProcessManager(environment, gitEnvironment);
            environment.GitExecutablePath = await gitEnvironment.FindGitInstallationPath(processManager);

            var s = processManager.GetGitCreds(TestBasePath, environment, filesystem, gitEnvironment);
        }
    }
}
