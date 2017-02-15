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
        private const string WindowsPortableGitZip = ExtensionFolder + @"\resources\windows\PortableGit.zip";

        private IEnvironment CreateEnvironment(string extensionfolder)
        {
            var environment = Substitute.For<IEnvironment>();
            environment.ExtensionInstallPath.Returns(extensionfolder);
            return environment;
        }

        private IFileSystem CreateFileSystem(string[] filesThatExist, IDictionary<string, string[]> fileContents,
            IList<string> randomFileNames)
        {
            var fileSystem = Substitute.For<IFileSystem>();
            var realFileSystem = new FileSystem();

            fileSystem.Combine(Arg.Any<string>(), Arg.Any<string>()).Returns(info => {
                var path1 = (string)info[0];
                var path2 = (string)info[1];
                var combine = realFileSystem.Combine(path1, path2);
                Logger.Debug(@"FileSystem.Combine(""{0}"", ""{1}"") -> ""{2}""", path1, path2, combine);
                return combine;
            });

            fileSystem.Combine(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>()).Returns(info => {
                var path1 = (string)info[0];
                var path2 = (string)info[1];
                var path3 = (string)info[2];
                var combine = realFileSystem.Combine(path1, path2, path3);
                Logger.Debug(@"FileSystem.Combine(""{0}"", ""{1}"", ""{2}"") -> ""{3}""", path1, path2, path3, combine);
                return combine;
            });

            fileSystem.FileExists(Arg.Any<string>()).Returns(info => {
                var path1 = (string)info[0];
                var result = filesThatExist.Contains(path1);
                Logger.Debug(@"FileSystem.FileExists(""{0}"") -> {1}", path1, result);
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

                Logger.Debug(@"FileSystem.ReadAllText(""{0}"") -> {1}", path1, result != null);

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

                Logger.Debug(@"FileSystem.GetRandomFileName() -> {0}", result);

                return result;
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
            var environment = CreateEnvironment(ExtensionFolder);
            var sharpZipLibHelper = CreateSharpZipLibHelper();

            var filesThatExist = new[] { ExtensionFolder + @"\resources\windows\PortableGit.zip" };

            var fileContents = new Dictionary<string, string[]>();

            var randomFileNames = new[] { "randomFolder1", "randomFolder2" };

            var fileSystem = CreateFileSystem(filesThatExist, fileContents, randomFileNames);

            var portableGitManager = new PortableGitManager(environment, fileSystem, sharpZipLibHelper);
            portableGitManager.ExtractGitIfNeeded();

            const string shouldExtractTo = @"c:\ExtensionFolder\randomFolder1.deleteme";
            sharpZipLibHelper.Received().ExtractZipFile(WindowsPortableGitZip, shouldExtractTo);
        }

        [Test]
        public void ShouldNotExtractGitIfNotNeeded()
        {
            var environment = CreateEnvironment(ExtensionFolder);
            var sharpZipLibHelper = CreateSharpZipLibHelper();

            var filesThatExist = new[] {
                WindowsPortableGitZip,
                ExtensionFolder + @"\PortableGit_f02737a78695063deace08e96d5042710d3e32db\cmd\git.exe",
                ExtensionFolder + @"\PortableGit_f02737a78695063deace08e96d5042710d3e32db\VERSION"
            };

            var fileContents = new Dictionary<string, string[]> {
                {
                    ExtensionFolder + @"\PortableGit_f02737a78695063deace08e96d5042710d3e32db\VERSION",
                    new[] { "f02737a78695063deace08e96d5042710d3e32db" }
                }
            };

            var randomFileNames = new string[] { };

            var fileSystem = CreateFileSystem(filesThatExist, fileContents, randomFileNames);

            var portableGitManager = new PortableGitManager(environment, fileSystem, sharpZipLibHelper);
            portableGitManager.ExtractGitIfNeeded();

            sharpZipLibHelper.DidNotReceiveWithAnyArgs().ExtractZipFile(Arg.Any<string>(), Arg.Any<string>());
        }
    }
}
