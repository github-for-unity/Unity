using NUnit.Framework;
using GitHub.Unity;
using FluentAssertions;

namespace IntegrationTests
{
    [TestFixture]
    class RepositoryLocatorTests : BaseGitEnvironmentTest
    {
        [Test]
        public void FindRepoRootTest()
        {
            var filesystem = new FileSystem(TestBasePath);
            NPathFileSystemProvider.Current = filesystem;

            var environment = new DefaultEnvironment();
            environment.UnityProjectPath = TestBasePath;

            var repositoryLocator = new RepositoryLocator(environment.UnityProjectPath);

            repositoryLocator.FindRepositoryRoot().ToString().Should().Be(new NPath(TestBasePath).ToString());
        }
    }
}
