using GitHub.Unity;
using NUnit.Framework;
using System.Threading;
using System.Threading.Tasks;
using Rackspace.Threading;
using GitHub.Unity.Tests;

namespace GitHub.Unity.IntegrationTests
{
    [TestFixture]
    class LoginIntegrationTests : BaseIntegrationTest
    {
        //[Test]
        //public async void SimpleLogin()
        //{
        //    var program = new AppConfiguration();
        //    var filesystem = new FileSystem();
        //    var environment = new DefaultEnvironment();
        //    environment.GitExecutablePath = @"C:\soft\Git\cmd\git.exe";
        //    environment.RepositoryRoot = TestGitRepoPath;
        //    var platform = new Platform(environment, filesystem);
        //    var gitEnvironment = platform.GitEnvironment;
        //    var processManager = new ProcessManager(environment, gitEnvironment, filesystem);
        //    var credentialManager = new WindowsCredentialManager(environment, processManager);
        //    var api = new ApiClientFactory(program, credentialManager);
        //    var hostAddress = HostAddress.GitHubDotComHostAddress;
        //    var client = api.Create(UriString.ToUriString(HostAddress.GitHubDotComHostAddress.WebUri));
        //    int called = 0;
        //}

        [Test]
        public async void NetworkTaskTest()
        {
            var filesystem = new FileSystem();
            NPathFileSystemProvider.Current = filesystem;
            var environment = new DefaultEnvironment();

            var gitSetup = new GitSetup(environment, CancellationToken.None);
            var expectedPath = gitSetup.GitInstallationPath;

            bool setupDone = false;
            float percent;
            long remain;
            // Root paths
            if (!gitSetup.GitExecutablePath.FileExists())
            {
                setupDone = await gitSetup.SetupIfNeeded(
                    //new Progress<float>(x => Logger.Trace("Percentage: {0}", x)),
                    //new Progress<long>(x => Logger.Trace("Remaining: {0}", x))
                    new Progress<float>(x => percent = x),
                    new Progress<long>(x => remain = x)
                );
            }
            environment.GitExecutablePath = gitSetup.GitExecutablePath;
            environment.UnityProjectPath = TestGitRepoPath;
            var platform = new Platform(environment, filesystem);
            var gitEnvironment = platform.GitEnvironment;
            var processManager = new ProcessManager(environment, gitEnvironment);
            var gitClient = new GitClient(TestGitRepoPath);
            environment.Repository = gitClient.GetRepository();

            var credentialManager = new WindowsCredentialManager(environment, processManager);

            string credHelper = null;
            var task = new GitConfigGetTask(environment, processManager, null,
                "credential.helper", GitConfigSource.NonSpecified,
                x =>
                {
                    credHelper = x;
                },
                null);

            await task.RunAsync(CancellationToken.None);
            Assert.NotNull(credHelper);


            //string remoteUrl = null;
            //var ret = await GitTask.Run(environment, processManager, "remote get-url origin-http", x => remoteUrl = x);
            //Assert.True(ret);
        }
    }
}
