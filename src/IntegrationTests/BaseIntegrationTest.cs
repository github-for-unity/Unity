using System;
using System.IO;
using Ionic.Zip;
using NUnit.Framework;
using GitHub.Unity;

namespace IntegrationTests
{
    class BaseIntegrationTest
    {
        protected NPath TestBasePath { get; private set; }
        protected ILogging Logger { get; private set; }
        protected IEnvironment Environment { get; set; }
        protected IFileSystem FileSystem { get; set; }

        [SetUp]
        public void SetUp()
        {
            Logger = Logging.GetLogger(GetType());
            Environment = new DefaultEnvironment();
            FileSystem = new FileSystem(TestBasePath);
            NPathFileSystemProvider.Current = FileSystem;
            TestBasePath = NPath.CreateTempDirectory("integration-tests");
            FileSystem.SetCurrentDirectory(TestBasePath);
        }

        [TearDown]
        public void TearDown()
        {
            //TestBasePath.Delete();
        }

        protected void CreateDirStructure(NPath[] dirs)
        {
            foreach (var file in dirs)
            {
                TestBasePath.Combine(file).CreateFile();
            }
        }
    }

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