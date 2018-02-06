using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using GitHub.Unity;
using NUnit.Framework;
using System.Diagnostics;
using GitHub.Logging;
using System.Runtime.CompilerServices;

namespace IntegrationTests.Download
{
    [TestFixture]
    class DownloadTaskTests : BaseTaskManagerTest
    {
        const int Timeout = 30000;

        public override void OnSetup()
        {
            base.OnSetup();
            InitializeEnvironment(TestBasePath, initializeRepository: false);
        }

        private TestWebServer.HttpServer server;
        public override void TestFixtureSetUp()
        {
            base.TestFixtureSetUp();
            server = new TestWebServer.HttpServer(SolutionDirectory.Combine("files"));
            Task.Factory.StartNew(server.Start);
            ApplicationConfiguration.WebTimeout = 5000;
        }

        public override void TestFixtureTearDown()
        {
            base.TestFixtureTearDown();
            server.Stop();
            ApplicationConfiguration.WebTimeout = ApplicationConfiguration.DefaultWebTimeout;
        }

        private void StartTest(out Stopwatch watch, out ILogging logger, [CallerMemberName] string testName = "test")
        {
            watch = new Stopwatch();
            logger = LogHelper.GetLogger(testName);
            logger.Trace("Starting test");
        }

        private void StartTrackTime(Stopwatch watch, ILogging logger = null, string message = "")
        {
            if (!String.IsNullOrEmpty(message))
                logger.Trace(message);
            watch.Reset();
            watch.Start();
        }

        private void StopTrackTimeAndLog(Stopwatch watch, ILogging logger)
        {
            watch.Stop();
            logger.Trace($"Time: {watch.ElapsedMilliseconds}");
        }

        [Test]
        public void ResumingDownloadsWorks()
        {
            Stopwatch watch;
            ILogging logger;
            StartTest(out watch, out logger);

            var fileSystem = Environment.FileSystem;

            var gitLfs = new UriString($"http://localhost:{server.Port}/git-lfs.zip");
            var gitLfsMd5 = new UriString($"http://localhost:{server.Port}/git-lfs.zip.MD5.txt");

            var evtDone = new ManualResetEventSlim(false);

            string md5 = null;

            StartTrackTime(watch, logger, gitLfsMd5);
            new DownloadTextTask(TaskManager.Token, fileSystem, gitLfsMd5, TestBasePath)
                .Finally(r => {
                    md5 = r;
                    evtDone.Set();
                })
                .Start();

            evtDone.Wait(Timeout).Should().BeTrue("Finally raised the signal");
            StopTrackTimeAndLog(watch, logger);

            evtDone.Reset();
            Assert.NotNull(md5);

            string downloadPath = null;
            StartTrackTime(watch, logger, gitLfs);
            new DownloadTask(TaskManager.Token, fileSystem, gitLfs, TestBasePath)
                .Finally(r => {
                    downloadPath = r;
                    evtDone.Set();
                })
                .Start();

            evtDone.Wait(Timeout).Should().BeTrue("Finally raised the signal");;
            StopTrackTimeAndLog(watch, logger);

            evtDone.Reset();

            Assert.NotNull(downloadPath);

            var md5Sum = fileSystem.CalculateFileMD5(downloadPath);
            md5Sum.Should().BeEquivalentTo(md5);

            var downloadPathBytes = fileSystem.ReadAllBytes(downloadPath);
            Logger.Trace("File size {0} bytes", downloadPathBytes.Length);

            var cutDownloadPathBytes = downloadPathBytes.Take(downloadPathBytes.Length - 1000).ToArray();
            fileSystem.FileDelete(downloadPath);
            fileSystem.WriteAllBytes(downloadPath, cutDownloadPathBytes);

            StartTrackTime(watch, logger, "resuming download");
            new DownloadTask(TaskManager.Token, fileSystem, gitLfs, TestBasePath)
                .Finally(r => {
                    downloadPath = r;
                    evtDone.Set();
                })
                .Start();

            evtDone.Wait(Timeout).Should().BeTrue("Finally raised the signal");;
            StopTrackTimeAndLog(watch, logger);

            evtDone.Reset();

            var downloadHalfPathBytes = fileSystem.ReadAllBytes(downloadPath);
            Logger.Trace("File size {0} Bytes", downloadHalfPathBytes.Length);

            md5Sum = fileSystem.CalculateFileMD5(downloadPath);
            md5Sum.Should().BeEquivalentTo(md5);
        }

        [Test]
        public void DownloadingNonExistingFileThrows()
        {
            Stopwatch watch;
            ILogging logger;
            StartTest(out watch, out logger);

            var fileSystem = Environment.FileSystem;

            var taskFailed = false;
            Exception exceptionThrown = null;

            var autoResetEvent = new AutoResetEvent(false);

            var downloadTask = new DownloadTask(TaskManager.Token, fileSystem, $"http://localhost:{server.Port}/nope", TestBasePath);

            StartTrackTime(watch);
            downloadTask
                .Finally((b, exception) => {
                    taskFailed = !b;
                    exceptionThrown = exception;
                    autoResetEvent.Set();
                })
                .Start();

            var ret = autoResetEvent.WaitOne(Timeout);
            StopTrackTimeAndLog(watch, logger);

            ret.Should().BeTrue("Finally raised the signal");

            taskFailed.Should().BeTrue();
            exceptionThrown.Should().NotBeNull();
        }

