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

            var initTask = ApplicationManager.GitClient.Init();

            var unityYamlMergeExec = Environment.UnityApplication.Parent.Combine("Tools", "UnityYAMLMerge");
            var yamlMergeCommand = string.Format(@"'{0}' merge -p ""$BASE"" ""$REMOTE"" ""$LOCAL"" ""$MERGED""", unityYamlMergeExec);

            var yaml1 = ApplicationManager.GitClient.SetConfig("merge.unityyamlmerge.cmd", yamlMergeCommand, GitConfigSource.Local, dependsOn: initTask);
            var yaml2 = ApplicationManager.GitClient.SetConfig("merge.unityyamlmerge.trustExitCode", "false", GitConfigSource.Local, dependsOn: yaml1);

            var lfsTask = ApplicationManager.GitClient.LfsInstall(dependsOn: yaml2);

            var gitignore = targetPath.Combine(".gitignore");
            var gitAttrs = targetPath.Combine(".gitattributes");
            var assetsGitignore = targetPath.Combine("Assets", ".gitignore");

            var ignoresTask = new ActionTask(token, _ =>
            {
                SetProjectToTextSerialization();

                AssemblyResources.ToFile(ResourceType.Generic, ".gitignore", targetPath);
                AssemblyResources.ToFile(ResourceType.Generic, ".gitattributes", targetPath);

                assetsGitignore.CreateFile();
            }, dependsOn: lfsTask);

            var filesForInitialCommit = new List<string> { gitignore, gitAttrs, assetsGitignore };

            var addAndCommitTask = ApplicationManager.GitClient.AddAndCommit(filesForInitialCommit, "Initial commit", null, dependsOn: ignoresTask);

            var restartRepositoryTask = ApplicationManager.RestartRepository().SetDependsOn(addAndCommitTask);
            restartRepositoryTask.Start();
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