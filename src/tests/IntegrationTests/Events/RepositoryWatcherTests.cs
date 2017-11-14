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
        private const int ThreadSleepTimeout = 2000;

        [Test, Category("TimeSensitive")]
        public async Task ShouldDetectFileChanges()
        {
            await Initialize(TestRepoMasterCleanSynchronized);

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
                    repositoryWatcherListener.DidNotReceive().HeadChanged();
                    repositoryWatcherListener.Received().IndexChanged();
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
            await Initialize(TestRepoMasterCleanSynchronized);

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

                    await GitClient.SwitchBranch("feature/document").StartAsAsync();
                    await TaskManager.Wait();

                    watcherAutoResetEvent.HeadChanged.WaitOne(TimeSpan.FromSeconds(2)).Should().BeTrue();
                    watcherAutoResetEvent.IndexChanged.WaitOne(TimeSpan.FromSeconds(2)).Should().BeTrue();
                    watcherAutoResetEvent.RepositoryChanged.WaitOne(TimeSpan.FromSeconds(2)).Should().BeTrue();

                    Logger.Trace("Continue test");

                    repositoryWatcherListener.DidNotReceive().ConfigChanged();
                    repositoryWatcherListener.Received().HeadChanged();
                    repositoryWatcherListener.Received().IndexChanged();
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
            await Initialize(TestRepoMasterCleanSynchronized);

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

                    await GitClient.DeleteBranch("feature/document", true).StartAsAsync();
                    await TaskManager.Wait();

                    watcherAutoResetEvent.ConfigChanged.WaitOne(TimeSpan.FromSeconds(2)).Should().BeTrue();

                    Logger.Trace("Continue test");

                    repositoryWatcherListener.Received(1).ConfigChanged();
                    repositoryWatcherListener.DidNotReceive().HeadChanged();
                    repositoryWatcherListener.DidNotReceive().IndexChanged();
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
            await Initialize(TestRepoMasterCleanSynchronized);

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

                    await GitClient.CreateBranch("feature/document2", "feature/document").StartAsAsync();
                    await TaskManager.Wait();

                    Logger.Trace("Continue test");

                    repositoryWatcherListener.DidNotReceive().ConfigChanged();
                    repositoryWatcherListener.DidNotReceive().HeadChanged();
                    repositoryWatcherListener.DidNotReceive().IndexChanged();
                    repositoryWatcherListener.DidNotReceive().RepositoryChanged();

                    repositoryWatcherListener.ClearReceivedCalls();

                    Logger.Trace("Issuing Command");

                    await GitClient.CreateBranch("feature2/document2", "feature/document").StartAsAsync();
                    await TaskManager.Wait();

                    Logger.Trace("Continue test");

                    repositoryWatcherListener.DidNotReceive().ConfigChanged();
                    repositoryWatcherListener.DidNotReceive().HeadChanged();
                    repositoryWatcherListener.DidNotReceive().IndexChanged();
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
            await Initialize(TestRepoMasterCleanSynchronized);

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

                    await GitClient.RemoteRemove("origin").StartAsAsync();
                    await TaskManager.Wait();

                    watcherAutoResetEvent.ConfigChanged.WaitOne(TimeSpan.FromSeconds(2)).Should().BeTrue();

                    Logger.Trace("Continue test");

                    repositoryWatcherListener.Received().ConfigChanged();
                    repositoryWatcherListener.DidNotReceive().HeadChanged();
                    repositoryWatcherListener.DidNotReceive().IndexChanged();
                    repositoryWatcherListener.DidNotReceive().RepositoryChanged();

                    repositoryWatcherListener.ClearReceivedCalls();
                    watcherAutoResetEvent.ConfigChanged.Reset();

                    Logger.Trace("Issuing 2nd Command");

                    await GitClient.RemoteAdd("origin", "https://github.com/EvilStanleyGoldman/IOTestsRepo.git").StartAsAsync();
                    // give the fs watcher a bit of time to catch up
                    await TaskEx.Delay(500);
                    await TaskManager.Wait();

                    watcherAutoResetEvent.ConfigChanged.WaitOne(TimeSpan.FromSeconds(2)).Should().BeTrue();

                    Logger.Trace("Continue 2nd test");

                    repositoryWatcherListener.Received().ConfigChanged();
                    repositoryWatcherListener.DidNotReceive().HeadChanged();
                    repositoryWatcherListener.DidNotReceive().IndexChanged();
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
            await Initialize(TestRepoMasterCleanSynchronized);

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

                    await GitClient.Pull("origin", "master").StartAsAsync();
                    await TaskManager.Wait();

                    watcherAutoResetEvent.IndexChanged.WaitOne(TimeSpan.FromSeconds(2)).Should().BeTrue();
                    watcherAutoResetEvent.RepositoryChanged.WaitOne(TimeSpan.FromSeconds(2)).Should().BeTrue();

                    Logger.Trace("Continue test");

                    repositoryWatcherListener.DidNotReceive().ConfigChanged();
                    repositoryWatcherListener.DidNotReceive().HeadChanged();
                    repositoryWatcherListener.Received().IndexChanged();
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
            await Initialize(TestRepoMasterCleanUnsynchronized);

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

                    repositoryWatcherListener.DidNotReceive().ConfigChanged();
                    repositoryWatcherListener.DidNotReceive().HeadChanged();
                    repositoryWatcherListener.DidNotReceive().IndexChanged();
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
        void HeadChanged();
        void IndexChanged();
        void RepositoryChanged();
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
            repositoryWatcherListener.DidNotReceive().HeadChanged();
            repositoryWatcherListener.DidNotReceive().IndexChanged();
            repositoryWatcherListener.DidNotReceive().RepositoryChanged();
        }
    }

    class RepositoryWatcherAutoResetEvent
    {
        public AutoResetEvent HeadChanged { get; } = new AutoResetEvent(false);
        public AutoResetEvent ConfigChanged { get; } = new AutoResetEvent(false);
        public AutoResetEvent IndexChanged { get; } = new AutoResetEvent(false);
        public AutoResetEvent RepositoryChanged { get; } = new AutoResetEvent(false);
    }
}
