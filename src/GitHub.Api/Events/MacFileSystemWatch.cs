using System.Collections.Generic;
using System.IO;

namespace GitHub.Unity
{
    class MacFileSystemWatch : BaseFileSystemWatcher
    {
        private readonly FileSystemWatcher watcher;

        public MacFileSystemWatch(WatchArguments arguments) : base(arguments)
        {
            var value = System.Environment.GetEnvironmentVariable("MONO_MANAGED_WATCHER");

            System.Environment.SetEnvironmentVariable("MONO_MANAGED_WATCHER", "1");
            watcher = new FileSystemWatcher(Path);
            System.Environment.SetEnvironmentVariable("MONO_MANAGED_WATCHER", value);

            watcher.IncludeSubdirectories = Recursive;
            watcher.NotifyFilter = NotifyFilters.CreationTime |
                                NotifyFilters.Attributes |
                                NotifyFilters.DirectoryName |
                                NotifyFilters.FileName |
                                NotifyFilters.LastWrite |
                                NotifyFilters.Size;

            if (Filter != null)
            {
                watcher.Filter = Filter;
            }

            watcher.Created += (s, e) => RaiseCreated(e.FullPath);
            watcher.Changed += (s, e) => RaiseChanged(e.FullPath);
            watcher.Deleted += (s, e) => RaiseDeleted(e.FullPath);
            watcher.Renamed += (s, e) => RaiseRenamed(e.OldFullPath, e.FullPath);
        }

        private bool disposed;
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                if (disposed) return;
                disposed = true;
                Enable = false;
                watcher.Dispose();
            }
        }
    }
}