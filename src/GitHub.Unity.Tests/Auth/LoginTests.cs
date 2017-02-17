using GitHub.Unity;
using NUnit.Framework;
using System.Threading;
using System.Threading.Tasks;

namespace GitHub.Unity.Tests
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

        //[Test]
        //public async void NetworkTaskTest()
        //{
        //    var program = new AppConfiguration();
        //    var filesystem = new FileSystem();
        //    var environment = new DefaultEnvironment();
        //    var platform = new Platform(environment, filesystem);
        //    var gitEnvironment = platform.GitEnvironment;
        //    var processManager = new ProcessManager(environment, gitEnvironment, filesystem);
        //    var gitPath = await gitEnvironment.FindGitInstallationPath(processManager);
        //    environment.GitExecutablePath = gitPath;
        //    var credentialManager = new WindowsCredentialManager(environment, processManager);

        //    string credHelper = null;
        //    var task = new GitConfigGetTask(environment, processManager, null,
        //        "credential.helper", GitConfigSource.NonSpecified,
        //        x =>
        //        {
        //            credHelper = x;
        //        },
        //        null);

        //    await task.RunAsync();
        //    Assert.NotNull(credHelper);


        //    //string remoteUrl = null;
        //    //var ret = await GitTask.Run(environment, processManager, "remote get-url origin-http", x => remoteUrl = x);
        //    //Assert.True(ret);
        //}
    }
}
