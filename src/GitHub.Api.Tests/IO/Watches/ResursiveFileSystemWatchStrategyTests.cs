using System;
using System.Collections.Generic;
using System.IO;
using GitHub.Api;
using NSubstitute;
using NUnit.Framework;

namespace GitHub.Unity.Tests
{
    [TestFixture]
    public class ResursiveFileSystemWatchStrategyTests
    {
        private SubstituteFactory Factory { get; set; }

        [TestFixtureSetUp]
        public void TestFixtureSetup()
        {
            Factory = new SubstituteFactory();
            NPathFileSystemProvider.Current = Factory.CreateFileSystem(new CreateFileSystemOptions());
        }

    

        [Test]
        public void ShouldProperlyDetachListener()
        {
            TestFileSystemWatch testWatcher = null;
            var testWatchFactory = Factory.CreateTestWatchFactory(new CreateTestWatchFactoryOptions(createdWatch => { testWatcher = createdWatch; }));

            var fileSystem = Factory.CreateFileSystem(new CreateFileSystemOptions
            {
                ChildDirectories = new Dictionary<SubstituteFactory.ContentsKey, string[]> { { new SubstituteFactory.ContentsKey(@"c:\temp"), new string[0] } }
            });

            var defaultFileSystemWatchStrategy = new RecursiveFileSystemWatchStrategy(testWatchFactory, fileSystem);

            var testWatchListener = Substitute.For<IFileSystemWatchListener>();
            defaultFileSystemWatchStrategy.AddListener(testWatchListener);
            defaultFileSystemWatchStrategy.RemoveListener(testWatchListener);

            defaultFileSystemWatchStrategy.Watch(@"c:\temp");

            testWatcher.RaiseCreated(@"c:\temp", "file.txt");
            testWatcher.RaiseChanged(@"c:\temp", "file.txt");
            testWatcher.RaiseDeleted(@"c:\temp", "file.txt");
            testWatcher.RaiseRenamed(@"c:\temp", "file.txt", "file.txt");
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

            var fileSystem = Factory.CreateFileSystem(new CreateFileSystemOptions {
                ChildDirectories = new Dictionary<SubstituteFactory.ContentsKey, string[]> { { new SubstituteFactory.ContentsKey(@"c:\temp"), new string[0] } }
            });

            var defaultFileSystemWatchStrategy = new RecursiveFileSystemWatchStrategy(testWatchFactory, fileSystem);

            var testWatchListener = Substitute.For<IFileSystemWatchListener>();
            defaultFileSystemWatchStrategy.AddListener(testWatchListener);

            var directories = new[] { @"c:\temp", @"c:\temp1" };
            foreach (var directory in directories)
            {
                defaultFileSystemWatchStrategy.Watch(directory);
            }

            for (var index = 0; index < testWatchers.Count; index++)
            {
                var testWatcher = testWatchers[index];
                testWatcher.RaiseCreated(directories[index], "file.txt");
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

            var fileSystem = Factory.CreateFileSystem(new CreateFileSystemOptions
            {
                ChildDirectories = new Dictionary<SubstituteFactory.ContentsKey, string[]> { { new SubstituteFactory.ContentsKey(@"c:\temp"), new string[0] } }
            });

            var defaultFileSystemWatchStrategy = new RecursiveFileSystemWatchStrategy(testWatchFactory, fileSystem);

            var testWatchListener = Substitute.For<IFileSystemWatchListener>();
            defaultFileSystemWatchStrategy.AddListener(testWatchListener);

            defaultFileSystemWatchStrategy.Watch(@"c:\temp");

            testWatcher.RaiseCreated(@"c:\temp", "file.txt");

            testWatchListener.ReceivedWithAnyArgs(1).OnCreate(Arg.Any<object>(), Arg.Any<FileSystemEventArgs>());
            testWatchListener.ReceivedWithAnyArgs(0).OnChange(Arg.Any<object>(), Arg.Any<FileSystemEventArgs>());
            testWatchListener.ReceivedWithAnyArgs(0).OnDelete(Arg.Any<object>(), Arg.Any<FileSystemEventArgs>());
            testWatchListener.ReceivedWithAnyArgs(0).OnRename(Arg.Any<object>(), Arg.Any<RenamedEventArgs>());
            testWatchListener.ReceivedWithAnyArgs(0).OnError(Arg.Any<object>(), Arg.Any<ErrorEventArgs>());

            defaultFileSystemWatchStrategy.RemoveListener(testWatchListener);

            testWatcher.RaiseChanged(@"c:\temp", "file.txt");

            testWatchListener.ReceivedWithAnyArgs(1).OnCreate(Arg.Any<object>(), Arg.Any<FileSystemEventArgs>());
            testWatchListener.ReceivedWithAnyArgs(0).OnChange(Arg.Any<object>(), Arg.Any<FileSystemEventArgs>());
            testWatchListener.ReceivedWithAnyArgs(0).OnDelete(Arg.Any<object>(), Arg.Any<FileSystemEventArgs>());
            testWatchListener.ReceivedWithAnyArgs(0).OnRename(Arg.Any<object>(), Arg.Any<RenamedEventArgs>());
            testWatchListener.ReceivedWithAnyArgs(0).OnError(Arg.Any<object>(), Arg.Any<ErrorEventArgs>());

            defaultFileSystemWatchStrategy.AddListener(testWatchListener);

            testWatcher.RaiseDeleted(@"c:\temp", "file.txt");

            testWatchListener.ReceivedWithAnyArgs(1).OnCreate(Arg.Any<object>(), Arg.Any<FileSystemEventArgs>());
            testWatchListener.ReceivedWithAnyArgs(0).OnChange(Arg.Any<object>(), Arg.Any<FileSystemEventArgs>());
            testWatchListener.ReceivedWithAnyArgs(1).OnDelete(Arg.Any<object>(), Arg.Any<FileSystemEventArgs>());
            testWatchListener.ReceivedWithAnyArgs(0).OnRename(Arg.Any<object>(), Arg.Any<RenamedEventArgs>());
            testWatchListener.ReceivedWithAnyArgs(0).OnError(Arg.Any<object>(), Arg.Any<ErrorEventArgs>());

            defaultFileSystemWatchStrategy.RemoveListener(testWatchListener);

            testWatcher.RaiseRenamed(@"c:\temp", "file.txt", "file.txt");

            testWatchListener.ReceivedWithAnyArgs(1).OnCreate(Arg.Any<object>(), Arg.Any<FileSystemEventArgs>());
            testWatchListener.ReceivedWithAnyArgs(0).OnChange(Arg.Any<object>(), Arg.Any<FileSystemEventArgs>());
            testWatchListener.ReceivedWithAnyArgs(1).OnDelete(Arg.Any<object>(), Arg.Any<FileSystemEventArgs>());
            testWatchListener.ReceivedWithAnyArgs(0).OnRename(Arg.Any<object>(), Arg.Any<RenamedEventArgs>());
            testWatchListener.ReceivedWithAnyArgs(0).OnError(Arg.Any<object>(), Arg.Any<ErrorEventArgs>());

            defaultFileSystemWatchStrategy.AddListener(testWatchListener);

            testWatcher.RaiseError(new Exception());

            testWatchListener.ReceivedWithAnyArgs(1).OnCreate(Arg.Any<object>(), Arg.Any<FileSystemEventArgs>());
            testWatchListener.ReceivedWithAnyArgs(0).OnChange(Arg.Any<object>(), Arg.Any<FileSystemEventArgs>());
            testWatchListener.ReceivedWithAnyArgs(1).OnDelete(Arg.Any<object>(), Arg.Any<FileSystemEventArgs>());
            testWatchListener.ReceivedWithAnyArgs(0).OnRename(Arg.Any<object>(), Arg.Any<RenamedEventArgs>());
            testWatchListener.ReceivedWithAnyArgs(1).OnError(Arg.Any<object>(), Arg.Any<ErrorEventArgs>());
        }
    }
}