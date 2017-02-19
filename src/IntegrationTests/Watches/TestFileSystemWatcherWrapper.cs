using System;
using System.IO;

namespace IntegrationTests
{
    class TestFileSystemWatcher : BaseFileSystemWatch
    {
        public TestFileSystemWatcher(string path, bool recursive = false, string filter = null)
            : base(new WatchArguments { Path = path, Recursive = recursive, Filter = filter })
        {
        }

        public override string ToString()
        {
            return Filter == null
                ? string.Format("TestFileSystemWatch Path:\"{0}\" Recursive:{1}", Path, Recursive)
                : string.Format("TestFileSystemWatch Path:\"{0}\" Recursive:{1} Filter:\"{2}\"", Path, Recursive, Filter);
        }
    }
}
