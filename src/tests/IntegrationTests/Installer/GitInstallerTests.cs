using System;
using System.Threading;
using GitHub.Unity;
using NSubstitute;
using NUnit.Framework;

namespace IntegrationTests
{
    [TestFixture]
    class GitInstallerTests : BaseTaskManagerTest
    {
        [Test]
        public void GitInstallTest()
        {
            InitializeTaskManager();

            var cacheContainer = Substitute.For<ICacheContainer>();
            Environment = new IntegrationTestEnvironment(cacheContainer, TestBasePath, SolutionDirectory, enableTrace: true);

            var gitInstallationPath = TestBasePath.Combine("GitInstall").CreateDirectory();

            var installDetails = new GitInstallDetails(gitInstallationPath, DefaultEnvironment.OnWindows);

            var zipArchivesPath = TestBasePath.Combine("ZipArchives").CreateDirectory();
            var gitArchivePath = AssemblyResources.ToFile(ResourceType.Platform, "git.zip", zipArchivesPath, Environment);
            var gitLfsArchivePath = AssemblyResources.ToFile(ResourceType.Platform, "git-lfs.zip", zipArchivesPath, Environment);

            var gitInstaller = new GitInstaller(Environment, CancellationToken.None, installDetails, gitArchivePath, gitLfsArchivePath);

            var autoResetEvent = new AutoResetEvent(false);

            NPath result = null;
            Exception ex = null;

            gitInstaller.SetupGitIfNeeded(new ActionTask<NPath>(CancellationToken.None, (b, path) => {
                    result = path;
                    autoResetEvent.Set();
                }),
                new ActionTask(CancellationToken.None, (b, exception) => {
                    ex = exception;
                    autoResetEvent.Set();
                }));

            autoResetEvent.WaitOne();

            if (result == null)
            {
                if (ex != null)
                {
                    throw ex;
                }

                throw new Exception("Did not install git");
            }
        }
    }
}