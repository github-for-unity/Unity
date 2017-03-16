using System.Threading;
using GitHub.Unity;
using NSubstitute;
using NUnit.Framework;

namespace UnitTests
{
    [TestFixture]
    class RepositoryManagerTests : TestBase
    {
        [Test, Ignore()]
        public void InitialTest()
        {
            NPathFileSystemProvider.Current = Substitute.For<IFileSystem>();

            var cancellationToken = new CancellationToken();
            var platform = Substitute.For<IPlatform>();

            var repositoryRepositoryPathConfiguration = new RepositoryPathConfiguration(@"c:\Temp");
            var gitConfig = Substitute.For<IGitConfig>();
            var repositoryWatcher = Substitute.For<IRepositoryWatcher>();
            var repositoryProcessRunner = Substitute.For<IRepositoryProcessRunner>();

            var repositoryManager = new RepositoryManager(repositoryRepositoryPathConfiguration, platform, gitConfig, repositoryWatcher, repositoryProcessRunner, cancellationToken);
        }
    }
}
