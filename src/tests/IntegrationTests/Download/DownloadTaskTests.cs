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
            var downloadResult = await downloadTask.StartAwait();

            downloadResult.Should().BeTrue();

            var downloadPathBytes = fileSystem.ReadAllBytes(downloadPath);
            Logger.Trace("File size {0} bytes", downloadPathBytes.Length);

            var md5Sum = fileSystem.CalculateMD5(downloadPath);
            md5Sum.Should().Be(TestDownloadMD5.ToUpperInvariant());

            var random = new Random();
            var takeCount = random.Next(downloadPathBytes.Length);

            Logger.Trace("Cutting the first {0} Bytes", downloadPathBytes.Length - takeCount);

            var cutDownloadPathBytes = downloadPathBytes.Take(takeCount).ToArray();
            fileSystem.WriteAllBytes(downloadHalfPath, cutDownloadPathBytes);

            downloadTask = new DownloadTask(CancellationToken.None, fileSystem, TestDownload, downloadHalfPath, TestDownloadMD5, 1);
            downloadResult = await downloadTask.StartAwait();

            downloadResult.Should().BeTrue();

            var downloadHalfPathBytes = fileSystem.ReadAllBytes(downloadHalfPath);
            Logger.Trace("File size {0} Bytes", downloadHalfPathBytes.Length);

            md5Sum = fileSystem.CalculateMD5(downloadPath);
            md5Sum.Should().Be(TestDownloadMD5.ToUpperInvariant());
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
