using System;
using System.IO;
using GitHub.Api.IO;
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

        private IFileSystemWatchFactory CreateTestWatchFactory(Action<TestFileSystemWatch> onWatchCreated)
        {
            var logger = Logging.GetLogger("TestFileSystemWatchFactory");

            var fileSystemWatchFactory = Substitute.For<IFileSystemWatchFactory>();
            fileSystemWatchFactory.CreateWatch(Arg.Any<string>()).Returns(info => {
                logger.Trace(@"CreateWatch(""{0}"")", (string)info[0]);

                var fileSystemWatch = new TestFileSystemWatch();

                onWatchCreated(fileSystemWatch);

                return fileSystemWatch;
            });

            fileSystemWatchFactory.CreateWatch(Arg.Any<string>(), Arg.Any<string>()).Returns(info => {
                logger.Trace(@"CreateWatch(""{0}"", ""{1}"")", (string)info[0], (string)info[1]);

                var fileSystemWatch = new TestFileSystemWatch();

                onWatchCreated(fileSystemWatch);

                return fileSystemWatch;
            });

            return fileSystemWatchFactory;
        }

        [Test]
        public void ShouldRaiseEvents()
        {
            TestFileSystemWatch testWatcher = null;
            var testWatchFactory = CreateTestWatchFactory(createdWatch => { testWatcher = createdWatch; });

            var defaultFileSystemWatchStrategy = new DefaultFileSystemWatchStrategy(testWatchFactory);

            var testWatchListener = Substitute.For<IFileSystemWatchListener>();
            defaultFileSystemWatchStrategy.AddListener(testWatchListener);

            defaultFileSystemWatchStrategy.Watch(@"c:\temp");

            testWatcher.RaiseCreated(@"c:\temp", "file.txt");

            testWatchListener.ReceivedWithAnyArgs(1).OnCreate(Arg.Any<object>(), Arg.Any<FileSystemEventArgs>());
            testWatchListener.ReceivedWithAnyArgs(0).OnChange(Arg.Any<object>(), Arg.Any<FileSystemEventArgs>());
            testWatchListener.ReceivedWithAnyArgs(0).OnDelete(Arg.Any<object>(), Arg.Any<FileSystemEventArgs>());
            testWatchListener.ReceivedWithAnyArgs(0).OnRename(Arg.Any<object>(), Arg.Any<RenamedEventArgs>());
            testWatchListener.ReceivedWithAnyArgs(0).OnError(Arg.Any<object>(), Arg.Any<ErrorEventArgs>());

            testWatcher.RaiseChanged(@"c:\temp", "file.txt");

            testWatchListener.ReceivedWithAnyArgs(1).OnCreate(Arg.Any<object>(), Arg.Any<FileSystemEventArgs>());
            testWatchListener.ReceivedWithAnyArgs(1).OnChange(Arg.Any<object>(), Arg.Any<FileSystemEventArgs>());
            testWatchListener.ReceivedWithAnyArgs(0).OnDelete(Arg.Any<object>(), Arg.Any<FileSystemEventArgs>());
            testWatchListener.ReceivedWithAnyArgs(0).OnRename(Arg.Any<object>(), Arg.Any<RenamedEventArgs>());
            testWatchListener.ReceivedWithAnyArgs(0).OnError(Arg.Any<object>(), Arg.Any<ErrorEventArgs>());

            testWatcher.RaiseDeleted(@"c:\temp", "file.txt");

            testWatchListener.ReceivedWithAnyArgs(1).OnCreate(Arg.Any<object>(), Arg.Any<FileSystemEventArgs>());
            testWatchListener.ReceivedWithAnyArgs(1).OnChange(Arg.Any<object>(), Arg.Any<FileSystemEventArgs>());
            testWatchListener.ReceivedWithAnyArgs(1).OnDelete(Arg.Any<object>(), Arg.Any<FileSystemEventArgs>());
            testWatchListener.ReceivedWithAnyArgs(0).OnRename(Arg.Any<object>(), Arg.Any<RenamedEventArgs>());
            testWatchListener.ReceivedWithAnyArgs(0).OnError(Arg.Any<object>(), Arg.Any<ErrorEventArgs>());

            testWatcher.RaiseRenamed(@"c:\temp", "file.txt", "file.txt");

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
            var testWatchFactory = CreateTestWatchFactory(createdWatch => { testWatcher = createdWatch; });

            var defaultFileSystemWatchStrategy = new DefaultFileSystemWatchStrategy(testWatchFactory);

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
    }
}
