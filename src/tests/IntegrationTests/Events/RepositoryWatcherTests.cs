using System;
using System.Threading;
using FluentAssertions;
using GitHub.Unity;
using NSubstitute;
using NUnit.Framework;
using TestUtils;
using System.Threading.Tasks;

namespace IntegrationTests
{
    class RepositoryWatcherTests : BaseGitEnvironmentTest
    {
        private const int ThreadSleepTimeout = 2000;

        [Test]
        public async Task ShouldDetectFileChanges()
        {
            Initialize(TestRepoMasterCleanSynchronized);

            using (var repositoryWatcher = CreateRepositoryWatcher(TestRepoMasterCleanSynchronized))
            {
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
                    await TaskManager.Wait();

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
        }

        [Test]
        public async Task ShouldDetectBranchChange()
        {
            Initialize(TestRepoMasterCleanSynchronized);

            using (var repositoryWatcher = CreateRepositoryWatcher(TestRepoMasterCleanSynchronized))
            {
                var watcherAutoResetEvent = new RepositoryWatcherAutoResetEvent();

                var repositoryWatcherListener = Substitute.For<IRepositoryWatcherListener>();
                repositoryWatcherListener.AttachListener(repositoryWatcher, watcherAutoResetEvent);

                repositoryWatcher.Initialize();
                repositoryWatcher.Start();

                try
                {
                    Logger.Trace("Issuing Command");

                    Assert.DoesNotThrow(async () => await GitClient.SwitchBranch("feature/document").Start().Task);
                    await TaskManager.Wait();

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
        }

        [Test]
        public async Task ShouldDetectBranchDelete()
        {
            Initialize(TestRepoMasterCleanSynchronized);

            using (var repositoryWatcher = CreateRepositoryWatcher(TestRepoMasterCleanSynchronized))
            {
                var watcherAutoResetEvent = new RepositoryWatcherAutoResetEvent();

                var repositoryWatcherListener = Substitute.For<IRepositoryWatcherListener>();
                repositoryWatcherListener.AttachListener(repositoryWatcher, watcherAutoResetEvent);

                repositoryWatcher.Initialize();
                repositoryWatcher.Start();

                try
                {
                    Logger.Trace("Issuing Command");

                    Assert.DoesNotThrow(async () => await GitClient.DeleteBranch("feature/document", true).Start().Task);
                    await TaskManager.Wait();

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
        }

        [Test]
        public async Task ShouldDetectBranchCreate()
        {
            Initialize(TestRepoMasterCleanSynchronized);

            using (var repositoryWatcher = CreateRepositoryWatcher(TestRepoMasterCleanSynchronized))
            {
                var watcherAutoResetEvent = new RepositoryWatcherAutoResetEvent();

                var repositoryWatcherListener = Substitute.For<IRepositoryWatcherListener>();
                repositoryWatcherListener.AttachListener(repositoryWatcher, watcherAutoResetEvent);

                repositoryWatcher.Initialize();
                repositoryWatcher.Start();

                try
                {
                    Logger.Trace("Issuing Command");

                    Assert.DoesNotThrow(async () => await GitClient.CreateBranch("feature/document2", "feature/document").Start().Task);
                    await TaskManager.Wait();

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

                    Assert.DoesNotThrow(async () => await GitClient.CreateBranch("feature2/document2", "feature/document").Start().Task);
                    await TaskManager.Wait();

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
        }

        [Test]
        public async Task ShouldDetectChangesToRemotes()
        {
            Initialize(TestRepoMasterCleanSynchronized);

            using (var repositoryWatcher = CreateRepositoryWatcher(TestRepoMasterCleanSynchronized))
            {
                var watcherAutoResetEvent = new RepositoryWatcherAutoResetEvent();

                var repositoryWatcherListener = Substitute.For<IRepositoryWatcherListener>();
                repositoryWatcherListener.AttachListener(repositoryWatcher, watcherAutoResetEvent);

                repositoryWatcher.Initialize();
                repositoryWatcher.Start();

                try
                {
                    Logger.Trace("Issuing Command");

                    Assert.DoesNotThrow(async () => await GitClient.RemoteRemove("origin").Start().Task);
                    await TaskManager.Wait();

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

                    Assert.DoesNotThrow(async () => await GitClient.RemoteAdd("origin", "https://github.com/EvilStanleyGoldman/IOTestsRepo.git").Start().Task);
                    // give the fs watcher a bit of time to catch up
                    await TaskEx.Delay(500);
                    await TaskManager.Wait();

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
        }

        [Test]
        public async Task ShouldDetectGitPull()
        {
            Initialize(TestRepoMasterCleanSynchronized);

            using (var repositoryWatcher = CreateRepositoryWatcher(TestRepoMasterCleanSynchronized))
            {
                var watcherAutoResetEvent = new RepositoryWatcherAutoResetEvent();

                var repositoryWatcherListener = Substitute.For<IRepositoryWatcherListener>();
                repositoryWatcherListener.AttachListener(repositoryWatcher, watcherAutoResetEvent);

                repositoryWatcher.Initialize();
                repositoryWatcher.Start();

                try
                {
                    Logger.Trace("Issuing Command");

                    Assert.DoesNotThrow(async () => await GitClient.Pull("origin", "master").Start().Task);
                    await TaskManager.Wait();

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
        }

        [Test]
        public async Task ShouldDetectGitFetch()
        {
            Initialize(TestRepoMasterCleanUnsynchronized);

            using (var repositoryWatcher = CreateRepositoryWatcher(TestRepoMasterCleanUnsynchronized))
            {
                var watcherAutoResetEvent = new RepositoryWatcherAutoResetEvent();

                var repositoryWatcherListener = Substitute.For<IRepositoryWatcherListener>();
                repositoryWatcherListener.AttachListener(repositoryWatcher, watcherAutoResetEvent, true);

                repositoryWatcher.Initialize();
                repositoryWatcher.Start();

                try
                {
                    Logger.Trace("Issuing Command");

                    Assert.DoesNotThrow(async () => await GitClient.Fetch("origin").Start().Task);
                    await TaskManager.Wait();

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
