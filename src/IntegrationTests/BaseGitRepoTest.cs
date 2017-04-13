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

            UsageFile = TestBasePath.Combine("usage.json");

            TestRepoMasterCleanUnsynchronized = TestBasePath.Combine("IOTestsRepo", "IOTestsRepo_master_clean_unsync");
            TestRepoMasterCleanSynchronized = TestBasePath.Combine("IOTestsRepo", "IOTestsRepo_master_clean_sync");
            TestRepoMasterDirtyUnsynchronized = TestBasePath.Combine("IOTestsRepo", "IOTestsRepo_master_dirty_unsync");

            using (var zipFile = new ZipFile(TestZipFilePath))
            {
                zipFile.ExtractAll(TestBasePath.ToString(), ExtractExistingFileAction.OverwriteSilently);
            }
        }

        protected NPath UsageFile { get; set; }

        protected NPath TestRepoMasterCleanSynchronized { get; private set; }

        protected NPath TestRepoMasterCleanUnsynchronized { get; private set; }

        protected NPath TestRepoMasterDirtyUnsynchronized { get; private set; }

        private static string TestZipFilePath => Path.Combine(SolutionDirectory, "IOTestsRepo.zip");
    }
}
