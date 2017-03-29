using System.Threading;
using FluentAssertions;
using GitHub.Unity;
using NUnit.Framework;
using Rackspace.Threading;

namespace IntegrationTests
{
    class GitSetupTests : BaseGitRepoTest
    {
        [Test]
        public void InstallGit()
        {
            var environmentPath = NPath.CreateTempDirectory("integration-test-environment");
            var environment = new IntegrationTestEnvironment(SolutionDirectory, environmentPath);
            var gitSetup = new GitSetup(environment, CancellationToken.None);
            var expectedPath = gitSetup.GitInstallationPath;

            var setupDone = false;
            var percent = -1f;
            gitSetup.GitExecutablePath.FileExists().Should().BeFalse();

            setupDone = gitSetup.SetupIfNeeded(percentage: new Progress<float>(x => percent = x)).Result;

            setupDone.Should().BeTrue();
            percent.Should().Be(1);

            Logger.Trace("Expected GitExecutablePath: {0}", gitSetup.GitExecutablePath);
            gitSetup.GitExecutablePath.FileExists().Should().BeTrue();

            environment.GitExecutablePath = gitSetup.GitExecutablePath;

            var platform = new Platform(environment, FileSystem, new TestUIDispatcher());
            var gitEnvironment = platform.GitEnvironment;
            var processManager = new ProcessManager(environment, gitEnvironment);

            var gitBranches = processManager.GetGitBranches(TestRepoMasterDirty, environment.GitExecutablePath);

            gitBranches.Should().BeEquivalentTo(
                new GitBranch("master", "origin/master: behind 1", true),
                new GitBranch("feature/document", "origin/feature/document", false));
        }
    }
}
