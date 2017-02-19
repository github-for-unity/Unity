using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using FluentAssertions;
using GitHub.Api;
using NCrunch.Framework;
using NSubstitute;
using NUnit.Framework;

namespace GitHub.Unity.Tests
{
    [Isolated, TestFixture]
    public class FileSystemWatcherIntegrationTests
    {
        [SetUp]
        public void Setup()
        {
            tempPath = fileSystem.GetTempPath();
            tempNPath = new NPath(tempPath);
            testNpath = tempNPath.CreateDirectory(fileSystem.GetRandomFileName());

            fooBarTxtNpath = testNpath.CreateFile("foobar.txt");
            fooBarTxtNpath.WriteAllText("foobar");

            childOneNpath = testNpath.CreateDirectory("child1");

            childOneFooBarTxtNpath = childOneNpath.CreateFile("foobar.txt");
            childOneFooBarTxtNpath.WriteAllText("foobar");
        }

        private FileSystem fileSystem;
        private NPath tempNPath;
        private NPath testNpath;
        private NPath fooBarTxtNpath;
        private NPath childOneNpath;
        private NPath childOneFooBarTxtNpath;
        private ILogging logger;
        private string tempPath;

        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            NPathFileSystemProvider.Current.Should().BeNull("This test should be run in isolation");

            fileSystem = new FileSystem();
            NPathFileSystemProvider.Current = fileSystem;

            logger = Logging.GetLogger<FileSystemWatcherIntegrationTests>();
        }

        [TestFixtureTearDown]
        public void TestFixtureTearDown()
        {
            NPathFileSystemProvider.Current = null;
        }

        private IFileSystemWatchWrapperFactory GetWrappedFileSystemWatchWrapperFactory()
        {
            var logger = Logging.GetLogger("WrappedFileSystemWatchWrapperFactory");

            var actualWatchWrapperFactory = new FileSystemWatchWrapperFactory();
            var fileSystemWatchWrapperFactory = Substitute.For<IFileSystemWatchWrapperFactory>();
            fileSystemWatchWrapperFactory.CreateWatch(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<string>())
                                         .Returns(info => {
                                             var path = (string)info[0];
                                             var recursive = (bool)info[1];
                                             var filter = (string)info[2];

                                             logger.Trace(@"CreateWatch(""{0}"", {1}, ""{2}"")", path, recursive,
                                                 filter);

                                             return actualWatchWrapperFactory.CreateWatch(path, recursive, filter);
                                         });

            return fileSystemWatchWrapperFactory;
        }

        private void PerformNonRecursiveTest(FileSystemWatch fileSystemWatch)
        {
            var maxWaitTime = TimeSpan.FromMilliseconds(100);
            var semaphoreSlim = new SemaphoreSlim(0);

            var changeEvents = new List<FileSystemEventArgs>();
            var deleteEvents = new List<FileSystemEventArgs>();
            var createEvents = new List<FileSystemEventArgs>();
            var renameEvents = new List<RenamedEventArgs>();
            var errorEvents = new List<ErrorEventArgs>();

            var watchListener = CreateTestWatchListener(changeEvents, semaphoreSlim, deleteEvents, createEvents,
                renameEvents, errorEvents);

            fileSystemWatch.AttachListener(watchListener);
            fileSystemWatch.Enable = true;

            //Change a file in the main root
            fooBarTxtNpath.WriteAllText("FOOBAR");

            //Wait for 2 events
            semaphoreSlim.Wait(maxWaitTime);
            semaphoreSlim.Wait(maxWaitTime);
            semaphoreSlim.CurrentCount.Should().Be(0);

            //Observe 2 Change Events
            //http://stackoverflow.com/questions/1764809/filesystemwatcher-changed-event-is-raised-twice
            changeEvents.Should().HaveCount(2);
            changeEvents.Clear();

            //Change a file in a child folder
            childOneFooBarTxtNpath.WriteAllText("FOOBAR");
            Thread.Sleep(maxWaitTime);

            //Observe 0 Change Events
            semaphoreSlim.CurrentCount.Should().Be(0);
            changeEvents.Should().HaveCount(0);
            changeEvents.Clear();

            //Delete a file in the main root
            fooBarTxtNpath.Delete();

            //Wait for 1 event
            semaphoreSlim.Wait(maxWaitTime);
            semaphoreSlim.CurrentCount.Should().Be(0);

            //Observe 1 Delete Event
            deleteEvents.Should().HaveCount(1);
            deleteEvents.Clear();

            //Delete a file in the child folder
            childOneFooBarTxtNpath.Delete();
            Thread.Sleep(maxWaitTime);

            //Observe 0 Delete Events
            semaphoreSlim.CurrentCount.Should().Be(0);
            deleteEvents.Should().HaveCount(0);
            deleteEvents.Clear();

            //Delete the child folder
            childOneNpath.Delete();

            //Wait for 2 events
            semaphoreSlim.Wait(maxWaitTime);
            semaphoreSlim.Wait(maxWaitTime);
            semaphoreSlim.CurrentCount.Should().Be(0);

            //Observe 1 Change Event
            changeEvents.Should().HaveCount(1);
            changeEvents.Clear();

            //Observe 1 Delete Event
            deleteEvents.Should().HaveCount(1);
            deleteEvents.Clear();

            //Create a folder
            var depthRoot = testNpath.CreateDirectory("depth");
            var depthWalker = depthRoot;

            //Wait for 2 events
            semaphoreSlim.Wait(maxWaitTime);
            semaphoreSlim.CurrentCount.Should().Be(0);

            //Observe 1 Create Events
            createEvents.Should().HaveCount(1);
            createEvents.Clear();

            for (var i = 0; i < 10; i++)
            {
                depthWalker = depthWalker.CreateDirectory("depth" + i);
            }

            semaphoreSlim.Wait(maxWaitTime);
            semaphoreSlim.CurrentCount.Should().Be(0);

            //Observe 1 Change Event
            changeEvents.Should().HaveCount(1);
            changeEvents.Clear();
        }

