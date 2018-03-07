using GitHub.Unity;

namespace IntegrationTests
{
    class BaseGitEnvironmentTest : BaseIntegrationTest
    {
        public override void OnSetup()
        {
            base.OnSetup();
            Logger.Trace($"Extracting {TestZipFilePath} to {TestBasePath}");
            ZipHelper.ExtractZipFile(TestZipFilePath, TestBasePath.ToString(), TaskManager.Token, (value, total) => true);
        }

        public override void OnTearDown()
        {
            RepositoryManager?.Stop();
            RepositoryManager?.Dispose();
            RepositoryManager = null;
            base.OnTearDown();
        }
    }
}
