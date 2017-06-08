using NUnit.Framework;
using GitHub.Unity;
using FluentAssertions;
using System.Threading.Tasks;

namespace IntegrationTests
{
    [TestFixture]
    class RepositoryLocatorTests : BaseGitEnvironmentTest
    {
        [Test]
        public async Task FindRepoRootTest()
        {
            await Initialize(TestRepoMasterCleanSynchronized);

            var repositoryLocator = new RepositoryLocator(Environment.UnityProjectPath);

            repositoryLocator.FindRepositoryRoot().ToString().Should().Be(new NPath(TestRepoMasterCleanSynchronized).ToString());
        }
    }
}
