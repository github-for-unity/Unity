using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using GitHub.Unity;
using NUnit.Framework;
using System.Diagnostics;
using GitHub.Logging;
using System.Collections.Generic;

namespace IntegrationTests.Download
{
    [TestFixture]
    class TestsWithHttpServer : BaseTestWithHttpServer
    {
        public override void OnSetup()
        {
            base.OnSetup();
            InitializeEnvironment(TestBasePath, false, false);
        }

        [Test]
        public async Task DownloadAndVerificationWorks()
        {
            Stopwatch watch;
            ILogging logger;
            StartTest(out watch, out logger);

            var package = Package.Load(Environment, new UriString($"http://localhost:{server.Port}/unity/git/windows/git-lfs.json"));

            var downloader = new Downloader(Environment.FileSystem);
            downloader.QueueDownload(package.Uri, TestBasePath);

            StartTrackTime(watch, logger, package.Url);
            var task = await TaskEx.WhenAny(downloader.Start().Task, TaskEx.Delay(Timeout));
            StopTrackTimeAndLog(watch, logger);

            Assert.AreEqual(downloader.Task, task);
            Assert.IsTrue(downloader.Successful);
            var result = await downloader.Task;

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(TestBasePath.Combine(package.Uri.Filename), result[0].File);
            Assert.AreEqual(package.Md5, result[0].File.CalculateMD5());
        }

        [Test]
        public async Task DownloadingNonExistingFileThrows()
        {
            Stopwatch watch;
            ILogging logger;
            StartTest(out watch, out logger);

            var package = new Package { Url = $"http://localhost:{server.Port}/nope" };

            var downloader = new Downloader(Environment.FileSystem);
            StartTrackTime(watch, logger, package.Url);
            downloader.QueueDownload(package.Uri, TestBasePath);

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

            var gitPackage = Package.Load(Environment, new UriString($"http://localhost:{server.Port}/unity/git/windows/git.json"));
            var gitLfsPackage = Package.Load(Environment, new UriString($"http://localhost:{server.Port}/unity/git/windows/git-lfs.json"));

            var package = new Package { Url = gitPackage.Url, Md5 = gitLfsPackage.Md5 };

            var downloader = new Downloader(Environment.FileSystem);
            downloader.QueueDownload(package.Uri, TestBasePath);

            StartTrackTime(watch, logger, package.Url);
            var task = await TaskEx.WhenAny(downloader.Start().Task, TaskEx.Delay(Timeout));
            StopTrackTimeAndLog(watch, logger);

            Assert.AreEqual(downloader.Task, task);
            Assert.IsTrue(downloader.Successful);
            var result = await downloader.Task;

            Assert.AreNotEqual(package.Md5, result[0].File.CalculateMD5());
        }

        [Test]
        public async Task ResumingWorks()
        {
            Stopwatch watch;
            ILogging logger;
            StartTest(out watch, out logger);

            var fileSystem = NPath.FileSystem;
            var package = Package.Load(Environment, new UriString($"http://localhost:{server.Port}/unity/git/windows/git-lfs.json"));

            var downloader = new Downloader(fileSystem);
            StartTrackTime(watch, logger, package.Url);
            downloader.QueueDownload(package.Uri, TestBasePath);

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

            downloader = new Downloader(fileSystem);
            StartTrackTime(watch, logger, "resuming download");
            downloader.QueueDownload(package.Uri, TestBasePath);
            task = await TaskEx.WhenAny(downloader.Start().Task, TaskEx.Delay(Timeout));
            StopTrackTimeAndLog(watch, logger);
            Assert.AreEqual(downloader.Task, task);
            result = await downloader.Task;
            downloadData = result.FirstOrDefault();

            var md5Sum = downloadData.File.CalculateMD5();
            md5Sum.Should().BeEquivalentTo(package.Md5);
        }

            [Test]
        public async Task SucceedIfEverythingIsAlreadyDownloaded()
        {
            Stopwatch watch;
            ILogging logger;
            StartTest(out watch, out logger);

            var fileSystem = NPath.FileSystem;
            var package = Package.Load(Environment, new UriString($"http://localhost:{server.Port}/unity/git/windows/git-lfs.json"));

            var downloader = new Downloader(fileSystem);
            StartTrackTime(watch, logger, package.Url);
            downloader.QueueDownload(package.Uri, TestBasePath);

            var task = await TaskEx.WhenAny(downloader.Start().Task, TaskEx.Delay(Timeout));
            StopTrackTimeAndLog(watch, logger);
            Assert.AreEqual(downloader.Task, task);
            var downloadData = await downloader.Task;
            var downloadPath = downloadData.FirstOrDefault().File;

            downloader = new Downloader(fileSystem);
            StartTrackTime(watch, logger, "downloading again");
            downloader.QueueDownload(package.Uri, TestBasePath);
            task = await TaskEx.WhenAny(downloader.Start().Task, TaskEx.Delay(Timeout));
            StopTrackTimeAndLog(watch, logger);
            Assert.AreEqual(downloader.Task, task);
            downloadData = await downloader.Task;
            downloadPath = downloadData.FirstOrDefault().File;

            var md5Sum = downloadPath.CalculateMD5();
            md5Sum.Should().BeEquivalentTo(package.Md5);
        }

