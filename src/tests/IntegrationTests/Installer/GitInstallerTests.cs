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
            InitializePlatform(TestBasePath, setupGit: false, initializeRepository: false);
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
            ZipHelper.Instance = null;
        }

        [Test]
        public void GitInstallWindows()
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

            var zipHelper = Substitute.For<IZipHelper>();
            zipHelper.Extract(Arg.Any<string>(), Arg.Do<string>(x =>
            {
                var n = x.ToNPath();
                n.EnsureDirectoryExists();
                if (n.FileName == "git-lfs")
                {
                    n.Combine("git-lfs" + Environment.ExecutableExtension).WriteAllText("");
                }
            }), Arg.Any<CancellationToken>(), Arg.Any<Func<long, long, bool>>()).Returns(true);
            ZipHelper.Instance = zipHelper;
            var gitInstaller = new GitInstaller(Environment, ProcessManager, TaskManager, installDetails);

            TaskCompletionSource<GitInstaller.GitInstallationState> end = new TaskCompletionSource<GitInstaller.GitInstallationState>();
            var startTask = gitInstaller.SetupGitIfNeeded().Finally((_, state) => end.TrySetResult(state));
            startTask.Start();
            GitInstaller.GitInstallationState result = null;
            Assert.DoesNotThrow(async () => result = await end.Task);
            result.Should().NotBeNull();

            Assert.AreEqual(gitInstallationPath.Combine(installDetails.PackageNameWithVersion), result.GitInstallationPath);
            result.GitExecutablePath.Should().Be(gitInstallationPath.Combine(installDetails.PackageNameWithVersion, "cmd", "git" + Environment.ExecutableExtension));
            result.GitLfsExecutablePath.Should().Be(gitInstallationPath.Combine(installDetails.PackageNameWithVersion, "mingw32", "libexec", "git-core", "git-lfs" + Environment.ExecutableExtension));

            var isCustomGitExec = result.GitExecutablePath != result.GitExecutablePath;

            Environment.GitExecutablePath = result.GitExecutablePath;
            Environment.GitLfsExecutablePath = result.GitLfsExecutablePath;

            Environment.IsCustomGitExecutable = isCustomGitExec;
            
            var procTask = new SimpleProcessTask(TaskManager.Token, "something")
                .Configure(ProcessManager);
            procTask.Process.StartInfo.EnvironmentVariables["PATH"].Should().StartWith(gitInstallationPath.ToString());
        }

        //[Test]
        public void GitInstallMac()
        {
            var filesystem = Substitute.For<IFileSystem>();
            DefaultEnvironment.OnMac = true;
            DefaultEnvironment.OnWindows = false;

            var gitInstallationPath = TestBasePath.Combine("GitInstall").CreateDirectory();

            var installDetails = new GitInstaller.GitInstallDetails(gitInstallationPath, Environment.IsWindows)
                {
                    GitZipMd5Url = $"http://localhost:{server.Port}/{new Uri(GitInstaller.GitInstallDetails.DefaultGitZipMd5Url).AbsolutePath}",
                    GitZipUrl = $"http://localhost:{server.Port}/{new Uri(GitInstaller.GitInstallDetails.DefaultGitZipUrl).AbsolutePath}",
                    GitLfsZipMd5Url = $"http://localhost:{server.Port}/{new Uri(GitInstaller.GitInstallDetails.DefaultGitLfsZipMd5Url).AbsolutePath}",
                    GitLfsZipUrl = $"http://localhost:{server.Port}/{new Uri(GitInstaller.GitInstallDetails.DefaultGitLfsZipUrl).AbsolutePath}",
                };

            TestBasePath.Combine("git").CreateDirectory();

            var gitInstaller = new GitInstaller(Environment, ProcessManager, TaskManager, installDetails);
            var startTask = gitInstaller.SetupGitIfNeeded();
            var endTask = new FuncTask<GitInstaller.GitInstallationState, GitInstaller.GitInstallationState>(TaskManager.Token, (s, state) => state);
            startTask.OnEnd += (thisTask, path, success, exception) => thisTask.GetEndOfChain().Then(endTask);
            startTask.Start();
            GitInstaller.GitInstallationState result = null;
            Assert.DoesNotThrow(async () => result = await endTask.Task);
            result.Should().NotBeNull();

            Assert.AreEqual(gitInstallationPath.Combine(installDetails.PackageNameWithVersion), result.GitInstallationPath);
            result.GitExecutablePath.Should().Be(gitInstallationPath.Combine("bin", "git" + Environment.ExecutableExtension));
            result.GitLfsExecutablePath.Should().Be(gitInstallationPath.Combine(installDetails.PackageNameWithVersion, "libexec", "git-core", "git-lfs" + Environment.ExecutableExtension));

            var isCustomGitExec = result.GitExecutablePath != result.GitExecutablePath;

            Environment.GitExecutablePath = result.GitExecutablePath;
            Environment.GitLfsExecutablePath = result.GitLfsExecutablePath;

            Environment.IsCustomGitExecutable = isCustomGitExec;
            
            var procTask = new SimpleProcessTask(TaskManager.Token, "something")
                .Configure(ProcessManager);
            procTask.Process.StartInfo.EnvironmentVariables["PATH"].Should().StartWith(gitInstallationPath.ToString());
        }
    }
}