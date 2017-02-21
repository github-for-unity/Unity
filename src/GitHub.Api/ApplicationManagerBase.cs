using Rackspace.Threading;
using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace GitHub.Unity
{
    class ApplicationManagerBase : IApplicationManager
    {
        protected static readonly ILogging logger = Logging.GetLogger<IApplicationManager>();

        public ApplicationManagerBase(SynchronizationContext synchronizationContext)
        {
            SynchronizationContext = synchronizationContext;
            SynchronizationContext.SetSynchronizationContext(SynchronizationContext);
            ThreadingHelper.SetMainThread();
            Scheduler = TaskScheduler.FromCurrentSynchronizationContext();
            ThreadingHelper.MainThreadScheduler = Scheduler;
            CancellationTokenSource = new CancellationTokenSource();
        }

        public virtual Task Run()
        {
            Task task = null;
            try
            {
                task = RunInternal();
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                throw;
            }
            return task;
        }

        private async Task RunInternal()
        {
            await ThreadingHelper.SwitchToThreadAsync();
            var gitSetup = new GitSetup(Environment, CancellationToken);
            var expectedPath = gitSetup.GitInstallationPath;

            bool setupDone = false;
            if (!gitSetup.GitExecutablePath.FileExists())
            {
                setupDone = await gitSetup.SetupIfNeeded(
                    //new Progress<float>(x => logger.Trace("Percentage: {0}", x)),
                    //new Progress<long>(x => logger.Trace("Remaining: {0}", x))
                );
            }

            if (setupDone)
                Environment.GitExecutablePath = gitSetup.GitExecutablePath;
            else
                Environment.GitExecutablePath = await LookForGitInstallationPath();
            Environment.Repository = GitClient.GetRepository();
        }

        private async Task<string> LookForGitInstallationPath()
        {
            NPath cachedGitInstallPath = null;
            var path = SystemSettings.Get("GitInstallPath");
            if (!String.IsNullOrEmpty(path))
                cachedGitInstallPath = path.ToNPath();

            // Root paths
            if (cachedGitInstallPath == null ||
               !cachedGitInstallPath.DirectoryExists())
            {
                return await GitEnvironment.FindGitInstallationPath(ProcessManager);
            }
            else
            {
                return cachedGitInstallPath.ToString();
            }
        }


        public CancellationToken CancellationToken { get; protected set; }
        public ICredentialManager CredentialManager { get; protected set; }
        public IFileSystem FileSystem { get; protected set; }
        public IGitClient GitClient { get; protected set; }
        public GitObjectFactory GitObjectFactory { get; protected set; }
        public ISettings LocalSettings { get; protected set; }
        public IPlatform Platform { get; protected set; }
        public IProcessManager ProcessManager { get; protected set; }
        public ISettings SystemSettings { get; protected set; }
        public ITaskResultDispatcher TaskResultDispatcher { get; protected set; }
        public ISettings UserSettings { get; protected set; }
        public virtual IGitEnvironment GitEnvironment { get; set; }
        public virtual IEnvironment Environment { get; set; }
        protected CancellationTokenSource CancellationTokenSource { get; private set; }
        protected TaskScheduler Scheduler { get; private set; }
        protected SynchronizationContext SynchronizationContext { get; private set; }

    }
}