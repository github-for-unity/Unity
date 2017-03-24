using System.Collections.Generic;
using System.IO;
using System.Linq;
using GitHub.Unity;
using NSubstitute;

namespace TestUtils
{
    class SubstituteFactory
    {
        public SubstituteFactory()
        {}

        public IEnvironment CreateEnvironment(CreateEnvironmentOptions createEnvironmentOptions = null)
        {
            createEnvironmentOptions = createEnvironmentOptions ?? new CreateEnvironmentOptions();

            var userPath = createEnvironmentOptions.UserProfilePath.ToNPath();
            var localAppData = userPath.Parent.Combine("LocalAppData").ToString();
            var appData = userPath.Parent.Combine("AppData").ToString();

            var environment = Substitute.For<IEnvironment>();
            environment.RepositoryPath.Returns(createEnvironmentOptions.RepositoryPath);
            environment.ExtensionInstallPath.Returns(createEnvironmentOptions.Extensionfolder);
            environment.UserProfilePath.Returns(createEnvironmentOptions.UserProfilePath);
            environment.UnityProjectPath.Returns(createEnvironmentOptions.UnityProjectPath);
            environment.GetSpecialFolder(System.Environment.SpecialFolder.LocalApplicationData).Returns(localAppData);
            environment.GetSpecialFolder(System.Environment.SpecialFolder.ApplicationData).Returns(appData);
            return environment;
        }

