using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using FluentAssertions;
using GitHub.Unity;
using NUnit.Framework;
using TestUtils;

namespace UnitTests
{
    [TestFixture]
    class RepositoryManagerTests
    {
        [SetUp]
        public void SetUp()
        {
            NPathFileSystemProvider.Current =
                SubstituteFactory.CreateFileSystem(new CreateFileSystemOptions() {
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

        [TestFixtureSetUp]
        public void TestFixtureSetUp()
        {
            SubstituteFactory = new SubstituteFactory();
        }

        private readonly CancellationToken cancellationToken = CancellationToken.None;
        private IRepositoryWatcher repositoryWatcher;
        private IPlatform platform;
        private RepositoryPathConfiguration repositoryRepositoryPathConfiguration;
        private IGitConfig gitConfig;
        private Dictionary<CreateRepositoryProcessRunnerOptions.GitConfigGetKey, string> gitConfigGetResults;

        protected SubstituteFactory SubstituteFactory { get; private set; }

        private RepositoryManager CreateRepositoryManager(IRepositoryProcessRunner repositoryProcessRunner)
        {
            return new RepositoryManager(repositoryRepositoryPathConfiguration, platform, gitConfig, repositoryWatcher,
                repositoryProcessRunner, cancellationToken);
        }

        private IRepositoryProcessRunner CreateRepositoryProcessRunner(IList<GitStatus> gitStatusResults = null,
            IList<IList<GitLock>> gitListLocksResults = null)
        {
            return
                SubstituteFactory.CreateRepositoryProcessRunner(new CreateRepositoryProcessRunnerOptions {
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
        public void ShouldNotRefreshWithNoResults()
        {
            var repositoryProcessRunner = CreateRepositoryProcessRunner(new GitStatus[0], new IList<GitLock>[0]);
            var repositoryManager = CreateRepositoryManager(repositoryProcessRunner);

            GitStatus? expected = null;
            repositoryManager.OnRefreshTrackedFileList += s => { expected = s; };

            repositoryManager.Refresh();

            expected.HasValue.Should().BeFalse();
        }

        [Test]
        public void ShouldRefreshWithStatusResponseAndNoGitLockResponse()
        {
            var gitStatus = new GitStatus {
                LocalBranch = "master",
                Entries =
                    new List<GitStatusEntry> {
                        new GitStatusEntry("Some.sln", null, "Some.sln", GitFileStatus.Modified)
                    }
            };

            var repositoryProcessRunner = CreateRepositoryProcessRunner(new[] { gitStatus }, new IList<GitLock>[0]);
            var repositoryManager = CreateRepositoryManager(repositoryProcessRunner);

            GitStatus? expected = null;
            repositoryManager.OnRefreshTrackedFileList += s => { expected = s; };

            repositoryManager.Refresh();

            expected.HasValue.Should().BeTrue();

            Debug.Assert(expected != null, "expected != null");
            expected.Value.AssertEqual(gitStatus);
        }
    }
}
