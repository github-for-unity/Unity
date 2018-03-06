using System;
using NUnit.Framework;
using GitHub.Unity;
using NCrunch.Framework;
using System.Threading;
using GitHub.Logging;
using System.Linq;
using System.IO;

namespace IntegrationTests
{
    [Isolated]
    class BaseIntegrationTest
    {
        public IRepositoryManager RepositoryManager { get; set; }

        protected IApplicationManager ApplicationManager { get; set; }

        protected NPath DotGitConfig { get; set; }

        protected NPath DotGitHead { get; set; }

        protected NPath DotGitIndex { get; set; }

        protected NPath RemotesPath { get; set; }

        protected NPath BranchesPath { get; set; }

        protected NPath DotGitPath { get; set; }

        protected NPath TestRepoMasterCleanSynchronized { get; set; }

        protected NPath TestRepoMasterCleanUnsynchronized { get; set; }

        protected NPath TestRepoMasterCleanUnsynchronizedRussianLanguage { get; set; }

        protected NPath TestRepoMasterDirtyUnsynchronized { get; set; }

        protected NPath TestRepoMasterTwoRemotes { get; set; }

        protected static string TestZipFilePath => Path.Combine(SolutionDirectory, "IOTestsRepo.zip");
        protected ITaskManager TaskManager { get; set; }
        protected SynchronizationContext SyncContext { get; set; }

        protected IPlatform Platform { get; set; }
        protected IProcessManager ProcessManager { get; set; }
        protected IProcessEnvironment GitEnvironment => Platform.GitEnvironment;
        protected IGitClient GitClient { get; set; }

        protected NPath TestBasePath { get; set; }
        protected ILogging Logger { get; set; }
        public IEnvironment Environment { get; set; }
        public IRepository Repository => Environment.Repository;

        protected TestUtils.SubstituteFactory Factory { get; set; }
        protected static NPath SolutionDirectory => TestContext.CurrentContext.TestDirectory.ToNPath();

        protected void InitializeEnvironment(NPath repoPath = null,
            NPath environmentPath = null,
            bool enableEnvironmentTrace = false,
            bool initializeRepository = true
            )
        {
            var cacheContainer = new CacheContainer();
            cacheContainer.SetCacheInitializer(CacheType.Branches, () => BranchesCache.Instance);
            cacheContainer.SetCacheInitializer(CacheType.GitAheadBehind, () => GitAheadBehindCache.Instance);
            cacheContainer.SetCacheInitializer(CacheType.GitLocks, () => GitLocksCache.Instance);
            cacheContainer.SetCacheInitializer(CacheType.GitLog, () => GitLogCache.Instance);
            cacheContainer.SetCacheInitializer(CacheType.GitStatus, () => GitStatusCache.Instance);
            cacheContainer.SetCacheInitializer(CacheType.GitUser, () => GitUserCache.Instance);
            cacheContainer.SetCacheInitializer(CacheType.RepositoryInfo, () => RepositoryInfoCache.Instance);

            Environment = new IntegrationTestEnvironment(cacheContainer,
                repoPath,
                SolutionDirectory,
                environmentPath,
                enableEnvironmentTrace,
                initializeRepository);
        }

        protected void InitializePlatform(NPath repoPath, NPath environmentPath, bool enableEnvironmentTrace, bool initializeRepository = true, bool setupGit = true)
        {
            InitializeTaskManager();
            InitializeEnvironment(repoPath, environmentPath, enableEnvironmentTrace, initializeRepository);

            Platform = new Platform(Environment);
            ProcessManager = new ProcessManager(Environment, GitEnvironment, TaskManager.Token);

            Platform.Initialize(ProcessManager, TaskManager);

            if (setupGit)
                SetupGit(Environment.GetSpecialFolder(System.Environment.SpecialFolder.LocalApplicationData).ToNPath());
        }

        protected void InitializeTaskManager()
        {
            TaskManager = new TaskManager();
            SyncContext = new ThreadSynchronizationContext(TaskManager.Token);
            TaskManager.UIScheduler = new SynchronizationContextTaskScheduler(SyncContext);
        }

