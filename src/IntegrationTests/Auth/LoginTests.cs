using System.Collections.Generic;
using System.Linq;
using GitHub.Unity;
using NUnit.Framework;
using System.Threading;
using System.Threading.Tasks;
using Rackspace.Threading;
using TestUtils;

namespace IntegrationTests
{
    [TestFixture]
    class LoginIntegrationTests : BaseGitEnvironmentTest
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
            var filesystem = new FileSystem(TestRepoMasterDirtyUnsynchronized);
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

        [Test, Ignore]
        public async void NetworkTaskTest()
        {
            var filesystem = new FileSystem();
            NPathFileSystemProvider.Current = filesystem;
            IPlatform platform = null;
            platform = new Platform(Environment, filesystem, new TestUIDispatcher(() => {
                Logger.Debug("Called");
                platform.CredentialManager.Save(new Credential("https://github.com", "username", "token")).Wait();
                return true;
            }));
            var gitEnvironment = platform.GitEnvironment;
            var processManager = new ProcessManager(Environment, gitEnvironment);
            await platform.Initialize(processManager);

            var taskRunner = new TaskRunner(new MainThreadSynchronizationContextBase(), CancellationToken.None);
            var repositoryManagerFactory = new RepositoryManagerFactory();
            using (var repoManager = repositoryManagerFactory.CreateRepositoryManager(platform, taskRunner, TestRepoMasterDirtyUnsynchronized, CancellationToken.None))
            {
                var repository = repoManager.Repository;
                Environment.Repository = repoManager.Repository;

                repository.Pull(new TaskResultDispatcher<string>(x => { Logger.Debug("Pull result: {0}", x); }));

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
