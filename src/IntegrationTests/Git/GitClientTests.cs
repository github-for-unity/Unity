using System.Threading.Tasks;
using NUnit.Framework;
using GitHub.Unity;
using Rackspace.Threading;
using System.Threading;
using FluentAssertions;
using NCrunch.Framework;

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
            var platform = new Platform(Environment, FileSystem, new TestUIDispatcher());
            var gitEnvironment = platform.GitEnvironment;
            var processManager = new ProcessManager(Environment, gitEnvironment);

            var gitBranches = processManager.GetGitBranches(TestRepoPath, Environment.GitExecutablePath);

            gitBranches.Should().BeEquivalentTo(
                new GitBranch("master", string.Empty, true),
                new GitBranch("feature/document", string.Empty, false));
        }
    }
}