        private void PerformRecursiveTest(FileSystemWatch fileSystemWatch)
        {
            var maxWaitTime = TimeSpan.FromMilliseconds(100);
            var semaphoreSlim = new SemaphoreSlim(0);

            var changeEvents = new List<FileSystemEventArgs>();
            var deleteEvents = new List<FileSystemEventArgs>();
            var createEvents = new List<FileSystemEventArgs>();
            var renameEvents = new List<RenamedEventArgs>();
            var errorEvents = new List<ErrorEventArgs>();

            var watchListener = CreateTestWatchListener(changeEvents, semaphoreSlim, deleteEvents, createEvents,
                renameEvents, errorEvents);

            fileSystemWatch.AttachListener(watchListener);
            fileSystemWatch.Enable = true;

            //Change a file in the main root
            fooBarTxtNpath.WriteAllText("FOOBAR");

            //Wait for 2 events
            semaphoreSlim.Wait(maxWaitTime);
            semaphoreSlim.Wait(maxWaitTime);
            semaphoreSlim.CurrentCount.Should().Be(0);

            //Observe 2 Change Events
            //http://stackoverflow.com/questions/1764809/filesystemwatcher-changed-event-is-raised-twice
            changeEvents.Should().HaveCount(2);
            changeEvents.Clear();

            //Change a file in a child folder
            childOneFooBarTxtNpath.WriteAllText("FOOBAR");
           
            //Wait for 2 events
            semaphoreSlim.Wait(maxWaitTime);
            semaphoreSlim.Wait(maxWaitTime);
            semaphoreSlim.CurrentCount.Should().Be(0);

            //Observe 2 Change Events
            changeEvents.Should().HaveCount(2);
            changeEvents.Clear();

            //Delete a file in the main root
            fooBarTxtNpath.Delete();

            //Wait for 1 event
            semaphoreSlim.Wait(maxWaitTime);
            semaphoreSlim.CurrentCount.Should().Be(0);

            //Observe 1 Delete Event
            deleteEvents.Should().HaveCount(1);
            deleteEvents.Clear();

            //Delete a file in the child folder
            childOneFooBarTxtNpath.Delete();
          
            //Wait for 1 event
            semaphoreSlim.Wait(maxWaitTime);
            semaphoreSlim.CurrentCount.Should().Be(0);

            //Observe 1 Delete Event
            deleteEvents.Should().HaveCount(1);
            deleteEvents.Clear();

            //Create a folder
            var depthRoot = testNpath.CreateDirectory("depth");
            var depthWalker = depthRoot;

            for (var i = 0; i < 10; i++)
            {
                depthWalker = depthWalker.CreateDirectory("depth" + i);
            }

            //Wait for 20 Create/Change events
            for (int i = 0; i < 20; i++)
            {
                semaphoreSlim.Wait(maxWaitTime);
            }
            semaphoreSlim.CurrentCount.Should().Be(0);

            //Observe 10 Create Events
            createEvents.Should().HaveCount(11);
            createEvents.Clear();

            //Observe 10 Change Events
            changeEvents.Should().HaveCount(9);
            changeEvents.Clear();

            depthRoot.Delete();

            for (int i = 0; i < 21; i++)
            {
                semaphoreSlim.Wait(maxWaitTime);
            }
            semaphoreSlim.CurrentCount.Should().Be(0);

            //Observe 11 Delete Events
            deleteEvents.Should().HaveCount(11);
            deleteEvents.Clear();

            //Observe 10 Change Events
            changeEvents.Should().HaveCount(10);
            changeEvents.Clear();
        }

