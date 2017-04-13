using System.Threading;
using FluentAssertions;
using GitHub.Unity;
using NSubstitute;
using NUnit.Framework;
using TestUtils;

namespace IntegrationTests
{
    class RepositoryWatcherTests : BaseGitEnvironmentTest
    {
        private const int ThreadSleepTimeout = 2000;

        [Test]
        public void ShouldDetectFileChanges()
        {
            InitializeEnvironment(TestRepoMasterCleanSynchronized);

            var repositoryWatcher = CreateRepositoryWatcher(TestRepoMasterCleanSynchronized);

            var repositoryWatcherListener = Substitute.For<IRepositoryWatcherListener>();
            repositoryWatcherListener.AttachListener(repositoryWatcher);

            repositoryWatcher.Initialize();
            repositoryWatcher.Start();

            try
            {
                var foobarTxt = TestRepoMasterCleanSynchronized.Combine("foobar.txt");
                foobarTxt.WriteAllText("foobar");

                Thread.Sleep(ThreadSleepTimeout);

                repositoryWatcherListener.DidNotReceive().ConfigChanged();
                repositoryWatcherListener.DidNotReceive().HeadChanged(Args.String);
                repositoryWatcherListener.Received().IndexChanged();
                repositoryWatcherListener.DidNotReceive().LocalBranchCreated(Args.String);
                repositoryWatcherListener.DidNotReceive().LocalBranchDeleted(Args.String);
                repositoryWatcherListener.DidNotReceive().LocalBranchChanged(Args.String);
                repositoryWatcherListener.DidNotReceive().RemoteBranchChanged(Args.String, Args.String);
                repositoryWatcherListener.DidNotReceive().RemoteBranchCreated(Args.String, Args.String);
                repositoryWatcherListener.DidNotReceive().RemoteBranchDeleted(Args.String, Args.String);
                repositoryWatcherListener.Received().RepositoryChanged();
            }
            finally
            {
                repositoryWatcher.Stop();
            }
        }

        [Test]
        public void ShouldDetectBranchChange()
        {
            InitializeEnvironment(TestRepoMasterCleanSynchronized);

            var repositoryWatcher = CreateRepositoryWatcher(TestRepoMasterCleanSynchronized);

            var repositoryWatcherListener = Substitute.For<IRepositoryWatcherListener>();
            repositoryWatcherListener.AttachListener(repositoryWatcher);

            repositoryWatcher.Initialize();
            repositoryWatcher.Start();

            try
            {
                SwitchBranch("feature/document");

                Thread.Sleep(ThreadSleepTimeout);

                repositoryWatcherListener.DidNotReceive().ConfigChanged();
                repositoryWatcherListener.Received(1).HeadChanged("ref: refs/heads/feature/document");
                repositoryWatcherListener.Received().IndexChanged();
                repositoryWatcherListener.DidNotReceive().LocalBranchCreated(Args.String);
                repositoryWatcherListener.DidNotReceive().LocalBranchDeleted(Args.String);
                repositoryWatcherListener.DidNotReceive().LocalBranchChanged(Args.String);
                repositoryWatcherListener.DidNotReceive().RemoteBranchChanged(Args.String, Args.String);
                repositoryWatcherListener.DidNotReceive().RemoteBranchCreated(Args.String, Args.String);
                repositoryWatcherListener.DidNotReceive().RemoteBranchDeleted(Args.String, Args.String);
                repositoryWatcherListener.Received().RepositoryChanged();
            }
            finally
            {
                repositoryWatcher.Stop();
            }
        }

