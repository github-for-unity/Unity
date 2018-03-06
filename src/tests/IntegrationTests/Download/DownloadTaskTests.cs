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
using System.Collections.Generic;

namespace IntegrationTests.Download
{
    class BaseDownloaderTest : BaseTaskManagerTest
    {
        protected const int Timeout = 30000;
        protected TestWebServer.HttpServer server;

        public override void OnSetup()
        {
            base.OnSetup();
            InitializeEnvironment(TestBasePath, initializeRepository: false);
        }

        public override void TestFixtureSetUp()
        {
            base.TestFixtureSetUp();
            server = new TestWebServer.HttpServer(SolutionDirectory.Combine("files"));
            Task.Factory.StartNew(server.Start);
            ApplicationConfiguration.WebTimeout = 50000;
        }

        public override void TestFixtureTearDown()
        {
            base.TestFixtureTearDown();
            server.Stop();
            ApplicationConfiguration.WebTimeout = ApplicationConfiguration.DefaultWebTimeout;
        }

        protected void StartTest(out Stopwatch watch, out ILogging logger, [CallerMemberName] string testName = "test")
        {
            watch = new Stopwatch();
            logger = LogHelper.GetLogger(testName);
            logger.Trace("Starting test");
        }

        protected void StartTrackTime(Stopwatch watch, ILogging logger = null, string message = "")
        {
            if (!String.IsNullOrEmpty(message))
                logger.Trace(message);
            watch.Reset();
            watch.Start();
        }

        protected void StopTrackTimeAndLog(Stopwatch watch, ILogging logger)
        {
            watch.Stop();
            logger.Trace($"Time: {watch.ElapsedMilliseconds}");
        }
    }

    [TestFixture]
    class DownloaderTests : BaseDownloaderTest
    {
        [Test]
        public async Task DownloadAndVerificationWorks()
        {
            Stopwatch watch;
            ILogging logger;
            StartTest(out watch, out logger);

            var fileSystem = Environment.FileSystem;
            var fileUrl = new UriString($"http://localhost:{server.Port}/git-lfs.zip");
            var md5Url = new UriString($"http://localhost:{server.Port}/git-lfs.zip.MD5.txt");

            var downloader = new Downloader();
            StartTrackTime(watch, logger, md5Url);
            downloader.QueueDownload(fileUrl, md5Url, TestBasePath);
            
            var task = await TaskEx.WhenAny(downloader.Start().Task, TaskEx.Delay(Timeout));
            StopTrackTimeAndLog(watch, logger);
            Assert.AreEqual(downloader.Task, task);
            Assert.IsTrue(downloader.Successful);
            var result = await downloader.Task;
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(TestBasePath.Combine(fileUrl.Filename), result[0].File);
        }

        [Test]
        public async Task DownloadingNonExistingFileThrows()
        {
            Stopwatch watch;
            ILogging logger;
            StartTest(out watch, out logger);

            var fileSystem = Environment.FileSystem;
            var fileUrl = new UriString($"http://localhost:{server.Port}/nope");
            var md5Url = new UriString($"http://localhost:{server.Port}/git-lfs.zip.MD5.txt");

            var downloader = new Downloader();
            StartTrackTime(watch, logger, md5Url);
            downloader.QueueDownload(fileUrl, md5Url, TestBasePath);
            var task = await TaskEx.WhenAny(downloader.Start().Task, TaskEx.Delay(Timeout));
            StopTrackTimeAndLog(watch, logger);
            Assert.AreEqual(downloader.Task, task);
            Assert.Throws(typeof(DownloadException), async () => await downloader.Task);
        }

