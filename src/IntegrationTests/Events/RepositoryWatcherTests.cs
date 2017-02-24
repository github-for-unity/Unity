using System;
using System.Linq;
using System.Threading;
using FluentAssertions;
using GitHub.Unity;
using NUnit.Framework;

namespace IntegrationTests.Events
{
    class RepositoryWatcherTests : BaseGitIntegrationTest
    {
        protected override void OnSetup()
        {
            base.OnSetup();

            DotGitPath = TestBasePath.Combine(".git");

            if (DotGitPath.FileExists())
            {
                DotGitPath =
                    DotGitPath.ReadAllLines()
                              .Where(x => x.StartsWith("gitdir:"))
                              .Select(x => x.Substring(7).Trim())
                              .First();
            }

            BranchesPath = DotGitPath.Combine("refs", "heads");
            RemotesPath = DotGitPath.Combine("refs", "remotes");
            DotGitIndex = DotGitPath.Combine("index");
            DotGitHead = DotGitPath.Combine("HEAD");
            DotGitConfig = DotGitPath.Combine("config");
        }

        private RepositoryWatcher CreateRepositoryWatcher()
        {
            return new RepositoryWatcher(Platform, TestBasePath, DotGitPath, DotGitIndex, DotGitHead, BranchesPath, RemotesPath, DotGitConfig);
        }

        protected NPath DotGitConfig { get; private set; }

        protected NPath DotGitHead { get; private set; }

        protected NPath DotGitIndex { get; private set; }

        protected NPath RemotesPath { get; private set; }

        protected NPath BranchesPath { get; private set; }

        protected NPath DotGitPath { get; private set; }

        [Test]
        public void ShouldDetectFileChanges()
        {
            var repositoryWatcher = CreateRepositoryWatcher();

            var repositoryChanged = 0;
            repositoryWatcher.RepositoryChanged += () => { repositoryChanged++; };

            repositoryWatcher.Start();

            var foobarTxt = TestBasePath.Combine("foobar.txt");
            foobarTxt.WriteAllText("foobar");

            Thread.Sleep(100);
            
            //http://stackoverflow.com/questions/1764809/filesystemwatcher-changed-event-is-raised-twice
            repositoryChanged.Should().Be(2);
        }

        [Test]
        public void ShouldDetectBranchChange()
        {
            var repositoryWatcher = CreateRepositoryWatcher();

            var completed = false;
            var repositoryChanged = 0;

            repositoryWatcher.RepositoryChanged += () => { repositoryChanged++; };
            repositoryWatcher.Start();

            var taskResultDispatcher = new TaskResultDispatcher<string>(s => {
                completed = true;
            });

            new GitSwitchBranchesTask(Environment, ProcessManager, taskResultDispatcher, "feature/document");

            Thread.Sleep(2000);

            completed.Should().BeTrue();
        }
    }
}