        [Test]
        public void ShouldDetectBranchDelete()
        {
            InitializeEnvironment(TestRepoMasterCleanSynchronized);

            var repositoryWatcher = CreateRepositoryWatcher(TestRepoMasterCleanSynchronized);

            var repositoryWatcherListener = Substitute.For<IRepositoryWatcherListener>();
            repositoryWatcherListener.AttachListener(repositoryWatcher);

            repositoryWatcher.Initialize();
            repositoryWatcher.Start();

            try
            {
                DeleteBranch("feature/document");

                Thread.Sleep(ThreadSleepTimeout);

                repositoryWatcherListener.Received(1).ConfigChanged();
                repositoryWatcherListener.DidNotReceive().HeadChanged(Args.String);
                repositoryWatcherListener.DidNotReceive().IndexChanged();
                repositoryWatcherListener.DidNotReceive().LocalBranchCreated(Args.String);
                repositoryWatcherListener.Received(1).LocalBranchDeleted("feature/document");
                repositoryWatcherListener.DidNotReceive().LocalBranchChanged(Args.String);
                repositoryWatcherListener.DidNotReceive().RemoteBranchChanged(Args.String, Args.String);
                repositoryWatcherListener.DidNotReceive().RemoteBranchCreated(Args.String, Args.String);
                repositoryWatcherListener.DidNotReceive().RemoteBranchDeleted(Args.String, Args.String);
                repositoryWatcherListener.DidNotReceive().RepositoryChanged();
            }
            finally
            {
                repositoryWatcher.Stop();
            }
        }

        [Test]
        public void ShouldDetectBranchCreate()
        {
            InitializeEnvironment(TestRepoMasterCleanSynchronized);

            var repositoryWatcher = CreateRepositoryWatcher(TestRepoMasterCleanSynchronized);

            var repositoryWatcherListener = Substitute.For<IRepositoryWatcherListener>();
            repositoryWatcherListener.AttachListener(repositoryWatcher);

            repositoryWatcher.Initialize();
            repositoryWatcher.Start();

            try
            {
                CreateBranch("feature/document2", "feature/document");

                Thread.Sleep(ThreadSleepTimeout);

                repositoryWatcherListener.DidNotReceive().ConfigChanged();
                repositoryWatcherListener.DidNotReceive().HeadChanged(Args.String);
                repositoryWatcherListener.DidNotReceive().IndexChanged();
                repositoryWatcherListener.Received(1).LocalBranchCreated("feature/document2");
                repositoryWatcherListener.DidNotReceive().LocalBranchDeleted(Args.String);
                repositoryWatcherListener.DidNotReceive().LocalBranchChanged("feature/document2");
                repositoryWatcherListener.DidNotReceive().RemoteBranchChanged(Args.String, Args.String);
                repositoryWatcherListener.DidNotReceive().RemoteBranchCreated(Args.String, Args.String);
                repositoryWatcherListener.DidNotReceive().RemoteBranchDeleted(Args.String, Args.String);
                repositoryWatcherListener.DidNotReceive().RepositoryChanged();

                repositoryWatcherListener.ClearReceivedCalls();

                CreateBranch("feature2/document2", "feature/document");

                Thread.Sleep(ThreadSleepTimeout);

                repositoryWatcherListener.DidNotReceive().ConfigChanged();
                repositoryWatcherListener.DidNotReceive().HeadChanged(Args.String);
                repositoryWatcherListener.DidNotReceive().IndexChanged();
                repositoryWatcherListener.Received(1).LocalBranchCreated("feature2/document2");
                repositoryWatcherListener.DidNotReceive().LocalBranchDeleted(Args.String);
                repositoryWatcherListener.DidNotReceive().LocalBranchChanged(Args.String);
                repositoryWatcherListener.DidNotReceive().RemoteBranchChanged(Args.String, Args.String);
                repositoryWatcherListener.DidNotReceive().RemoteBranchCreated(Args.String, Args.String);
                repositoryWatcherListener.DidNotReceive().RemoteBranchDeleted(Args.String, Args.String);
                repositoryWatcherListener.DidNotReceive().RepositoryChanged();

                repositoryWatcherListener.ClearReceivedCalls();
            }
            finally
            {
                repositoryWatcher.Stop();
            }
        }