        [Test]
        public async Task FailsIfVerificationFails()
        {
            Stopwatch watch;
            ILogging logger;
            StartTest(out watch, out logger);

            var fileSystem = Environment.FileSystem;
            var fileUrl = new UriString($"http://localhost:{server.Port}/git.zip");
            var md5Url = new UriString($"http://localhost:{server.Port}/git-lfs.zip.MD5.txt");

            var downloader = new Downloader();
            StartTrackTime(watch, logger, md5Url);
            downloader.QueueDownload(fileUrl, md5Url, TestBasePath);
            var task = await TaskEx.WhenAny(downloader.Start().Task, TaskEx.Delay(Timeout));
            StopTrackTimeAndLog(watch, logger);
            Assert.AreEqual(downloader.Task, task);
            Assert.Throws(typeof(DownloadException), async () => await downloader.Task);
        }

        [Test]
        public async Task ResumingWorks()
        {
            Stopwatch watch;
            ILogging logger;
            StartTest(out watch, out logger);

            var fileSystem = Environment.FileSystem;
            var fileUrl = new UriString($"http://localhost:{server.Port}/git-lfs.zip");
            var md5Url = new UriString($"http://localhost:{server.Port}/git-lfs.zip.MD5.txt");

            var downloader = new Downloader();
            StartTrackTime(watch, logger, md5Url);
            downloader.QueueDownload(fileUrl, md5Url, TestBasePath);
            var task = await TaskEx.WhenAny(downloader.Start().Task, TaskEx.Delay(Timeout));
            StopTrackTimeAndLog(watch, logger);
            Assert.AreEqual(downloader.Task, task);
            var result = await downloader.Task;
            var downloadData = result.FirstOrDefault();

            var downloadPathBytes = fileSystem.ReadAllBytes(downloadData.File);
            Logger.Trace("File size {0} bytes", downloadPathBytes.Length);

            var cutDownloadPathBytes = downloadPathBytes.Take(downloadPathBytes.Length - 1000).ToArray();
            fileSystem.FileDelete(downloadData.File);
            fileSystem.WriteAllBytes(downloadData + ".partial", cutDownloadPathBytes);

            downloader = new Downloader();
            StartTrackTime(watch, logger, "resuming download");
            downloader.QueueDownload(fileUrl, md5Url, TestBasePath);
            task = await TaskEx.WhenAny(downloader.Start().Task, TaskEx.Delay(Timeout));
            StopTrackTimeAndLog(watch, logger);
            Assert.AreEqual(downloader.Task, task);
            result = await downloader.Task;
            downloadData = result.FirstOrDefault();

            var md5Sum = downloadData.File.CalculateMD5();
            var md5 = TestBasePath.Combine(md5Url.Filename).ReadAllText();
            md5Sum.Should().BeEquivalentTo(md5);
        }

            [Test]
        public async Task SucceedIfEverythingIsAlreadyDownloaded()
        {
            Stopwatch watch;
            ILogging logger;
            StartTest(out watch, out logger);

            var fileSystem = Environment.FileSystem;
            var fileUrl = new UriString($"http://localhost:{server.Port}/git-lfs.zip");
            var md5Url = new UriString($"http://localhost:{server.Port}/git-lfs.zip.MD5.txt");

            var downloader = new Downloader();
            StartTrackTime(watch, logger, md5Url);
            downloader.QueueDownload(fileUrl, md5Url, TestBasePath);
            var task = await TaskEx.WhenAny(downloader.Start().Task, TaskEx.Delay(Timeout));
            StopTrackTimeAndLog(watch, logger);
            Assert.AreEqual(downloader.Task, task);
            var downloadData = await downloader.Task;
            var downloadPath = downloadData.FirstOrDefault().File;

            downloader = new Downloader();
            StartTrackTime(watch, logger, "downloading again");
            downloader.QueueDownload(fileUrl, md5Url, TestBasePath);
            task = await TaskEx.WhenAny(downloader.Start().Task, TaskEx.Delay(Timeout));
            StopTrackTimeAndLog(watch, logger);
            Assert.AreEqual(downloader.Task, task);
            downloadData = await downloader.Task;
            downloadPath = downloadData.FirstOrDefault().File;

            var md5Sum = downloadPath.CalculateMD5();
            var md5 = TestBasePath.Combine(md5Url.Filename).ReadAllText();
            md5Sum.Should().BeEquivalentTo(md5);
        }

