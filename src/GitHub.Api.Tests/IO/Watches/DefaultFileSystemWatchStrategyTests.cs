using System;
using System.Collections.Generic;
using System.IO;
using GitHub.Api;
using NSubstitute;
using NUnit.Framework;

namespace GitHub.Unity.Tests
{
    [TestFixture]
    public class DefaultFileSystemWatchStrategyTests
    {
        private SubstituteFactory Factory { get; set; }

        [TestFixtureSetUp]
        public void TestFixtureSetup()
        {
            Factory = new SubstituteFactory();
            NPathFileSystemProvider.Current = Factory.CreateFileSystem(new CreateFileSystemOptions());
        }

        [Test]
        public void ShouldRaiseEvents()
        {
            TestFileSystemWatch testWatcher = null;
            var fileSystemWatchStrategy = Factory.CreateTestWatchFactory(new CreateTestWatchFactoryOptions(createdWatch => { testWatcher = createdWatch; }));

            var defaultFileSystemWatchStrategy = new DefaultFileSystemWatchStrategy(fileSystemWatchStrategy);

            var testWatchListener = Substitute.For<IFileSystemWatchListener>();
            defaultFileSystemWatchStrategy.AddListener(testWatchListener);

            defaultFileSystemWatchStrategy.Watch(@"c:\temp");

            testWatcher.RaiseCreated("file.txt");

            testWatchListener.ReceivedWithAnyArgs(1).OnCreate(Arg.Any<object>(), Arg.Any<FileSystemEventArgs>());
            testWatchListener.ReceivedWithAnyArgs(0).OnChange(Arg.Any<object>(), Arg.Any<FileSystemEventArgs>());
            testWatchListener.ReceivedWithAnyArgs(0).OnDelete(Arg.Any<object>(), Arg.Any<FileSystemEventArgs>());
            testWatchListener.ReceivedWithAnyArgs(0).OnRename(Arg.Any<object>(), Arg.Any<RenamedEventArgs>());
            testWatchListener.ReceivedWithAnyArgs(0).OnError(Arg.Any<object>(), Arg.Any<ErrorEventArgs>());

            testWatcher.RaiseChanged("file.txt");

            testWatchListener.ReceivedWithAnyArgs(1).OnCreate(Arg.Any<object>(), Arg.Any<FileSystemEventArgs>());
            testWatchListener.ReceivedWithAnyArgs(1).OnChange(Arg.Any<object>(), Arg.Any<FileSystemEventArgs>());
            testWatchListener.ReceivedWithAnyArgs(0).OnDelete(Arg.Any<object>(), Arg.Any<FileSystemEventArgs>());
            testWatchListener.ReceivedWithAnyArgs(0).OnRename(Arg.Any<object>(), Arg.Any<RenamedEventArgs>());
            testWatchListener.ReceivedWithAnyArgs(0).OnError(Arg.Any<object>(), Arg.Any<ErrorEventArgs>());

            testWatcher.RaiseDeleted("file.txt");

            testWatchListener.ReceivedWithAnyArgs(1).OnCreate(Arg.Any<object>(), Arg.Any<FileSystemEventArgs>());
            testWatchListener.ReceivedWithAnyArgs(1).OnChange(Arg.Any<object>(), Arg.Any<FileSystemEventArgs>());
            testWatchListener.ReceivedWithAnyArgs(1).OnDelete(Arg.Any<object>(), Arg.Any<FileSystemEventArgs>());
            testWatchListener.ReceivedWithAnyArgs(0).OnRename(Arg.Any<object>(), Arg.Any<RenamedEventArgs>());
            testWatchListener.ReceivedWithAnyArgs(0).OnError(Arg.Any<object>(), Arg.Any<ErrorEventArgs>());

            testWatcher.RaiseRenamed("file.txt", "file.txt");

            testWatchListener.ReceivedWithAnyArgs(1).OnCreate(Arg.Any<object>(), Arg.Any<FileSystemEventArgs>());
            testWatchListener.ReceivedWithAnyArgs(1).OnChange(Arg.Any<object>(), Arg.Any<FileSystemEventArgs>());
            testWatchListener.ReceivedWithAnyArgs(1).OnDelete(Arg.Any<object>(), Arg.Any<FileSystemEventArgs>());
            testWatchListener.ReceivedWithAnyArgs(1).OnRename(Arg.Any<object>(), Arg.Any<RenamedEventArgs>());
            testWatchListener.ReceivedWithAnyArgs(0).OnError(Arg.Any<object>(), Arg.Any<ErrorEventArgs>());

            testWatcher.RaiseError(new Exception());

            testWatchListener.ReceivedWithAnyArgs(1).OnCreate(Arg.Any<object>(), Arg.Any<FileSystemEventArgs>());
            testWatchListener.ReceivedWithAnyArgs(1).OnChange(Arg.Any<object>(), Arg.Any<FileSystemEventArgs>());
            testWatchListener.ReceivedWithAnyArgs(1).OnDelete(Arg.Any<object>(), Arg.Any<FileSystemEventArgs>());
            testWatchListener.ReceivedWithAnyArgs(1).OnRename(Arg.Any<object>(), Arg.Any<RenamedEventArgs>());
            testWatchListener.ReceivedWithAnyArgs(1).OnError(Arg.Any<object>(), Arg.Any<ErrorEventArgs>());
        }

