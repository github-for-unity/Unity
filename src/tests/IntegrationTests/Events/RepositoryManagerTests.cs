using System;
using System.Collections.Generic;
using FluentAssertions;
using GitHub.Unity;
using NSubstitute;
using NUnit.Framework;
using TestUtils;
using TestUtils.Events;
using System.Threading.Tasks;

namespace IntegrationTests
{
    class RepositoryManagerTests : BaseGitEnvironmentTest
    {
        [Test]
        public async Task ShouldDetectFileChanges()
        {
            Initialize(TestRepoMasterCleanSynchronized);

            var managerAutoResetEvent = new RepositoryManagerAutoResetEvent();

            var repositoryManagerListener = Substitute.For<IRepositoryManagerListener>();
            repositoryManagerListener.AttachListener(RepositoryManager, managerAutoResetEvent);

            var expected = new GitStatus {
                Behind = 1,
                LocalBranch = "master",
                RemoteBranch = "origin/master",
                Entries =
                    new List<GitStatusEntry> {
                        new GitStatusEntry("foobar.txt", TestRepoMasterCleanSynchronized.Combine("foobar.txt"),
                            "foobar.txt", GitFileStatus.Untracked)
                    }
            };

            var result = new GitStatus();
            RepositoryManager.OnRepositoryChanged += status => { result = status; };

            Logger.Trace("Issuing Changes");

            var foobarTxt = TestRepoMasterCleanSynchronized.Combine("foobar.txt");
            foobarTxt.WriteAllText("foobar");

            await TaskManager.Wait();
            // give the fs watcher a bit of time to catch up
            await TaskEx.Delay(200);
            await TaskManager.Wait();

            managerAutoResetEvent.OnRepositoryChanged.WaitOne(TimeSpan.FromSeconds(2)).Should().BeTrue();

            Logger.Trace("Continue test");

            repositoryManagerListener.Received().OnRepositoryChanged(Args.GitStatus);
            result.AssertEqual(expected);

            repositoryManagerListener.ReceivedWithAnyArgs().OnIsBusyChanged(Args.Bool);
            repositoryManagerListener.DidNotReceive().OnActiveBranchChanged();
            repositoryManagerListener.DidNotReceive().OnActiveRemoteChanged();
            repositoryManagerListener.DidNotReceive().OnHeadChanged();
            repositoryManagerListener.DidNotReceive().OnLocalBranchListChanged();
            repositoryManagerListener.DidNotReceive().OnRemoteBranchListChanged();
            repositoryManagerListener.DidNotReceive().OnRemoteOrTrackingChanged();
            repositoryManagerListener.DidNotReceive().OnLocksUpdated(Args.EnumerableGitLock);
        }

