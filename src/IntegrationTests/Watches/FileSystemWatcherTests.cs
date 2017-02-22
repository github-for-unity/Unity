using GitHub.Unity;
using NSubstitute;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace IntegrationTests
{
    class TestUIDispatcher : BaseUIDispatcher
    {
        protected override void Run(Action<bool> onClose)
        {
            base.Run(onClose);
        }
    }

    [TestFixture]
    class FileSystemWatcherTests : BaseIntegrationTest
    {
        [Test]
        public void WatchesADirectoryTree()
        {
            int expected = 9;
            int count = 0;
            var platform = new Platform(Environment, FileSystem, new TestUIDispatcher());
            var watcher = platform.FileSystemWatchFactory.GetOrCreate(TestBasePath, true);

            watcher.Created += f =>
            {
                Logger.Debug("Created {0}", f);
                count++;
            };

            watcher.Enable = true;

            var files = new NPath[]
            {
                "file1",
                "dir1/dir1/file1",
                "dir1/dir1/dir1/file1",
                "dir1/dir1/dir2/file1",
                "dir1/dir1/dir2/file2",
            };

            CreateDirStructure(files);

            Thread.Sleep(50);

            watcher.Dispose();
            Assert.AreEqual(expected, count);
        }

        [Test]
        public void WatchesAFile()
        {
            int expected = 9;
            int count = 0;
            var platform = new Platform(Environment, FileSystem, new TestUIDispatcher());
            var file = TestBasePath.Combine("file.txt").CreateFile();
            var watcher = platform.FileSystemWatchFactory.GetOrCreate(file);

            watcher.Created += f =>
            {
                Logger.Debug("Created {0}", f);
                count++;
            };

            watcher.Changed += f =>
            {
                Logger.Debug("Changed {0} {1}", f, f.ReadAllText());
                count++;
            };

            watcher.Renamed += (old, n) =>
            {
                Logger.Debug("Renamed {0} {1}", old, n);
                count++;
            };

            watcher.Deleted += f =>
            {
                Logger.Debug("Deleted {0}", f);
                count++;
            };

            watcher.Enable = true;

            File.WriteAllText(file, "test");
            //file.WriteAllText("text");

            Thread.Sleep(120);
            Assert.AreEqual(1, count);

            watcher.Dispose();
        }
    }
}
