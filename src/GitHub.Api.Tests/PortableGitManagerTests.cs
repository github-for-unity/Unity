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
        private IEnvironment CreateEnvironment(string extensionfolder)
        {
            var environment = Substitute.For<IEnvironment>();
            environment.ExtensionInstallPath.Returns(extensionfolder);
            return environment;
        }

        private IFileSystem CreateFileSystem(string[] thatExist, Dictionary<string, string[]> dictionary)
        {
            var fileSystem = Substitute.For<IFileSystem>();
            var realFileSystem = new FileSystem();

            fileSystem.Combine(Arg.Any<string>(), Arg.Any<string>()).Returns(info =>
            {
                var path1 = (string)info[0];
                var path2 = (string)info[1];
                var combine = realFileSystem.Combine(path1, path2);
                Logger.Debug(@"FileSystem.Combine(""{0}"", ""{1}"") -> ""{2}""", path1, path2, combine);
                return combine;
            });

            fileSystem.Combine(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>()).Returns(info =>
            {
                var path1 = (string)info[0];
                var path2 = (string)info[1];
                var path3 = (string)info[2];
                var combine = realFileSystem.Combine(path1, path2, path3);
                Logger.Debug(@"FileSystem.Combine(""{0}"", ""{1}"", ""{2}"") -> ""{3}""", path1, path2, path3, combine);
                return combine;
            });

            fileSystem.FileExists(Arg.Any<string>()).Returns(info =>
            {
                var path1 = (string)info[0];
                var result = thatExist.Contains(path1);
                Logger.Debug(@"FileSystem.FileExists(""{0}"") -> {1}", path1, result);
                return result;
            });

            fileSystem.ReadAllText(Arg.Any<string>()).Returns(info =>
            {
                var path1 = (string)info[0];

                string result = null;

                string[] fileContent;
                if (dictionary.TryGetValue(path1, out fileContent))
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

            var archiveFolderName = realFileSystem.GetRandomFileName();
            fileSystem.GetRandomFileName().Returns(info => archiveFolderName);
            return fileSystem;
        }

        private ISharpZipLibHelper CreateSharpZipLibHelper()
        {
            var sharpZipLibHelper = Substitute.For<ISharpZipLibHelper>();
            return sharpZipLibHelper;
        }

        [OneTimeSetUp]
        public void TestFixtureSetup()
        {
            Logging.LoggerFactory = s => new ConsoleLogAdapter(s);
            Logger = Logging.GetLogger<PortableGitManagerTests>();
        }

        public ILogging Logger { get; set; }

        [Test]
        public void ShouldFindExistingInstallation()
        {
            const string extensionfolder = @"c:\ExtensionFolder";

            var environment = CreateEnvironment(extensionfolder);
            var sharpZipLibHelper = CreateSharpZipLibHelper();

            var filesThatExist = new[] {
                extensionfolder + @"\resources\windows\PortableGit.zip",
                extensionfolder + @"\PortableGit_f02737a78695063deace08e96d5042710d3e32db\cmd\git.exe",
                extensionfolder + @"\PortableGit_f02737a78695063deace08e96d5042710d3e32db\VERSION"
            };

            var fileContents = new Dictionary<string, string[]> {
                {
                    extensionfolder + @"\PortableGit_f02737a78695063deace08e96d5042710d3e32db\VERSION",
                    new [] {
                        "f02737a78695063deace08e96d5042710d3e32db"
                    }
                }
            };

            var fileSystem = CreateFileSystem(filesThatExist, fileContents);

            var portableGitManager = new PortableGitManager(environment, fileSystem, sharpZipLibHelper);
            portableGitManager.ExtractGitIfNeeded();
        }
    }
}
