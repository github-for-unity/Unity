using System;
using System.Collections.Generic;
using System.IO;

namespace GitHub.Api
{
    class RecursiveFileSystemWatchStrategy: FileSystemWatchStrategyBase
    {
        private readonly object watchesLock = new object();

        private readonly IFileSystemWatchFactory watchFactory;
        private readonly IFileSystem fileSystem;

        private Dictionary<PathAndFilter, IFileSystemWatch> watches = new Dictionary<PathAndFilter, IFileSystemWatch>();

        public RecursiveFileSystemWatchStrategy(IFileSystemWatchFactory watchFactory, IFileSystem fileSystem)
        {
            this.watchFactory = watchFactory;
            this.fileSystem = fileSystem;
        }

        public override void Watch(string path, string filter = null)
        {
            Guard.ArgumentNotNull(path, nameof(path));

            var key = new PathAndFilter { Path = path, Filter = filter };

            IFileSystemWatch watch;
            lock (watchesLock)
            {
                if (watches.ContainsKey(key))
                {
                    throw new Exception("path and filter combination already watched");
                }

                Logger.Debug("Watching Path:{0} Filter:{1}", path, filter == null ? "[NONE]" : filter);

                if (filter != null)
                {
                    watch = watchFactory.CreateWatch(path, filter);
                }
                else
                {
                    watch = watchFactory.CreateWatch(path);
                }
                watches.Add(key, watch);
            }

            watch.AddListener(this);

            if (fileSystem.ExistingPathIsDirectory(path))
            {
                var directories = fileSystem.GetDirectories(path);
                foreach (var directory in directories)
                {
                    Watch(directory, filter);
                }
            }
        }

        public override void ClearWatch(string path, string filter = null)
        {
        }
    }
}
