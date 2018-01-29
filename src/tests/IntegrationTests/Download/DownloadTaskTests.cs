using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using GitHub.Unity;
using NUnit.Framework;
using System.Diagnostics;

namespace IntegrationTests.Download
{
    [TestFixture]
    class DownloadTaskTests : BaseTaskManagerTest
    {
        public override void OnSetup()
        {
            base.OnSetup();
            InitializeEnvironment(TestBasePath, initializeRepository: false);
        }

        [Test]
        public async Task TestDownloadTask()
        {
            Logger.Info("Starting Test: TestDownloadTask");

            var fileSystem = Environment.FileSystem;

            var gitLfs = new UriString("https://ghfvs-installer.github.com/unity/portable_git/git-lfs.zip");
            var gitLfsMd5 = new UriString("https://ghfvs-installer.github.com/unity/portable_git/git-lfs.zip.MD5.txt?cb=1");

            var md5 = await new DownloadTextTask(TaskManager.Token, fileSystem, gitLfsMd5, TestBasePath)
                .StartAwait();

            var downloadPath = await new DownloadTask(TaskManager.Token, fileSystem, gitLfs, TestBasePath)
                .StartAwait();

            var md5Sum = fileSystem.CalculateFileMD5(downloadPath);
            md5Sum.Should().BeEquivalentTo(md5);

            var downloadPathBytes = fileSystem.ReadAllBytes(downloadPath);
            Logger.Trace("File size {0} bytes", downloadPathBytes.Length);

            var random = new Random();
            var takeCount = random.Next(downloadPathBytes.Length);

            Logger.Trace("Cutting the first {0} Bytes", downloadPathBytes.Length - takeCount);

            var cutDownloadPathBytes = downloadPathBytes.Take(takeCount).ToArray();
            fileSystem.WriteAllBytes(downloadPath, cutDownloadPathBytes);

            downloadPath = await new DownloadTask(TaskManager.Token, fileSystem, gitLfs, TestBasePath)
                .StartAwait();

            var downloadHalfPathBytes = fileSystem.ReadAllBytes(downloadPath);
            Logger.Trace("File size {0} Bytes", downloadHalfPathBytes.Length);

            md5Sum = fileSystem.CalculateFileMD5(downloadPath);
            md5Sum.Should().BeEquivalentTo(md5);
        }

        [Test]
        public void TestDownloadFailure()
        {
            Logger.Info("Starting Test: TestDownloadFailure");

            var fileSystem = Environment.FileSystem;

            var downloadPath = TestBasePath.Combine("5MB.zip");

            var taskFailed = false;
            Exception exceptionThrown = null;

            var autoResetEvent = new AutoResetEvent(false);

            var downloadTask = new DownloadTask(TaskManager.Token, fileSystem,
                "http://www.unknown.com/5MB.gz", TestBasePath)
                .Finally((b, exception) => {
                    taskFailed = !b;
                    exceptionThrown = exception;
                    autoResetEvent.Set();
                });

            downloadTask.Start();

            autoResetEvent.WaitOne();

            taskFailed.Should().BeTrue();
            exceptionThrown.Should().NotBeNull();
        }

        [Test]
        public void TestDownloadTextTask()
        {
            Logger.Info("Starting Test: TestDownloadTextTask");

            var fileSystem = Environment.FileSystem;

            var downloadTask = new DownloadTextTask(TaskManager.Token, fileSystem,
                "https://github.com/robots.txt",
                TestBasePath);
            var result = downloadTask.Start().Result;
            var resultLines = result.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            resultLines[0].Should().Be("# If you would like to crawl GitHub contact us at support@github.com.");
        }

        [Test]
        public void TestDownloadTextFailure()
        {
            Logger.Info("Starting Test: TestDownloadTextFailure");

            var fileSystem = Environment.FileSystem;

            var downloadTask = new DownloadTextTask(TaskManager.Token, fileSystem,
                "https://ggggithub.com/robots.txt");
            var exceptionThrown = false;

            try
            {
                var result = downloadTask.Start().Result;
            }
            catch (Exception e)
            {
                exceptionThrown = true;
            }

            exceptionThrown.Should().BeTrue();
        }
        
        [Test]
        public void TestDownloadFileAndHash()
        {
            Logger.Info("Starting Test: TestDownloadFileAndHash");

            var fileSystem = Environment.FileSystem;

            var gitLfs = new UriString("https://ghfvs-installer.github.com/unity/portable_git/git-lfs.zip");
            var gitLfsMd5 = new UriString("https://ghfvs-installer.github.com/unity/portable_git/git-lfs.zip.MD5.txt?cb=1");

            var downloadGitLfsMd5Task = new DownloadTextTask(TaskManager.Token, fileSystem,
                gitLfsMd5, TestBasePath);

            var downloadGitLfsTask = new DownloadTask(TaskManager.Token, fileSystem,
                gitLfs, TestBasePath);

            var result = true;
            Exception exception = null;

            var autoResetEvent = new AutoResetEvent(false);

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

            autoResetEvent.WaitOne();

            result.Should().BeTrue();
            exception.Should().BeNull();
        }

        [Test]
        public void TestDownloadShutdownTimeWhenInterrupted()
        {
            Logger.Info("Starting Test: TestDownloadShutdownTimeWhenInterrupted");

            var fileSystem = Environment.FileSystem;

            var gitArchivePath = TestBasePath.Combine("git.zip");

            var evtStop = new AutoResetEvent(false);
            var evtFinally = new AutoResetEvent(false);
            Exception exception = null;

            var watch = new Stopwatch();

            var downloadGitTask = new DownloadTask(TaskManager.Token, fileSystem,
                "https://ghfvs-installer.github.com/unity/portable_git/git.zip",
                TestBasePath)

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

            evtStop.WaitOne();

            watch.Start();
            TaskManager.Dispose();
            evtFinally.WaitOne();
            watch.Stop();

            exception.Should().NotBeNull();
            watch.ElapsedMilliseconds.Should().BeLessThan(250);
        }
    }
}
