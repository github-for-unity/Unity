using System.Linq;

namespace GitHub.Unity
{
    class RepositoryWatcher
    {
        private readonly NPath dotGitPath;

        public IFileSystemWatch FileHierarchyWatcher { get; private set; }
        public IFileSystemWatch GitConfigWatcher { get; private set; }
        public IFileSystemWatch GitHeadWatcher { get; private set; }
        public IFileSystemWatch GitRefsWatcher { get; private set; }
        public IFileSystemWatch GitIndexWatcher { get; private set; }

        public RepositoryWatcher(NPath path, IPlatform platform)
        {
            RepositoryPath = path;
            dotGitPath = path.Combine(".git");
            if (dotGitPath.FileExists())
            {
                dotGitPath = dotGitPath.ReadAllLines()
                    .Where(x => x.StartsWith("gitdir:"))
                    .Select(x => x.Substring(7).Trim())
                    .First();
            }

            FileHierarchyWatcher = platform.FileSystemWatchFactory.GetOrCreate(path, true);
            GitRefsWatcher = platform.FileSystemWatchFactory.GetOrCreate(dotGitPath.Combine("refs", "heads"), true);
            GitConfigWatcher = platform.FileSystemWatchFactory.GetOrCreate(dotGitPath.Combine("config"), false);
            GitHeadWatcher = platform.FileSystemWatchFactory.GetOrCreate(dotGitPath.Combine("HEAD"), false);
            GitIndexWatcher = platform.FileSystemWatchFactory.GetOrCreate(dotGitPath.Combine("index"), false);
        }

        public void Run()
        {
            FileHierarchyWatcher.Enable = true;
            GitRefsWatcher.Enable = true;
            GitConfigWatcher.Enable = true;
            GitHeadWatcher.Enable = true;
            GitIndexWatcher.Enable = true;
        }

        protected static ILogging Logger { get; } = Logging.GetLogger<GitClient>();
        public string RepositoryPath { get; }
    }

    class StatusEventDispatcher
    {
        public StatusEventDispatcher(IEnvironment environment, IPlatform platform)
        {
            var fsw = platform.FileSystemWatchFactory.GetOrCreate(environment.RepositoryPath, true);
        }
    }
}