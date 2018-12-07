using System;
using System.Threading.Tasks;
using FluentAssertions;
using GitHub.Unity;
using NUnit.Framework;

namespace IntegrationTests
{
    [TestFixture]
    class A_GitClientTests : BaseGitTestWithHttpServer
    {
        protected override int Timeout { get; set; } = 5 * 60 * 1000;

        [Test]
        public void AaSetupGitFirst()
        {
            InitializePlatformAndEnvironment(TestRepoMasterCleanSynchronized);
        }

        [Test]
        public void ShouldGetGitVersion()
        {
            if (!DefaultEnvironment.OnWindows)
                return;

            InitializePlatformAndEnvironment(TestRepoMasterCleanSynchronized);

            var result = GitClient.Version().RunSynchronously();
            var expected = TheVersion.Parse("2.17.0");
            result.Major.Should().Be(expected.Major);
            result.Minor.Should().Be(expected.Minor);
            result.Patch.Should().Be(expected.Patch);
        }

        [Test]
        public void ShouldGetGitLfsVersion()
        {
            if (!DefaultEnvironment.OnWindows)
                return;

            InitializePlatformAndEnvironment(TestRepoMasterCleanSynchronized);

            var result = GitClient.LfsVersion().RunSynchronously();
            var expected = TheVersion.Parse("2.4.0");
            result.Should().Be(expected);
        }
    }
}
