using System;
using System.Threading.Tasks;
using FluentAssertions;
using GitHub.Unity;
using NUnit.Framework;

namespace IntegrationTests
{
    [TestFixture]
    class GitClientTests : BaseGitEnvironmentTest
    {
        [Test]
        public async Task ShouldGetGitVersion()
        {
            Initialize(TestRepoMasterCleanSynchronized);

            var result = await GitClient.Version().StartAwait();

            if (Environment.IsWindows)
            {
                result.Should().Be(new Version(2,11,1));
            }
            else
            {
                result.Should().NotBeNull();
            }
        }

        [Test]
        public async Task ShouldGetGitLfsVersion()
        {
            Initialize(TestRepoMasterCleanSynchronized);

            var result = await GitClient.LfsVersion().StartAwait();

            if (Environment.IsWindows)
            {
                result.Should().Be(new Version(2, 3, 4));
            }
            else
            {
                result.Should().NotBeNull();
            }
        }
    }
}