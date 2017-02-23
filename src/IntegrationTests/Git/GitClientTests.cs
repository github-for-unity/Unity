using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using GitHub.Unity;
using Rackspace.Threading;
using System.Threading;
using FluentAssertions;

namespace IntegrationTests
{
    [TestFixture, Ignore]
    class GitClientTests : BaseGitIntegrationTest
    {
        [Test]
        public void FindRepoRootTest()
        {
            var filesystem = new FileSystem(TestGitRepoPath);
            NPathFileSystemProvider.Current = filesystem;
            var environment = new DefaultEnvironment();
            environment.UnityProjectPath = TestGitRepoPath;
            var platform = new Platform(environment, filesystem, new TestUIDispatcher());
            var gitEnvironment = platform.GitEnvironment;

            var repositoryLocator = new RepositoryLocator(environment.UnityProjectPath);
            Assert.AreEqual(new NPath(TestGitRepoPath).ToString(), repositoryLocator.RepositoryPath);
        }

        [Test]
        public async Task InstallGit()
        {
            var filesystem = new FileSystem();
            NPathFileSystemProvider.Current = filesystem;
            var environment = new DefaultEnvironment();

            var gitSetup = new GitSetup(environment, CancellationToken.None);
            var expectedPath = gitSetup.GitInstallationPath;

            bool setupDone = false;
            float percent;
            long remain;
            // Root paths
            if (!gitSetup.GitExecutablePath.FileExists())
            {
                setupDone = await gitSetup.SetupIfNeeded(
                    //new Progress<float>(x => Logger.Trace("Percentage: {0}", x)),
                    //new Progress<long>(x => Logger.Trace("Remaining: {0}", x))
                    new Progress<float>(x => percent = x),
                    new Progress<long>(x => remain = x)
                );
            }
            environment.GitExecutablePath = gitSetup.GitExecutablePath;
            environment.UnityProjectPath = TestGitRepoPath;
            var platform = new Platform(environment, filesystem, new TestUIDispatcher());
            var gitEnvironment = platform.GitEnvironment;
            var processManager = new ProcessManager(environment, gitEnvironment);

            var gitBranches = processManager.GetGitBranches(TestGitRepoPath, environment.GitExecutablePath);

            gitBranches.Should().BeEquivalentTo(
                new GitBranch("master", string.Empty, true),
                new GitBranch("feature/document", string.Empty, false));
        }
    }
}