        [Category("DoNotRunOnAppVeyor")]
        public async Task DownloadsRunSideBySide()
        {
            Stopwatch watch;
            ILogging logger;
            StartTest(out watch, out logger);

            var fileUrl1 = new UriString($"http://localhost:{server.Port}/git-lfs.zip");
            var md5Url1 = new UriString($"http://localhost:{server.Port}/git-lfs.zip.MD5.txt");
            var fileUrl2 = new UriString($"http://localhost:{server.Port}/git.zip");
            var md5Url2 = new UriString($"http://localhost:{server.Port}/git.zip.MD5.txt");

            var events = new List<string>();

            var downloader = new Downloader();
            downloader.QueueDownload(fileUrl2, md5Url2, TestBasePath);
            downloader.QueueDownload(fileUrl1, md5Url1, TestBasePath);
            downloader.DownloadStart += d => events.Add("start " + d.Url.Filename);
            downloader.DownloadComplete += d => events.Add("end " + d.Url.Filename);
            downloader.DownloadFailed += (d, _) => events.Add("failed " + d.Url.Filename);

            server.Delay = 1;
            StartTrackTime(watch, logger);
            var task = await TaskEx.WhenAny(downloader.Start().Task, TaskEx.Delay(Timeout));
            StopTrackTimeAndLog(watch, logger);
            server.Delay = 0;

            Assert.AreEqual(downloader.Task, task);

            CollectionAssert.AreEqual(new string[] {
                "start git.zip",
                "start git-lfs.zip",
                "end git-lfs.zip",
                "end git.zip",
            }, events);
        }
    }

