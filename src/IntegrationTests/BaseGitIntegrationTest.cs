using System;
using System.IO;
using GitHub.Unity;
using Ionic.Zip;
using NUnit.Framework;

namespace IntegrationTests
{
    class BaseGitIntegrationTest
    {
        private static string SolutionDirectory => TestContext.CurrentContext.TestDirectory;

        private static string TestZipFilePath => Path.Combine(SolutionDirectory, "IOTestsRepo.zip");

        private static string GetBase64Guid() =>
            Convert.ToBase64String(Guid.NewGuid().ToByteArray())
                   .Replace('/', '_')
                   .Replace('+', '_')
                   .Replace('=', '_');

        protected string TestGitRepoPath { get; private set; }
        protected ILogging Logger { get; private set; }

        [SetUp]
        public void SetUp()
        {
            Logging.LoggerFactory = s => new ConsoleLogAdapter(s);
            var base64Guid = GetBase64Guid();
            TestGitRepoPath = Path.Combine(Path.GetTempPath(), base64Guid) + Path.DirectorySeparatorChar;

            Directory.CreateDirectory(TestGitRepoPath);

            using (var zipFile = new ZipFile(TestZipFilePath))
            {
                zipFile.ExtractAll(TestGitRepoPath, ExtractExistingFileAction.OverwriteSilently);
            }
            Logger = Logging.GetLogger(GetType());
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