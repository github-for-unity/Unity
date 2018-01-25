using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using GitHub.Unity;
using Microsoft.Win32.SafeHandles;
using NSubstitute;
using NUnit.Framework;
using Rackspace.Threading;

namespace IntegrationTests
{
    [TestFixture]
    class UnzipTaskTests : BaseTaskManagerTest
    {
        [Test]
        public void TaskSucceeds()
        {
            InitializeTaskManager();

            var cacheContainer = Substitute.For<ICacheContainer>();
            Environment = new IntegrationTestEnvironment(cacheContainer, TestBasePath, SolutionDirectory);

            var destinationPath = TestBasePath.Combine("git_zip").CreateDirectory();
            var archiveFilePath = AssemblyResources.ToFile(ResourceType.Platform, "git.zip", destinationPath, Environment);

            var extractedPath = TestBasePath.Combine("git_zip_extracted").CreateDirectory();

            var unzipTask = new UnzipTask(CancellationToken.None, archiveFilePath, extractedPath, Environment.FileSystem, GitInstallDetails.GitExtractedMD5, 
                new Progress<float>(zipFileProgress => {
                        var zipProgress = zipFileProgress * 100;
                        Logger.Trace("Pct Complete {0:0.00}%", zipProgress);
                }),
                new Progress<long>(ticksRemaining => {
                    var timeSpan = TimeSpan.FromTicks(ticksRemaining);
                    Logger.Trace("Estimated Time Remaining: {0}s", timeSpan.TotalSeconds);
                }));

            unzipTask.Start().Wait();

            extractedPath.DirectoryExists().Should().BeTrue();
        }

        [Test]
        public void TaskFailsWhenMD5Incorect()
        {
            InitializeTaskManager();

            var cacheContainer = Substitute.For<ICacheContainer>();
            Environment = new IntegrationTestEnvironment(cacheContainer, TestBasePath, SolutionDirectory);

            var destinationPath = TestBasePath.Combine("git_zip").CreateDirectory();
            var archiveFilePath = AssemblyResources.ToFile(ResourceType.Platform, "git.zip", destinationPath, Environment);

            var extractedPath = TestBasePath.Combine("git_zip_extracted").CreateDirectory();


            var failed = false;
            Exception exception = null;

            var unzipTask = new UnzipTask(CancellationToken.None, archiveFilePath, extractedPath, Environment.FileSystem, "AABBCCDD")
                .Finally((b, ex) => {
                    failed = true;
                    exception = ex;
                });

            unzipTask.Start().Wait();

            extractedPath.DirectoryExists().Should().BeFalse();
            failed.Should().BeTrue();
            exception.Should().NotBeNull();
            exception.Should().BeOfType<UnzipTaskException>();
        }
    }
}