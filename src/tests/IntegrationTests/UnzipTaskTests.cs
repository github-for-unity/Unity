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
            var cacheContainer = Substitute.For<ICacheContainer>();
            Environment = new IntegrationTestEnvironment(cacheContainer, TestBasePath, SolutionDirectory);
            InitializeTaskManager();

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
    }
}
