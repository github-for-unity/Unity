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

            TestRepoMasterClean = TestBasePath.Combine("IOTestsRepo", "IOTestsRepo_master_clean");
            TestRepoMasterDirty = TestBasePath.Combine("IOTestsRepo", "IOTestsRepo_master_dirty");

            using (var zipFile = new ZipFile(TestZipFilePath))
            {
                zipFile.ExtractAll(TestBasePath.ToString(), ExtractExistingFileAction.OverwriteSilently);
            }
        }
        
        protected NPath TestRepoMasterClean { get; private set; }

        protected NPath TestRepoMasterDirty { get; private set; }

        private static string TestZipFilePath => Path.Combine(SolutionDirectory, "IOTestsRepo.zip");
    }
}