        [Test]
        public async Task ShouldAddAndCommitFiles()
        {
            Initialize(TestRepoMasterCleanSynchronized);

            var managerAutoResetEvent = new RepositoryManagerAutoResetEvent();

            var repositoryManagerListener = Substitute.For<IRepositoryManagerListener>();
            repositoryManagerListener.AttachListener(RepositoryManager, managerAutoResetEvent);

            var expectedAfterChanges = new GitStatus {
                Behind = 1,
                LocalBranch = "master",
                RemoteBranch = "origin/master",
                Entries =
                    new List<GitStatusEntry> {
                        new GitStatusEntry("Assets\\TestDocument.txt",
                            TestRepoMasterCleanSynchronized.Combine("Assets", "TestDocument.txt"),
                            "Assets\\TestDocument.txt", GitFileStatus.Modified),
                        new GitStatusEntry("foobar.txt", TestRepoMasterCleanSynchronized.Combine("foobar.txt"),
                            "foobar.txt", GitFileStatus.Untracked)
                    }
            };

            var expectedAfterCommit = new GitStatus {
                Ahead = 1,
                Behind = 1,
                LocalBranch = "master",
                RemoteBranch = "origin/master",
                Entries = new List<GitStatusEntry>()
            };

            var result = new GitStatus();
            RepositoryManager.OnRepositoryChanged += status => {
                result = status;
            };

            Logger.Trace("Issuing Changes");

            var foobarTxt = TestRepoMasterCleanSynchronized.Combine("foobar.txt");
            foobarTxt.WriteAllText("foobar");

            var testDocumentTxt = TestRepoMasterCleanSynchronized.Combine("Assets", "TestDocument.txt");
            testDocumentTxt.WriteAllText("foobar");
            await TaskManager.Wait();

            managerAutoResetEvent.OnRepositoryChanged.WaitOne(TimeSpan.FromSeconds(5000)).Should().BeTrue();

            Logger.Trace("Continue test");

            repositoryManagerListener.Received().OnRepositoryChanged(Args.GitStatus);
            result.AssertEqual(expectedAfterChanges);

            repositoryManagerListener.ReceivedWithAnyArgs().OnIsBusyChanged(Args.Bool);
            repositoryManagerListener.DidNotReceive().OnActiveBranchChanged();
            repositoryManagerListener.DidNotReceive().OnActiveRemoteChanged();
            repositoryManagerListener.DidNotReceive().OnHeadChanged();
            repositoryManagerListener.DidNotReceive().OnLocalBranchListChanged();
            repositoryManagerListener.DidNotReceive().OnRemoteBranchListChanged();
            repositoryManagerListener.DidNotReceive().OnRemoteOrTrackingChanged();
            repositoryManagerListener.DidNotReceive().OnLocksUpdated(Args.EnumerableGitLock);

            repositoryManagerListener.ClearReceivedCalls();

            Logger.Trace("Issuing Command");

            Assert.DoesNotThrow(async () => await RepositoryManager.CommitFiles(new List<string>() { "Assets\\TestDocument.txt", "foobar.txt" }, "IntegrationTest Commit", string.Empty).Start().Task);
            await TaskManager.Wait();

            managerAutoResetEvent.OnActiveBranchChanged.WaitOne(TimeSpan.FromSeconds(5)).Should().BeTrue();

            Logger.Trace("Continue test");

            repositoryManagerListener.Received(1).OnActiveBranchChanged();
            repositoryManagerListener.DidNotReceive().OnActiveRemoteChanged();
            repositoryManagerListener.DidNotReceive().OnHeadChanged();
            repositoryManagerListener.DidNotReceive().OnLocalBranchListChanged();
            repositoryManagerListener.DidNotReceive().OnRemoteBranchListChanged();
            repositoryManagerListener.DidNotReceive().OnRemoteOrTrackingChanged();
            repositoryManagerListener.DidNotReceive().OnLocksUpdated(Args.EnumerableGitLock);
        }

        [Test]
        public async Task ShouldDetectBranchChange()
        {
            Initialize(TestRepoMasterCleanSynchronized);

            var managerAutoResetEvent = new RepositoryManagerAutoResetEvent();

            var repositoryManagerListener = Substitute.For<IRepositoryManagerListener>();
            repositoryManagerListener.AttachListener(RepositoryManager, managerAutoResetEvent);

            var expected = new GitStatus {
                LocalBranch = "feature/document",
                RemoteBranch = "origin/feature/document",
                Entries = new List<GitStatusEntry>()
            };

            var result = new GitStatus();
            RepositoryManager.OnRepositoryChanged += status => { result = status; };

            Logger.Trace("Issuing Command");

            Assert.DoesNotThrow(async () => await RepositoryManager.SwitchBranch("feature/document").Start().Task);
            await TaskManager.Wait();

            // give the fs watcher a bit of time to catch up
            await TaskEx.Delay(100);
            await TaskManager.Wait();

            managerAutoResetEvent.OnActiveBranchChanged.WaitOne(TimeSpan.FromSeconds(3)).Should().BeTrue();
            managerAutoResetEvent.OnRepositoryChanged.WaitOne(TimeSpan.FromSeconds(3)).Should().BeTrue();
            managerAutoResetEvent.OnHeadChanged.WaitOne(TimeSpan.FromSeconds(3)).Should().BeTrue();

            Logger.Trace("Continue test");

            repositoryManagerListener.Received().OnRepositoryChanged(Args.GitStatus);
            result.AssertEqual(expected);
            repositoryManagerListener.ReceivedWithAnyArgs().OnIsBusyChanged(Args.Bool);
            repositoryManagerListener.Received(1).OnActiveBranchChanged();
            repositoryManagerListener.DidNotReceive().OnActiveRemoteChanged();
            repositoryManagerListener.Received(1).OnHeadChanged();
            repositoryManagerListener.DidNotReceive().OnLocalBranchListChanged();
            repositoryManagerListener.DidNotReceive().OnRemoteBranchListChanged();
            repositoryManagerListener.DidNotReceive().OnRemoteOrTrackingChanged();
            repositoryManagerListener.DidNotReceive().OnLocksUpdated(Args.EnumerableGitLock);
        }