        [Test]
        public void ShouldDetectChangesToRemotes()
        {
            InitializeEnvironment(TestRepoMasterCleanSynchronized);

            var repositoryWatcher = CreateRepositoryWatcher(TestRepoMasterCleanSynchronized);

            var repositoryWatcherListener = Substitute.For<IRepositoryWatcherListener>();
            repositoryWatcherListener.AttachListener(repositoryWatcher);

            repositoryWatcher.Initialize();
            repositoryWatcher.Start();

            try
            {
                GitRemoteRemove("origin");

                Thread.Sleep(ThreadSleepTimeout);

                repositoryWatcherListener.Received().ConfigChanged();
                repositoryWatcherListener.DidNotReceive().HeadChanged(Args.String);
                repositoryWatcherListener.DidNotReceive().IndexChanged();
                repositoryWatcherListener.DidNotReceive().LocalBranchCreated(Args.String);
                repositoryWatcherListener.DidNotReceive().LocalBranchDeleted(Args.String);
                repositoryWatcherListener.DidNotReceive().LocalBranchChanged(Args.String);
                repositoryWatcherListener.Received(1).RemoteBranchDeleted("origin", "feature/document-2");
                repositoryWatcherListener.Received(1).RemoteBranchDeleted("origin", "feature/other-feature");
                repositoryWatcherListener.Received(1).RemoteBranchDeleted("origin", "master");
                repositoryWatcherListener.DidNotReceive().RepositoryChanged();

                repositoryWatcherListener.ClearReceivedCalls();

                GitRemoteAdd("origin", "https://github.com/EvilStanleyGoldman/IOTestsRepo.git");

                Thread.Sleep(ThreadSleepTimeout);

                repositoryWatcherListener.Received().ConfigChanged();
                repositoryWatcherListener.DidNotReceive().HeadChanged(Args.String);
                repositoryWatcherListener.DidNotReceive().IndexChanged();
                repositoryWatcherListener.DidNotReceive().LocalBranchCreated(Args.String);
                repositoryWatcherListener.DidNotReceive().LocalBranchDeleted(Args.String);
                repositoryWatcherListener.DidNotReceive().LocalBranchChanged(Args.String);
                repositoryWatcherListener.DidNotReceive().RemoteBranchChanged(Args.String, Args.String);
                repositoryWatcherListener.DidNotReceive().RemoteBranchCreated(Args.String, Args.String);
                repositoryWatcherListener.DidNotReceive().RemoteBranchDeleted(Args.String, Args.String);
                repositoryWatcherListener.DidNotReceive().RepositoryChanged();
            }
            finally
            {
                repositoryWatcher.Stop();
            }
        }

        [Test]
        public void ShouldDetectGitPull()
        {
            InitializeEnvironment(TestRepoMasterCleanSynchronized);

            var repositoryWatcher = CreateRepositoryWatcher(TestRepoMasterCleanSynchronized);

            var repositoryWatcherListener = Substitute.For<IRepositoryWatcherListener>();
            repositoryWatcherListener.AttachListener(repositoryWatcher);

            repositoryWatcher.Initialize();
            repositoryWatcher.Start();

            try
            {
                GitPull("origin", "master");

                Thread.Sleep(ThreadSleepTimeout);

                repositoryWatcherListener.DidNotReceive().ConfigChanged();
                repositoryWatcherListener.DidNotReceive().HeadChanged(Args.String);
                repositoryWatcherListener.Received().IndexChanged();
                repositoryWatcherListener.DidNotReceive().LocalBranchCreated(Args.String);
                repositoryWatcherListener.DidNotReceive().LocalBranchDeleted(Args.String);
                repositoryWatcherListener.Received().LocalBranchChanged("master");
                repositoryWatcherListener.DidNotReceive().RemoteBranchChanged(Args.String, Args.String);
                repositoryWatcherListener.DidNotReceive().RemoteBranchCreated(Args.String, Args.String);
                repositoryWatcherListener.DidNotReceive().RemoteBranchDeleted(Args.String, Args.String);
                repositoryWatcherListener.Received().RepositoryChanged();
            }
            finally
            {
                repositoryWatcher.Stop();
            }
        }

