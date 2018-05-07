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
                    GitPackageFeed = $"http://localhost:{server.Port}/unity/git/windows/{GitInstaller.GitInstallDetails.GitPackageName}",
                    GitLfsPackageFeed = $"http://localhost:{server.Port}/unity/git/windows/{GitInstaller.GitInstallDetails.GitLfsPackageName}",
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
            var gitInstaller = new GitInstaller(Environment, ProcessManager, TaskManager, installDetails: installDetails);

            var result = gitInstaller.SetupGitIfNeeded();
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
                    GitPackageFeed = $"http://localhost:{server.Port}/unity/git/mac/{GitInstaller.GitInstallDetails.GitPackageName}",
                    GitLfsPackageFeed = $"http://localhost:{server.Port}/unity/git/mac/{GitInstaller.GitInstallDetails.GitLfsPackageName}",
                };

            TestBasePath.Combine("git").CreateDirectory();

            var gitInstaller = new GitInstaller(Environment, ProcessManager, TaskManager, installDetails: installDetails);
            var result = gitInstaller.SetupGitIfNeeded();
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