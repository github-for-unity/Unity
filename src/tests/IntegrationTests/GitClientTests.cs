using System.Threading.Tasks;
using FluentAssertions;
using GitHub.Unity;
using NUnit.Framework;

namespace IntegrationTests
{
    [TestFixture/*, Category("TimeSensitive")*/]
    class GitClientTests : BaseGitEnvironmentTest
    {
        [Test]
        public async Task ShouldGetGitVersion()
        {
            await Initialize(TestRepoMasterCleanSynchronized);

            var version = GitClient.Version();
            version.Start().Wait();

            var versionResult = version.Result;
            if (Environment.IsWindows)
            {
                versionResult.Should().Be($"2.11.1");
            }
            else
            {
                versionResult.Should().NotBe(string.Empty);
            }
        }

        [Test]
        public async Task ShouldGetGitLfsVersion()
        {
            await Initialize(TestRepoMasterCleanSynchronized);

            var version = GitClient.LfsVersion();
            version.Start().Wait();

            var versionResult = version.Result;
            if (Environment.IsWindows)
            {
                versionResult.Should().Be("2.2.0");
            }
            else
            {
                versionResult.Should().NotBe(string.Empty);
            }
        }
    }
}