        [Test]
        public void ShouldDetectGitFetch()
        {
            InitializeEnvironment(TestRepoMasterCleanUnsynchronized);

            var repositoryWatcher = CreateRepositoryWatcher(TestRepoMasterCleanUnsynchronized);

            var repositoryWatcherListener = Substitute.For<IRepositoryWatcherListener>();
            repositoryWatcherListener.AttachListener(repositoryWatcher);

            repositoryWatcher.Initialize();
            repositoryWatcher.Start();

            try
            {
                GitFetch("origin");

                Thread.Sleep(ThreadSleepTimeout);

                repositoryWatcherListener.DidNotReceive().ConfigChanged();
                repositoryWatcherListener.DidNotReceive().HeadChanged(Args.String);
                repositoryWatcherListener.DidNotReceive().IndexChanged();
                repositoryWatcherListener.DidNotReceive().LocalBranchCreated(Args.String);
                repositoryWatcherListener.DidNotReceive().LocalBranchDeleted(Args.String);
                repositoryWatcherListener.DidNotReceive().LocalBranchChanged(Args.String);
                repositoryWatcherListener.DidNotReceive().RemoteBranchChanged(Args.String, Args.String);
                repositoryWatcherListener.Received(1).RemoteBranchCreated("origin", "feature/new-feature");
                repositoryWatcherListener.Received(1).RemoteBranchCreated("origin", "feature/other-feature");
                repositoryWatcherListener.DidNotReceive().RemoteBranchDeleted(Args.String, Args.String);
                repositoryWatcherListener.DidNotReceive().RepositoryChanged();
            }
            finally
            {
                repositoryWatcher.Stop();
            }
        }

        protected void SwitchBranch(string branch)
        {
            var completed = false;
            var gitSwitchBranchesTask = new GitSwitchBranchesTask(Environment, ProcessManager,
                new TaskResultDispatcher<string>(s => { completed = true; }), branch);

            gitSwitchBranchesTask.RunAsync(CancellationToken.None).Wait();
            Thread.Sleep(100);

            completed.Should().BeTrue();
        }

        protected void GitPull(string remote, string branch)
        {
            var completed = false;

            var credentialManager = new GitCredentialManager(Environment, ProcessManager);
            var gitPullTask = new GitPullTask(Environment, ProcessManager,
                new TaskResultDispatcher<string>(s => { completed = true; }), credentialManager, new TestUIDispatcher(),
                remote, branch);

            gitPullTask.RunAsync(CancellationToken.None).Wait();
            Thread.Sleep(100);

            completed.Should().BeTrue();
        }

        protected void GitFetch(string remote)
        {
            var completed = false;

            var credentialManager = new GitCredentialManager(Environment, ProcessManager);
            var gitPullTask = new GitFetchTask(Environment, ProcessManager,
                new TaskResultDispatcher<string>(s => { completed = true; }), credentialManager, new TestUIDispatcher(),
                remote);

            gitPullTask.RunAsync(CancellationToken.None).Wait();
            Thread.Sleep(100);

            completed.Should().BeTrue();
        }

        protected void DeleteBranch(string branch)
        {
            var completed = false;
            var gitBranchDeleteTask = new GitBranchDeleteTask(Environment, ProcessManager,
                new TaskResultDispatcher<string>(s => { completed = true; }), branch, true);

            gitBranchDeleteTask.RunAsync(CancellationToken.None).Wait();
            Thread.Sleep(200);

            completed.Should().BeTrue();
        }

