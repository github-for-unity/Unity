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

            var downloadTask = new DownloadTask(CancellationToken.None, TestDownload, downloadPath);
            await downloadTask.StartAwait();

            var downloadPathBytes = fileSystem.ReadAllBytes(downloadPath);
            Logger.Trace("File size {0} bytes", downloadPathBytes.Length);

            var computedHash = fileSystem.CalculateMD5(downloadPath);
            computedHash.Should().Be(TestDownloadMD5.ToUpperInvariant());

            var takeCount = downloadPathBytes.Length / 2;

            Logger.Trace("Cutting {0} Bytes", downloadPathBytes.Length - takeCount);

            var cutDownloadPathBytes = downloadPathBytes.Take(takeCount).ToArray();
            fileSystem.WriteAllBytes(downloadHalfPath, cutDownloadPathBytes);

            downloadTask = new DownloadTask(CancellationToken.None, TestDownload, downloadHalfPath);
            await downloadTask.StartAwait();

            var downloadHalfPathBytes = fileSystem.ReadAllBytes(downloadHalfPath);
            Logger.Trace("File size {0} Bytes", downloadHalfPathBytes.Length);

            computedHash = fileSystem.CalculateMD5(downloadHalfPath);
            computedHash.Should().Be(TestDownloadMD5.ToUpperInvariant());
        }
    }
}
