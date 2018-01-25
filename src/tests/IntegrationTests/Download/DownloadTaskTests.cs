using System;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using GitHub.Unity;
using NUnit.Framework;

namespace IntegrationTests.Download
{
    [TestFixture]
    class DownloadTaskTests: BaseTaskManagerTest
    {
        private const string TestDownload = "http://ipv4.download.thinkbroadband.com/5MB.zip";
        private const string TestDownloadMD5 = "b3215c06647bc550406a9c8ccc378756";

        [Test]
        public async Task TestDownloadTask()
        {
            InitializeTaskManager();

            var fileSystem = new FileSystem();

            var downloadPath = TestBasePath.Combine("5MB.zip");
            var downloadHalfPath = TestBasePath.Combine("5MB-split.zip");

            var downloadTask = new DownloadTask(CancellationToken.None, fileSystem, TestDownload, downloadPath);
            await downloadTask.StartAwait();

            var downloadPathBytes = fileSystem.ReadAllBytes(downloadPath);
            Logger.Trace("File size {0} bytes", downloadPathBytes.Length);

            var md5Sum = fileSystem.CalculateFileMD5(downloadPath);
            md5Sum.Should().Be(TestDownloadMD5);

            var random = new Random();
            var takeCount = random.Next(downloadPathBytes.Length);

            Logger.Trace("Cutting the first {0} Bytes", downloadPathBytes.Length - takeCount);

            var cutDownloadPathBytes = downloadPathBytes.Take(takeCount).ToArray();
            fileSystem.WriteAllBytes(downloadHalfPath, cutDownloadPathBytes);

            downloadTask = new DownloadTask(CancellationToken.None, fileSystem, TestDownload, downloadHalfPath, TestDownloadMD5, 1);
            await downloadTask.StartAwait();

            var downloadHalfPathBytes = fileSystem.ReadAllBytes(downloadHalfPath);
            Logger.Trace("File size {0} Bytes", downloadHalfPathBytes.Length);

            md5Sum = fileSystem.CalculateFileMD5(downloadPath);
            md5Sum.Should().Be(TestDownloadMD5);
        }

        [Test]
        public void TestDownloadFailure()
        {
            InitializeTaskManager();

            var fileSystem = new FileSystem();

            var downloadPath = TestBasePath.Combine("5MB.zip");

            var taskFailed = false;
            Exception exceptionThrown = null;

            var autoResetEvent = new AutoResetEvent(false);

            var downloadTask = new DownloadTask(CancellationToken.None, fileSystem, "http://www.unknown.com/5MB.gz", downloadPath, null, 1)
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
            InitializeTaskManager();

            var downloadTask = new DownloadTextTask(CancellationToken.None, "https://github.com/robots.txt");
            var result = downloadTask.Start().Result;
            var resultLines = result.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            resultLines[0].Should().Be("# If you would like to crawl GitHub contact us at support@github.com.");
        }

        [Test]
        public void TestDownloadTextFailure()
        {
            InitializeTaskManager();

            var downloadTask = new DownloadTextTask(CancellationToken.None, "https://ggggithub.com/robots.txt");
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
            InitializeTaskManager();

            var gitArchivePath = TestBasePath.Combine("git.zip");
            var gitLfsArchivePath = TestBasePath.Combine("git-lfs.zip");

            var fileSystem = new FileSystem();

            var downloadGitMd5Task = new DownloadTextTask(CancellationToken.None,
                "https://ghfvs-installer.github.com/unity/portable_git/git.zip.MD5.txt?cb=1");

            var downloadGitTask = new DownloadTask(CancellationToken.None, fileSystem,
                "https://ghfvs-installer.github.com/unity/portable_git/git.zip", gitArchivePath, retryCount: 1);

            var downloadGitLfsMd5Task = new DownloadTextTask(CancellationToken.None,
                "https://ghfvs-installer.github.com/unity/portable_git/git-lfs.zip.MD5.txt?cb=1");

            var downloadGitLfsTask = new DownloadTask(CancellationToken.None, fileSystem,
                "https://ghfvs-installer.github.com/unity/portable_git/git-lfs.zip", gitLfsArchivePath, retryCount: 1);

            var result = true;
            Exception exception = null;

            var autoResetEvent = new AutoResetEvent(false);

            downloadGitMd5Task
                .Then((b, s) =>
                {
                    downloadGitTask.ValidationHash = s;
                })
                .Then(downloadGitTask)
                .Then(downloadGitLfsMd5Task)
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
    }
}
