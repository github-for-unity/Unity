using System;
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
                versionResult.Should().Be(new Version(2,11,1));
            }
            else
            {
                versionResult.Should().NotBeNull();
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
                versionResult.Should().Be(new Version(2, 3, 4));
            }
            else
            {
                versionResult.Should().NotBeNull();
            }
        }
    }
}