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
    class GitInstallerTests : BaseTaskManagerTest
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
        [Category("DoNotRunOnAppVeyor")]
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

            //var gitArchivePath = AssemblyResources.ToFile(ResourceType.Platform, "git.zip", zipArchivesPath, Environment);
            //var gitLfsArchivePath = AssemblyResources.ToFile(ResourceType.Platform, "git-lfs.zip", zipArchivesPath, Environment);
            
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