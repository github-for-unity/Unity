using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GitHub.Unity;
using NSubstitute;

namespace GitHub.Unity.Tests
{
    class CreateTestWatchFactoryOptions
    {
        public CreateTestWatchFactoryOptions(Action<TestFileSystemWatcherWrapper> onWatchCreated)
        {
            OnWatchCreated = onWatchCreated;
        }

        public Action<TestFileSystemWatcherWrapper> OnWatchCreated { get; private set; }
    }

    class SubstituteFactory
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
                var path = (string)info[0];

                var result = false;
                if (createFileSystemOptions.FilesThatExist != null)
                {
                    result = createFileSystemOptions.FilesThatExist.Contains(path);
                }

                logger.Trace(@"FileSystem.FileExists(""{0}"") -> {1}", path, result);
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

                var result = true;
                if (createFileSystemOptions.DirectoriesThatExist != null)
                {
                    result = createFileSystemOptions.DirectoriesThatExist.Contains(path1);
                }

                logger.Trace(@"FileSystem.DirectoryExists(""{0}"") -> {1}", path1, result);
                return result;
            });

            fileSystem.ExistingPathIsDirectory(Arg.Any<string>()).Returns(info => {
                var path = (string)info[0];

                var result = false;
                if (createFileSystemOptions.DirectoriesThatExist != null)
                {
                    result = createFileSystemOptions.DirectoriesThatExist.Contains(path);
                }

                logger.Trace(@"FileSystem.ExistingPathIsDirectory(""{0}"") -> {1}", path, result);
                return result;
            });

            fileSystem.ReadAllText(Arg.Any<string>()).Returns(info => {
                var path = (string)info[0];

                string result = null;

                if (createFileSystemOptions.FileContents != null)
                {
                    IList<string> fileContent;
                    if (createFileSystemOptions.FileContents.TryGetValue(path, out fileContent))
                    {
                        result = string.Join(string.Empty, fileContent.ToArray());
                    }
                }

                logger.Trace(@"FileSystem.ReadAllText(""{0}"") -> {1}", path, result != null);

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

            fileSystem.GetFiles(Arg.Any<string>()).Returns(info => {
                var path = (string)info[0];

                var result = new string[0];
                if (createFileSystemOptions.ChildFiles != null)
                {
                    var key = new ContentsKey(path);
                    if (createFileSystemOptions.ChildFiles.ContainsKey(key))
                    {
                        result = createFileSystemOptions.ChildFiles[key].ToArray();
                    }
                }

                logger.Trace(@"FileSystem.GetFiles(""{0}"") -> {1} items", path, result.Length);

                return result;
            });

            fileSystem.GetFiles(Arg.Any<string>(), Arg.Any<string>()).Returns(info => {
                var path = (string)info[0];
                var pattern = (string)info[1];

                var result = new string[0];
                if (createFileSystemOptions.ChildFiles != null)
                {
                    var key = new ContentsKey(path, pattern);
                    if (createFileSystemOptions.ChildFiles.ContainsKey(key))
                    {
                        result = createFileSystemOptions.ChildFiles[key].ToArray();
                    }
                }

                logger.Trace(@"FileSystem.GetFiles(""{0}"", ""{1}"") -> {2} items", path, pattern, result.Length);

                return result;
            });

            fileSystem.GetFiles(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<SearchOption>()).Returns(info => {
                var path = (string)info[0];
                var pattern = (string)info[1];
                var searchOption = (SearchOption)info[2];

                var result = new string[0];
                if (createFileSystemOptions.ChildFiles != null)
                {
                    var key = new ContentsKey(path, pattern, searchOption);
                    if (createFileSystemOptions.ChildFiles.ContainsKey(key))
                    {
                        result = createFileSystemOptions.ChildFiles[key].ToArray();
                    }
                }

                logger.Trace(@"FileSystem.GetFiles(""{0}"", ""{1}"", {2}) -> {3} items", path, pattern, searchOption,
                    result.Length);

                return result;
            });

            fileSystem.GetDirectories(Arg.Any<string>()).Returns(info => {
                var path = (string)info[0];

                string[] result = new string[0];
                if (createFileSystemOptions.ChildDirectories != null)
                {
                    var key = new ContentsKey(path);
                    if (createFileSystemOptions.ChildDirectories.ContainsKey(key))
                    {
                        result = createFileSystemOptions.ChildDirectories[key].ToArray();
                    }
                }

                logger.Trace(@"FileSystem.GetDirectories(""{0}"") -> {1} itemss", path, result.Length);

                return result;
            });

            fileSystem.GetDirectories(Arg.Any<string>(), Arg.Any<string>()).Returns(info => {
                var path = (string)info[0];
                var pattern = (string)info[1];

                string[] result = new string[0];
                if (createFileSystemOptions.ChildDirectories != null)
                {
                    var key = new ContentsKey(path, pattern);
                    if (createFileSystemOptions.ChildDirectories.ContainsKey(key))
                    {
                        result = createFileSystemOptions.ChildDirectories[key].ToArray();
                    }
                }

                logger.Trace(@"FileSystem.GetDirectories(""{0}"", ""{1}"") -> {2} items", path, pattern, result.Length);

                return result;
            });

            fileSystem.GetDirectories(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<SearchOption>()).Returns(info => {
                var path = (string)info[0];
                var pattern = (string)info[1];
                var searchOption = (SearchOption)info[2];

                string[] result = null;
                if (createFileSystemOptions.ChildDirectories != null)
                {
                    var key = new ContentsKey(path, pattern, searchOption);
                    if (createFileSystemOptions.ChildDirectories.ContainsKey(key))
                    {
                        result = createFileSystemOptions.ChildDirectories[key].ToArray();
                    }
                }

                var resultLength = result != null ? $"{result.Length} items" : "ERROR";

                logger.Trace(@"FileSystem.GetDirectories(""{0}"", ""{1}"", {2}) -> {3}", path, pattern, searchOption,
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
        public IFileSystemWatchWrapperFactory CreateTestWatchFactory(
            CreateTestWatchFactoryOptions createTestWatchFactoryOptions = null)
        {
            var logger = Logging.GetLogger("TestFileSystemWatcherWrapper");

            var fileSystemWatchFactory = Substitute.For<IFileSystemWatchWrapperFactory>();
            fileSystemWatchFactory.CreateWatch(Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<string>()).Returns(info => {
                var path = (string)info[0];
                var recursive = (bool)info[1];
                var filter = (string)info[2];

                if (filter != null)
                {
                    logger.Trace(@"CreateWatch(""{0}"", {1}, ""{2}"")", path, recursive, filter);
                }
                else
                {
                    logger.Trace(@"CreateWatch(""{0}"", {1}, null)", path, recursive);
                }

                var fileSystemWatch = new TestFileSystemWatcherWrapper(path, recursive, filter);

                createTestWatchFactoryOptions?.OnWatchCreated?.Invoke(fileSystemWatch);

                return fileSystemWatch;
            });

            return fileSystemWatchFactory;
        }

        public struct ContentsKey
    {
        public readonly string Path;
        public readonly string Pattern;
            public readonly SearchOption? SearchOption;

            public ContentsKey(string path, string pattern, SearchOption? searchOption)
        {
            Path = path;
            Pattern = pattern;
            SearchOption = searchOption;
        }

            public ContentsKey(string path, string pattern) : this(path, pattern, null)
            {}

            public ContentsKey(string path) : this(path, null)
            {}
    }
}
