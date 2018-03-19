using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using GitHub.Unity;
using Microsoft.Win32.SafeHandles;
using NSubstitute;
using NUnit.Framework;

namespace IntegrationTests
{
    [TestFixture]
    class UnzipTaskTests : BaseIntegrationTest
    {
        [Test]
        public async Task UnzipWorks()
        {
            InitializeTaskManager();

            var cacheContainer = Substitute.For<ICacheContainer>();
            Environment = new IntegrationTestEnvironment(cacheContainer, TestBasePath, SolutionDirectory);

            var destinationPath = TestBasePath.Combine("gitlfs_zip").CreateDirectory();
            var archiveFilePath = AssemblyResources.ToFile(ResourceType.Platform, "git-lfs.zip", destinationPath, Environment);

            var extractedPath = TestBasePath.Combine("gitlfs_zip_extracted").CreateDirectory();

            var unzipTask = new UnzipTask(CancellationToken.None, archiveFilePath, extractedPath,
                    ZipHelper.Instance,
                    Environment.FileSystem)
                .Progress(p => 
                {
                });

            await unzipTask.StartAwait();

            extractedPath.DirectoryExists().Should().BeTrue();
        }

        [Test]
        public void FailsWhenMD5Incorrect()
        {
            InitializeTaskManager();

            var cacheContainer = Substitute.For<ICacheContainer>();
            Environment = new IntegrationTestEnvironment(cacheContainer, TestBasePath, SolutionDirectory);

            var destinationPath = TestBasePath.Combine("gitlfs_zip").CreateDirectory();
            var archiveFilePath = AssemblyResources.ToFile(ResourceType.Platform, "git-lfs.zip", destinationPath, Environment);

            var extractedPath = TestBasePath.Combine("gitlfs_zip_extracted").CreateDirectory();

            var unzipTask = new UnzipTask(CancellationToken.None, archiveFilePath, extractedPath, 
                ZipHelper.Instance, Environment.FileSystem);

            Assert.Throws<UnzipException>(async () => await unzipTask.StartAwait());

            extractedPath.DirectoryExists().Should().BeFalse();
        }
    }
}