    [TestFixture]
    class DownloadTaskTests : BaseDownloaderTest
    {
        [Test]
        public async Task ResumingDownloadsWorks()
        {
            Stopwatch watch;
            ILogging logger;
            StartTest(out watch, out logger);

            var fileSystem = Environment.FileSystem;

            var gitLfs = new UriString($"http://localhost:{server.Port}/git-lfs.zip");
            var gitLfsMd5 = new UriString($"http://localhost:{server.Port}/git-lfs.zip.MD5.txt");

            var downloadTask = new DownloadTask(TaskManager.Token, fileSystem, gitLfsMd5, TestBasePath);

            StartTrackTime(watch, logger, gitLfsMd5);
            var task = await TaskEx.WhenAny(downloadTask.Start().Task, TaskEx.Delay(Timeout));
            StopTrackTimeAndLog(watch, logger);

            task.ShouldBeEquivalentTo(downloadTask.Task);
            var downloadPath = await downloadTask.Task;
            var md5 = downloadPath.ReadAllText();
            Assert.NotNull(md5);

            StartTrackTime(watch, logger, gitLfs);
            downloadTask = new DownloadTask(TaskManager.Token, fileSystem, gitLfs, TestBasePath);

            StartTrackTime(watch, logger, gitLfsMd5);
            task = await TaskEx.WhenAny(downloadTask.Start().Task, TaskEx.Delay(Timeout));
            StopTrackTimeAndLog(watch, logger);
            task.ShouldBeEquivalentTo(downloadTask.Task);

            downloadPath = await downloadTask.Task;
            Assert.NotNull(downloadPath);

            var md5Sum = downloadPath.CalculateMD5();
            md5Sum.Should().BeEquivalentTo(md5);

            var downloadPathBytes = downloadPath.ReadAllBytes();
            Logger.Trace("File size {0} bytes", downloadPathBytes.Length);

            var cutDownloadPathBytes = downloadPathBytes.Take(downloadPathBytes.Length - 1000).ToArray();
            downloadPath.Delete();
            new NPath(downloadPath + ".partial").WriteAllBytes(cutDownloadPathBytes);

            downloadTask = new DownloadTask(TaskManager.Token, fileSystem, gitLfs, TestBasePath);

            StartTrackTime(watch, logger, gitLfs);
            task = await TaskEx.WhenAny(downloadTask.Start().Task, TaskEx.Delay(Timeout));
            StopTrackTimeAndLog(watch, logger);
            task.ShouldBeEquivalentTo(downloadTask.Task);
            downloadPath = await downloadTask.Task;

            md5Sum = downloadPath.CalculateMD5();
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
                .Finally((success, exception) => {
                    taskFailed = !success;
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

        //[Test]
        //public void DownloadingATextFileWorks()
        //{
        //    Stopwatch watch;
        //    ILogging logger;
        //    StartTest(out watch, out logger);

        //    var fileSystem = Environment.FileSystem;

        //    var gitLfsMd5 = new UriString($"http://localhost:{server.Port}/git-lfs.zip.MD5.txt");

        //    var downloadTask = new DownloadTextTask(TaskManager.Token, fileSystem, gitLfsMd5, TestBasePath);

        //    var autoResetEvent = new AutoResetEvent(false);
        //    string result = null;

        //    StartTrackTime(watch);
        //    downloadTask
        //        .Finally((success, r) => {
        //            result = r;
        //            autoResetEvent.Set();
        //        })
        //        .Start();

        //    autoResetEvent.WaitOne(Timeout).Should().BeTrue("Finally raised the signal");;
        //    StopTrackTimeAndLog(watch, logger);

        //    result.Should().Be("105DF1302560C5F6AA64D1930284C126");
        //}

        [Test]
        public void DownloadingFromNonExistingDomainThrows()
        {
            Stopwatch watch;
            ILogging logger;
            StartTest(out watch, out logger);

            var fileSystem = Environment.FileSystem;

            var downloadTask = new DownloadTask(TaskManager.Token, fileSystem, "http://ggggithub.com/robots.txt");
            var exceptionThrown = false;

            var autoResetEvent = new AutoResetEvent(false);

            StartTrackTime(watch);
            downloadTask
            .Finally(success => {
                exceptionThrown = !success;
                autoResetEvent.Set();
            })
            .Start();

            autoResetEvent.WaitOne(Timeout).Should().BeTrue("Finally raised the signal");;
            StopTrackTimeAndLog(watch, logger);

            exceptionThrown.Should().BeTrue();
        }
        
        //[Test]
        //public void DownloadingAFileWithHashValidationWorks()
        //{
        //    Stopwatch watch;
        //    ILogging logger;
        //    StartTest(out watch, out logger);

        //    var fileSystem = Environment.FileSystem;

        //    var gitLfs = new UriString($"http://localhost:{server.Port}/git-lfs.zip");
        //    var gitLfsMd5 = new UriString($"http://localhost:{server.Port}/git-lfs.zip.MD5.txt");

        //    var downloadGitLfsMd5Task = new DownloadTextTask(TaskManager.Token, fileSystem, gitLfsMd5, TestBasePath);
        //    var downloadGitLfsTask = new DownloadTask(TaskManager.Token, fileSystem, gitLfs, TestBasePath);

        //    var result = true;
        //    Exception exception = null;

        //    var autoResetEvent = new AutoResetEvent(false);

        //    StartTrackTime(watch);
        //    downloadGitLfsMd5Task
        //        .Then(downloadGitLfsTask)
        //        .Finally((b, ex) => {
        //            result = b;
        //            exception = ex;
        //            autoResetEvent.Set();
        //        })
        //        .Start();

        //    autoResetEvent.WaitOne(Timeout).Should().BeTrue("Finally raised the signal");;
        //    StopTrackTimeAndLog(watch, logger);

        //    result.Should().BeTrue();
        //    exception.Should().BeNull();
        //}

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
