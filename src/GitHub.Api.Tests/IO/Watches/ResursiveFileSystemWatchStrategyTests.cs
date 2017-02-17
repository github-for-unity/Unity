using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using FluentAssertions;
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
        public void ShouldRaiseEventsFromDirectoryWithNoChildren()
        {
            TestFileSystemWatch testWatcher = null;
            var testWatchFactory =
                Factory.CreateTestWatchFactory(
                    new CreateTestWatchFactoryOptions(createdWatch => { testWatcher = createdWatch; }));

            var fileSystem =
                Factory.CreateFileSystem(new CreateFileSystemOptions {
                    DirectoriesThatExist = new[] { @"c:\temp" },
                    ChildDirectories =
                        new Dictionary<SubstituteFactory.ContentsKey, IList<string>> {
                            { new SubstituteFactory.ContentsKey(@"c:\temp"), new string[0] }
                        }
                });

            var fileSystemWatchStrategy = new RecursiveFileSystemWatchStrategy(testWatchFactory, fileSystem);

            var testWatchListener = Substitute.For<IFileSystemWatchListener>();
            fileSystemWatchStrategy.AddListener(testWatchListener);

            fileSystemWatchStrategy.Watch(@"c:\temp");

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
        public void ShouldProperlyDetachListenerFromDirectoryWithNoChildren()
        {
            TestFileSystemWatch testWatcher = null;
            var testWatchFactory =
                Factory.CreateTestWatchFactory(
                    new CreateTestWatchFactoryOptions(createdWatch => { testWatcher = createdWatch; }));

            var fileSystem =
                Factory.CreateFileSystem(new CreateFileSystemOptions {
                    DirectoriesThatExist = new[] { @"c:\temp" },
                    ChildDirectories =
                        new Dictionary<SubstituteFactory.ContentsKey, IList<string>> {
                            { new SubstituteFactory.ContentsKey(@"c:\temp"), new string[0] }
                        }
                });

            var fileSystemWatchStrategy = new RecursiveFileSystemWatchStrategy(testWatchFactory, fileSystem);

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
        public void ShouldRaiseEventsFromMultipleWatchersFromDirectoriesWithNoChildren()
        {
            var testWatchers = new List<TestFileSystemWatch>();
            var testWatchFactory =
                Factory.CreateTestWatchFactory(
                    new CreateTestWatchFactoryOptions(createdWatch => { testWatchers.Add(createdWatch); }));

            var fileSystem =
                Factory.CreateFileSystem(new CreateFileSystemOptions {
                    DirectoriesThatExist = new[] { @"c:\temp" },
                    ChildDirectories =
                        new Dictionary<SubstituteFactory.ContentsKey, IList<string>> {
                            { new SubstituteFactory.ContentsKey(@"c:\temp"), new string[0] }
                        }
                });

            var fileSystemWatchStrategy = new RecursiveFileSystemWatchStrategy(testWatchFactory, fileSystem);

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
        public void ShouldRemoveListenerFromDirectoryWithNoChildren()
        {
            TestFileSystemWatch testWatcher = null;
            var testWatchFactory =
                Factory.CreateTestWatchFactory(
                    new CreateTestWatchFactoryOptions(createdWatch => { testWatcher = createdWatch; }));

            var fileSystem =
                Factory.CreateFileSystem(new CreateFileSystemOptions {
                    DirectoriesThatExist = new[] { @"c:\temp" },
                    ChildDirectories =
                        new Dictionary<SubstituteFactory.ContentsKey, IList<string>> {
                            { new SubstituteFactory.ContentsKey(@"c:\temp"), new string[0] }
                        }
                });

            var fileSystemWatchStrategy = new RecursiveFileSystemWatchStrategy(testWatchFactory, fileSystem);

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

        [Test]
        public void ShouldRaiseEventsFromDirectoryWithChildren()
        {
            TestFileSystemWatch parentWatch = null;
            TestFileSystemWatch child1Watch = null;
            TestFileSystemWatch child2Watch = null;

            var testWatchFactory = Factory.CreateTestWatchFactory(new CreateTestWatchFactoryOptions(createdWatch => {
                switch (createdWatch.Path)
                {
                    case @"c:\temp":
                        parentWatch = createdWatch;
                        break;

                    case @"c:\temp\child1":
                        child1Watch = createdWatch;
                        break;

                    case @"c:\temp\child2":
                        child2Watch = createdWatch;
                        break;

                    default:
                        throw new Exception("Unexpected Path");
                }
            }));

            var fileSystem =
                Factory.CreateFileSystem(new CreateFileSystemOptions {
                    DirectoriesThatExist = new[] { @"c:\temp", @"c:\temp\child1", @"c:\temp\child2" },
                    ChildDirectories =
                        new Dictionary<SubstituteFactory.ContentsKey, IList<string>> {
                            {
                                new SubstituteFactory.ContentsKey(@"c:\temp"),
                                new[] { @"c:\temp\child1", @"c:\temp\child2" }
                            }
                        }
                });

            var fileSystemWatchStrategy = new RecursiveFileSystemWatchStrategy(testWatchFactory, fileSystem);

            var testWatchListener = Substitute.For<IFileSystemWatchListener>();
            fileSystemWatchStrategy.AddListener(testWatchListener);

            fileSystemWatchStrategy.Watch(@"c:\temp");

            parentWatch.Should().NotBeNull();
            child1Watch.Should().NotBeNull();
            child2Watch.Should().NotBeNull();

            parentWatch.RaiseCreated("file.txt");

            testWatchListener.ReceivedWithAnyArgs(1).OnCreate(Arg.Any<object>(), Arg.Any<FileSystemEventArgs>());
            testWatchListener.ReceivedWithAnyArgs(0).OnChange(Arg.Any<object>(), Arg.Any<FileSystemEventArgs>());
            testWatchListener.ReceivedWithAnyArgs(0).OnDelete(Arg.Any<object>(), Arg.Any<FileSystemEventArgs>());
            testWatchListener.ReceivedWithAnyArgs(0).OnRename(Arg.Any<object>(), Arg.Any<RenamedEventArgs>());
            testWatchListener.ReceivedWithAnyArgs(0).OnError(Arg.Any<object>(), Arg.Any<ErrorEventArgs>());

            child1Watch.RaiseChanged("file.txt");

            testWatchListener.ReceivedWithAnyArgs(1).OnCreate(Arg.Any<object>(), Arg.Any<FileSystemEventArgs>());
            testWatchListener.ReceivedWithAnyArgs(1).OnChange(Arg.Any<object>(), Arg.Any<FileSystemEventArgs>());
            testWatchListener.ReceivedWithAnyArgs(0).OnDelete(Arg.Any<object>(), Arg.Any<FileSystemEventArgs>());
            testWatchListener.ReceivedWithAnyArgs(0).OnRename(Arg.Any<object>(), Arg.Any<RenamedEventArgs>());
            testWatchListener.ReceivedWithAnyArgs(0).OnError(Arg.Any<object>(), Arg.Any<ErrorEventArgs>());

            child2Watch.RaiseDeleted("file.txt");

            testWatchListener.ReceivedWithAnyArgs(1).OnCreate(Arg.Any<object>(), Arg.Any<FileSystemEventArgs>());
            testWatchListener.ReceivedWithAnyArgs(1).OnChange(Arg.Any<object>(), Arg.Any<FileSystemEventArgs>());
            testWatchListener.ReceivedWithAnyArgs(1).OnDelete(Arg.Any<object>(), Arg.Any<FileSystemEventArgs>());
            testWatchListener.ReceivedWithAnyArgs(0).OnRename(Arg.Any<object>(), Arg.Any<RenamedEventArgs>());
            testWatchListener.ReceivedWithAnyArgs(0).OnError(Arg.Any<object>(), Arg.Any<ErrorEventArgs>());

            parentWatch.RaiseRenamed("file.txt", "file.txt");

            testWatchListener.ReceivedWithAnyArgs(1).OnCreate(Arg.Any<object>(), Arg.Any<FileSystemEventArgs>());
            testWatchListener.ReceivedWithAnyArgs(1).OnChange(Arg.Any<object>(), Arg.Any<FileSystemEventArgs>());
            testWatchListener.ReceivedWithAnyArgs(1).OnDelete(Arg.Any<object>(), Arg.Any<FileSystemEventArgs>());
            testWatchListener.ReceivedWithAnyArgs(1).OnRename(Arg.Any<object>(), Arg.Any<RenamedEventArgs>());
            testWatchListener.ReceivedWithAnyArgs(0).OnError(Arg.Any<object>(), Arg.Any<ErrorEventArgs>());

            child1Watch.RaiseError(new Exception());

            testWatchListener.ReceivedWithAnyArgs(1).OnCreate(Arg.Any<object>(), Arg.Any<FileSystemEventArgs>());
            testWatchListener.ReceivedWithAnyArgs(1).OnChange(Arg.Any<object>(), Arg.Any<FileSystemEventArgs>());
            testWatchListener.ReceivedWithAnyArgs(1).OnDelete(Arg.Any<object>(), Arg.Any<FileSystemEventArgs>());
            testWatchListener.ReceivedWithAnyArgs(1).OnRename(Arg.Any<object>(), Arg.Any<RenamedEventArgs>());
            testWatchListener.ReceivedWithAnyArgs(1).OnError(Arg.Any<object>(), Arg.Any<ErrorEventArgs>());
        }

        [Test]
        public void ShouldWatchNewChildDirectories()
        {
            TestFileSystemWatch parentWatch = null;
            TestFileSystemWatch child1Watch = null;
            TestFileSystemWatch child2Watch = null;

            const string tempPath = @"c:\temp";
            const string child1Path = @"c:\temp\child1";

            const string child2Name = @"child2";
            const string child2Path = @"c:\temp\child2";

            var testWatchFactory = Factory.CreateTestWatchFactory(new CreateTestWatchFactoryOptions(createdWatch => {
                switch (createdWatch.Path)
                {
                    case tempPath:
                        parentWatch = createdWatch;
                        break;

                    case child1Path:
                        child1Watch = createdWatch;
                        break;

                    case child2Path:
                        child2Watch = createdWatch;
                        break;

                    default:
                        throw new Exception("Unexpected Path");
                }
            }));

            var directoriesThatExist = new List<string> { tempPath, child1Path };
            var childDirectoriesOfTempFolder = new List<string> { child1Path };

            var fileSystem =
                Factory.CreateFileSystem(new CreateFileSystemOptions {
                    DirectoriesThatExist = directoriesThatExist,
                    ChildDirectories =
                        new Dictionary<SubstituteFactory.ContentsKey, IList<string>> {
                            {
                                new SubstituteFactory.ContentsKey(tempPath),
                                childDirectoriesOfTempFolder
                            }
                        }
                });

            var fileSystemWatchStrategy = new RecursiveFileSystemWatchStrategy(testWatchFactory, fileSystem);

            var testWatchListener = Substitute.For<IFileSystemWatchListener>();
            fileSystemWatchStrategy.AddListener(testWatchListener);

            fileSystemWatchStrategy.Watch(tempPath);

            parentWatch.Should().NotBeNull();
            child1Watch.Should().NotBeNull();
            child2Watch.Should().BeNull();

            directoriesThatExist.Add(child2Path);
            childDirectoriesOfTempFolder.Add(child2Path);

            parentWatch.RaiseCreated(child2Name);

            child2Watch.Should().NotBeNull();

            directoriesThatExist.Remove(child2Path);
            childDirectoriesOfTempFolder.Remove(child2Path);

            parentWatch.RaiseDeleted(child2Name);
        }
    }
}
