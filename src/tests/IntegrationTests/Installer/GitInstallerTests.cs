using System;
using System.Threading;
using FluentAssertions;
using GitHub.Unity;
using NSubstitute;
using NUnit.Framework;

namespace IntegrationTests
{
    [TestFixture]
    class GitInstallerTests : BaseTaskManagerTest
    {
        [Test]
        public void GitInstallTest()
        {
            InitializeTaskManager();

            var cacheContainer = Substitute.For<ICacheContainer>();
            Environment = new IntegrationTestEnvironment(cacheContainer, TestBasePath, SolutionDirectory, enableTrace: true);

            var gitInstallationPath = TestBasePath.Combine("GitInstall").CreateDirectory();

            var installDetails = new GitInstallDetails(gitInstallationPath, DefaultEnvironment.OnWindows);

            var gitInstaller = new GitInstaller(Environment, CancellationToken.None, installDetails);

            var autoResetEvent = new AutoResetEvent(false);

            bool? result = null;
            NPath resultPath = null;
            Exception ex = null;

            gitInstaller.SetupGitIfNeeded(new ActionTask<NPath>(CancellationToken.None, (b, path) => {
                    result = true;
                    resultPath = path;
                    autoResetEvent.Set();
                }),
                new ActionTask(CancellationToken.None, (b, exception) => {
                    result = false;
                    ex = exception;
                    autoResetEvent.Set();
                }));

            autoResetEvent.WaitOne();

            result.HasValue.Should().BeTrue();
            result.Value.Should().BeTrue();
            resultPath.Should().NotBeNull();
            ex.Should().BeNull();
        }
    }
}