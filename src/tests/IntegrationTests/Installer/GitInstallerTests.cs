using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using GitHub.Unity;
using NSubstitute;
using NUnit.Framework;

namespace IntegrationTests
{
    [TestFixture]
    class GitInstallerTests : BaseIntegrationTest
    {
        const int Timeout = 30000;
        public override void OnSetup()
        {
            base.OnSetup();
            InitializeEnvironment(TestBasePath, initializeRepository: false);
        }

        private TestWebServer.HttpServer server;
        public override void TestFixtureSetUp()
        {
            base.TestFixtureSetUp();
            server = new TestWebServer.HttpServer(SolutionDirectory.Combine("files"));
            Task.Factory.StartNew(server.Start);
            ApplicationConfiguration.WebTimeout = 10000;
        }

        public override void TestFixtureTearDown()
        {
            base.TestFixtureTearDown();
            server.Stop();
            ApplicationConfiguration.WebTimeout = ApplicationConfiguration.DefaultWebTimeout;
        }

        [Test]
        public void GitInstallTest()
        {
            var gitInstallationPath = TestBasePath.Combine("GitInstall").CreateDirectory();

            var installDetails = new GitInstaller.GitInstallDetails(gitInstallationPath, DefaultEnvironment.OnWindows)
                {
                    GitZipMd5Url = $"http://localhost:{server.Port}/{new UriString(GitInstaller.GitInstallDetails.DefaultGitZipMd5Url).Filename}",
                    GitZipUrl = $"http://localhost:{server.Port}/{new UriString(GitInstaller.GitInstallDetails.DefaultGitZipUrl).Filename}",
                    GitLfsZipMd5Url = $"http://localhost:{server.Port}/{new UriString(GitInstaller.GitInstallDetails.DefaultGitLfsZipMd5Url).Filename}",
                    GitLfsZipUrl = $"http://localhost:{server.Port}/{new UriString(GitInstaller.GitInstallDetails.DefaultGitLfsZipUrl).Filename}",
                };

            TestBasePath.Combine("git").CreateDirectory();

            var gitInstaller = new GitInstaller(Environment, ProcessManager, TaskManager, installDetails);
            var startTask = gitInstaller.SetupGitIfNeeded();
            var endTask = new FuncTask<NPath, NPath>(TaskManager.Token, (s, path) => path);
            startTask.OnEnd += (thisTask, path, success, exception) => thisTask.GetEndOfChain().Then(endTask);
            startTask.Start();
            NPath? resultPath = null;
            Assert.DoesNotThrow(async () => resultPath = await endTask.Task);
            resultPath.Should().NotBeNull();
        }
    }
}