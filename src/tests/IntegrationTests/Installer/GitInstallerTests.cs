using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using GitHub.Unity;
using NSubstitute;
using NUnit.Framework;
using System.IO;
using System.Linq;

namespace IntegrationTests
{
    [TestFixture]
    class GitInstallerTests : BaseIntegrationTest
    {
        const int Timeout = 30000;
        public override void OnSetup()
        {
            base.OnSetup();
            InitializeEnvironment(TestBasePath, false, false);
            InitializePlatform(TestBasePath, setupGit: false);
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
            var gitInstaller = new GitInstaller(Environment, ProcessManager, TaskManager.Token, null, installDetails: installDetails);

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
        public void MacSkipsInstallWhenSettingsGitExists()
        {
            DefaultEnvironment.OnMac = true;
            DefaultEnvironment.OnWindows = false;

            var filesystem = Substitute.For<IFileSystem>();
            filesystem.FileExists(Arg.Any<string>()).Returns(true);
            filesystem.DirectoryExists(Arg.Any<string>()).Returns(true);
            filesystem.DirectorySeparatorChar.Returns('/');
            Environment.FileSystem = filesystem;

            var gitInstallationPath = "/usr/local".ToNPath();
            var gitExecutablePath = gitInstallationPath.Combine("bin/git");
            var gitLfsInstallationPath = gitInstallationPath;
            var gitLfsExecutablePath = gitLfsInstallationPath.Combine("bin/git-lfs");

            var installDetails = new GitInstaller.GitInstallDetails(gitInstallationPath, Environment.IsWindows)
                {
                    GitPackageFeed = $"http://localhost:{server.Port}/unity/git/mac/{GitInstaller.GitInstallDetails.GitPackageName}",
                    GitLfsPackageFeed = $"http://localhost:{server.Port}/unity/git/mac/{GitInstaller.GitInstallDetails.GitLfsPackageName}",
                };

            var ret = new string[] { gitLfsExecutablePath };
            filesystem.GetFiles(Arg.Any<string>(), Arg.Is<string>(installDetails.GitLfsExecutable), Arg.Any<SearchOption>())
                      .Returns(ret);

            var settings = Substitute.For<ISettings>();
            var settingsRet = gitExecutablePath.ToString();
            settings.Get(Arg.Is<string>(Constants.GitInstallPathKey), Arg.Any<string>()).Returns(settingsRet);
            var installer = new GitInstaller(Environment, ProcessManager, TaskManager.Token, settings, installDetails);

            var result = installer.SetupGitIfNeeded();
            Assert.AreEqual(gitInstallationPath, result.GitInstallationPath);
            Assert.AreEqual(gitLfsInstallationPath, result.GitLfsInstallationPath);
            Assert.AreEqual(gitExecutablePath, result.GitExecutablePath);
            Assert.AreEqual(gitLfsExecutablePath, result.GitLfsExecutablePath);
        }

        //[Test]
        public void WindowsSkipsInstallWhenSettingsGitExists()
        {
            DefaultEnvironment.OnMac = false;
            DefaultEnvironment.OnWindows = true;

            var filesystem = Substitute.For<IFileSystem>();
            filesystem.FileExists(Arg.Any<string>()).Returns(true);
            filesystem.DirectoryExists(Arg.Any<string>()).Returns(true);
            filesystem.DirectorySeparatorChar.Returns('\\');
            Environment.FileSystem = filesystem;

            var gitInstallationPath = "c:/Program Files/Git".ToNPath();
            var gitExecutablePath = gitInstallationPath.Combine("cmd/git.exe");
            var gitLfsInstallationPath = gitInstallationPath;
            var gitLfsExecutablePath = gitLfsInstallationPath.Combine("mingw32/libexec/git-core/git-lfs.exe");

            var installDetails = new GitInstaller.GitInstallDetails(gitInstallationPath, Environment.IsWindows)
                {
                    GitPackageFeed = $"http://localhost:{server.Port}/unity/git/windows/{GitInstaller.GitInstallDetails.GitPackageName}",
                    GitLfsPackageFeed = $"http://localhost:{server.Port}/unity/git/windows/{GitInstaller.GitInstallDetails.GitLfsPackageName}",
                };

            var ret = new string[] { gitLfsExecutablePath };
            filesystem.GetFiles(Arg.Any<string>(), Arg.Is<string>(installDetails.GitLfsExecutable), Arg.Any<SearchOption>())
                .Returns(ret);

            var settings = Substitute.For<ISettings>();
            var settingsRet = gitExecutablePath.ToString();
            settings.Get(Arg.Is<string>(Constants.GitInstallPathKey), Arg.Any<string>()).Returns(settingsRet);
            var installer = new GitInstaller(Environment, ProcessManager, TaskManager.Token, settings, installDetails);
            var result = installer.SetupGitIfNeeded();
            Assert.AreEqual(gitInstallationPath, result.GitInstallationPath);
            Assert.AreEqual(gitLfsInstallationPath, result.GitLfsInstallationPath);
            Assert.AreEqual(gitExecutablePath, result.GitExecutablePath);
            Assert.AreEqual(gitLfsExecutablePath, result.GitLfsExecutablePath);
        }
    }

    [TestFixture]
    class GitInstallerTestsWithHttp : BaseTestWithHttpServer
    {
        public override void OnSetup()
        {
            base.OnSetup();
            InitializeEnvironment(TestBasePath, false, false);
            InitializePlatform(TestBasePath, setupGit: false);
        }

