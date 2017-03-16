using System.Collections.Generic;
using System.IO;
using System.Threading;
using GitHub.Unity;
using NUnit.Framework;
using TestUtils;

namespace UnitTests
{
    [TestFixture]
    class RepositoryManagerTests
    {
        protected SubstituteFactory SubstituteFactory { get; private set; }

        [TestFixtureSetUp]
        public void TestFixtureSetup()
        {
            SubstituteFactory = new SubstituteFactory();
        }

        [Test]
        public void InitialTest()
        {
            NPathFileSystemProvider.Current =
                SubstituteFactory.CreateFileSystem(new CreateFileSystemOptions() {
                    ChildFiles =
                        new Dictionary<SubstituteFactory.ContentsKey, IList<string>> {
                            {
                                new SubstituteFactory.ContentsKey(@"c:\Temp\.git\refs\heads", "*", SearchOption.TopDirectoryOnly),
                                new[] { "master" }
                            },
                            {
                                new SubstituteFactory.ContentsKey(@"c:\Temp\.git\refs\heads\features", "*", SearchOption.TopDirectoryOnly),
                                new[] { "feature1" }
                            },
                        },
                    ChildDirectories = 
                        new Dictionary<SubstituteFactory.ContentsKey, IList<string>> {
                            {
                                new SubstituteFactory.ContentsKey(@"c:\Temp\.git\refs\heads", "*", SearchOption.TopDirectoryOnly),
                                new[] { "features" }
                            },
                            {
                                new SubstituteFactory.ContentsKey(@"c:\Temp\.git\refs\heads\features", "*", SearchOption.TopDirectoryOnly),
                                new string[0]
                            },
                        },
                });

            var cancellationToken = CancellationToken.None;

            var platform = SubstituteFactory.CreatePlatform();

            var repositoryRepositoryPathConfiguration = new RepositoryPathConfiguration(@"c:\Temp");
            var gitConfig = SubstituteFactory.CreateGitConfig();
            var repositoryWatcher = SubstituteFactory.CreateRepositoryWatcher();
            var repositoryProcessRunner = SubstituteFactory.CreateRepositoryProcessRunner();

            var repositoryManager = new RepositoryManager(repositoryRepositoryPathConfiguration, platform, gitConfig,
                repositoryWatcher, repositoryProcessRunner, cancellationToken);
        }
    }
}
