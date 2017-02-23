using System.IO;
using NSubstitute;
using System.Linq;
using GitHub.Unity;

namespace UnitTests
{
    class TestBase
    {
        public IEnvironment CreateEnvironment(CreateEnvironmentOptions createEnvironmentOptions = null)
        {
            createEnvironmentOptions = createEnvironmentOptions ?? new CreateEnvironmentOptions();

            var environment = Substitute.For<IEnvironment>();
            environment.ExtensionInstallPath.Returns(createEnvironmentOptions.Extensionfolder);
            environment.UserProfilePath.Returns(createEnvironmentOptions.UserProfilePath);
            return environment;
        }

        public IFileSystem CreateFileSystem(CreateFileSystemOptions createFileSystemOptions = null, string currentDirectory = null)
        {
            createFileSystemOptions = createFileSystemOptions ?? new CreateFileSystemOptions();
            if (currentDirectory == null)
                currentDirectory = CreateFileSystemOptions.DefaultTemporaryPath;

            var fileSystem = Substitute.For<IFileSystem>();
            var realFileSystem = new FileSystem();
            var logger = Logging.GetLogger("TestFileSystem");

            fileSystem.DirectorySeparatorChar.Returns(realFileSystem.DirectorySeparatorChar);
            fileSystem.GetCurrentDirectory().Returns(currentDirectory);

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

            fileSystem.FileExists(Arg.Any<string>()).Returns(info => {
                var path1 = (string)info[0];

                var result = false;
                if (createFileSystemOptions.FilesThatExist != null)
                {
                    result = createFileSystemOptions.FilesThatExist.Contains(path1);
                }

                logger.Trace(@"FileSystem.FileExists(""{0}"") -> {1}", path1, result);
                return result;
            });

            fileSystem.WhenForAnyArgs(system => system.FileCopy(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>()))
                      .Do(
                          info => {
                              logger.Trace(@"FileSystem.FileCopy(""{0}"", ""{1}"", ""{2}"")", (string)info[0],
                                  (string)info[1], (bool)info[2]);
                          });

            fileSystem.DirectoryExists(Arg.Any<string>()).Returns(info => {
                var path1 = (string)info[0];

                var result = false;
                if (createFileSystemOptions.DirectoriesThatExist != null)
                {
                    result = createFileSystemOptions.DirectoriesThatExist.Contains(path1);
                }

                logger.Trace(@"FileSystem.DirectoryExists(""{0}"") -> {1}", path1, result);
                return result;
            });
            fileSystem.ReadAllText(Arg.Any<string>()).Returns(info => {
                var path1 = (string)info[0];

                string result = null;

                if (createFileSystemOptions.FileContents != null)
                {
                    string[] fileContent;
                    if (createFileSystemOptions.FileContents.TryGetValue(path1, out fileContent))
                    {
                        result = string.Join(string.Empty, fileContent);
                    }
                }

                logger.Trace(@"FileSystem.ReadAllText(""{0}"") -> {1}", path1, result != null);

                return result;
            });

            var randomFileIndex = 0;
            fileSystem.GetRandomFileName().Returns(info => {
                string result = null;
                if (createFileSystemOptions.RandomFileNames != null)
                {
                    result = createFileSystemOptions.RandomFileNames[randomFileIndex];

                    randomFileIndex++;
                    randomFileIndex = randomFileIndex % createFileSystemOptions.RandomFileNames.Count;
                }

                logger.Trace(@"FileSystem.GetRandomFileName() -> {0}", result);

                return result;
            });

            fileSystem.GetTempPath().Returns(info => {
                logger.Trace(@"FileSystem.GetTempPath() -> {0}", createFileSystemOptions.TemporaryPath);

                return createFileSystemOptions.TemporaryPath;
            });

            fileSystem.GetFiles(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<SearchOption>()).Returns(info => {
                var path = (string)info[0];
                var pattern = (string)info[1];
                var searchOption = (SearchOption)info[2];

                string[] result = null;
                if (createFileSystemOptions.FolderContents != null)
                {
                    var key = new FolderContentsKey(path, pattern, searchOption);
                    if (createFileSystemOptions.FolderContents.ContainsKey(key))
                    {
                        result = createFileSystemOptions.FolderContents[key];
                    }
                }

                var resultLength = result != null ? $"{result.Length} items" : "ERROR";

                logger.Trace(@"FileSystem.GetFiles(""{0}"", ""{1}"", {2}) -> {3}", path, pattern, searchOption,
                    resultLength);

                return result;
            });

            fileSystem.GetFullPath(Arg.Any<string>())
                      .Returns(info => Path.GetFullPath((string)info[0]));

            return fileSystem;
        }

        public IZipHelper CreateSharpZipLibHelper()
        {
            return Substitute.For<IZipHelper>();
        }

    }
    public struct FolderContentsKey
    {
        public readonly string Path;
        public readonly string Pattern;
        public readonly SearchOption SearchOption;

        public FolderContentsKey(string path, string pattern, SearchOption searchOption)
        {
            Path = path;
            Pattern = pattern;
            SearchOption = searchOption;
        }
    }
}