using System.IO;
using GitHub.Unity;
using Ionic.Zip;
using NUnit.Framework;

namespace IntegrationTests
{
    class BaseGitRepoTest : BaseIntegrationTest
    {
        private static string SolutionDirectory => TestContext.CurrentContext.TestDirectory;
        private static string TestZipFilePath => Path.Combine(SolutionDirectory, "IOTestsRepo.zip");
		
        protected NPath TestRepoPath { get; private set; }

        protected override void OnSetup()
        {
            base.OnSetup();

            using (var zipFile = new ZipFile(TestZipFilePath))
            {
                zipFile.ExtractAll(TestBasePath.ToString(), ExtractExistingFileAction.OverwriteSilently);
            }

            TestRepoPath = TestBasePath.Combine("IOTestsRepo");
        }
    }
}