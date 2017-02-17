using System;
using System.Collections.Generic;

namespace GitHub.Api
{
    class DefaultFileSystemWatchStrategy : FileSystemWatchStrategyBase
    {
        private readonly object watchesLock = new object();
        private readonly IFileSystemWatchFactory watchFactory;

        private Dictionary<string, IFileSystemWatch> watches = new Dictionary<string, IFileSystemWatch>();

        public DefaultFileSystemWatchStrategy(IFileSystemWatchFactory watchFactory)
        {
            this.watchFactory = watchFactory;
        }

        public override void Watch(string path, string filter = null)
        {
            Guard.ArgumentNotNull(path, nameof(path));

            IFileSystemWatch watch;
            lock(watchesLock)
            {
                if (watches.ContainsKey(path))
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

                watches.Add(path, watch);
            }

            watch.AddListener(this);
        }

        public override bool ClearWatch(string path)
        {
            lock(watchesLock)
            {
                IFileSystemWatch value;
                if (!watches.TryGetValue(path, out value))
                {
                    return false;
                }

                watches.Remove(path);

                value.Enable = false;
                value.RemoveListener(this);
                value.Dispose();
                return true;
            }
        }

        public override void Dispose()
        {
            base.Dispose();

            if (watches == null)
            {
                return;
            }

            var watchList = watches;
            watches = null;

            foreach (var watcher in watchList.Values)
            {
                watcher.Dispose();
            }
        }
    }
}
