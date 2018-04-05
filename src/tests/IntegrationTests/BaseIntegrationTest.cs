using System;
using System.Diagnostics;
using NUnit.Framework;
using GitHub.Unity;
using NCrunch.Framework;
using System.Threading;
using GitHub.Logging;
using System.Linq;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

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

        protected void InitializeEnvironment(NPath repoPath,
            NPath? environmentPath = null,
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

        protected void InitializePlatform(NPath repoPath, NPath? environmentPath = null,
            bool enableEnvironmentTrace = true,
            bool setupGit = true,
            string testName = "",
            bool initializeRepository = true)
        {
            InitializeTaskManager();
            InitializeEnvironment(repoPath, environmentPath, enableEnvironmentTrace, initializeRepository);

            Platform = new Platform(Environment);
            ProcessManager = new ProcessManager(Environment, GitEnvironment, TaskManager.Token);

            Platform.Initialize(ProcessManager, TaskManager);

            if (setupGit)
                SetupGit(Environment.GetSpecialFolder(System.Environment.SpecialFolder.LocalApplicationData).ToNPath(), testName);
        }

        protected void InitializeTaskManager()
        {
            TaskManager = new TaskManager();
            SyncContext = new ThreadSynchronizationContext(TaskManager.Token);
            TaskManager.UIScheduler = new SynchronizationContextTaskScheduler(SyncContext);
        }

        protected IEnvironment InitializePlatformAndEnvironment(NPath repoPath,
            NPath? environmentPath = null,
            bool enableEnvironmentTrace = false,
            bool setupGit = true,
            Action<IRepositoryManager> onRepositoryManagerCreated = null,
            [CallerMemberName] string testName = "")
        {
            InitializePlatform(repoPath, environmentPath, enableEnvironmentTrace, setupGit, testName);

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

        protected void SetupGit(NPath pathToSetupGitInto, string testName)
        {
            var autoResetEvent = new AutoResetEvent(false);

            var installDetails = new GitInstaller.GitInstallDetails(pathToSetupGitInto, Environment.IsWindows);

            var zipArchivesPath = pathToSetupGitInto.Combine("downloads").CreateDirectory();

            Logger.Trace($"Saving git zips into {zipArchivesPath} and unzipping to {pathToSetupGitInto}");

            AssemblyResources.ToFile(ResourceType.Platform, "git.zip", zipArchivesPath, Environment);
            AssemblyResources.ToFile(ResourceType.Platform, "git.zip.md5", zipArchivesPath, Environment);
            AssemblyResources.ToFile(ResourceType.Platform, "git-lfs.zip", zipArchivesPath, Environment);
            AssemblyResources.ToFile(ResourceType.Platform, "git-lfs.zip.md5", zipArchivesPath, Environment);

            var gitInstaller = new GitInstaller(Environment, ProcessManager, TaskManager, installDetails);

            NPath? result = null;
            Exception ex = null;

            var setupTask = gitInstaller.SetupGitIfNeeded().Finally((success, state) =>
                {
                    result = state.GitExecutablePath;
                    autoResetEvent.Set();
                });
            setupTask.Start();

            if (!autoResetEvent.WaitOne(TimeSpan.FromMinutes(5)))
                throw new TimeoutException($"Test setup unzipping {zipArchivesPath} to {pathToSetupGitInto} timed out");

            if (result == null)
            {
                if (ex != null)
                {
                    throw ex;
                }

                throw new Exception("Did not install git");
            }

            Environment.GitExecutablePath = result.Value;
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
            TestBasePath = NPath.CreateTempDirectory("integration tests");
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
            Environment?.CacheContainer.Dispose();
            BranchesCache.Instance = null;
            GitAheadBehindCache.Instance = null;
            GitLocksCache.Instance = null;
            GitLogCache.Instance = null;
            GitStatusCache.Instance = null;
            GitUserCache.Instance = null;
            RepositoryInfoCache.Instance = null;

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

        protected void StartTest(out Stopwatch watch, out ILogging logger, [CallerMemberName] string testName = "test")
        {
            watch = new Stopwatch();
            logger = LogHelper.GetLogger(testName);
            logger.Trace("Starting test");
        }

        protected void EndTest(ILogging logger)
        {
            logger.Trace("Ending test");
        }

        protected void StartTrackTime(Stopwatch watch, ILogging logger = null, string message = "")
        {
            if (!String.IsNullOrEmpty(message))
                logger.Trace(message);
            watch.Reset();
            watch.Start();
        }

        protected void StopTrackTimeAndLog(Stopwatch watch, ILogging logger)
        {
            watch.Stop();
            logger.Trace($"Time: {watch.ElapsedMilliseconds}");
        }
    }
}