using System.Threading;
using FluentAssertions;
using GitHub.Unity;
using NUnit.Framework;
using Rackspace.Threading;
using TestUtils;

namespace IntegrationTests
{
    class GitSetupTests : BaseGitRepoTest
    {
        [Test, Category("Long")]
        public void InstallGit()
        {
            var environmentPath = NPath.CreateTempDirectory("integration-test-environment");
            var environment = new IntegrationTestEnvironment(SolutionDirectory, environmentPath);
            var gitSetup = new GitSetup(environment, FileSystem, CancellationToken.None);
            var expectedPath = gitSetup.GitInstallationPath;

            var setupDone = false;
            var percent = -1f;
            gitSetup.GitExecutablePath.FileExists().Should().BeFalse();

            setupDone = gitSetup.SetupIfNeeded(percentage: new Progress<float>(x => percent = x)).Result;

            setupDone.Should().BeTrue();
            percent.Should().Be(1);

            Logger.Trace("Expected GitExecutablePath: {0}", gitSetup.GitExecutablePath);
            gitSetup.GitExecutablePath.FileExists().Should().BeTrue();
            
            var GitLfsDestinationPath = gitSetup.GitInstallationPath;
            if (environment.IsWindows)
            {
                GitLfsDestinationPath = GitLfsDestinationPath.Combine("mingw32");
            }
            GitLfsDestinationPath = GitLfsDestinationPath.Combine("libexec", "git-core", "git-lfs.exe");
            GitLfsDestinationPath.FileExists().Should().BeTrue();

            var calculateMd5 = NPathFileSystemProvider.Current.CalculateMD5(GitLfsDestinationPath);
            GitInstaller.GitLfsExecutableMD5.Should().Be(calculateMd5);

            environment.GitExecutablePath = gitSetup.GitExecutablePath;

            setupDone = gitSetup.SetupIfNeeded(percentage: new Progress<float>(x => percent = x)).Result;
            setupDone.Should().BeFalse();

            var platform = new Platform(environment, FileSystem, new TestUIDispatcher());
            var gitEnvironment = platform.GitEnvironment;
            var processManager = new ProcessManager(environment, gitEnvironment);

            var gitBranches = processManager.GetGitBranches(TestRepoMasterDirtyUnsynchronized, environment.GitExecutablePath);

            gitBranches.Should().BeEquivalentTo(
                new GitBranch("master", "origin/master: behind 1", true),
                new GitBranch("feature/document", "origin/feature/document", false));
        }
    }
}
