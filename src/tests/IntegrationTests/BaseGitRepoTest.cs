using System.IO;
using System.Threading;
using GitHub.Unity;

namespace IntegrationTests
{
    class BaseGitRepoTest : BaseIntegrationTest
    {
        public override void OnSetup()
        {
            base.OnSetup();

            TestRepoMasterCleanUnsynchronized = TestBasePath.Combine("IOTestsRepo", "IOTestsRepo_master_clean_unsync");
            TestRepoMasterCleanUnsynchronizedRussianLanguage = TestBasePath.Combine("IOTestsRepo", "IOTestsRepo_master_clean_sync_with_russian_language");
            TestRepoMasterCleanSynchronized = TestBasePath.Combine("IOTestsRepo", "IOTestsRepo_master_clean_sync");
            TestRepoMasterDirtyUnsynchronized = TestBasePath.Combine("IOTestsRepo", "IOTestsRepo_master_dirty_unsync");
            TestRepoMasterTwoRemotes = TestBasePath.Combine("IOTestsRepo", "IOTestsRepo_master_two_remotes");

            ZipHelper.ExtractZipFile(TestZipFilePath, TestBasePath.ToString(), CancellationToken.None);
        }

        protected NPath TestRepoMasterCleanSynchronized { get; private set; }
        protected NPath TestRepoMasterCleanUnsynchronized { get; private set; }
        protected NPath TestRepoMasterCleanUnsynchronizedRussianLanguage { get; private set; }
        protected NPath TestRepoMasterDirtyUnsynchronized { get; private set; }
        protected NPath TestRepoMasterTwoRemotes { get; private set; }
        private static string TestZipFilePath => Path.Combine(SolutionDirectory, "IOTestsRepo.zip");
    }
}
