using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using GitHub.Api;

namespace GitHub.Unity.Tests
{
    [TestFixture]
    class GitClientTests : BaseIntegrationTest
    {
        [Test]
        public void FindRepoRootTest()
        {
            var filesystem = new FileSystem();
            NPathFileSystemProvider.Current = filesystem;
            var environment = new DefaultEnvironment();
            environment.UnityProjectPath = TestGitRepoPath;
            var platform = new Platform(environment, filesystem);
            var gitEnvironment = platform.GitEnvironment;
            var processManager = new ProcessManager(environment, gitEnvironment, filesystem);

            using (var gitclient = new GitClient(environment.UnityProjectPath, filesystem, processManager))
            {
                Assert.AreEqual(new NPath(TestGitRepoPath).ToString(), gitclient.RepositoryPath);
            }
        }
    }
}
