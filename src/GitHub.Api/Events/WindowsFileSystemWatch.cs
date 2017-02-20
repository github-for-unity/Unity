using System;
using System.Collections.Generic;
using System.IO;

namespace GitHub.Unity
{
    class WindowsFileSystemWatch : BaseFileSystemWatcher
    {
        private readonly FileSystemWatcher watcher;

        public WindowsFileSystemWatch(WatchArguments arguments) : base(arguments)
        {
            watcher = new FileSystemWatcher(arguments.Path);
            watcher.IncludeSubdirectories = Recursive;
            watcher.NotifyFilter = NotifyFilters.CreationTime |
                                NotifyFilters.Attributes |
                                NotifyFilters.FileName |
                                NotifyFilters.LastWrite;

            if (!arguments.FilesOnly)
                watcher.NotifyFilter |= NotifyFilters.DirectoryName;

            watcher.Filter = Filter ?? "";

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

        public override bool Enable
        {
            get { return watcher.EnableRaisingEvents; }
            set { watcher.EnableRaisingEvents = value; }
        }
    }
}