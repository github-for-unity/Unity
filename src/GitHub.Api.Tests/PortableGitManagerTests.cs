using System.Collections.Generic;
using System.IO;
using System.Linq;
using FluentAssertions;
using GitHub.Api;
using NSubstitute;
using NUnit.Framework;

namespace GitHub.Unity.Tests
{
    [TestFixture]
    public class PortableGitManagerTests
    {
        private const string ExtensionFolder = @"c:\ExtensionFolder";
        private const string UserProfilePath = @"c:\UserProfile";
        private const string WindowsPortableGitZip = ExtensionFolder + @"\resources\windows\PortableGit.zip";
        private const string WindowsGitLfsZip =
            ExtensionFolder + @"\resources\windows\git-lfs-windows-386-2.0-pre-d9833cd.zip";

        private static IEnvironment CreateEnvironment(ILogging logger, string extensionfolder = ExtensionFolder,
            string userProfilePath = UserProfilePath)
        {
            var environment = Substitute.For<IEnvironment>();
            environment.ExtensionInstallPath.Returns(extensionfolder);
            environment.UserProfilePath.Returns(userProfilePath);
            return environment;
        }

        private static IFileSystem CreateFileSystem(ILogging logger, string[] filesThatExist = null,
            IDictionary<string, string[]> fileContents = null, IList<string> randomFileNames = null,
            string temporaryPath = @"c:\temp", string[] directoriesThatExist = null)
        {
            var fileSystem = Substitute.For<IFileSystem>();
            var realFileSystem = new FileSystem();

            fileSystem.Combine(Arg.Any<string>(), Arg.Any<string>()).Returns(info => {
                var path1 = (string)info[0];
                var path2 = (string)info[1];
                var combine = realFileSystem.Combine(path1, path2);
                logger.Trace(@"FileSystem.Combine(""{0}"", ""{1}"") -> ""{2}""", path1, path2, combine);
                return combine;
            });

            fileSystem.Combine(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>()).Returns(info => {
                var path1 = (string)info[0];
                var path2 = (string)info[1];
                var path3 = (string)info[2];
                var combine = realFileSystem.Combine(path1, path2, path3);
                logger.Trace(@"FileSystem.Combine(""{0}"", ""{1}"", ""{2}"") -> ""{3}""", path1, path2, path3, combine);
                return combine;
            });

            if (filesThatExist != null)
            {
                fileSystem.FileExists(Arg.Any<string>()).Returns(info => {
                    var path1 = (string)info[0];
                    var result = filesThatExist.Contains(path1);
                    logger.Trace(@"FileSystem.FileExists(""{0}"") -> {1}", path1, result);
                    return result;
                });
            }

            if (directoriesThatExist != null)
            {
                fileSystem.DirectoryExists(Arg.Any<string>()).Returns(info => {
                    var path1 = (string)info[0];
                    var result = directoriesThatExist.Contains(path1);
                    logger.Trace(@"FileSystem.DirectoryExists(""{0}"") -> {1}", path1, result);
                    return result;
                });
            }

            if (fileContents != null)
            {
                fileSystem.ReadAllText(Arg.Any<string>()).Returns(info => {
                    var path1 = (string)info[0];

                    string result = null;

                    string[] fileContent;
                    if (fileContents.TryGetValue(path1, out fileContent))
                    {
                        result = string.Join(string.Empty, fileContent);
                    }

                    logger.Trace(@"FileSystem.ReadAllText(""{0}"") -> {1}", path1, result != null);

                    if (result == null)
                    {
                        throw new FileNotFoundException(path1);
                    }

                    return result;
                });
            }

            if (randomFileNames != null)
            {
                var randomFileIndex = 0;
                fileSystem.GetRandomFileName().Returns(info => {
                    var result = randomFileNames[randomFileIndex];

                    randomFileIndex++;
                    randomFileIndex = randomFileIndex % randomFileNames.Count;

                    logger.Trace(@"FileSystem.GetRandomFileName() -> {0}", result);

                    return result;
                });
            }

            fileSystem.GetTempPath().Returns(info => {
                logger.Trace(@"FileSystem.GetTempPath() -> {0}", temporaryPath);

                return temporaryPath;
            });

            return fileSystem;
        }

        private static IZipHelper CreateSharpZipLibHelper()
        {
            return Substitute.For<IZipHelper>();
        }

        private ILogging Logger { get; set; }

        [TestFixtureSetUp]
        public void TestFixtureSetup()
        {
            NPathFileSystemProvider.Current = CreateFileSystem(Logger);

            Logging.LoggerFactory = s => new ConsoleLogAdapter(s);
            Logger = Logging.GetLogger<PortableGitManagerTests>();
        }