        [Test]
        public void ShouldProperlyDetachListener()
        {
            TestFileSystemWatch testWatcher = null;
            var testWatchFactory = Factory.CreateTestWatchFactory(new CreateTestWatchFactoryOptions(createdWatch => { testWatcher = createdWatch; }));

            var fileSystemWatchStrategy = new DefaultFileSystemWatchStrategy(testWatchFactory);

            var testWatchListener = Substitute.For<IFileSystemWatchListener>();
            fileSystemWatchStrategy.AddListener(testWatchListener);
            fileSystemWatchStrategy.RemoveListener(testWatchListener);

            fileSystemWatchStrategy.Watch(@"c:\temp");

            testWatcher.RaiseCreated("file.txt");
            testWatcher.RaiseChanged("file.txt");
            testWatcher.RaiseDeleted("file.txt");
            testWatcher.RaiseRenamed("file.txt", "file.txt");
            testWatcher.RaiseError(new Exception());

            testWatchListener.ReceivedWithAnyArgs(0).OnCreate(Arg.Any<object>(), Arg.Any<FileSystemEventArgs>());
            testWatchListener.ReceivedWithAnyArgs(0).OnChange(Arg.Any<object>(), Arg.Any<FileSystemEventArgs>());
            testWatchListener.ReceivedWithAnyArgs(0).OnDelete(Arg.Any<object>(), Arg.Any<FileSystemEventArgs>());
            testWatchListener.ReceivedWithAnyArgs(0).OnRename(Arg.Any<object>(), Arg.Any<RenamedEventArgs>());
            testWatchListener.ReceivedWithAnyArgs(0).OnError(Arg.Any<object>(), Arg.Any<ErrorEventArgs>());
        }

        [Test]
        public void ShouldRaiseEventsFromMultipleWatchers()
        {
            var testWatchers = new List<TestFileSystemWatch>();
            var testWatchFactory = Factory.CreateTestWatchFactory(new CreateTestWatchFactoryOptions(createdWatch => { testWatchers.Add(createdWatch); }));

            var fileSystemWatchStrategy = new DefaultFileSystemWatchStrategy(testWatchFactory);

            var testWatchListener = Substitute.For<IFileSystemWatchListener>();
            fileSystemWatchStrategy.AddListener(testWatchListener);

            var directories = new[] { @"c:\temp", @"c:\temp1" };
            foreach (var directory in directories)
            {
                fileSystemWatchStrategy.Watch(directory);
            }

            for (var index = 0; index < testWatchers.Count; index++)
            {
                var testWatcher = testWatchers[index];
                testWatcher.RaiseCreated("file.txt");
            }

            testWatchListener.ReceivedWithAnyArgs(2).OnCreate(Arg.Any<object>(), Arg.Any<FileSystemEventArgs>());
            testWatchListener.ReceivedWithAnyArgs(0).OnChange(Arg.Any<object>(), Arg.Any<FileSystemEventArgs>());
            testWatchListener.ReceivedWithAnyArgs(0).OnDelete(Arg.Any<object>(), Arg.Any<FileSystemEventArgs>());
            testWatchListener.ReceivedWithAnyArgs(0).OnRename(Arg.Any<object>(), Arg.Any<RenamedEventArgs>());
            testWatchListener.ReceivedWithAnyArgs(0).OnError(Arg.Any<object>(), Arg.Any<ErrorEventArgs>());
        }

