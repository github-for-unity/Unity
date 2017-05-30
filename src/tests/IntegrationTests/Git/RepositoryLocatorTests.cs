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
            Initialize(TestRepoMasterCleanSynchronized);

            var repositoryLocator = new RepositoryLocator(Environment.UnityProjectPath);

            repositoryLocator.FindRepositoryRoot().ToString().Should().Be(new NPath(TestRepoMasterCleanSynchronized).ToString());
        }
    }
}