        protected IEnvironment InitializePlatformAndEnvironment(NPath repoPath,
            NPath environmentPath = null,
            bool enableEnvironmentTrace = false,
            bool setupGit = true,
            Action<IRepositoryManager> onRepositoryManagerCreated = null)
        {
            InitializePlatform(repoPath, environmentPath, enableEnvironmentTrace, setupGit);

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

            RepositoryManager = GitHub.Unity.RepositoryManager.CreateInstance(Platform, TaskManager, GitClient, Environment.FileSystem, repoPath);
            RepositoryManager.Initialize();

            onRepositoryManagerCreated?.Invoke(RepositoryManager);

            Environment.Repository?.Initialize(RepositoryManager, TaskManager);

            RepositoryManager.Start();
            Environment.Repository?.Start();
            return Environment;
        }

        protected void SetupGit(NPath pathToSetupGitInto)
        {
            var autoResetEvent = new AutoResetEvent(false);

            var installDetails = new GitInstaller.GitInstallDetails(pathToSetupGitInto, true);

            var zipArchivesPath = pathToSetupGitInto.Combine("downloads").CreateDirectory();
            AssemblyResources.ToFile(ResourceType.Platform, "git.zip", zipArchivesPath, Environment);
            AssemblyResources.ToFile(ResourceType.Platform, "git-lfs.zip", zipArchivesPath, Environment);

            var gitInstaller = new GitInstaller(Environment, TaskManager.Token, installDetails);

            NPath result = null;
            Exception ex = null;

            var setupTask = gitInstaller.SetupGitIfNeeded();
            setupTask.OnEnd += (thisTask, path, success, exception) => {
                result = path;
                ex = exception;
                autoResetEvent.Set();
            };

            autoResetEvent.WaitOne();

            if (result == null)
            {
                if (ex != null)
                {
                    throw ex;
                }

                throw new Exception("Did not install git");
            }

            Environment.GitExecutablePath = result;
            GitClient = new GitClient(Environment, ProcessManager, TaskManager.Token);
        }

        [TestFixtureSetUp]
        public virtual void TestFixtureSetUp()
        {
            Logger = LogHelper.GetLogger(GetType());
            Factory = new TestUtils.SubstituteFactory();
            GitHub.Unity.Guard.InUnitTestRunner = true;
        }

        [TestFixtureTearDown]
        public virtual void TestFixtureTearDown()
        {
        }

        [SetUp]
        public virtual void OnSetup()
        {
            TestBasePath = NPath.CreateTempDirectory("integration-tests");
            NPath.FileSystem.SetCurrentDirectory(TestBasePath);
            TestRepoMasterCleanUnsynchronized = TestBasePath.Combine("IOTestsRepo", "IOTestsRepo_master_clean_unsync");
            TestRepoMasterCleanUnsynchronizedRussianLanguage = TestBasePath.Combine("IOTestsRepo", "IOTestsRepo_master_clean_sync_with_russian_language");
            TestRepoMasterCleanSynchronized = TestBasePath.Combine("IOTestsRepo", "IOTestsRepo_master_clean_sync");
            TestRepoMasterDirtyUnsynchronized = TestBasePath.Combine("IOTestsRepo", "IOTestsRepo_master_dirty_unsync");
            TestRepoMasterTwoRemotes = TestBasePath.Combine("IOTestsRepo", "IOTestsRepo_master_two_remotes");

            InitializeTaskManager();
        }

        [TearDown]
        public virtual void OnTearDown()
        {
            TaskManager.Dispose();
            Logger.Debug("Deleting TestBasePath: {0}", TestBasePath.ToString());
            for (var i = 0; i < 5; i++)
            {
                try
                {
                    TestBasePath.Delete();
                    break;
                }
                catch (Exception)
                {
                    Thread.Sleep(100);
                }
            }
            if (TestBasePath.Exists())
                Logger.Warning("Error deleting TestBasePath: {0}", TestBasePath.ToString());

            NPath.FileSystem = null;
        }
    }
}