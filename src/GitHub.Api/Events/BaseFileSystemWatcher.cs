using System;
using System.IO;

namespace GitHub.Unity
{
    abstract class BaseFileSystemWatcher : IFileSystemWatch
    {
        private readonly WatchArguments arguments;

        public BaseFileSystemWatcher()
        {}

        public BaseFileSystemWatcher(WatchArguments arguments)
        {
            this.arguments = arguments;
        }

        protected virtual void RaiseCreated(string path)
        {
            Created?.Invoke(path);
        }

        protected void RaiseChanged(string path)
        {
            Changed?.Invoke(path);
        }

        protected virtual void RaiseDeleted(string path)
        {
            Deleted?.Invoke(path);
        }

        protected virtual void RaiseRenamed(string oldPath, string newPath)
        {
            Renamed?.Invoke(oldPath, newPath);
        }

        protected virtual void Dispose(bool disposing)
        {
        }

        public void Dispose()
        {
            Dispose(true);
        }

        public event Action<NPath> Changed;
        public event Action<NPath> Created;
        public event Action<NPath> Deleted;
        /// <summary>
        /// Old path, new path
        /// </summary>
        public event Action<NPath, NPath> Renamed;

        public NPath Path { get { return arguments.Path; } }
        public string Filter { get { return arguments.Filter; } }
        public bool Recursive { get { return arguments.Recursive; } }

        public virtual bool Enable { get; set; }

        protected WatchArguments Arguments { get { return arguments; } }
    }
}
