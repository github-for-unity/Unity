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
        protected IApplicationManager ApplicationManager { get; }
        private IEnvironment Environment { get { return ApplicationManager.Environment; } }
        protected ITaskManager TaskManager { get { return ApplicationManager.TaskManager; } }

        public RepositoryInitializerBase(IApplicationManager applicationManager)
        {
            Logger = Logging.GetLogger(GetType());

            this.ApplicationManager = applicationManager;
        }

        public void Run()
        {
            var targetPath = NPath.CurrentDirectory;
            var token = ApplicationManager.CancellationToken;

            var initTask = new GitInitTask(token);

            var unityYamlMergeExec = Environment.UnityApplication.ToNPath().Parent.Combine("Tools", "UnityYAMLMerge");
            var yamlMergeCommand = string.Format(@"'{0}' merge -p ""$BASE"" ""$REMOTE"" ""$LOCAL"" ""$MERGED""", unityYamlMergeExec);
            var yaml1 = new GitConfigSetTask("merge.unityyamlmerge.cmd", yamlMergeCommand, GitConfigSource.Local, token);
            var yaml2 = new GitConfigSetTask("merge.unityyamlmerge.trustExitCode", "false", GitConfigSource.Local, token);

            var lfsTask = new GitLfsInstallTask(token);

            var gitignore = targetPath.Combine(".gitignore");
            var gitAttrs = targetPath.Combine(".gitattributes");
            var assetsGitignore = targetPath.Combine("Assets", ".gitignore");

            var ignoresTask = new ActionTask(token, _ =>
            {
                SetProjectToTextSerialization();


                AssemblyResources.ToFile(ResourceType.Generic, ".gitignore", targetPath);
                AssemblyResources.ToFile(ResourceType.Generic, ".gitattributes", targetPath);

                assetsGitignore.CreateFile();
            });

            var filesForInitialCommit = new List<string> { gitignore, gitAttrs, assetsGitignore };

            var addTask = new GitAddTask(filesForInitialCommit, token);
            var commitTask = new GitCommitTask("Initial commit", null, token);

            initTask
                .ContinueWith(yaml1)
                .ContinueWith(yaml2)
                .ContinueWith(lfsTask)
                .ContinueWith(ignoresTask)
                .ContinueWith(addTask)
                .ContinueWith(commitTask)
                .ContinueWith(_ => ApplicationManager.RestartRepository().Start(TaskManager.ConcurrentScheduler));
            initTask.Schedule(TaskManager);

            //task.Critical = false;
            //task.Queued = TaskQueueSetting.QueueSingle;
            //task.Blocking = false;
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