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
            InitializeEnvironment(TestRepoMasterClean);

            var repositoryLocator = new RepositoryLocator(Environment.UnityProjectPath);

            repositoryLocator.FindRepositoryRoot().ToString().Should().Be(new NPath(TestRepoMasterClean).ToString());
        }
    }
}