        protected void GitRemoteAdd(string remote, string url)
        {
            var completed = false;

            var gitRemoteAddTask = new GitRemoteAddTask(Environment, ProcessManager,
                new TaskResultDispatcher<string>(s => { completed = true; }), remote, url);

            gitRemoteAddTask.RunAsync(CancellationToken.None).Wait();
            Thread.Sleep(100);

            completed.Should().BeTrue();
        }

        protected void GitRemoteRemove(string remote)
        {
            var completed = false;

            var gitRemoteRemoveTask = new GitRemoteRemoveTask(Environment, ProcessManager,
                new TaskResultDispatcher<string>(s => { completed = true; }), remote);

            gitRemoteRemoveTask.RunAsync(CancellationToken.None).Wait();
            Thread.Sleep(100);

            completed.Should().BeTrue();
        }

        protected void CreateBranch(string branch, string baseBranch)
        {
            var completed = false;

            var gitBranchCreateTask = new GitBranchCreateTask(Environment, ProcessManager,
                new TaskResultDispatcher<string>(s => { completed = true; }), branch, baseBranch);

            gitBranchCreateTask.RunAsync(CancellationToken.None).Wait();
            Thread.Sleep(200);

            completed.Should().BeTrue();
        }

        private RepositoryWatcher CreateRepositoryWatcher(NPath path)
        {
            var paths = new RepositoryPathConfiguration(path);
            return new RepositoryWatcher(Platform, paths, CancellationToken.None);
        }
    }

    public interface IRepositoryWatcherListener
    {
        void ConfigChanged();
        void HeadChanged(string obj);
        void IndexChanged();
        void LocalBranchCreated(string branch);
        void LocalBranchDeleted(string branch);
        void LocalBranchChanged(string branch);
        void RemoteBranchChanged(string remote, string branch);
        void RemoteBranchCreated(string remote, string branch);
        void RemoteBranchDeleted(string remote, string branch);
        void RepositoryChanged();
    }

    static class RepositoryWatcherListenerExtensions
    {
        public static void AttachListener(this IRepositoryWatcherListener listener, IRepositoryWatcher repositoryWatcher)
        {
            repositoryWatcher.HeadChanged += listener.HeadChanged;
            repositoryWatcher.ConfigChanged += listener.ConfigChanged;
            repositoryWatcher.IndexChanged += listener.IndexChanged;
            repositoryWatcher.LocalBranchChanged += listener.LocalBranchChanged;
            repositoryWatcher.LocalBranchCreated += listener.LocalBranchCreated;
            repositoryWatcher.LocalBranchDeleted += listener.LocalBranchDeleted;
            repositoryWatcher.RemoteBranchChanged += listener.RemoteBranchChanged;
            repositoryWatcher.RemoteBranchCreated += listener.RemoteBranchCreated;
            repositoryWatcher.RemoteBranchDeleted += listener.RemoteBranchDeleted;
            repositoryWatcher.RepositoryChanged += listener.RepositoryChanged;
        }

        public static void AssertDidNotReceiveAnyCalls(this IRepositoryWatcherListener repositoryWatcherListener)
        {
            repositoryWatcherListener.DidNotReceive().ConfigChanged();
            repositoryWatcherListener.DidNotReceive().HeadChanged(Args.String);
            repositoryWatcherListener.DidNotReceive().IndexChanged();
            repositoryWatcherListener.DidNotReceive().LocalBranchCreated(Args.String);
            repositoryWatcherListener.DidNotReceive().LocalBranchDeleted(Args.String);
            repositoryWatcherListener.DidNotReceive().LocalBranchChanged(Args.String);
            repositoryWatcherListener.DidNotReceive().RemoteBranchChanged(Args.String, Args.String);
            repositoryWatcherListener.DidNotReceive().RemoteBranchCreated(Args.String, Args.String);
            repositoryWatcherListener.DidNotReceive().RemoteBranchDeleted(Args.String, Args.String);
            repositoryWatcherListener.DidNotReceive().RepositoryChanged();
        }
    }
}