        [Test]
        public void DownloadingATextFileWorks()
        {
            Stopwatch watch;
            ILogging logger;
            StartTest(out watch, out logger);

            var fileSystem = Environment.FileSystem;

            var gitLfsMd5 = new UriString($"http://localhost:{server.Port}/git-lfs.zip.MD5.txt");

            var downloadTask = new DownloadTextTask(TaskManager.Token, fileSystem, gitLfsMd5, TestBasePath);

            var autoResetEvent = new AutoResetEvent(false);
            string result = null;

            StartTrackTime(watch);
            downloadTask
                .Finally(r => {
                    result = r;
                    autoResetEvent.Set();
                })
                .Start();

            autoResetEvent.WaitOne(Timeout).Should().BeTrue("Finally raised the signal");;
            StopTrackTimeAndLog(watch, logger);

            result.Should().Be("105DF1302560C5F6AA64D1930284C126");
        }

        [Test]
        public void DownloadingFromNonExistingDomainThrows()
        {
            Stopwatch watch;
            ILogging logger;
            StartTest(out watch, out logger);

            var fileSystem = Environment.FileSystem;

            var downloadTask = new DownloadTextTask(TaskManager.Token, fileSystem, "http://ggggithub.com/robots.txt");
            var exceptionThrown = false;

            var autoResetEvent = new AutoResetEvent(false);

            StartTrackTime(watch);
            downloadTask
            .Finally((b, exception) => {
                exceptionThrown = exception != null;
                autoResetEvent.Set();
            })
            .Start();

            autoResetEvent.WaitOne(Timeout).Should().BeTrue("Finally raised the signal");;
            StopTrackTimeAndLog(watch, logger);

            exceptionThrown.Should().BeTrue();
        }
        
        [Test]
        public void DownloadingAFileWithHashValidationWorks()
        {
            Stopwatch watch;
            ILogging logger;
            StartTest(out watch, out logger);

            var fileSystem = Environment.FileSystem;

            var gitLfs = new UriString($"http://localhost:{server.Port}/git-lfs.zip");
            var gitLfsMd5 = new UriString($"http://localhost:{server.Port}/git-lfs.zip.MD5.txt");

            var downloadGitLfsMd5Task = new DownloadTextTask(TaskManager.Token, fileSystem, gitLfsMd5, TestBasePath);
            var downloadGitLfsTask = new DownloadTask(TaskManager.Token, fileSystem, gitLfs, TestBasePath);

            var result = true;
            Exception exception = null;

            var autoResetEvent = new AutoResetEvent(false);

            StartTrackTime(watch);
            downloadGitLfsMd5Task
                .Then((b, s) =>
                {
                    downloadGitLfsTask.ValidationHash = s;
                })
                .Then(downloadGitLfsTask)
                .Finally((b, ex) => {
                    result = b;
                    exception = ex;
                    autoResetEvent.Set();
                })
                .Start();

            autoResetEvent.WaitOne(Timeout).Should().BeTrue("Finally raised the signal");;
            StopTrackTimeAndLog(watch, logger);

            result.Should().BeTrue();
            exception.Should().BeNull();
        }

        [Test]
        public void ShutdownTimeWhenTaskManagerDisposed()
        {
            Stopwatch watch;
            ILogging logger;
            StartTest(out watch, out logger);

            server.Delay = 100;

            var fileSystem = Environment.FileSystem;

            var evtStop = new AutoResetEvent(false);
            var evtFinally = new AutoResetEvent(false);
            Exception exception = null;

            var gitLfs = new UriString($"http://localhost:{server.Port}/git-lfs.zip");

            StartTrackTime(watch, logger, gitLfs);
            var downloadGitTask = new DownloadTask(TaskManager.Token, fileSystem, gitLfs, TestBasePath)

                // An exception is thrown when we stop the task manager
                // since we're stopping the task manager, no other tasks
                // will run, which means we can only hook with Catch
                // or with the Finally overload that runs on the same thread (not as a task)
                .Catch(e =>
                {
                    exception = e;
                    evtFinally.Set();
                })
                .Progress(p =>
                {
                    evtStop.Set();
                });

            downloadGitTask.Start();

            evtStop.WaitOne(Timeout).Should().BeTrue("Progress raised the signal");
            StopTrackTimeAndLog(watch, logger);


            StartTrackTime(watch, logger, "TaskManager.Dispose()");
            TaskManager.Dispose();
            evtFinally.WaitOne(Timeout).Should().BeTrue("Catch raised the signal");
            StopTrackTimeAndLog(watch, logger);

            server.Delay = 0;
            server.Abort();

            exception.Should().NotBeNull();
            watch.ElapsedMilliseconds.Should().BeLessThan(250);
        }
    }
}
