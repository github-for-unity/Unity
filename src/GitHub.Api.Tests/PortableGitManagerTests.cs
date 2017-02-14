using GitHub.Api;
using NSubstitute;
using NUnit.Framework;

namespace GitHub.Unity.Tests
{
    [TestFixture]
    public class PortableGitManagerTests
    {
        private const string Extensionfolder = @"c:\ExtensionFolder";
        private const string WindowsPortableGitZip = Extensionfolder + @"\resources\windows\PortableGit.zip";

        private static IEnvironment CreateEnvironment()
        {
            var environment = Substitute.For<IEnvironment>();
            environment.ExtensionInstallPath.Returns(Extensionfolder);
            return environment;
        }

        private static IFileSystem CreateFileSystem()
        {
            var fileSystem = Substitute.For<IFileSystem>();
            var realFileSystem = new FileSystem();

            fileSystem.Combine(Arg.Any<string>(), Arg.Any<string>())
                      .Returns(info => realFileSystem.Combine((string)info[0], (string)info[1]));

            fileSystem.FileExists(WindowsPortableGitZip).Returns(info => true);

            var archiveFolderName = realFileSystem.GetRandomFileName();
            fileSystem.GetRandomFileName().Returns(info => archiveFolderName);
            return fileSystem;
        }

        private static ISharpZipLibHelper CreateSharpZipLibHelper()
        {
            var sharpZipLibHelper = Substitute.For<ISharpZipLibHelper>();
            return sharpZipLibHelper;
        }

        [OneTimeSetUp]
        public void TestFixtureSetup()
        {
            Logging.LoggerFactory = s => new ConsoleLogAdapter(s);
        }

        [Test]
        public void Test()
        {
            var environment = CreateEnvironment();
            var sharpZipLibHelper = CreateSharpZipLibHelper();
            var fileSystem = CreateFileSystem();

            var portableGitManager = new PortableGitManager(environment, fileSystem, sharpZipLibHelper);
            portableGitManager.ExtractGitIfNeeded();
        }
    }
}
