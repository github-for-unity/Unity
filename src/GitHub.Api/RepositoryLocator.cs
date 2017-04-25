using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace GitHub.Unity
{
    interface ITaskQueueScheduler
    {
        void Queue(ITask task);
    }

    class RepositoryInitializerBase
    {
        private readonly IEnvironment environment;
        private readonly IProcessManager processManager;
        private readonly ITaskQueueScheduler scheduler;
        private readonly IApplicationManager applicationManager;

        public RepositoryInitializerBase(IEnvironment environment, IProcessManager processManager,
            ITaskQueueScheduler scheduler, IApplicationManager applicationManager)
        {
            Logger = Logging.GetLogger(GetType());

            this.environment = environment;
            this.processManager = processManager;
            this.scheduler = scheduler;
            this.applicationManager = applicationManager;
        }

        public void Run()
        {
            var targetPath = NPath.CurrentDirectory;

            var token = CancellationToken.None;
            var task = new BaseTask(() =>
            {
                Logger.Trace("Git Init");

                var initTask = new GitInitTask(environment, processManager, null);
                return initTask.RunAsync(token)
                    .ContinueWith(_ =>
                    {
                        Logger.Trace("LFS install");

                        var t = new GitLfsInstallTask(environment, processManager, null);
                        return t.RunAsync(token);
                    }, token, TaskContinuationOptions.NotOnCanceled | TaskContinuationOptions.NotOnFaulted, ThreadingHelper.TaskScheduler)
                    .ContinueWith(_ =>
                    {
                        Logger.Trace("Adding files");

                        SetProjectToTextSerialization();

                        var gitignore = targetPath.Combine(".gitignore");
                        var gitAttrs = targetPath.Combine(".gitattributes");

                        AssemblyResources.ToFile(ResourceType.Generic, ".gitignore", targetPath);
                        AssemblyResources.ToFile(ResourceType.Generic, ".gitattributes", targetPath);

                        var assetsGitignore = targetPath.Combine("Assets", ".gitignore");
                        assetsGitignore.CreateFile();

                        var filesForInitialCommit = new List<string> { gitignore, gitAttrs, assetsGitignore };

                        var addTask = new GitAddTask(environment, processManager, null, filesForInitialCommit);
                        return addTask.RunAsync(token);
                    }, token, TaskContinuationOptions.NotOnCanceled | TaskContinuationOptions.NotOnFaulted, ThreadingHelper.TaskScheduler)
                    .ContinueWith(_ =>
                    {
                        Logger.Trace("Commiting");

                        var commitTask = new GitCommitTask(environment, processManager, null, "Initial commit", null);
                        return commitTask.RunAsync(token);
                    }, token, TaskContinuationOptions.NotOnCanceled | TaskContinuationOptions.NotOnFaulted, ThreadingHelper.TaskScheduler)
                    .ContinueWith(_ =>
                    {
                        Logger.Trace("Restarting");

                        applicationManager.RestartRepository();
                        return true;
                    }, token, TaskContinuationOptions.NotOnCanceled | TaskContinuationOptions.NotOnFaulted, ThreadingHelper.TaskScheduler);

            });

            task.Critical = false;
            task.Queued = TaskQueueSetting.QueueSingle;
            task.Blocking = false;
            task.Label = "Initializing repository";

            scheduler.Queue(task);
        }

        protected virtual void SetProjectToTextSerialization()
        {
        }

        protected static ILogging Logger { get; private set; }
    }

    interface IRepositoryLocator
    {
        NPath FindRepositoryRoot();
    }

    class RepositoryLocator : IRepositoryLocator
    {
        private readonly NPath localPath;
        private NPath repositoryPath;

        public RepositoryLocator(NPath localPath)
        {
            this.localPath = localPath;
            Guard.ArgumentNotNullOrWhiteSpace(localPath, nameof(localPath));

            if (localPath.IsRelative)
                throw new InvalidOperationException("GitClient localPath has to be absolute");
        }

        public NPath FindRepositoryRoot()
        {
            if (repositoryPath == null)
            {
                repositoryPath = FindRepositoryRoot(localPath);
            }

            return repositoryPath;
        }

        private NPath FindRepositoryRoot(NPath path)
        {
            Logger.Trace("{0} {1}", path, path.Exists(".git"));
            if (path.Exists(".git"))
                return path;

            if (!path.IsRoot)
                return FindRepositoryRoot(path.Parent);

            return null;
        }

        protected static ILogging Logger { get; } = Logging.GetLogger<RepositoryLocator>();
    }
}