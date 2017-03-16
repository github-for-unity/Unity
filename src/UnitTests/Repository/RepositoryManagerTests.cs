using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using FluentAssertions;
using GitHub.Unity;
using NSubstitute;
using NUnit.Framework;
using TestUtils;

namespace UnitTests
{
    [TestFixture]
    class RepositoryManagerTests
    {
        private readonly CancellationToken cancellationToken = CancellationToken.None;
        private IRepositoryWatcher repositoryWatcher;

        protected SubstituteFactory SubstituteFactory { get; private set; }

        [TestFixtureSetUp]
        public void TestFixtureSetup()
        {
            SubstituteFactory = new SubstituteFactory();
        }

        [Test]
        public void ShouldBeConstructable()
        {
            var repositoryManager = CreateRepositoryManager();
            repositoryManager.Should().NotBeNull();
        }

        [Test]
        public void ShouldRefresh()
        {
            var repositoryManager = CreateRepositoryManager();
            Action action = () => repositoryManager.Refresh();
            action.ShouldThrow<NotImplementedException>();
        }

        [Test]
        public void ShouldRefreshOnWatcherRepositoryChanged()
        {
            var repositoryManager = CreateRepositoryManager();

            Action action = () => repositoryWatcher.RepositoryChanged += Raise.Event<Action>();
            action.ShouldThrow<NotImplementedException>();
        }

        private RepositoryManager CreateRepositoryManager()
        {
            NPathFileSystemProvider.Current =
                SubstituteFactory.CreateFileSystem(new CreateFileSystemOptions() {
                    ChildFiles =
                        new Dictionary<SubstituteFactory.ContentsKey, IList<string>> {
                            {
                                new SubstituteFactory.ContentsKey(@"c:\Temp\.git\refs\heads", "*", SearchOption.TopDirectoryOnly),
                                new[] { "master" }
                            }, {
                                new SubstituteFactory.ContentsKey(@"c:\Temp\.git\refs\heads\features", "*",
                                    SearchOption.TopDirectoryOnly),
                                new[] { "feature1" }
                            },
                        },
                    ChildDirectories =
                        new Dictionary<SubstituteFactory.ContentsKey, IList<string>> {
                            {
                                new SubstituteFactory.ContentsKey(@"c:\Temp\.git\refs\heads", "*", SearchOption.TopDirectoryOnly),
                                new[] { @"c:\Temp\.git\refs\heads\features" }
                            }, {
                                new SubstituteFactory.ContentsKey(@"c:\Temp\.git\refs\heads\features", "*",
                                    SearchOption.TopDirectoryOnly),
                                new string[0]
                            },
                        },
                    FileContents =
                        new Dictionary<string, IList<string>> {
                            { @"c:\Temp\.git\HEAD", new[] { "ref: refs/heads/fixes/repository-manager-refresh" } }
                        }
                });

            var platform = SubstituteFactory.CreatePlatform();

            var repositoryRepositoryPathConfiguration = new RepositoryPathConfiguration(@"c:\Temp");
            var gitConfig = SubstituteFactory.CreateGitConfig();

            repositoryWatcher = SubstituteFactory.CreateRepositoryWatcher();

            var repositoryProcessRunner =
                SubstituteFactory.CreateRepositoryProcessRunner(new CreateRepositoryProcessRunnerOptions {
                    GitConfigGetResults =
                        new Dictionary<CreateRepositoryProcessRunnerOptions.GitConfigGetKey, string> {
                            {
                                new CreateRepositoryProcessRunnerOptions.GitConfigGetKey("user.email", GitConfigSource.User),
                                "someone@somewhere.com"
                            }, {
                                new CreateRepositoryProcessRunnerOptions.GitConfigGetKey("user.name", GitConfigSource.User),
                                "Someone Somewhere"
                            }
                        }
                });

            return new RepositoryManager(repositoryRepositoryPathConfiguration, platform, gitConfig,
                repositoryWatcher, repositoryProcessRunner, cancellationToken);
        }
    }
}
