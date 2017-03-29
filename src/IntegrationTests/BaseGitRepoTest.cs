using System.IO;
using System.Threading;
using GitHub.Unity;
using Ionic.Zip;
using NUnit.Framework;

namespace IntegrationTests
{
    class BaseGitRepoTest : BaseIntegrationTest
    {
        protected override void OnSetup()
        {
            base.OnSetup();

            TestRepoPath = TestBasePath.Combine("IOTestsRepo");

            using (var zipFile = new ZipFile(TestZipFilePath))
            {
                zipFile.ExtractAll(TestBasePath.ToString(), ExtractExistingFileAction.OverwriteSilently);
            }
        }
        
        protected NPath TestRepoPath { get; private set; }

        private static string TestZipFilePath => Path.Combine(SolutionDirectory, "IOTestsRepo.zip");
    }
}
