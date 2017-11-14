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
    [TestFixture, Category("TimeSensitive")]
    class RepositoryWatcherTests : BaseGitEnvironmentTest
    {
        [Test, Category("TimeSensitive")]
        public async Task ShouldDetectFileChanges()
        {
            await Initialize(TestRepoMasterCleanSynchronized, initializeRepository: false);

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

                    watcherAutoResetEvent.RepositoryChanged.WaitOne(TimeSpan.FromSeconds(2)).Should().BeTrue();

                    Logger.Trace("Continue test");

                    repositoryWatcherListener.DidNotReceive().HeadChanged();
                    repositoryWatcherListener.DidNotReceive().ConfigChanged();
                    repositoryWatcherListener.DidNotReceive().RepositoryCommitted();
                    repositoryWatcherListener.DidNotReceive().IndexChanged();
                    repositoryWatcherListener.Received().RepositoryChanged();
                    repositoryWatcherListener.DidNotReceive().LocalBranchesChanged();
                    repositoryWatcherListener.DidNotReceive().RemoteBranchesChanged();
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
            await Initialize(TestRepoMasterCleanSynchronized, initializeRepository: false);

            using (var repositoryWatcher = CreateRepositoryWatcher(TestRepoMasterCleanSynchronized))
            {
                var watcherAutoResetEvent = new RepositoryWatcherAutoResetEvent();

                var repositoryWatcherListener = Substitute.For<IRepositoryWatcherListener>();
                repositoryWatcherListener.AttachListener(repositoryWatcher, watcherAutoResetEvent);

                repositoryWatcher.Initialize();
                repositoryWatcher.Start();
                repositoryWatcher.Stop();

                try
                {
                    Logger.Trace("Issuing Command");

                    await GitClient.SwitchBranch("feature/document").StartAsAsync();
                    await TaskManager.Wait();

                    Logger.Trace("Completed Command");

                    repositoryWatcher.Start();

                    watcherAutoResetEvent.HeadChanged.WaitOne(TimeSpan.FromSeconds(2)).Should().BeTrue();
                    watcherAutoResetEvent.IndexChanged.WaitOne(TimeSpan.FromSeconds(2)).Should().BeTrue();
                    watcherAutoResetEvent.RepositoryChanged.WaitOne(TimeSpan.FromSeconds(2)).Should().BeTrue();

                    Logger.Trace("Continue test");

                    repositoryWatcherListener.Received(1).HeadChanged();
                    repositoryWatcherListener.DidNotReceive().ConfigChanged();
                    repositoryWatcherListener.DidNotReceive().RepositoryCommitted();
                    repositoryWatcherListener.Received(1).IndexChanged();
                    repositoryWatcherListener.Received(1).RepositoryChanged();
                    repositoryWatcherListener.DidNotReceive().LocalBranchesChanged();
                    repositoryWatcherListener.DidNotReceive().RemoteBranchesChanged();
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
            await Initialize(TestRepoMasterCleanSynchronized, initializeRepository: false);

            using (var repositoryWatcher = CreateRepositoryWatcher(TestRepoMasterCleanSynchronized))
            {
                var watcherAutoResetEvent = new RepositoryWatcherAutoResetEvent();

                var repositoryWatcherListener = Substitute.For<IRepositoryWatcherListener>();
                repositoryWatcherListener.AttachListener(repositoryWatcher, watcherAutoResetEvent);

                repositoryWatcher.Initialize();
                repositoryWatcher.Start();
                repositoryWatcher.Stop();

                try
                {
                    Logger.Trace("Issuing Command");

                    await GitClient.DeleteBranch("feature/document", true).StartAsAsync();
                    await TaskManager.Wait();

                    Logger.Trace("Completed Command");

                    repositoryWatcher.Start();

                    watcherAutoResetEvent.ConfigChanged.WaitOne(TimeSpan.FromSeconds(2)).Should().BeTrue();

                    Logger.Trace("Continue test");

                    repositoryWatcherListener.DidNotReceive().HeadChanged();
                    repositoryWatcherListener.Received(1).ConfigChanged();
                    repositoryWatcherListener.DidNotReceive().RepositoryCommitted();
                    repositoryWatcherListener.DidNotReceive().IndexChanged();
                    repositoryWatcherListener.DidNotReceive().RepositoryChanged();
                    repositoryWatcherListener.Received(1).LocalBranchesChanged();
                    repositoryWatcherListener.DidNotReceive().RemoteBranchesChanged();
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
            await Initialize(TestRepoMasterCleanSynchronized, initializeRepository: false);

            using (var repositoryWatcher = CreateRepositoryWatcher(TestRepoMasterCleanSynchronized))
            {
                var watcherAutoResetEvent = new RepositoryWatcherAutoResetEvent();

                var repositoryWatcherListener = Substitute.For<IRepositoryWatcherListener>();
                repositoryWatcherListener.AttachListener(repositoryWatcher, watcherAutoResetEvent);

                repositoryWatcher.Initialize();
                repositoryWatcher.Start();
                repositoryWatcher.Stop();

                try
                {
                    Logger.Trace("Issuing Command");

                    await GitClient.CreateBranch("feature/document2", "feature/document").StartAsAsync();
                    await TaskManager.Wait();

                    Logger.Trace("Continue test");

                    repositoryWatcher.Start();

                    watcherAutoResetEvent.LocalBranchesChanged.WaitOne(TimeSpan.FromSeconds(2)).Should().BeTrue();

                    repositoryWatcherListener.DidNotReceive().HeadChanged();
                    repositoryWatcherListener.DidNotReceive().ConfigChanged();
                    repositoryWatcherListener.DidNotReceive().RepositoryCommitted();
                    repositoryWatcherListener.DidNotReceive().IndexChanged();
                    repositoryWatcherListener.DidNotReceive().RepositoryChanged();
                    repositoryWatcherListener.Received(1).LocalBranchesChanged();
                    repositoryWatcherListener.DidNotReceive().RemoteBranchesChanged();
                    repositoryWatcherListener.ClearReceivedCalls();

                    repositoryWatcher.Stop();

                    Logger.Trace("Issuing Command");

                    await GitClient.CreateBranch("feature2/document2", "feature/document").StartAsAsync();
                    await TaskManager.Wait();

                    Logger.Trace("Continue test");

                    repositoryWatcher.Start();

                    watcherAutoResetEvent.LocalBranchesChanged.WaitOne(TimeSpan.FromSeconds(2)).Should().BeTrue();

                    repositoryWatcherListener.DidNotReceive().HeadChanged();
                    repositoryWatcherListener.DidNotReceive().ConfigChanged();
                    repositoryWatcherListener.DidNotReceive().RepositoryCommitted();
                    repositoryWatcherListener.DidNotReceive().IndexChanged();
                    repositoryWatcherListener.DidNotReceive().RepositoryChanged();
                    repositoryWatcherListener.Received(1).LocalBranchesChanged();
                    repositoryWatcherListener.DidNotReceive().RemoteBranchesChanged();
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
            await Initialize(TestRepoMasterCleanSynchronized, initializeRepository: false);

            using (var repositoryWatcher = CreateRepositoryWatcher(TestRepoMasterCleanSynchronized))
            {
                var watcherAutoResetEvent = new RepositoryWatcherAutoResetEvent();

                var repositoryWatcherListener = Substitute.For<IRepositoryWatcherListener>();
                repositoryWatcherListener.AttachListener(repositoryWatcher, watcherAutoResetEvent);

                repositoryWatcher.Initialize();
                repositoryWatcher.Start();
                repositoryWatcher.Stop();

                try
                {
                    Logger.Trace("Issuing Command");

                    await GitClient.RemoteRemove("origin").StartAsAsync();
                    await TaskManager.Wait();

                    Logger.Trace("Continue test");

                    repositoryWatcher.Start();
                    watcherAutoResetEvent.ConfigChanged.WaitOne(TimeSpan.FromSeconds(2)).Should().BeTrue();
                    watcherAutoResetEvent.RemoteBranchesChanged.WaitOne(TimeSpan.FromSeconds(2)).Should().BeTrue();

                    repositoryWatcherListener.DidNotReceive().HeadChanged();
                    repositoryWatcherListener.Received(1).ConfigChanged();
                    repositoryWatcherListener.DidNotReceive().RepositoryCommitted();
                    repositoryWatcherListener.DidNotReceive().IndexChanged();
                    repositoryWatcherListener.DidNotReceive().RepositoryChanged();
                    repositoryWatcherListener.DidNotReceive().LocalBranchesChanged();
                    repositoryWatcherListener.Received(1).RemoteBranchesChanged();
                    repositoryWatcherListener.ClearReceivedCalls();

                    repositoryWatcher.Stop();

                    Logger.Trace("Issuing 2nd Command");

                    await GitClient.RemoteAdd("origin", "https://github.com/EvilStanleyGoldman/IOTestsRepo.git").StartAsAsync();
                    await TaskManager.Wait();

                    Logger.Trace("Continue 2nd test");

                    repositoryWatcher.Start();
                    watcherAutoResetEvent.ConfigChanged.WaitOne(TimeSpan.FromSeconds(2)).Should().BeTrue();
                    watcherAutoResetEvent.RemoteBranchesChanged.WaitOne(TimeSpan.FromSeconds(2)).Should().BeTrue();

                    repositoryWatcherListener.DidNotReceive().HeadChanged();
                    repositoryWatcherListener.Received(1).ConfigChanged();
                    repositoryWatcherListener.DidNotReceive().RepositoryCommitted();
                    repositoryWatcherListener.DidNotReceive().IndexChanged();
                    repositoryWatcherListener.DidNotReceive().RepositoryChanged();
                    repositoryWatcherListener.DidNotReceive().LocalBranchesChanged();
                    repositoryWatcherListener.Received(1).RemoteBranchesChanged();
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
            await Initialize(TestRepoMasterCleanSynchronized, initializeRepository: false);

            using (var repositoryWatcher = CreateRepositoryWatcher(TestRepoMasterCleanSynchronized))
            {
                var watcherAutoResetEvent = new RepositoryWatcherAutoResetEvent();

                var repositoryWatcherListener = Substitute.For<IRepositoryWatcherListener>();
                repositoryWatcherListener.AttachListener(repositoryWatcher, watcherAutoResetEvent);

                repositoryWatcher.Initialize();
                repositoryWatcher.Start();
                repositoryWatcher.Stop();

                try
                {
                    Logger.Trace("Issuing Command");

                    await GitClient.Pull("origin", "master").StartAsAsync();
                    await TaskManager.Wait();

                    Logger.Trace("Continue test");

                    repositoryWatcher.Start();

                    watcherAutoResetEvent.IndexChanged.WaitOne(TimeSpan.FromSeconds(2)).Should().BeTrue();
                    watcherAutoResetEvent.RepositoryChanged.WaitOne(TimeSpan.FromSeconds(2)).Should().BeTrue();

                    repositoryWatcherListener.DidNotReceive().HeadChanged();
                    repositoryWatcherListener.DidNotReceive().ConfigChanged();
                    repositoryWatcherListener.DidNotReceive().RepositoryCommitted();
                    repositoryWatcherListener.Received(1).IndexChanged();
                    repositoryWatcherListener.Received(1).RepositoryChanged();
                    repositoryWatcherListener.Received(1).LocalBranchesChanged();
                    repositoryWatcherListener.DidNotReceive().RemoteBranchesChanged();
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
            await Initialize(TestRepoMasterCleanUnsynchronized, initializeRepository: false);

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

                    await GitClient.Fetch("origin").StartAsAsync();
                    await TaskManager.Wait();

                    Logger.Trace("Continue test");

                    repositoryWatcherListener.AssertDidNotReceiveAnyCalls();
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
        void HeadChanged();
        void IndexChanged();
        void ConfigChanged();
        void RepositoryCommitted();
        void RepositoryChanged();
        void LocalBranchesChanged();
        void RemoteBranchesChanged();
    }

    static class RepositoryWatcherListenerExtensions
    {
        public static void AttachListener(this IRepositoryWatcherListener listener, IRepositoryWatcher repositoryWatcher, RepositoryWatcherAutoResetEvent autoResetEvent = null, bool trace = false)
        {
            var logger = trace ? Logging.GetLogger<IRepositoryWatcherListener>() : null;

            repositoryWatcher.HeadChanged += () =>
            {
                logger?.Trace("HeadChanged");
                listener.HeadChanged();
                autoResetEvent?.HeadChanged.Set();
            };

            repositoryWatcher.IndexChanged += () =>
            {
                logger?.Trace("IndexChanged");
                listener.IndexChanged();
                autoResetEvent?.IndexChanged.Set();
            };

            repositoryWatcher.ConfigChanged += () =>
            {
                logger?.Trace("ConfigChanged");
                listener.ConfigChanged();
                autoResetEvent?.ConfigChanged.Set();
            };

            repositoryWatcher.RepositoryCommitted += () =>
            {
                logger?.Trace("ConfigChanged");
                listener.RepositoryCommitted();
                autoResetEvent?.RepositoryCommitted.Set();
            };

            repositoryWatcher.RepositoryChanged += () =>
            {
                logger?.Trace("RepositoryChanged");
                listener.RepositoryChanged();
                autoResetEvent?.RepositoryChanged.Set();
            };

            repositoryWatcher.LocalBranchesChanged += () =>
            {
                logger?.Trace("LocalBranchesChanged");
                listener.LocalBranchesChanged();
                autoResetEvent?.LocalBranchesChanged.Set();
            };

            repositoryWatcher.RemoteBranchesChanged += () =>
            {
                logger?.Trace("RemoteBranchesChanged");
                listener.RemoteBranchesChanged();
                autoResetEvent?.RemoteBranchesChanged.Set();
            };
        }

        public static void AssertDidNotReceiveAnyCalls(this IRepositoryWatcherListener repositoryWatcherListener)
        {
            repositoryWatcherListener.DidNotReceive().HeadChanged();
            repositoryWatcherListener.DidNotReceive().ConfigChanged();
            repositoryWatcherListener.DidNotReceive().RepositoryCommitted();
            repositoryWatcherListener.DidNotReceive().IndexChanged();
            repositoryWatcherListener.DidNotReceive().RepositoryChanged();
            repositoryWatcherListener.DidNotReceive().LocalBranchesChanged();
            repositoryWatcherListener.DidNotReceive().RemoteBranchesChanged();
        }
    }

    class RepositoryWatcherAutoResetEvent
    {
        public AutoResetEvent HeadChanged { get; } = new AutoResetEvent(false);
        public AutoResetEvent ConfigChanged { get; } = new AutoResetEvent(false);
        public AutoResetEvent RepositoryCommitted { get; } = new AutoResetEvent(false);
        public AutoResetEvent IndexChanged { get; } = new AutoResetEvent(false);
        public AutoResetEvent RepositoryChanged { get; } = new AutoResetEvent(false);
        public AutoResetEvent LocalBranchesChanged { get; } = new AutoResetEvent(false);
        public AutoResetEvent RemoteBranchesChanged { get; } = new AutoResetEvent(false);
    }
}