        [Test]
        public async Task ShouldDetectBranchDelete()
        {
            Initialize(TestRepoMasterCleanSynchronized);

            var managerAutoResetEvent = new RepositoryManagerAutoResetEvent();

            var repositoryManagerListener = Substitute.For<IRepositoryManagerListener>();
            repositoryManagerListener.AttachListener(RepositoryManager, managerAutoResetEvent);

            Logger.Trace("Issuing Command");

            Assert.DoesNotThrow(async () => await RepositoryManager.DeleteBranch("feature/document", true).Start().Task);
            await TaskManager.Wait();

            managerAutoResetEvent.OnLocalBranchListChanged.WaitOne(TimeSpan.FromSeconds(2)).Should().BeTrue();
            managerAutoResetEvent.OnRemoteOrTrackingChanged.WaitOne(TimeSpan.FromSeconds(2)).Should().BeTrue();

            Logger.Trace("Continue test");

            repositoryManagerListener.DidNotReceive().OnRepositoryChanged(Args.GitStatus);
            repositoryManagerListener.ReceivedWithAnyArgs().OnIsBusyChanged(Args.Bool);
            repositoryManagerListener.DidNotReceive().OnActiveBranchChanged();
            repositoryManagerListener.DidNotReceive().OnActiveRemoteChanged();
            repositoryManagerListener.DidNotReceive().OnHeadChanged();
            repositoryManagerListener.Received(1).OnLocalBranchListChanged();
            repositoryManagerListener.DidNotReceive().OnRemoteBranchListChanged();
            repositoryManagerListener.Received().OnRemoteOrTrackingChanged();
            repositoryManagerListener.DidNotReceive().OnLocksUpdated(Args.EnumerableGitLock);
        }

        [Test]
        public async Task ShouldDetectBranchCreate()
        {
            Initialize(TestRepoMasterCleanSynchronized);

            var managerAutoResetEvent = new RepositoryManagerAutoResetEvent();

            var repositoryManagerListener = Substitute.For<IRepositoryManagerListener>();
            repositoryManagerListener.AttachListener(RepositoryManager, managerAutoResetEvent);

            Logger.Trace("Issuing Command");

            Assert.DoesNotThrow(async () => await RepositoryManager.CreateBranch("feature/document2", "feature/document").Start().Task);
            await TaskManager.Wait();

            managerAutoResetEvent.OnLocalBranchListChanged.WaitOne(TimeSpan.FromSeconds(2)).Should().BeTrue();

            Logger.Trace("Continue test");

            repositoryManagerListener.DidNotReceive().OnRepositoryChanged(Args.GitStatus);
            repositoryManagerListener.ReceivedWithAnyArgs().OnIsBusyChanged(Args.Bool);
            repositoryManagerListener.DidNotReceive().OnActiveBranchChanged();
            repositoryManagerListener.DidNotReceive().OnActiveRemoteChanged();
            repositoryManagerListener.DidNotReceive().OnHeadChanged();
            repositoryManagerListener.Received(1).OnLocalBranchListChanged();
            repositoryManagerListener.DidNotReceive().OnRemoteBranchListChanged();
            repositoryManagerListener.DidNotReceive().OnRemoteOrTrackingChanged();
            repositoryManagerListener.DidNotReceive().OnLocksUpdated(Args.EnumerableGitLock);

            repositoryManagerListener.ClearReceivedCalls();

            Logger.Trace("Issuing Command");

            Assert.DoesNotThrow(async () => await RepositoryManager.CreateBranch("feature2/document2", "feature/document").Start().Task);
            await TaskManager.Wait();

            managerAutoResetEvent.OnLocalBranchListChanged.WaitOne(TimeSpan.FromSeconds(2)).Should().BeTrue();

            Logger.Trace("Continue test");

            repositoryManagerListener.DidNotReceive().OnRepositoryChanged(Args.GitStatus);
            repositoryManagerListener.DidNotReceive().OnActiveBranchChanged();
            repositoryManagerListener.DidNotReceive().OnActiveRemoteChanged();
            repositoryManagerListener.DidNotReceive().OnHeadChanged();
            repositoryManagerListener.Received(1).OnLocalBranchListChanged();
            repositoryManagerListener.DidNotReceive().OnRemoteBranchListChanged();
            repositoryManagerListener.DidNotReceive().OnRemoteOrTrackingChanged();
            repositoryManagerListener.DidNotReceive().OnLocksUpdated(Args.EnumerableGitLock);

            repositoryManagerListener.ClearReceivedCalls();
        }

