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
        protected static TimeSpan Timeout = TimeSpan.FromMinutes(5);

        [Test]
        public async Task ShouldGetGitVersion()
        {
            if (!DefaultEnvironment.OnWindows)
                return;

            InitializePlatformAndEnvironment(TestRepoMasterCleanSynchronized);

            var task = GitClient.Version();
            var taskDone = await TaskEx.WhenAny(task.Start().Task, TaskEx.Delay(Timeout));
            Assert.AreEqual(task.Task, taskDone);
            var result = await task.Task;

            var expected = new Version(2,11,1);
            result.Should().Be(expected);
        }

        [Test]
        public async Task ShouldGetGitLfsVersion()
        {
            if (!DefaultEnvironment.OnWindows)
                return;

            InitializePlatformAndEnvironment(TestRepoMasterCleanSynchronized);

            var task = GitClient.LfsVersion();
            var taskDone = await TaskEx.WhenAny(task.Start().Task, TaskEx.Delay(Timeout));
            Assert.AreEqual(task.Task, taskDone);
            var result = await task.Task;

            var expected = new Version(2,3,4);
            result.Should().Be(expected);
        }
    }
}