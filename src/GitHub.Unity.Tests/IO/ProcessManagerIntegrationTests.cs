using System.Collections.Generic;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using GitHub.Api;

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