        [Test]
        public async Task ShouldDetectChangesToRemotes()
        {
            Initialize(TestRepoMasterCleanSynchronized);

            var managerAutoResetEvent = new RepositoryManagerAutoResetEvent();

            var repositoryManagerListener = Substitute.For<IRepositoryManagerListener>();
            repositoryManagerListener.AttachListener(RepositoryManager, managerAutoResetEvent);

            Logger.Trace("Issuing Command");

            Assert.DoesNotThrow(async () => await RepositoryManager.RemoteRemove("origin").Start().Task);
            await TaskManager.Wait();

            managerAutoResetEvent.OnActiveBranchChanged.WaitOne(TimeSpan.FromSeconds(2)).Should().BeTrue();
            managerAutoResetEvent.OnActiveRemoteChanged.WaitOne(TimeSpan.FromSeconds(2)).Should().BeTrue();
            managerAutoResetEvent.OnRemoteOrTrackingChanged.WaitOne(TimeSpan.FromSeconds(2)).Should().BeTrue();

            Logger.Trace("Continue test");

            repositoryManagerListener.DidNotReceive().OnRepositoryChanged(Args.GitStatus);
            repositoryManagerListener.ReceivedWithAnyArgs().OnIsBusyChanged(Args.Bool);
            repositoryManagerListener.Received().OnActiveBranchChanged();
            repositoryManagerListener.Received().OnActiveRemoteChanged();
            repositoryManagerListener.DidNotReceive().OnHeadChanged();
            repositoryManagerListener.DidNotReceive().OnLocalBranchListChanged();
            repositoryManagerListener.DidNotReceive().OnRemoteBranchListChanged();
            repositoryManagerListener.Received().OnRemoteOrTrackingChanged();
            repositoryManagerListener.DidNotReceive().OnLocksUpdated(Args.EnumerableGitLock);

            repositoryManagerListener.ClearReceivedCalls();

            Logger.Trace("Issuing Command");

            Assert.DoesNotThrow(async () => await RepositoryManager.RemoteAdd("origin", "https://github.com/EvilStanleyGoldman/IOTestsRepo.git").Start().Task);
            await TaskManager.Wait();

            managerAutoResetEvent.OnActiveRemoteChanged.WaitOne(TimeSpan.FromSeconds(2)).Should().BeTrue();
            managerAutoResetEvent.OnRemoteOrTrackingChanged.WaitOne(TimeSpan.FromSeconds(2)).Should().BeTrue();

            Logger.Trace("Continue test");

            repositoryManagerListener.DidNotReceive().OnRepositoryChanged(Args.GitStatus);
            repositoryManagerListener.ReceivedWithAnyArgs().OnIsBusyChanged(Args.Bool);
            repositoryManagerListener.DidNotReceive().OnActiveBranchChanged();
            repositoryManagerListener.Received().OnActiveRemoteChanged();
            repositoryManagerListener.DidNotReceive().OnHeadChanged();
            repositoryManagerListener.DidNotReceive().OnLocalBranchListChanged();
            repositoryManagerListener.DidNotReceive().OnRemoteBranchListChanged();
            repositoryManagerListener.Received().OnRemoteOrTrackingChanged();
            repositoryManagerListener.DidNotReceive().OnLocksUpdated(Args.EnumerableGitLock);
        }

