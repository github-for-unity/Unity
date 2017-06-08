using System.Collections.Generic;
using FluentAssertions;
using GitHub.Unity;
using NUnit.Framework;
using Rackspace.Threading;
using System.Threading.Tasks;

namespace IntegrationTests
{
    class GitSetupTests : BaseGitEnvironmentTest
    {
        [Test, Category("DoNotRunOnAppVeyor")]
        public async Task InstallGit()
        {
            var environmentPath = NPath.CreateTempDirectory("integration-test-environment");
            var environment = await Initialize(TestRepoMasterDirtyUnsynchronized, environmentPath);

            var gitSetup = new GitSetup(environment, TaskManager.Token);
            var expectedPath = gitSetup.GitInstallationPath;

            var setupDone = false;
            var percent = -1f;
            gitSetup.GitExecutablePath.FileExists().Should().BeFalse();

            setupDone = await gitSetup.SetupIfNeeded(percentage: new Progress<float>(x => percent = x));

            if (environment.IsWindows)
            {
                environment.GitExecutablePath = gitSetup.GitExecutablePath;

                setupDone.Should().BeTrue();
                percent.Should().Be(1);

                Logger.Trace("Expected GitExecutablePath: {0}", gitSetup.GitExecutablePath);
                gitSetup.GitExecutablePath.FileExists().Should().BeTrue();

                var gitLfsDestinationPath = gitSetup.GitInstallationPath;
                gitLfsDestinationPath = gitLfsDestinationPath.Combine("mingw32");

                gitLfsDestinationPath = gitLfsDestinationPath.Combine("libexec", "git-core", "git-lfs.exe");
                gitLfsDestinationPath.FileExists().Should().BeTrue();

                var calculateMd5 = NPath.FileSystem.CalculateMD5(gitLfsDestinationPath);
                GitInstaller.GitLfsExecutableMD5.Should().Be(calculateMd5);

                setupDone = await gitSetup.SetupIfNeeded(percentage: new Progress<float>(x => percent = x));
                setupDone.Should().BeFalse();
            }
            else
            {
                environment.GitExecutablePath = "/usr/local/bin/git".ToNPath();
                setupDone.Should().BeFalse();
            }

            var platform = new Platform(environment);
            var gitEnvironment = platform.GitEnvironment;
            var processManager = new ProcessManager(environment, gitEnvironment, TaskManager.Token);

            List<GitBranch> gitBranches = null;
            gitBranches = await processManager
                .GetGitBranches(TestRepoMasterDirtyUnsynchronized, environment.GitExecutablePath)
                .StartAsAsync();

            gitBranches.Should().BeEquivalentTo(
                new GitBranch("master", "origin/master: behind 1", true),
                new GitBranch("feature/document", "origin/feature/document", false));
        }
    }
}