        [Test]
        public void GitLfsIsInstalledIfMissing()
        {
            var gitInstallationPath = TestBasePath.Combine("GitInstall").CreateDirectory();

            var installDetails = new GitInstaller.GitInstallDetails(gitInstallationPath, DefaultEnvironment.OnWindows)
                {
                    GitPackageFeed = $"http://localhost:{server.Port}/unity/git/windows/{GitInstaller.GitInstallDetails.GitPackageName}",
                    GitLfsPackageFeed = $"http://localhost:{server.Port}/unity/git/windows/{GitInstaller.GitInstallDetails.GitLfsPackageName}",
                };

            var package = Package.Load(Environment, installDetails.GitPackageFeed);
            var downloader = new Downloader();
            downloader.Catch(e => true);
            downloader.QueueDownload(package.Uri, installDetails.ZipPath);
            downloader.RunWithReturn(true);

            var tempZipExtractPath = TestBasePath.Combine("Temp", "git_zip_extract_zip_paths");

            var gitExtractPath = tempZipExtractPath.Combine("git").CreateDirectory();
            ZipHelper.Instance.Extract(installDetails.GitZipPath, gitExtractPath, TaskManager.Token, null);
            var source = gitExtractPath;
            var target = installDetails.GitInstallationPath;
            target.DeleteIfExists();
            target.EnsureParentDirectoryExists();
            source.Move(target);

            var gitInstaller = new GitInstaller(Environment, ProcessManager, TaskManager.Token, null, installDetails: installDetails);

            var result = gitInstaller.SetupGitIfNeeded();
            result.Should().NotBeNull();

            Assert.AreEqual(gitInstallationPath.Combine(installDetails.PackageNameWithVersion), result.GitInstallationPath);
            result.GitExecutablePath.Should().Be(gitInstallationPath.Combine(installDetails.PackageNameWithVersion, "cmd", "git" + Environment.ExecutableExtension));
            result.GitLfsExecutablePath.Should().Be(gitInstallationPath.Combine(installDetails.PackageNameWithVersion, "mingw32", "libexec", "git-core", "git-lfs" + Environment.ExecutableExtension));
        }

        [Test]
        public void GitLfsIsInstalledIfMissingWithCustomGitPath()
        {
            var defaultGitInstall = TestBasePath.Combine("DefaultInstall").CreateDirectory();
            var customGitInstall = TestBasePath.Combine("CustomGitInstall").CreateDirectory();

            var installDetails = new GitInstaller.GitInstallDetails(defaultGitInstall, DefaultEnvironment.OnWindows)
                {
                    GitPackageFeed = $"http://localhost:{server.Port}/unity/git/windows/{GitInstaller.GitInstallDetails.GitPackageName}",
                    GitLfsPackageFeed = $"http://localhost:{server.Port}/unity/git/windows/{GitInstaller.GitInstallDetails.GitLfsPackageName}",
                };

            var package = Package.Load(Environment, installDetails.GitPackageFeed);
            var downloader = new Downloader();
            downloader.Catch(e => true);
            downloader.QueueDownload(package.Uri, installDetails.ZipPath);
            downloader.RunWithReturn(true);

            var tempZipExtractPath = TestBasePath.Combine("Temp", "git_zip_extract_zip_paths");

            var gitExtractPath = tempZipExtractPath.Combine("git").CreateDirectory();
            ZipHelper.Instance.Extract(installDetails.GitZipPath, gitExtractPath, TaskManager.Token, null);
            var source = gitExtractPath;
            var target = customGitInstall;
            target.DeleteIfExists();
            target.EnsureParentDirectoryExists();
            source.Move(target);
            var gitExec = customGitInstall.Combine("cmd/git.exe");
            Environment.SystemSettings.Set(Constants.GitInstallPathKey, gitExec.ToString());

            var gitInstaller = new GitInstaller(Environment, ProcessManager, TaskManager.Token, Environment.SystemSettings, installDetails: installDetails);

            var state = gitInstaller.SetupGitIfNeeded();
            state.Should().NotBeNull();

            var gitLfsBasePath = defaultGitInstall.Combine(installDetails.PackageNameWithVersion);
            var gitLfsExec = gitLfsBasePath.Combine("mingw32", "libexec", "git-core", "git-lfs.exe");
            state.GitInstallationPath.Should().Be(customGitInstall);
            state.GitExecutablePath.Should().Be(gitExec);
            state.GitLfsInstallationPath.Should().Be(gitLfsBasePath);
            state.GitLfsExecutablePath.Should().Be(gitLfsExec);
            gitLfsExec.FileExists().Should().BeTrue();

            var isCustomGitExec = state.GitExecutablePath != installDetails.GitExecutablePath;

            Environment.GitExecutablePath = state.GitExecutablePath;
            Environment.GitLfsExecutablePath = state.GitLfsExecutablePath;
            Environment.IsCustomGitExecutable = isCustomGitExec;

            var procTask = new SimpleProcessTask(TaskManager.Token, "something")
                .Configure(ProcessManager);
            var pathList = procTask.Process.StartInfo.EnvironmentVariables["PATH"].ToNPathList(Environment).TakeWhile(x => x != "END");
            pathList.First().Should().Be(gitExec.Parent);
            pathList.Any(x => x == gitLfsExec.Parent).Should().BeTrue();
        }
    }
}
