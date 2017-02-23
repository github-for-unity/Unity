using System.Collections.Generic;
using System.Linq;
using GitHub.Unity;
using NUnit.Framework;
using System.Threading;
using System.Threading.Tasks;
using Rackspace.Threading;

namespace IntegrationTests
{
    [TestFixture]
    class LoginIntegrationTests : BaseGitIntegrationTest
    {
        string FindCommonPath(IEnumerable<string> paths)
        {
            var longestPath =
                paths.First(first => first.Length == paths.Max(second => second.Length))
                .ToNPath();

            NPath commonParent = longestPath;
            foreach (var path in paths)
            {
                var cp = commonParent.GetCommonParent(path);
                if (cp != null)
                    commonParent = cp;
                else
                {
                    commonParent = null;
                    break;
                }
            }
            return commonParent;
        }

        [Test]
        public void CommonParentTest()
        {
            var filesystem = new FileSystem(TestGitRepoPath);
            NPathFileSystemProvider.Current = filesystem;
            var environment = new DefaultEnvironment();

            var ret = FindCommonPath(new string[]
            {
                "Assets/Test/Path/file",
                "Assets/Test/something",
                "Assets/Test/Path/another",
                "Assets/alkshdsd",
                "Assets/Test/sometkjh",
            });

            Assert.AreEqual("Assets", ret);
        }

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
            IPlatform platform = null;
            platform = new Platform(environment, filesystem, new TestUIDispatcher(() =>
            {
                Logger.Debug("Called");
                platform.CredentialManager.Save(new Credential("https://github.com", "username", "token")).Wait();
                return true;
            }));
            var gitEnvironment = platform.GitEnvironment;
            var processManager = new ProcessManager(environment, gitEnvironment);
            await platform.Initialize(processManager);
            using (var repoManager = new RepositoryManager(TestGitRepoPath, platform, CancellationToken.None))
            {
                var repository = repoManager.Repository;
                environment.Repository = repoManager.Repository;

                var task = repository.Pull(
                     new TaskResultDispatcher<string>(x =>
                    {
                        Logger.Debug("Pull result: {0}", x);
                    })
                );
                await task.RunAsync(CancellationToken.None);

                //string credHelper = null;
                //var task = new GitConfigGetTask(environment, processManager,
                //    new TaskResultDispatcher<string>(x =>
                //    {
                //        Logger.Debug("CredHelper set to {0}", x);
                //        credHelper = x;
                //    }),
                //    "credential.helper", GitConfigSource.NonSpecified);

                //await task.RunAsync(CancellationToken.None);
                //Assert.NotNull(credHelper);
            }

            //string remoteUrl = null;
            //var ret = await GitTask.Run(environment, processManager, "remote get-url origin-http", x => remoteUrl = x);
            //Assert.True(ret);
        }
    }
}
