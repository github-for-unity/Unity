using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using GitHub.Unity;
using Rackspace.Threading;
using System.Threading;
using FluentAssertions;
using NCrunch.Framework;
using TestUtils;

namespace IntegrationTests
{
    [TestFixture]
    class GitClientTests : BaseGitIntegrationTest
    {
        [Test]
        public void FindRepoRootTest()
        {
            var environment = new DefaultEnvironment();
            environment.UnityProjectPath = TestRepoPath;

            var repositoryLocator = new RepositoryLocator(environment.UnityProjectPath);

            repositoryLocator.FindRepositoryRoot().ToString().Should().Be(new NPath(TestRepoPath).ToString());
        }

        [Test]
        public void InstallGit()
        {
            var environmentPath = NPath.CreateTempDirectory("integration-test-environment");
            var environment = new IntegrationTestEnvironment(environmentPath);
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

            var platform = new Platform(environment, filesystem, new TestUIDispatcher());
            var gitEnvironment = platform.GitEnvironment;
            var processManager = new ProcessManager(environment, gitEnvironment);

            var gitBranches = processManager.GetGitBranches(TestBasePath, environment.GitExecutablePath);

            gitBranches.Should()
                       .BeEquivalentTo(new GitBranch("master", string.Empty, true),
                           new GitBranch("feature/document", string.Empty, false));
        }
    }
}
