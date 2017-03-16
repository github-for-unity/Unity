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
        private IPlatform platform;
        private RepositoryPathConfiguration repositoryRepositoryPathConfiguration;
        private IGitConfig gitConfig;
        private Dictionary<CreateRepositoryProcessRunnerOptions.GitConfigGetKey, string> gitConfigGetResults;

        protected SubstituteFactory SubstituteFactory { get; private set; }

        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            SubstituteFactory = new SubstituteFactory();
        }

        [SetUp]
        public void SetUp()
        {
            NPathFileSystemProvider.Current =
                SubstituteFactory.CreateFileSystem(new CreateFileSystemOptions()
                {
                    ChildFiles =
                        new Dictionary<SubstituteFactory.ContentsKey, IList<string>> {
                            {
                                new SubstituteFactory.ContentsKey(@"c:\Temp\.git\refs\heads", "*",
                                    SearchOption.TopDirectoryOnly),
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
                                new SubstituteFactory.ContentsKey(@"c:\Temp\.git\refs\heads", "*",
                                    SearchOption.TopDirectoryOnly),
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

            platform = SubstituteFactory.CreatePlatform();

            repositoryRepositoryPathConfiguration = new RepositoryPathConfiguration(@"c:\Temp");
            gitConfig = SubstituteFactory.CreateGitConfig();

            repositoryWatcher = SubstituteFactory.CreateRepositoryWatcher();

            gitConfigGetResults = new Dictionary<CreateRepositoryProcessRunnerOptions.GitConfigGetKey, string> {
                {
                    new CreateRepositoryProcessRunnerOptions.GitConfigGetKey("user.email", GitConfigSource.User),
                    "someone@somewhere.com"
                }, {
                    new CreateRepositoryProcessRunnerOptions.GitConfigGetKey("user.name", GitConfigSource.User),
                    "Someone Somewhere"
                }
            };
        }

        private RepositoryManager CreateRepositoryManager(IRepositoryProcessRunner repositoryProcessRunner)
        {
            return new RepositoryManager(repositoryRepositoryPathConfiguration, platform, gitConfig, repositoryWatcher,
                repositoryProcessRunner, cancellationToken);
        }

        private IRepositoryProcessRunner CreateRepositoryProcessRunner(IList<GitStatus> gitStatusResults = null, IList<IList<GitLock>> gitListLocksResults = null)
        {
            gitStatusResults = gitStatusResults ?? new[] { new GitStatus { Entries = new List<GitStatusEntry>() } };
            gitListLocksResults = gitListLocksResults ?? new IList<GitLock>[] { new GitLock[0] };
            return SubstituteFactory.CreateRepositoryProcessRunner(new CreateRepositoryProcessRunnerOptions {
                GitConfigGetResults = gitConfigGetResults,
                GitStatusResults = gitStatusResults,
                GitListLocksResults = gitListLocksResults
            });
        }

        [Test]
        public void ShouldBeConstructable()
        {
            var repositoryProcessRunner = CreateRepositoryProcessRunner();
            var repositoryManager = CreateRepositoryManager(repositoryProcessRunner);
            repositoryManager.Should().NotBeNull();
        }

        [Test]
        public void ShouldRefresh()
        {
            var repositoryProcessRunner = CreateRepositoryProcessRunner();
            var repositoryManager = CreateRepositoryManager(repositoryProcessRunner);

            GitStatus? status = null;
            repositoryManager.OnRefreshTrackedFileList += s => { status = s; };

            repositoryManager.Refresh();

            status.HasValue.Should().BeTrue();
        }

        [Test]
        public void ShouldRefreshOnWatcherRepositoryChanged()
        {
            var repositoryProcessRunner = CreateRepositoryProcessRunner();
            var repositoryManager = CreateRepositoryManager(repositoryProcessRunner);

            GitStatus? status = null;
            repositoryManager.OnRefreshTrackedFileList += s => { status = s; };

            repositoryWatcher.RepositoryChanged += Raise.Event<Action>();

            status.HasValue.Should().BeTrue();
        }
    }
}
