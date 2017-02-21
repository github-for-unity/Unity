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


    class RepositoryInitializer
    {
        private readonly IEnvironment environment;
        private readonly IProcessManager processManager;
        private readonly ITaskQueueScheduler scheduler;
        private readonly IApplicationManager applicationManager;

        public RepositoryInitializer(IEnvironment environment, IProcessManager processManager,
            ITaskQueueScheduler scheduler, IApplicationManager applicationManager)
        {
            this.environment = environment;
            this.processManager = processManager;
            this.scheduler = scheduler;
            this.applicationManager = applicationManager;
        }

        public void Run()
        {
            var targetPath = NPath.CurrentDirectory;

            //var finishedDispatcher = new TaskResultDispatcher<string>(_ =>
            //{
            //    applicationManager.RestartRepository();
            //});

            //var addDone = new TaskResultDispatcher<string>(_ =>
            //{
            //    var commitTask = new GitCommitTask(environment, processManager, finishedDispatcher, "Initial commit", null);
            //    scheduler.Queue(commitTask);
            //});

            //var initDone = new TaskResultDispatcher<string>(_ =>
            //{
            //    scheduler.Queue(addTask);
            //});



            var token = CancellationToken.None;
            var task = new BaseTask(() =>
            {
                var initTask = new GitInitTask(environment, processManager, null);
                return initTask.RunAsync(token)
                    .ContinueWith(_ =>
                    {
                        Logger.Trace("LFS init");

                        var t = new GitInitTask(environment, processManager, null);
                        t.SetArguments("lfs init");
                        return t.RunAsync(token);
                    }, token, TaskContinuationOptions.NotOnCanceled | TaskContinuationOptions.NotOnFaulted, TaskScheduler.Default)
                    .ContinueWith(_ =>
                    {
                        Logger.Trace("Adding files");

                        var gitignore = targetPath.Combine(".gitignore");
                        var gitAttrs = targetPath.Combine(".gitattributes");
                        var filesForInitialCommit = new List<string> { gitignore, gitAttrs };
                        AssemblyResources.ToFile(ResourceType.Generic, ".gitignore", targetPath);
                        AssemblyResources.ToFile(ResourceType.Generic, ".gitattributes", targetPath);

                        var assetsPath = targetPath.Combine("Assets");
                        var hasFiles = assetsPath.Files(true).Any();
                        if (!hasFiles)
                        {
                            var placeholder = assetsPath.CreateFile(".placeholder");
                            filesForInitialCommit.Add(placeholder);
                        }
                        var addTask = new GitAddTask(environment, processManager, null, filesForInitialCommit);
                        return addTask.RunAsync(token);
                    }, token, TaskContinuationOptions.NotOnCanceled | TaskContinuationOptions.NotOnFaulted, TaskScheduler.Default)
                    .ContinueWith(_ =>
                    {
                        Logger.Trace("Commiting");

                        var commitTask = new GitCommitTask(environment, processManager, null, "Initial commit", null);
                        return commitTask.RunAsync(token);
                    }, token, TaskContinuationOptions.NotOnCanceled | TaskContinuationOptions.NotOnFaulted, TaskScheduler.Default)
                    .ContinueWith(_ =>
                    {
                        Logger.Trace("Restarting");

                        applicationManager.RestartRepository();
                        return true;
                    }, token, TaskContinuationOptions.NotOnCanceled | TaskContinuationOptions.NotOnFaulted, TaskScheduler.Default);

            });

            task.Critical = true;
            task.Queued = TaskQueueSetting.QueueSingle;
            task.Blocking = true;
            task.Label = "Initializing repository";

            scheduler.Queue(task);

            //var initTask = new GitInitTask(environment, processManager, initDone);
            //scheduler.Queue(initTask);
        }

        protected static ILogging Logger { get; } = Logging.GetLogger<RepositoryInitializer>();
    }

    interface IRepositoryLocator
    {
        NPath FindRepositoryRoot();
    }

    class RepositoryLocator : IRepositoryLocator
    {
        private readonly NPath localPath;

        public RepositoryLocator(NPath localPath)
        {
            this.localPath = localPath;
            Guard.ArgumentNotNullOrWhiteSpace(localPath, nameof(localPath));

            if (localPath.IsRelative)
                throw new InvalidOperationException("GitClient localPath has to be absolute");
        }

        public NPath FindRepositoryRoot()
        {
            RepositoryPath = FindRepositoryRoot(localPath);
            return RepositoryPath;
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

        public NPath RepositoryPath { get; private set; }
        protected static ILogging Logger { get; } = Logging.GetLogger<RepositoryLocator>();
    }
}