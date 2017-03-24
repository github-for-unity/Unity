using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using GitHub.Unity;
using Rackspace.Threading;
using System.Threading;
using FluentAssertions;
using NCrunch.Framework;
using TestUtils;

namespace IntegrationTests
{
    [TestFixture]
    class GitClientTests : BaseGitIntegrationTest
    {
        [Test]
        public void FindRepoRootTest()
        {
            var filesystem = new FileSystem(TestBasePath);
            NPathFileSystemProvider.Current = filesystem;

            var environment = new DefaultEnvironment();
            environment.UnityProjectPath = TestBasePath;

            var repositoryLocator = new RepositoryLocator(environment.UnityProjectPath);

            repositoryLocator.FindRepositoryRoot().ToString().Should().Be(new NPath(TestBasePath).ToString());
        }

        [Test]
        public void InstallGit()
        {
            var filesystem = new FileSystem();
            NPathFileSystemProvider.Current = filesystem;

            using (var environment = new IntegrationTestEnvironment())
            {
                var gitSetup = new GitSetup(environment, CancellationToken.None);
                var expectedPath = gitSetup.GitInstallationPath;

                var setupDone = false;
                var percent = -1f;

                // Root paths
                gitSetup.GitExecutablePath.FileExists().Should().BeFalse();

                const bool force = true;
                setupDone = gitSetup.SetupIfNeeded(force,
                    new Progress<float>(x => percent = x)
                ).Result;

                setupDone.Should().BeTrue();
                percent.Should().Be(1);

                //TODO: Fix this
                //Expected on windows if forced: c:\GitHubUnity\LocalAppData\GitHubUnityDebug\PortableGit_f02737a78695063deace08e96d5042710d3e32db\cmd\git.exe
                //Actual on windows if forced: C:\GitHubUnity\LocalAppData\GitHubUnityDebug\PortableGit_f02737a78695063deace08e96d5042710d3e32db\git\cmd\bin\git.exe

                Logger.Trace("Expected GitExecutablePath: {0}", gitSetup.GitExecutablePath);
                gitSetup.GitExecutablePath.FileExists().Should().BeTrue();

                environment.GitExecutablePath = gitSetup.GitExecutablePath;

                var platform = new Platform(environment, filesystem, new TestUIDispatcher());
                var gitEnvironment = platform.GitEnvironment;
                var processManager = new ProcessManager(environment, gitEnvironment);

                var gitBranches = processManager.GetGitBranches(TestBasePath, environment.GitExecutablePath);

                gitBranches.Should().BeEquivalentTo(
                    new GitBranch("master", string.Empty, true),
                    new GitBranch("feature/document", string.Empty, false));
            }
        }
    }
}
