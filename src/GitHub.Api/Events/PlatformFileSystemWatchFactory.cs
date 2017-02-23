using System.Collections.Generic;

namespace GitHub.Unity
{
    class PlatformFileSystemWatchFactory : IFileSystemWatchFactory
    {
        private readonly IEnvironment environment;
        private readonly Dictionary<WatchArguments, IFileSystemWatch> watchers =
            new Dictionary<WatchArguments, IFileSystemWatch>();

        public PlatformFileSystemWatchFactory(IEnvironment environment)
        {
            this.environment = environment;
        }

        public virtual IFileSystemWatch GetOrCreate(NPath path, bool recursive = false, bool filesOnly = false)
        {
            string filter = null;
            if (path.FileExists())
            {
                recursive = false;
                filter = path.FileName;
                path = path.Parent;
                filesOnly = true;
            }
            
            var arguments = new WatchArguments { Path = path, Filter = filter, Recursive = recursive, FilesOnly = filesOnly };
            IFileSystemWatch watch = null;
            if (!watchers.TryGetValue(arguments, out watch))
            {
                if (environment.IsWindows)
                {
                    return new WindowsFileSystemWatch(arguments);
                }
                else if (environment.IsMac)
                {
                    return new MacFileSystemWatch(arguments);
                }
                else
                {
                    return new LinuxFileSystemWatch(arguments);
                }
            }

            return watch;
        }

    }

    struct WatchArguments
    {
        public NPath Path;
        public string Filter;
        public bool Recursive;
        public bool FilesOnly;
    }
}