        private IFileSystemWatchListener CreateTestWatchListener(List<FileSystemEventArgs> changeEvents,
            SemaphoreSlim semaphoreSlim, List<FileSystemEventArgs> deleteEvents, List<FileSystemEventArgs> createEvents,
            List<RenamedEventArgs> renameEvents, List<ErrorEventArgs> errorEvents)
        {
            var watchListener = Substitute.For<IFileSystemWatchListener>();
            watchListener.When(listener => listener.OnChange(Arg.Any<object>(), Arg.Any<FileSystemEventArgs>()))
                         .Do(info => {
                             var fileSystemEventArgs = (FileSystemEventArgs)info[1];

                             logger.Debug("OnChange: Path:\"{0}\" ChangeType:\"{1}\"",
                                 fileSystemEventArgs.FullPath.Substring(tempPath.Length - 1),
                                 fileSystemEventArgs.ChangeType);

                             changeEvents.Add(fileSystemEventArgs);

                             semaphoreSlim.Release();
                         });

            watchListener.When(listener => listener.OnDelete(Arg.Any<object>(), Arg.Any<FileSystemEventArgs>()))
                         .Do(info => {
                             var fileSystemEventArgs = (FileSystemEventArgs)info[1];
                             logger.Debug("OnDelete: Path:\"{0}\" ChangeType:\"{1}\"",
                                 fileSystemEventArgs.FullPath.Substring(tempPath.Length - 1),
                                 fileSystemEventArgs.ChangeType);

                             deleteEvents.Add(fileSystemEventArgs);

                             semaphoreSlim.Release();
                         });

            watchListener.When(listener => listener.OnCreate(Arg.Any<object>(), Arg.Any<FileSystemEventArgs>()))
                         .Do(info => {
                             var fileSystemEventArgs = (FileSystemEventArgs)info[1];
                             logger.Debug("OnCreate: Path:\"{0}\" ChangeType:\"{1}\"",
                                 fileSystemEventArgs.FullPath.Substring(tempPath.Length - 1),
                                 fileSystemEventArgs.ChangeType);

                             createEvents.Add(fileSystemEventArgs);

                             semaphoreSlim.Release();
                         });

            watchListener.When(listener => listener.OnRename(Arg.Any<object>(), Arg.Any<RenamedEventArgs>()))
                         .Do(info => {
                             var renamedEventArgs = (RenamedEventArgs)info[1];
                             logger.Debug("OnRename: Path:\"{0}\" OldFullPath:\"{1}\" ChangeType:\"{2}\"",
                                 renamedEventArgs.FullPath.Substring(tempPath.Length - 1),
                                 renamedEventArgs.OldFullPath.Substring(tempPath.Length - 1),
                                 renamedEventArgs.ChangeType);

                             renameEvents.Add(renamedEventArgs);

                             semaphoreSlim.Release();
                         });

            watchListener.When(listener => listener.OnError(Arg.Any<object>(), Arg.Any<ErrorEventArgs>())).Do(info => {
                var errorEventArgs = (ErrorEventArgs)info[1];
                logger.Debug("OnError");

                errorEvents.Add(errorEventArgs);

                semaphoreSlim.Release();
            });
            return watchListener;
        }

        [Test]
        public void TestNonRecursiveAdaptiveFileSystemWatcher()
        {
            var fileSystemWatchWrapperFactory = GetWrappedFileSystemWatchWrapperFactory();

            var nonRecursiveFileSystemWatch = new AdaptiveFileSystemWatch(fileSystemWatchWrapperFactory, fileSystem,
                testNpath.ToString());

            PerformNonRecursiveTest(nonRecursiveFileSystemWatch);
        }

        [Test]
        public void TestNonRecursiveDefaultFileSystemWatcher()
        {
            var fileSystemWatchWrapperFactory = GetWrappedFileSystemWatchWrapperFactory();

            var nonRecursiveFileSystemWatch = new DefaultFileSystemWatch(fileSystemWatchWrapperFactory,
                testNpath.ToString());

            PerformNonRecursiveTest(nonRecursiveFileSystemWatch);
        }

        [Test]
        public void TestRecursiveAdaptiveFileSystemWatcher()
        {
            var fileSystemWatchWrapperFactory = GetWrappedFileSystemWatchWrapperFactory();

            var recursiveFileSystemWatch = new AdaptiveFileSystemWatch(fileSystemWatchWrapperFactory, fileSystem,
                testNpath.ToString(), true);

            PerformRecursiveTest(recursiveFileSystemWatch);
        }

        [Test]
        public void TestRecursiveDefaultFileSystemWatcher()
        {
            var fileSystemWatchWrapperFactory = GetWrappedFileSystemWatchWrapperFactory();

            var recursiveFileSystemWatch = new DefaultFileSystemWatch(fileSystemWatchWrapperFactory,
                testNpath.ToString(), true);

            PerformRecursiveTest(recursiveFileSystemWatch);
        }
    }
}
