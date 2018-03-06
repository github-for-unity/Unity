using GitHub.Unity;

namespace IntegrationTests
{
    class BaseGitEnvironmentTest : BaseIntegrationTest
    {
        public override void OnSetup()
        {
            base.OnSetup();
            Logger.Trace("Extracting Zip File to {0}", TestBasePath);
            ZipHelper.ExtractZipFile(TestZipFilePath, TestBasePath.ToString(), TaskManager.Token, (value, total) => true);
            Logger.Trace("Extracted Zip File");
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
