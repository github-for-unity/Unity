using System.Threading;
using System.Threading.Tasks;
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
        public void UnzipTest()
        {
            InitializeTaskManager();

            var cacheContainer = Substitute.For<ICacheContainer>();
            Environment = new IntegrationTestEnvironment(cacheContainer, TestBasePath, SolutionDirectory);

            var destinationPath = TestBasePath.Combine("git_zip").CreateDirectory();
            var archiveFilePath = AssemblyResources.ToFile(ResourceType.Platform, "git.zip", destinationPath, Environment);

            Logger.Trace("ArchiveFilePath: {0}", archiveFilePath);
            Logger.Trace("TestBasePath: {0}", TestBasePath);

            var extractedPath = TestBasePath.Combine("git_zip_extracted").CreateDirectory();

            var zipProgress = 0;
            Logger.Trace("Pct Complete {0}%", zipProgress);
            var unzipTask = new UnzipTask(CancellationToken.None, archiveFilePath, extractedPath, 
                new Progress<float>(zipFileProgress => {
                    var zipFileProgressInteger = (int) (zipFileProgress * 100);
                    if (zipProgress != zipFileProgressInteger)
                    {
                        zipProgress = zipFileProgressInteger;
                        Logger.Trace("Pct Complete {0}%", zipProgress);
                    }
                }));

            unzipTask.Start().Wait();
        }
    }
}