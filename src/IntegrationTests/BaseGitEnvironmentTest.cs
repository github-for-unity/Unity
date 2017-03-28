using System.Threading;
using GitHub.Unity;

namespace IntegrationTests
{
    class BaseGitEnvironmentTest : BaseGitRepoTest
    {
        protected override void OnSetup()
        {
            base.OnSetup();

            Environment = new IntegrationTestEnvironment {
                RepositoryPath = TestRepoPath,
                UnityProjectPath = TestRepoPath
            };

            var gitSetup = new GitSetup(Environment, CancellationToken.None);
            gitSetup.SetupIfNeeded().Wait();

            Environment.GitExecutablePath = gitSetup.GitExecutablePath;
        }
    }
}