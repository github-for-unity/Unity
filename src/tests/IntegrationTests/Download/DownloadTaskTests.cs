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
            md5Sum.Should().Be(TestDownloadMD5.ToUpperInvariant());

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
            md5Sum.Should().Be(TestDownloadMD5.ToUpperInvariant());
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
    }
}
