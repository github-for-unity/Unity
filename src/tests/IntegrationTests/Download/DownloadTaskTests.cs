using System;
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
        public async Task Blah()
        {
            InitializeTaskManager();

            var downloadPath = TestBasePath.Combine("5MB.zip");
            var downloadDetails = new DownloadDetails(TestDownload, downloadPath, false, TestDownloadMD5);

            var downloadTask = new DownloadTask(CancellationToken.None, downloadDetails);
            var downloadResult = await downloadTask.StartAwait();

            var resultBytes = downloadPath.ReadAllBytes();

            string computedHash;
            using (var md5 = MD5.Create())
            {
                var computedHashBytes = md5.ComputeHash(resultBytes);
                computedHash = BitConverter.ToString(computedHashBytes)
                    .ToLower()
                    .Replace("-", string.Empty);
            }

            computedHash.Should().Be(TestDownloadMD5);
        }
    }
}