        public IFileSystem CreateFileSystem(CreateFileSystemOptions createFileSystemOptions = null)
        {
            createFileSystemOptions = createFileSystemOptions ?? new CreateFileSystemOptions();
            var fileSystem = Substitute.For<IFileSystem>();
            var realFileSystem = new FileSystem();
            var logger = Logging.GetLogger("TestFileSystem");

            fileSystem.DirectorySeparatorChar.Returns(realFileSystem.DirectorySeparatorChar);
            fileSystem.GetCurrentDirectory().Returns(createFileSystemOptions.CurrentDirectory);

            fileSystem.Combine(Args.String, Args.String).Returns(info =>
            {
                var path1 = (string)info[0];
                var path2 = (string)info[1];
                var combine = realFileSystem.Combine(path1, path2);
                logger.Trace(@"FileSystem.Combine(""{0}"", ""{1}"") -> ""{2}""", path1, path2, combine);
                return combine;
            });

            fileSystem.Combine(Args.String, Args.String, Args.String).Returns(info =>
            {
                var path1 = (string)info[0];
                var path2 = (string)info[1];
                var path3 = (string)info[2];
                var combine = realFileSystem.Combine(path1, path2, path3);
                logger.Trace(@"FileSystem.Combine(""{0}"", ""{1}"", ""{2}"") -> ""{3}""", path1, path2, path3, combine);
                return combine;
            });

            fileSystem.FileExists(Args.String).Returns(info =>
            {
                var path = (string)info[0];

                var result = false;
                if (createFileSystemOptions.FilesThatExist != null)
                {
                    result = createFileSystemOptions.FilesThatExist.Contains(path);
                }

                logger.Trace(@"FileSystem.FileExists(""{0}"") -> {1}", path, result);
                return result;
            });

            fileSystem.WhenForAnyArgs(system => system.FileCopy(Args.String, Args.String, Args.Bool))
                      .Do(
                          info =>
                          {
                              logger.Trace(@"FileSystem.FileCopy(""{0}"", ""{1}"", ""{2}"")", (string)info[0],
                                  (string)info[1], (bool)info[2]);
                          });

            fileSystem.DirectoryExists(Args.String).Returns(info =>
            {
                var path1 = (string)info[0];

                var result = true;
                if (createFileSystemOptions.DirectoriesThatExist != null)
                {
                    result = createFileSystemOptions.DirectoriesThatExist.Contains(path1);
                }

                logger.Trace(@"FileSystem.DirectoryExists(""{0}"") -> {1}", path1, result);
                return result;
            });

            fileSystem.ExistingPathIsDirectory(Args.String).Returns(info =>
            {
                var path = (string)info[0];

                var result = false;
                if (createFileSystemOptions.DirectoriesThatExist != null)
                {
                    result = createFileSystemOptions.DirectoriesThatExist.Contains(path);
                }

                logger.Trace(@"FileSystem.ExistingPathIsDirectory(""{0}"") -> {1}", path, result);
                return result;
            });

            fileSystem.ReadAllText(Args.String).Returns(info =>
            {
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
            fileSystem.GetRandomFileName().Returns(info =>
            {
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

            fileSystem.GetTempPath().Returns(info =>
            {
                logger.Trace(@"FileSystem.GetTempPath() -> {0}", createFileSystemOptions.TemporaryPath);

                return createFileSystemOptions.TemporaryPath;
            });

            fileSystem.GetFiles(Args.String).Returns(info =>
            {
                var path = (string)info[0];

                string[] result = null;
                if (createFileSystemOptions.ChildFiles != null)
                {
                    var key = new ContentsKey(path);
                    if (createFileSystemOptions.ChildFiles.ContainsKey(key))
                    {
                        result = createFileSystemOptions.ChildFiles[key].ToArray();
                    }
                }

                var resultLength = result != null ? $"{result.Length} items" : "ERROR";

                logger.Trace(@"FileSystem.GetFiles(""{0}"") -> {1}", path, resultLength);

                return result;
            });

            fileSystem.GetFiles(Args.String, Args.String).Returns(info =>
            {
                var path = (string)info[0];
                var pattern = (string)info[1];

                string[] result = null;
                if (createFileSystemOptions.ChildFiles != null)
                {
                    var key = new ContentsKey(path, pattern);
                    if (createFileSystemOptions.ChildFiles.ContainsKey(key))
                    {
                        result = createFileSystemOptions.ChildFiles[key].ToArray();
                    }
                }

                var resultLength = result != null ? $"{result.Length} items" : "ERROR";

                logger.Trace(@"FileSystem.GetFiles(""{0}"", ""{1}"") -> {2}", path, pattern, resultLength);

                return result;
            });

            fileSystem.GetFiles(Args.String, Args.String, Args.SearchOption).Returns(info =>
            {
                var path = (string)info[0];
                var pattern = (string)info[1];
                var searchOption = (SearchOption)info[2];

                string[] result = null;
                if (createFileSystemOptions.ChildFiles != null)
                {
                    var key = new ContentsKey(path, pattern, searchOption);
                    if (createFileSystemOptions.ChildFiles.ContainsKey(key))
                    {
                        result = createFileSystemOptions.ChildFiles[key].ToArray();
                    }
                }

                var resultLength = result != null ? $"{result.Length} items" : "ERROR";

                logger.Trace(@"FileSystem.GetFiles(""{0}"", ""{1}"", {2}) -> {3}", path, pattern, searchOption,
                    resultLength);

                return result;
            });

            fileSystem.GetDirectories(Args.String).Returns(info =>
            {
                var path = (string)info[0];

                string[] result = null;
                if (createFileSystemOptions.ChildDirectories != null)
                {
                    var key = new ContentsKey(path);
                    if (createFileSystemOptions.ChildDirectories.ContainsKey(key))
                    {
                        result = createFileSystemOptions.ChildDirectories[key].ToArray();
                    }
                }

                var resultLength = result != null ? $"{result.Length} items" : "ERROR";

                logger.Trace(@"FileSystem.GetDirectories(""{0}"") -> {1}", path, resultLength);

                return result;
            });

            fileSystem.GetDirectories(Args.String, Args.String).Returns(info =>
            {
                var path = (string)info[0];
                var pattern = (string)info[1];

                string[] result = null;
                if (createFileSystemOptions.ChildDirectories != null)
                {
                    var key = new ContentsKey(path, pattern);
                    if (createFileSystemOptions.ChildDirectories.ContainsKey(key))
                    {
                        result = createFileSystemOptions.ChildDirectories[key].ToArray();
                    }
                }

                var resultLength = result != null ? $"{result.Length} items" : "ERROR";

                logger.Trace(@"FileSystem.GetDirectories(""{0}"", ""{1}"") -> {2}", path, pattern, resultLength);

                return result;
            });

            fileSystem.GetDirectories(Args.String, Args.String, Args.SearchOption).Returns(info =>
            {
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

            fileSystem.GetFullPath(Args.String)
                      .Returns(info => Path.GetFullPath((string)info[0]));

            return fileSystem;
        }

        public IZipHelper CreateSharpZipLibHelper()
        {
            return Substitute.For<IZipHelper>();
        }

        public IGitObjectFactory CreateGitObjectFactory(string gitRepoPath)
        {
            var gitObjectFactory = Substitute.For<IGitObjectFactory>();

            gitObjectFactory.CreateGitStatusEntry(Args.String, Args.GitFileStatus, Args.String, Args.Bool)
                                 .Returns(info => {
                                     var path = (string)info[0];
                                     var status = (GitFileStatus)info[1];
                                     var originalPath = (string)info[2];
                                     var staged = (bool)info[3];

                                     return new GitStatusEntry(path, gitRepoPath + @"\" + path, null, status,
                                         originalPath, staged);
                                 });

            gitObjectFactory.CreateGitLock(Args.String, Args.String)
                                 .Returns(info => {
                                     var path = (string)info[0];
                                     var user = (string)info[1];

                                     return new GitLock(path, gitRepoPath + @"\" + path, user);
                                 });

            return gitObjectFactory;
        }

        public IProcessEnvironment CreateProcessEnvironment(string root)
        {
            var processEnvironment = Substitute.For<IProcessEnvironment>();
            processEnvironment.FindRoot(Args.String).Returns(root);
            return processEnvironment;
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
            { }

            public ContentsKey(string path) : this(path, null)
            { }
        }
    }
}
