using System.Threading;
using GitHub.Unity;
using NCrunch.Framework;
using NUnit.Framework;

namespace IntegrationTests
{
    [TestFixture]
    class GitConfigGetTaskTests : BaseGitEnvironmentTest
    {
        [Test, Isolated]
        public void ShouldWork()
        {
            InitializeEnvironment(TestRepoMasterClean);

            var en = new WindowsGitEnvironment(Environment, FileSystem);
            var processManager = new ProcessManager(Environment, en, CancellationToken.None);

            string result = null;

            var gitConfigGetTask = new GitConfigGetTask(Environment, processManager, new TaskResultDispatcher<string>(
                s =>
                {
                    result = s;
                }), "credential.helper", GitConfigSource.NonSpecified);

            gitConfigGetTask.Run(CancellationToken.None);

            Logger.Trace("ShouldWork: {0}", result);
        }
    }
}