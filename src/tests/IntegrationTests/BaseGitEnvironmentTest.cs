using System;
using System.IO;
using System.Linq;
using System.Threading;
using GitHub.Unity;

namespace IntegrationTests
{
    class BaseGitEnvironmentTest : BasePlatformIntegrationTest
    {
        protected IEnvironment Initialize(NPath repoPath, NPath environmentPath = null,
            bool enableEnvironmentTrace = false, bool initializeRepository = true,
            Action<RepositoryManager> onRepositoryManagerCreated = null)
        {
            InitializePlatform(repoPath, environmentPath, enableEnvironmentTrace);

            var repositoryManager = GitHub.Unity.RepositoryManager.CreateInstance(Platform, TaskManager, GitClient, ProcessManager, Environment.FileSystem, repoPath);
            onRepositoryManagerCreated?.Invoke(repositoryManager);

            RepositoryManager = repositoryManager;
            RepositoryManager.Initialize();

            if (initializeRepository)
            {
                Environment.Repository = new Repository(repoPath, CacheContainer);
                Environment.Repository.Initialize(RepositoryManager);
            }

            RepositoryManager.Start();

            DotGitPath = repoPath.Combine(".git");

            if (DotGitPath.FileExists())
            {
                DotGitPath = DotGitPath.ReadAllLines().Where(x => x.StartsWith("gitdir:"))
                                       .Select(x => x.Substring(7).Trim().ToNPath()).First();
            }

            BranchesPath = DotGitPath.Combine("refs", "heads");
            RemotesPath = DotGitPath.Combine("refs", "remotes");
            DotGitIndex = DotGitPath.Combine("index");
            DotGitHead = DotGitPath.Combine("HEAD");
            DotGitConfig = DotGitPath.Combine("config");
            return Environment;
        }

        public override void OnSetup()
        {
            base.OnSetup();

            TestRepoMasterCleanUnsynchronized = TestBasePath.Combine("IOTestsRepo", "IOTestsRepo_master_clean_unsync");
            TestRepoMasterCleanUnsynchronizedRussianLanguage = TestBasePath.Combine("IOTestsRepo", "IOTestsRepo_master_clean_sync_with_russian_language");
            TestRepoMasterCleanSynchronized = TestBasePath.Combine("IOTestsRepo", "IOTestsRepo_master_clean_sync");
            TestRepoMasterDirtyUnsynchronized = TestBasePath.Combine("IOTestsRepo", "IOTestsRepo_master_dirty_unsync");
            TestRepoMasterTwoRemotes = TestBasePath.Combine("IOTestsRepo", "IOTestsRepo_master_two_remotes");

            Logger.Trace("Extracting Zip File to {0}", TestBasePath);
            ZipHelper.ExtractZipFile(TestZipFilePath, TestBasePath.ToString(), TaskManager.Token, (value, total) => true);
            Logger.Trace("Extracted Zip File");
        }

        public override void OnTearDown()
        {
            RepositoryManager?.Stop();
            RepositoryManager?.Dispose();
            RepositoryManager = null;
            base.OnTearDown();
        }

        public IRepositoryManager RepositoryManager { get; private set; }

        protected IApplicationManager ApplicationManager { get; set; }

        protected NPath DotGitConfig { get; private set; }

        protected NPath DotGitHead { get; private set; }

        protected NPath DotGitIndex { get; private set; }

        protected NPath RemotesPath { get; private set; }

        protected NPath BranchesPath { get; private set; }

        protected NPath DotGitPath { get; private set; }

        protected NPath TestRepoMasterCleanSynchronized { get; private set; }

        protected NPath TestRepoMasterCleanUnsynchronized { get; private set; }

        protected NPath TestRepoMasterCleanUnsynchronizedRussianLanguage { get; private set; }

        protected NPath TestRepoMasterDirtyUnsynchronized { get; private set; }

        protected NPath TestRepoMasterTwoRemotes { get; private set; }

        private static string TestZipFilePath => Path.Combine(SolutionDirectory, "IOTestsRepo.zip");
    }
}