        [Test]
        public async Task ShouldDetectGitPull()
        {
            Initialize(TestRepoMasterCleanSynchronized);

            var managerAutoResetEvent = new RepositoryManagerAutoResetEvent();

            var repositoryManagerListener = Substitute.For<IRepositoryManagerListener>();
            repositoryManagerListener.AttachListener(RepositoryManager, managerAutoResetEvent);

            var expected = new GitStatus {
                LocalBranch = "master",
                RemoteBranch = "origin/master",
                Entries = new List<GitStatusEntry>()
            };

            var result = new GitStatus();
            RepositoryManager.OnRepositoryChanged += status => { result = status; };

            Logger.Trace("Issuing Command");

            Assert.DoesNotThrow(async () => await RepositoryManager.Pull("origin", "master").Start().Task);
            await TaskManager.Wait();

            managerAutoResetEvent.OnRepositoryChanged.WaitOne(TimeSpan.FromSeconds(7)).Should().BeTrue();
            managerAutoResetEvent.OnActiveBranchChanged.WaitOne(TimeSpan.FromSeconds(7)).Should().BeTrue();

            WaitForNotBusy(managerAutoResetEvent, 3);
            WaitForNotBusy(managerAutoResetEvent, 3);

            Logger.Trace("Continue test");

            repositoryManagerListener.ReceivedWithAnyArgs().OnIsBusyChanged(Args.Bool);
            RepositoryManager.IsBusy.Should().BeFalse();

            repositoryManagerListener.Received().OnRepositoryChanged(Args.GitStatus);
            result.AssertEqual(expected);

            repositoryManagerListener.Received(1).OnActiveBranchChanged();
            repositoryManagerListener.DidNotReceive().OnActiveRemoteChanged();
            repositoryManagerListener.DidNotReceive().OnHeadChanged();
            repositoryManagerListener.DidNotReceive().OnLocalBranchListChanged();
            repositoryManagerListener.DidNotReceive().OnRemoteBranchListChanged();
            repositoryManagerListener.DidNotReceive().OnRemoteOrTrackingChanged();
            repositoryManagerListener.DidNotReceive().OnLocksUpdated(Args.EnumerableGitLock);
        }

        [Test]
        public async Task ShouldDetectGitFetch()
        {
            Initialize(TestRepoMasterCleanUnsynchronized);

            var managerAutoResetEvent = new RepositoryManagerAutoResetEvent();

            var repositoryManagerListener = Substitute.For<IRepositoryManagerListener>();
            repositoryManagerListener.AttachListener(RepositoryManager, managerAutoResetEvent);

            Logger.Trace("Issuing Command");

            Assert.DoesNotThrow(async () => await RepositoryManager.Fetch("origin").Start().Task);
            await TaskManager.Wait();

            managerAutoResetEvent.OnRemoteBranchListChanged.WaitOne(TimeSpan.FromSeconds(3)).Should().BeTrue();
            managerAutoResetEvent.OnRemoteBranchListChanged.WaitOne(TimeSpan.FromSeconds(3)).Should().BeTrue();

            Logger.Trace("Continue test");

            repositoryManagerListener.DidNotReceive().OnRepositoryChanged(Args.GitStatus);
            repositoryManagerListener.ReceivedWithAnyArgs().OnIsBusyChanged(Args.Bool);
            repositoryManagerListener.DidNotReceive().OnActiveBranchChanged();
            repositoryManagerListener.DidNotReceive().OnActiveRemoteChanged();
            repositoryManagerListener.DidNotReceive().OnHeadChanged();
            repositoryManagerListener.DidNotReceive().OnLocalBranchListChanged();
            repositoryManagerListener.Received(2).OnRemoteBranchListChanged();
            repositoryManagerListener.DidNotReceive().OnRemoteOrTrackingChanged();
            repositoryManagerListener.DidNotReceive().OnLocksUpdated(Args.EnumerableGitLock);
        }

        private void WaitForNotBusy(RepositoryManagerAutoResetEvent managerAutoResetEvent, int seconds = 1)
        {
            if (RepositoryManager.IsBusy)
            {
                Logger.Trace("Waiting for activity", seconds);
                managerAutoResetEvent.OnIsBusyChanged.WaitOne(TimeSpan.FromSeconds(seconds));
            }
        }
    }
}
