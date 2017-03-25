using System;
using System.Threading;
using GitHub.Unity;

namespace IntegrationTests
{
    class BaseGitEnvironmentTest : BaseGitRepoTest
    {
        protected IEnvironment Environment { get; private set; }

        protected override void OnSetup()
        {
            base.OnSetup();

            Environment = new IntegrationTestEnvironment {
                RepositoryPath = TestBasePath
            };

            var gitSetup = new GitSetup(Environment, CancellationToken.None);
            gitSetup.SetupIfNeeded().Wait();

            Environment.GitExecutablePath = gitSetup.GitExecutablePath;
        }
    }
}