        [Test]
        public void ShouldRemoveListener()
        {
            TestFileSystemWatch testWatcher = null;
            var testWatchFactory = Factory.CreateTestWatchFactory(new CreateTestWatchFactoryOptions(createdWatch => { testWatcher = createdWatch; }));

            var fileSystemWatchStrategy = new DefaultFileSystemWatchStrategy(testWatchFactory);

            var testWatchListener = Substitute.For<IFileSystemWatchListener>();
            fileSystemWatchStrategy.AddListener(testWatchListener);

            fileSystemWatchStrategy.Watch(@"c:\temp");

            testWatcher.RaiseCreated("file.txt");

            testWatchListener.ReceivedWithAnyArgs(1).OnCreate(Arg.Any<object>(), Arg.Any<FileSystemEventArgs>());
            testWatchListener.ReceivedWithAnyArgs(0).OnChange(Arg.Any<object>(), Arg.Any<FileSystemEventArgs>());
            testWatchListener.ReceivedWithAnyArgs(0).OnDelete(Arg.Any<object>(), Arg.Any<FileSystemEventArgs>());
            testWatchListener.ReceivedWithAnyArgs(0).OnRename(Arg.Any<object>(), Arg.Any<RenamedEventArgs>());
            testWatchListener.ReceivedWithAnyArgs(0).OnError(Arg.Any<object>(), Arg.Any<ErrorEventArgs>());

            fileSystemWatchStrategy.RemoveListener(testWatchListener);

            testWatcher.RaiseChanged("file.txt");

            testWatchListener.ReceivedWithAnyArgs(1).OnCreate(Arg.Any<object>(), Arg.Any<FileSystemEventArgs>());
            testWatchListener.ReceivedWithAnyArgs(0).OnChange(Arg.Any<object>(), Arg.Any<FileSystemEventArgs>());
            testWatchListener.ReceivedWithAnyArgs(0).OnDelete(Arg.Any<object>(), Arg.Any<FileSystemEventArgs>());
            testWatchListener.ReceivedWithAnyArgs(0).OnRename(Arg.Any<object>(), Arg.Any<RenamedEventArgs>());
            testWatchListener.ReceivedWithAnyArgs(0).OnError(Arg.Any<object>(), Arg.Any<ErrorEventArgs>());

            fileSystemWatchStrategy.AddListener(testWatchListener);

            testWatcher.RaiseDeleted("file.txt");

            testWatchListener.ReceivedWithAnyArgs(1).OnCreate(Arg.Any<object>(), Arg.Any<FileSystemEventArgs>());
            testWatchListener.ReceivedWithAnyArgs(0).OnChange(Arg.Any<object>(), Arg.Any<FileSystemEventArgs>());
            testWatchListener.ReceivedWithAnyArgs(1).OnDelete(Arg.Any<object>(), Arg.Any<FileSystemEventArgs>());
            testWatchListener.ReceivedWithAnyArgs(0).OnRename(Arg.Any<object>(), Arg.Any<RenamedEventArgs>());
            testWatchListener.ReceivedWithAnyArgs(0).OnError(Arg.Any<object>(), Arg.Any<ErrorEventArgs>());

            fileSystemWatchStrategy.RemoveListener(testWatchListener);

            testWatcher.RaiseRenamed("file.txt", "file.txt");

            testWatchListener.ReceivedWithAnyArgs(1).OnCreate(Arg.Any<object>(), Arg.Any<FileSystemEventArgs>());
            testWatchListener.ReceivedWithAnyArgs(0).OnChange(Arg.Any<object>(), Arg.Any<FileSystemEventArgs>());
            testWatchListener.ReceivedWithAnyArgs(1).OnDelete(Arg.Any<object>(), Arg.Any<FileSystemEventArgs>());
            testWatchListener.ReceivedWithAnyArgs(0).OnRename(Arg.Any<object>(), Arg.Any<RenamedEventArgs>());
            testWatchListener.ReceivedWithAnyArgs(0).OnError(Arg.Any<object>(), Arg.Any<ErrorEventArgs>());

            fileSystemWatchStrategy.AddListener(testWatchListener);

            testWatcher.RaiseError(new Exception());

            testWatchListener.ReceivedWithAnyArgs(1).OnCreate(Arg.Any<object>(), Arg.Any<FileSystemEventArgs>());
            testWatchListener.ReceivedWithAnyArgs(0).OnChange(Arg.Any<object>(), Arg.Any<FileSystemEventArgs>());
            testWatchListener.ReceivedWithAnyArgs(1).OnDelete(Arg.Any<object>(), Arg.Any<FileSystemEventArgs>());
            testWatchListener.ReceivedWithAnyArgs(0).OnRename(Arg.Any<object>(), Arg.Any<RenamedEventArgs>());
            testWatchListener.ReceivedWithAnyArgs(1).OnError(Arg.Any<object>(), Arg.Any<ErrorEventArgs>());
        }
    }
}