        [Test]
        public void ShouldExtractGitIfNeeded()
        {
            var sharpZipLibHelper = CreateSharpZipLibHelper();

            var fileSystem = CreateFileSystem(Logger, new[] { WindowsPortableGitZip },
                randomFileNames: new[] { "randomFile1", "randomFile2" }, directoriesThatExist: new string[] {
                    @"c:\UserProfile\GitHubUnity\PortableGit_f02737a78695063deace08e96d5042710d3e32db\"
                });

            NPathFileSystemProvider.Current = fileSystem;

            var portableGitManager = new PortableGitManager(CreateEnvironment(Logger), fileSystem, sharpZipLibHelper);
            portableGitManager.ExtractGitIfNeeded();

            const string shouldExtractTo = @"c:\temp\randomFile1.deleteme";
            sharpZipLibHelper.Received().ExtractZipFile(WindowsPortableGitZip, shouldExtractTo);
        }

        [Test]
        public void ShouldExtractGitLfsIfNeeded()
        {
            var sharpZipLibHelper = CreateSharpZipLibHelper();

            var filesThatExist = new[] { WindowsGitLfsZip };

            var fileContents = new Dictionary<string, string[]>();

            var fileSystem = CreateFileSystem(Logger, filesThatExist, fileContents, new[] { "randomFile1", "randomFile2" });

            var portableGitManager = new PortableGitManager(CreateEnvironment(Logger), fileSystem, sharpZipLibHelper);
            portableGitManager.ExtractGitLfsIfNeeded();

            const string shouldExtractTo = @"c:\temp\randomFile1.deleteme";
            sharpZipLibHelper.Received().ExtractZipFile(WindowsGitLfsZip, shouldExtractTo);
        }

        [Test]
        public void ShouldKnoowIfGitLfsIsExtracted()
        {
            var sharpZipLibHelper = CreateSharpZipLibHelper();

            var filesThatExist = new[] {
                UserProfilePath +
                @"\GitHubUnity\PortableGit_f02737a78695063deace08e96d5042710d3e32db\mingw32\libexec\git-core\git-lfs.exe"
            };

            var fileContents = new Dictionary<string, string[]>();

            var fileSystem = CreateFileSystem(Logger, filesThatExist, fileContents);

            var portableGitManager = new PortableGitManager(CreateEnvironment(Logger), fileSystem, sharpZipLibHelper);
            portableGitManager.IsGitLfsExtracted().Should().BeTrue();
        }

        [Test]
        public void ShouldKnowGitLfsDestinationDirectory()
        {
            var portableGitManager = new PortableGitManager(CreateEnvironment(Logger), CreateFileSystem(Logger),
                CreateSharpZipLibHelper());

            portableGitManager.GitLfsDestinationDirectory.Should()
                              .Be(
                                  @"c:\UserProfile\GitHubUnity\PortableGit_f02737a78695063deace08e96d5042710d3e32db\mingw32\libexec\git-core\git-lfs.exe");
        }

        [Test]
        public void ShouldNotExtractGitIfNotNeeded()
        {
            var filesThatExist = new[] {
                WindowsPortableGitZip,
                UserProfilePath + @"\GitHubUnity\PortableGit_f02737a78695063deace08e96d5042710d3e32db\cmd\git.exe"
            };

            var fileSystem = CreateFileSystem(Logger, filesThatExist);

            var portableGitManager = new PortableGitManager(CreateEnvironment(Logger), fileSystem, CreateSharpZipLibHelper());
            portableGitManager.ExtractGitIfNeeded();

            CreateSharpZipLibHelper().DidNotReceiveWithAnyArgs().ExtractZipFile(Arg.Any<string>(), Arg.Any<string>());
        }

        [Test]
        public void ShouldNotExtractGitLfsIfNotNeeded()
        {
            var sharpZipLibHelper = CreateSharpZipLibHelper();

            var filesThatExist = new[] {
                UserProfilePath +
                @"\GitHubUnity\PortableGit_f02737a78695063deace08e96d5042710d3e32db\mingw32\libexec\git-core\git-lfs.exe"
            };

            var fileContents = new Dictionary<string, string[]>();

            var fileSystem = CreateFileSystem(Logger, filesThatExist, fileContents);

            NPathFileSystemProvider.Current = fileSystem;

            var portableGitManager = new PortableGitManager(CreateEnvironment(Logger), fileSystem, sharpZipLibHelper);
            portableGitManager.ExtractGitLfsIfNeeded();

            CreateSharpZipLibHelper().DidNotReceiveWithAnyArgs().ExtractZipFile(Arg.Any<string>(), Arg.Any<string>());
        }
    }
}
