using System;
using System.Collections.Generic;
using GitHub.Unity;

namespace GitHub.Api
{
    class DefaultFileSystemWatch : FileSystemWatch
    {
        private IFileSystemWatcherWrapper watch;

        public DefaultFileSystemWatch(IFileSystemWatchWrapperFactory watchWrapperFactory, string path, bool recursive = false, string filter = null)
        {
            watch = watchWrapperFactory.CreateWatch(path, recursive, filter);
            watch.AddListener(this);
        }

        public override void Dispose()
        {
            base.Dispose();
            watch?.Dispose();
            watch = null;
        }

        public override bool Enable
        {
            get { return watch.Enable; }
            set { watch.Enable = value; }
        }
    }
}
