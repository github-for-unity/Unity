using System;
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

            var watcherAutoResetEvent = new RepositoryWatcherAutoResetEvent();

            var repositoryWatcherListener = Substitute.For<IRepositoryWatcherListener>();
            repositoryWatcherListener.AttachListener(repositoryWatcher, watcherAutoResetEvent);

            repositoryWatcher.Initialize();
            repositoryWatcher.Start();

            try
            {
                var foobarTxt = TestRepoMasterCleanSynchronized.Combine("foobar.txt");

                Logger.Trace("Issuing Changes");

                foobarTxt.WriteAllText("foobar");

                watcherAutoResetEvent.IndexChanged.WaitOne(TimeSpan.FromSeconds(2)).Should().BeTrue();
                watcherAutoResetEvent.RepositoryChanged.WaitOne(TimeSpan.FromSeconds(2)).Should().BeTrue();

                Logger.Trace("Continue test");

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

            var watcherAutoResetEvent = new RepositoryWatcherAutoResetEvent();

            var repositoryWatcherListener = Substitute.For<IRepositoryWatcherListener>();
            repositoryWatcherListener.AttachListener(repositoryWatcher, watcherAutoResetEvent);

            repositoryWatcher.Initialize();
            repositoryWatcher.Start();

            try
            {
                Logger.Trace("Issuing Command");

                SwitchBranch("feature/document");

                watcherAutoResetEvent.HeadChanged.WaitOne(TimeSpan.FromSeconds(2)).Should().BeTrue();
                watcherAutoResetEvent.IndexChanged.WaitOne(TimeSpan.FromSeconds(2)).Should().BeTrue();
                watcherAutoResetEvent.RepositoryChanged.WaitOne(TimeSpan.FromSeconds(2)).Should().BeTrue();

                Logger.Trace("Continue test");

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

            var watcherAutoResetEvent = new RepositoryWatcherAutoResetEvent();

            var repositoryWatcherListener = Substitute.For<IRepositoryWatcherListener>();
            repositoryWatcherListener.AttachListener(repositoryWatcher, watcherAutoResetEvent);

            repositoryWatcher.Initialize();
            repositoryWatcher.Start();

            try
            {
                Logger.Trace("Issuing Command");

                DeleteBranch("feature/document");

                watcherAutoResetEvent.ConfigChanged.WaitOne(TimeSpan.FromSeconds(2)).Should().BeTrue();
                watcherAutoResetEvent.LocalBranchDeleted.WaitOne(TimeSpan.FromSeconds(2)).Should().BeTrue();

                Logger.Trace("Continue test");

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

            var watcherAutoResetEvent = new RepositoryWatcherAutoResetEvent();

            var repositoryWatcherListener = Substitute.For<IRepositoryWatcherListener>();
            repositoryWatcherListener.AttachListener(repositoryWatcher, watcherAutoResetEvent);

            repositoryWatcher.Initialize();
            repositoryWatcher.Start();

            try
            {
                Logger.Trace("Issuing Command");

                CreateBranch("feature/document2", "feature/document");

                watcherAutoResetEvent.LocalBranchCreated.WaitOne(TimeSpan.FromSeconds(2)).Should().BeTrue();

                Logger.Trace("Continue test");

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

                Logger.Trace("Issuing Command");

                CreateBranch("feature2/document2", "feature/document");

                watcherAutoResetEvent.LocalBranchCreated.WaitOne(TimeSpan.FromSeconds(2)).Should().BeTrue();

                Logger.Trace("Continue test");

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

            var watcherAutoResetEvent = new RepositoryWatcherAutoResetEvent();

            var repositoryWatcherListener = Substitute.For<IRepositoryWatcherListener>();
            repositoryWatcherListener.AttachListener(repositoryWatcher, watcherAutoResetEvent);

            repositoryWatcher.Initialize();
            repositoryWatcher.Start();

            try
            {
                Logger.Trace("Issuing Command");

                GitRemoteRemove("origin");

                watcherAutoResetEvent.ConfigChanged.WaitOne(TimeSpan.FromSeconds(2)).Should().BeTrue();
                watcherAutoResetEvent.RemoteBranchDeleted.WaitOne(TimeSpan.FromSeconds(2)).Should().BeTrue();
                watcherAutoResetEvent.RemoteBranchDeleted.WaitOne(TimeSpan.FromSeconds(2));
                watcherAutoResetEvent.RemoteBranchDeleted.WaitOne(TimeSpan.FromSeconds(2));

                Logger.Trace("Continue test");

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
                watcherAutoResetEvent.ConfigChanged.Reset();

                Logger.Trace("Issuing 2nd Command");

                GitRemoteAdd("origin", "https://github.com/EvilStanleyGoldman/IOTestsRepo.git");
                watcherAutoResetEvent.ConfigChanged.WaitOne(TimeSpan.FromSeconds(2)).Should().BeTrue();

                Logger.Trace("Continue 2nd test");

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

        [Test, Ignore("Failing on CI, needs fixing")]
        public void ShouldDetectGitPull()
        {
            InitializeEnvironment(TestRepoMasterCleanSynchronized);

            var repositoryWatcher = CreateRepositoryWatcher(TestRepoMasterCleanSynchronized);

            var watcherAutoResetEvent = new RepositoryWatcherAutoResetEvent();

            var repositoryWatcherListener = Substitute.For<IRepositoryWatcherListener>();
            repositoryWatcherListener.AttachListener(repositoryWatcher, watcherAutoResetEvent);

            repositoryWatcher.Initialize();
            repositoryWatcher.Start();

            try
            {
                Logger.Trace("Issuing Command");

                GitPull("origin", "master");

                watcherAutoResetEvent.IndexChanged.WaitOne(TimeSpan.FromSeconds(2)).Should().BeTrue();
                watcherAutoResetEvent.LocalBranchChanged.WaitOne(TimeSpan.FromSeconds(2)).Should().BeTrue();
                watcherAutoResetEvent.RepositoryChanged.WaitOne(TimeSpan.FromSeconds(2)).Should().BeTrue();

                Logger.Trace("Continue test");

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

        [Test, Ignore("Failing on CI, needs fixing")]
        public void ShouldDetectGitFetch()
        {
            InitializeEnvironment(TestRepoMasterCleanUnsynchronized);

            var repositoryWatcher = CreateRepositoryWatcher(TestRepoMasterCleanUnsynchronized);

            var watcherAutoResetEvent = new RepositoryWatcherAutoResetEvent();

            var repositoryWatcherListener = Substitute.For<IRepositoryWatcherListener>();
            repositoryWatcherListener.AttachListener(repositoryWatcher, watcherAutoResetEvent, true);

            repositoryWatcher.Initialize();
            repositoryWatcher.Start();

            try
            {
                Logger.Trace("Issuing Command");

                GitFetch("origin");

                watcherAutoResetEvent.RemoteBranchCreated.WaitOne(TimeSpan.FromSeconds(2)).Should().BeTrue();
                watcherAutoResetEvent.RemoteBranchCreated.WaitOne(TimeSpan.FromSeconds(2));

                Logger.Trace("Continue test");

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
            var evt = new ManualResetEventSlim(false);
            GitClient.SwitchBranch(branch)
                .ContinueWithUI(_ => evt.Set());
            var completed = evt.Wait(100);
            completed.Should().BeTrue();
        }

        protected void GitPull(string remote, string branch)
        {
            var evt = new ManualResetEventSlim(false);
            GitClient.Pull(remote, branch)
                .ContinueWithUI(_ => evt.Set());
            var completed = evt.Wait(100);
            completed.Should().BeTrue();
        }

        protected void GitFetch(string remote)
        {
            var evt = new ManualResetEventSlim(false);
            GitClient.Fetch(remote)
                .ContinueWithUI(_ => evt.Set());
            var completed = evt.Wait(100);
            completed.Should().BeTrue();
        }

        protected void DeleteBranch(string branch)
        {
            var evt = new ManualResetEventSlim(false);
            GitClient.DeleteBranch(branch)
                .ContinueWithUI(_ => evt.Set());
            var completed = evt.Wait(100);
            completed.Should().BeTrue();
        }

        protected void GitRemoteAdd(string remote, string url)
        {
            var evt = new ManualResetEventSlim(false);
            GitClient.RemoteAdd(remote, url)
                .ContinueWithUI(_ => evt.Set());
            var completed = evt.Wait(100);
            completed.Should().BeTrue();
        }

        protected void GitRemoteRemove(string remote)
        {
            var evt = new ManualResetEventSlim(false);
            GitClient.RemoteRemove(remote)
                .ContinueWithUI(_ => evt.Set());
            var completed = evt.Wait(100);
            completed.Should().BeTrue();
        }

        protected void CreateBranch(string branch, string baseBranch)
        {
            var evt = new ManualResetEventSlim(false);
            GitClient.CreateBranch(branch, baseBranch)
                .ContinueWithUI(_ => evt.Set());
            var completed = evt.Wait(100);
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
        public static void AttachListener(this IRepositoryWatcherListener listener, IRepositoryWatcher repositoryWatcher, RepositoryWatcherAutoResetEvent autoResetEvent = null, bool trace = false)
        {
            var logger = trace ? Logging.GetLogger<IRepositoryWatcherListener>() : null;

            repositoryWatcher.HeadChanged += s =>
            {
                logger?.Trace("HeadChanged: {0}", s);
                listener.HeadChanged(s);
                autoResetEvent?.HeadChanged.Set();
            };

            repositoryWatcher.ConfigChanged += () =>
            {
                logger?.Trace("ConfigChanged");
                listener.ConfigChanged();
                autoResetEvent?.ConfigChanged.Set();
            };

            repositoryWatcher.IndexChanged += () =>
            {
                logger?.Trace("IndexChanged");
                listener.IndexChanged();
                autoResetEvent?.IndexChanged.Set();
            };

            repositoryWatcher.LocalBranchChanged += s =>
            {
                logger?.Trace("LocalBranchChanged: {0}", s);
                listener.LocalBranchChanged(s);
                autoResetEvent?.LocalBranchChanged.Set();
            };

            repositoryWatcher.LocalBranchCreated += s =>
            {
                logger?.Trace("LocalBranchCreated: {0}", s);
                listener.LocalBranchCreated(s);
                autoResetEvent?.LocalBranchCreated.Set();
            };

            repositoryWatcher.LocalBranchDeleted += s =>
            {
                logger?.Trace("LocalBranchDeleted: {0}", s);
                listener.LocalBranchDeleted(s);
                autoResetEvent?.LocalBranchDeleted.Set();
            };

            repositoryWatcher.RemoteBranchChanged += (s, s1) =>
            {
                logger?.Trace("RemoteBranchChanged: {0} {1}", s, s1);
                listener.RemoteBranchChanged(s, s1);
                autoResetEvent?.RemoteBranchChanged.Set();
            };

            repositoryWatcher.RemoteBranchCreated += (s, s1) =>
            {
                logger?.Trace("RemoteBranchCreated: {0} {1}", s, s1);
                listener.RemoteBranchCreated(s, s1);
                autoResetEvent?.RemoteBranchCreated.Set();
            };

            repositoryWatcher.RemoteBranchDeleted += (s, s1) =>
            {
                logger?.Trace("RemoteBranchDeleted: {0} {1}", s, s1);
                listener.RemoteBranchDeleted(s, s1);
                autoResetEvent?.RemoteBranchDeleted.Set();
            };

            repositoryWatcher.RepositoryChanged += () =>
            {
                logger?.Trace("RepositoryChanged");
                listener.RepositoryChanged();
                autoResetEvent?.RepositoryChanged.Set();
            };
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

    class RepositoryWatcherAutoResetEvent
    {
        public AutoResetEvent HeadChanged { get; } = new AutoResetEvent(false);
        public AutoResetEvent ConfigChanged { get; } = new AutoResetEvent(false);
        public AutoResetEvent IndexChanged { get; } = new AutoResetEvent(false);
        public AutoResetEvent LocalBranchChanged { get; } = new AutoResetEvent(false);
        public AutoResetEvent LocalBranchCreated { get; } = new AutoResetEvent(false);
        public AutoResetEvent LocalBranchDeleted { get; } = new AutoResetEvent(false);
        public AutoResetEvent RemoteBranchChanged { get; } = new AutoResetEvent(false);
        public AutoResetEvent RemoteBranchCreated { get; } = new AutoResetEvent(false);
        public AutoResetEvent RemoteBranchDeleted { get; } = new AutoResetEvent(false);
        public AutoResetEvent RepositoryChanged { get; } = new AutoResetEvent(false);
    }
}
