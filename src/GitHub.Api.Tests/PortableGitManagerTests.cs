using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        private const string TemporaryPath = @"c:\temp";
        private const string WindowsPortableGitZip = ExtensionFolder + @"\resources\windows\PortableGit.zip";

        private IEnvironment CreateEnvironment(string extensionfolder, string userProfilePath)
        {
            var environment = Substitute.For<IEnvironment>();
            environment.ExtensionInstallPath.Returns(extensionfolder);
            environment.UserProfilePath.Returns(userProfilePath);
            return environment;
        }

        private IFileSystem CreateFileSystem(string[] filesThatExist, IDictionary<string, string[]> fileContents,
            IList<string> randomFileNames, string temporaryPath)
        {
            var fileSystem = Substitute.For<IFileSystem>();
            var realFileSystem = new FileSystem();

            fileSystem.Combine(Arg.Any<string>(), Arg.Any<string>()).Returns(info => {
                var path1 = (string)info[0];
                var path2 = (string)info[1];
                var combine = realFileSystem.Combine(path1, path2);
                Logger.Trace(@"FileSystem.Combine(""{0}"", ""{1}"") -> ""{2}""", path1, path2, combine);
                return combine;
            });

            fileSystem.Combine(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>()).Returns(info => {
                var path1 = (string)info[0];
                var path2 = (string)info[1];
                var path3 = (string)info[2];
                var combine = realFileSystem.Combine(path1, path2, path3);
                Logger.Trace(@"FileSystem.Combine(""{0}"", ""{1}"", ""{2}"") -> ""{3}""", path1, path2, path3, combine);
                return combine;
            });

            fileSystem.FileExists(Arg.Any<string>()).Returns(info => {
                var path1 = (string)info[0];
                var result = filesThatExist.Contains(path1);
                Logger.Trace(@"FileSystem.FileExists(""{0}"") -> {1}", path1, result);
                return result;
            });

            fileSystem.ReadAllText(Arg.Any<string>()).Returns(info => {
                var path1 = (string)info[0];

                string result = null;

                string[] fileContent;
                if (fileContents.TryGetValue(path1, out fileContent))
                {
                    result = string.Join(string.Empty, fileContent);
                }

                Logger.Trace(@"FileSystem.ReadAllText(""{0}"") -> {1}", path1, result != null);

                if (result == null)
                {
                    throw new FileNotFoundException(path1);
                }

                return result;
            });

            var randomFileIndex = 0;
            fileSystem.GetRandomFileName().Returns(info => {
                var result = randomFileNames[randomFileIndex];

                randomFileIndex++;
                randomFileIndex = randomFileIndex % randomFileNames.Count;

                Logger.Trace(@"FileSystem.GetRandomFileName() -> {0}", result);

                return result;
            });

            fileSystem.GetTempPath().Returns(info => {
                Logger.Trace(@"FileSystem.GetTempPath() -> {0}", temporaryPath);

                return temporaryPath;
            });

            return fileSystem;
        }

        private ISharpZipLibHelper CreateSharpZipLibHelper()
        {
            return Substitute.For<ISharpZipLibHelper>();
        }

        [TestFixtureSetUp]
        public void TestFixtureSetup()
        {
            Logging.LoggerFactory = s => new ConsoleLogAdapter(s);
            Logger = Logging.GetLogger<PortableGitManagerTests>();
        }

        public ILogging Logger { get; set; }

        [Test]
        public void ShouldExtractGitIfNeeded()
        {
            var environment = CreateEnvironment(ExtensionFolder, UserProfilePath);
            var sharpZipLibHelper = CreateSharpZipLibHelper();

            var filesThatExist = new[] {
                WindowsPortableGitZip,
            };

            var fileContents = new Dictionary<string, string[]>();

            var randomFileNames = new[] { "randomFolder1", "randomFolder2" };

            var fileSystem = CreateFileSystem(filesThatExist, fileContents, randomFileNames, TemporaryPath);

            var portableGitManager = new PortableGitManager(environment, fileSystem, sharpZipLibHelper);
            portableGitManager.ExtractGitIfNeeded();

            const string shouldExtractTo = @"c:\temp\randomFolder1.deleteme";
            sharpZipLibHelper.Received().ExtractZipFile(WindowsPortableGitZip, shouldExtractTo);
        }

        [Test]
        public void ShouldNotExtractGitIfNotNeeded()
        {
            var environment = CreateEnvironment(ExtensionFolder, UserProfilePath);
            var sharpZipLibHelper = CreateSharpZipLibHelper();

            var filesThatExist = new[] {
                WindowsPortableGitZip,
                UserProfilePath + @"\GitHubUnity\PortableGit_f02737a78695063deace08e96d5042710d3e32db\cmd\git.exe",
                UserProfilePath + @"\GitHubUnity\PortableGit_f02737a78695063deace08e96d5042710d3e32db\VERSION"
            };

            var fileContents = new Dictionary<string, string[]> {
                {
                    UserProfilePath + @"\GitHubUnity\PortableGit_f02737a78695063deace08e96d5042710d3e32db\VERSION",
                    new[] { "f02737a78695063deace08e96d5042710d3e32db" }
                }
            };

            var randomFileNames = new[] { "randomFolder1", "randomFolder2" };

            var fileSystem = CreateFileSystem(filesThatExist, fileContents, randomFileNames, TemporaryPath);

            var portableGitManager = new PortableGitManager(environment, fileSystem, sharpZipLibHelper);
            portableGitManager.ExtractGitIfNeeded();

            sharpZipLibHelper.DidNotReceiveWithAnyArgs().ExtractZipFile(Arg.Any<string>(), Arg.Any<string>());
        }
    }
}
