using System.Threading;
using GitHub.Unity;
using NUnit.Framework;
using TestUtils;

namespace UnitTests
{
    [TestFixture]
    class RepositoryManagerTests
    {
        protected SubstituteFactory SubstituteFactory { get; private set; }

        [TestFixtureSetUp]
        public void TestFixtureSetup()
        {
            SubstituteFactory = new SubstituteFactory();
        }

        [Test]
        public void InitialTest()
        {
            NPathFileSystemProvider.Current = SubstituteFactory.CreateFileSystem();

            var cancellationToken = CancellationToken.None;

            var platform = SubstituteFactory.CreatePlatform();

            var repositoryRepositoryPathConfiguration = new RepositoryPathConfiguration(@"c:\Temp");
            var gitConfig = SubstituteFactory.CreateGitConfig();
            var repositoryWatcher = SubstituteFactory.CreateRepositoryWatcher();
            var repositoryProcessRunner = SubstituteFactory.CreateRepositoryProcessRunner();

            var repositoryManager = new RepositoryManager(repositoryRepositoryPathConfiguration, platform, gitConfig, repositoryWatcher, repositoryProcessRunner, cancellationToken);
        }
    }
}