        [Category("DoNotRunOnAppVeyor")]
        public async Task DownloadsRunSideBySide()
        {
            Stopwatch watch;
            ILogging logger;
            StartTest(out watch, out logger);

            var package1 = Package.Load(Environment, new UriString($"http://localhost:{server.Port}/unity/git/windows/git-lfs.json"));
            var package2 = Package.Load(Environment, new UriString($"http://localhost:{server.Port}/unity/git/windows/git.json"));

            var events = new List<string>();

            var downloader = new Downloader(Environment.FileSystem);
            downloader.QueueDownload(package2.Uri, TestBasePath);
            downloader.QueueDownload(package1.Uri, TestBasePath);
            downloader.OnDownloadStart += url => events.Add("start " + url.Filename);
            downloader.OnDownloadComplete += (url, file) => events.Add("end " + url.Filename);
            downloader.OnDownloadFailed += (url, ex) => events.Add("failed " + url.Filename);

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
    class DownloadTaskTestsWithHttpServer : BaseTestWithHttpServer
    {
        [Test]
        public async Task ResumingDownloadsWorks()
        {
            Stopwatch watch;
            ILogging logger;
            StartTest(out watch, out logger);

            InitializeEnvironment(TestBasePath, false, false);

            var fileSystem = NPath.FileSystem;

            var baseUrl = new UriString($"http://localhost:{server.Port}/unity/git/windows");
            var package = Package.Load(Environment, baseUrl.ToString() + "/git-lfs.json");

            var downloadTask = new DownloadTask(TaskManager.Token, fileSystem, package.Uri, TestBasePath);

            StartTrackTime(watch, logger, package.Url);
            var task = await TaskEx.WhenAny(downloadTask.Start().Task, TaskEx.Delay(Timeout));
            StopTrackTimeAndLog(watch, logger);
            task.ShouldBeEquivalentTo(downloadTask.Task);

            var downloadPath = await downloadTask.Task;
            Assert.NotNull(downloadPath);

            var downloadPathBytes = downloadPath.ReadAllBytes();
            Logger.Trace("File size {0} bytes", downloadPathBytes.Length);

            var cutDownloadPathBytes = downloadPathBytes.Take(downloadPathBytes.Length - 1000).ToArray();
            downloadPath.Delete();
            new NPath(downloadPath + ".partial").WriteAllBytes(cutDownloadPathBytes);

            downloadTask = new DownloadTask(TaskManager.Token, fileSystem, package.Uri, TestBasePath);

            StartTrackTime(watch, logger, package.Url);
            task = await TaskEx.WhenAny(downloadTask.Start().Task, TaskEx.Delay(Timeout));
            StopTrackTimeAndLog(watch, logger);
            task.ShouldBeEquivalentTo(downloadTask.Task);
            downloadPath = await downloadTask.Task;

            var md5Sum = downloadPath.CalculateMD5();
            md5Sum.Should().BeEquivalentTo(package.Md5);
        }

        [Test]
        public void DownloadingNonExistingFileThrows()
        {
            Stopwatch watch;
            ILogging logger;
            StartTest(out watch, out logger);

            var fileSystem = NPath.FileSystem;

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

        //    var fileSystem = NPath.FileSystem;

        //    var gitLfsMd5 = new UriString($"http://localhost:{server.Port}/unity/git/windows/git-lfs.zip.md5");

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

            var fileSystem = NPath.FileSystem;

            var downloadTask = new DownloadTask(TaskManager.Token, fileSystem, "http://ggggithub.com/robots.txt", TestBasePath);
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

        //    var fileSystem = NPath.FileSystem;

        //    var gitLfs = new UriString($"http://localhost:{server.Port}/unity/git/windows/git-lfs.zip");
        //    var gitLfsMd5 = new UriString($"http://localhost:{server.Port}/unity/git/windows/git-lfs.zip.md5");

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

            var fileSystem = NPath.FileSystem;

            var evtStop = new AutoResetEvent(false);
            var evtFinally = new AutoResetEvent(false);
            Exception exception = null;

            var gitLfs = new UriString($"http://localhost:{server.Port}/unity/git/windows/git-lfs.zip");

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
