using System;
using System.IO;
using Ionic.Zip;
using NUnit.Framework;

namespace GitHub.Unity.Tests.IO
{
    class BaseIOTest
    {
        private static string SolutionDirectory => TestContext.CurrentContext.TestDirectory;

        private static string TestZipFilePath => Path.Combine(SolutionDirectory, "IOTestsRepo.zip");

        private static string GetBase64Guid() => Convert.ToBase64String(Guid.NewGuid().ToByteArray());

        protected string TestGitRepoPath { get; private set; }

        [SetUp]
        public void SetUp()
        {
            var base64Guid = GetBase64Guid();
            TestGitRepoPath = Path.Combine(Path.GetTempPath(), base64Guid) + Path.DirectorySeparatorChar;

            Directory.CreateDirectory(TestGitRepoPath);

            using (var zipFile = new ZipFile(TestZipFilePath))
            {
                zipFile.ExtractAll(TestGitRepoPath, ExtractExistingFileAction.OverwriteSilently);
            }
        }

        [TearDown]
        public void TearDown()
        {
            try
            {
                Directory.Delete(TestGitRepoPath, true);
            }
            catch (Exception)
            {
                // ignored
            }
        }
    }
}