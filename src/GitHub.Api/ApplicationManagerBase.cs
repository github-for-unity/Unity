using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace GitHub.Unity
{
    public interface IAwaitable
    {
        IAwaiter GetAwaiter();
    }

    public interface IAwaiter : INotifyCompletion
    {
        bool IsCompleted { get; }
        void GetResult();
    }

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
            Environment.GitExecutablePath = DetermineGitInstallationPath();
            Environment.Repository = GitClient.GetRepository();
        }

        private string DetermineGitInstallationPath()
        {
            var cachedGitInstallPath = SystemSettings.Get("GitInstallPath");

            // Root paths
            if (string.IsNullOrEmpty(cachedGitInstallPath) || !cachedGitInstallPath.ToNPath().Exists())
            {
                return GitEnvironment.FindGitInstallationPath(ProcessManager).Result;
            }
            else
            {
                return cachedGitInstallPath;
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