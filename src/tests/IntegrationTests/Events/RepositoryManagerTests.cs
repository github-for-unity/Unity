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
    [TestFixture]
    class RepositoryManagerTests : BaseGitEnvironmentTest
    {
        private RepositoryManagerEvents repositoryManagerEvents;

        public override void OnSetup()
        {
            base.OnSetup();
            repositoryManagerEvents = new RepositoryManagerEvents();
        }

        [Test]
        public async Task ShouldDoNothingOnInitialize()
        {
            await Initialize(TestRepoMasterCleanSynchronized);

            var repositoryManagerListener = Substitute.For<IRepositoryManagerListener>();
            repositoryManagerListener.AttachListener(RepositoryManager, repositoryManagerEvents);

            RepositoryManager.WaitForEvents();
            WaitForNotBusy(repositoryManagerEvents, 2);

            repositoryManagerListener.AssertDidNotReceiveAnyCalls();
        }

        [Test]
        public async Task ShouldDetectFileChanges()
        {
            await Initialize(TestRepoMasterCleanSynchronized);

            var repositoryManagerListener = Substitute.For<IRepositoryManagerListener>();
            repositoryManagerListener.AttachListener(RepositoryManager, repositoryManagerEvents);

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
            Environment.Repository.OnStatusChanged += status => { result = status; };

            var foobarTxt = TestRepoMasterCleanSynchronized.Combine("foobar.txt");
            foobarTxt.WriteAllText("foobar");

            await TaskManager.Wait();
            RepositoryManager.WaitForEvents();
            WaitForNotBusy(repositoryManagerEvents);
            WaitForStatusUpdated(repositoryManagerEvents);

            repositoryManagerListener.Received().OnIsBusyChanged(Args.Bool);
            repositoryManagerListener.Received().OnStatusUpdated(Args.GitStatus);
            repositoryManagerListener.DidNotReceive().OnLocksUpdated(Args.EnumerableGitLock);
            repositoryManagerListener.DidNotReceive().OnHeadUpdated(Args.String);
            repositoryManagerListener.DidNotReceive().OnLocalBranchListUpdated(Arg.Any<Dictionary<string, ConfigBranch>>());
            repositoryManagerListener.DidNotReceive().OnRemoteBranchListUpdated(Arg.Any<Dictionary<string, Dictionary<string, ConfigBranch>>>());
            repositoryManagerListener.DidNotReceive().OnLocalBranchUpdated(Args.String);
            repositoryManagerListener.DidNotReceive().OnLocalBranchAdded(Args.String);
            repositoryManagerListener.DidNotReceive().OnLocalBranchRemoved(Args.String);
            repositoryManagerListener.DidNotReceive().OnRemoteBranchAdded(Args.String, Args.String);
            repositoryManagerListener.DidNotReceive().OnRemoteBranchRemoved(Args.String, Args.String);

            result.AssertEqual(expected);
        }

        [Test]
        public async Task ShouldAddAndCommitFiles()
        {
            await Initialize(TestRepoMasterCleanSynchronized);

            var repositoryManagerListener = Substitute.For<IRepositoryManagerListener>();
            repositoryManagerListener.AttachListener(RepositoryManager, repositoryManagerEvents);

            var expectedLocalBranch = "master";
            var expectedAfterChanges = new GitStatus {
                Behind = 1,
                LocalBranch = expectedLocalBranch,
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

            var result = new GitStatus();
            RepositoryManager.OnStatusUpdated += status => { result = status; };

            var foobarTxt = TestRepoMasterCleanSynchronized.Combine("foobar.txt");
            foobarTxt.WriteAllText("foobar");

            var testDocumentTxt = TestRepoMasterCleanSynchronized.Combine("Assets", "TestDocument.txt");
            testDocumentTxt.WriteAllText("foobar");

            await TaskManager.Wait();

            //Intentionally wait two cycles, in case the first cycle did not pick up all events
            RepositoryManager.WaitForEvents();
            WaitForNotBusy(repositoryManagerEvents);
            WaitForStatusUpdated(repositoryManagerEvents);

            RepositoryManager.WaitForEvents();
            WaitForNotBusy(repositoryManagerEvents);
            WaitForStatusUpdated(repositoryManagerEvents);

            repositoryManagerListener.Received().OnIsBusyChanged(Args.Bool);
            repositoryManagerListener.Received().OnStatusUpdated(Args.GitStatus);
            repositoryManagerListener.DidNotReceive().OnLocksUpdated(Args.EnumerableGitLock);
            repositoryManagerListener.DidNotReceive().OnHeadUpdated(Args.String);
            repositoryManagerListener.DidNotReceive().OnLocalBranchListUpdated(Arg.Any<Dictionary<string, ConfigBranch>>());
            repositoryManagerListener.DidNotReceive().OnRemoteBranchListUpdated(Arg.Any<Dictionary<string, Dictionary<string, ConfigBranch>>>());
            repositoryManagerListener.DidNotReceive().OnLocalBranchUpdated(Args.String);
            repositoryManagerListener.DidNotReceive().OnLocalBranchAdded(Args.String);
            repositoryManagerListener.DidNotReceive().OnLocalBranchRemoved(Args.String);
            repositoryManagerListener.DidNotReceive().OnRemoteBranchAdded(Args.String, Args.String);
            repositoryManagerListener.DidNotReceive().OnRemoteBranchRemoved(Args.String, Args.String);

            result.AssertEqual(expectedAfterChanges);

            repositoryManagerListener.ClearReceivedCalls();
            repositoryManagerEvents.Reset();

            await RepositoryManager
                .CommitFiles(new List<string> { "Assets\\TestDocument.txt", "foobar.txt" }, "IntegrationTest Commit", string.Empty)
                .StartAsAsync();

            await TaskManager.Wait();
            RepositoryManager.WaitForEvents();
            WaitForNotBusy(repositoryManagerEvents);

            repositoryManagerListener.Received().OnIsBusyChanged(Args.Bool);
            repositoryManagerListener.DidNotReceive().OnStatusUpdated(Args.GitStatus);
            repositoryManagerListener.DidNotReceive().OnLocksUpdated(Args.EnumerableGitLock);
            repositoryManagerListener.DidNotReceive().OnHeadUpdated(Args.String);
            repositoryManagerListener.DidNotReceive().OnLocalBranchListUpdated(Arg.Any<Dictionary<string, ConfigBranch>>());
            repositoryManagerListener.DidNotReceive().OnRemoteBranchListUpdated(Arg.Any<Dictionary<string, Dictionary<string, ConfigBranch>>>());
            repositoryManagerListener.Received().OnLocalBranchUpdated(expectedLocalBranch);
            repositoryManagerListener.DidNotReceive().OnLocalBranchAdded(Args.String);
            repositoryManagerListener.DidNotReceive().OnLocalBranchRemoved(Args.String);
            repositoryManagerListener.DidNotReceive().OnRemoteBranchAdded(Args.String, Args.String);
            repositoryManagerListener.DidNotReceive().OnRemoteBranchRemoved(Args.String, Args.String);
        }

        [Test]
        public async Task ShouldAddAndCommitAllFiles()
        {
            await Initialize(TestRepoMasterCleanSynchronized);

            var repositoryManagerListener = Substitute.For<IRepositoryManagerListener>();
            repositoryManagerListener.AttachListener(RepositoryManager, repositoryManagerEvents);

            var expectedLocalBranch = "master";
            var expectedAfterChanges = new GitStatus {
                Behind = 1,
                LocalBranch = expectedLocalBranch,
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

            var result = new GitStatus();
            RepositoryManager.OnStatusUpdated += status => { result = status; };

            var foobarTxt = TestRepoMasterCleanSynchronized.Combine("foobar.txt");
            foobarTxt.WriteAllText("foobar");

            var testDocumentTxt = TestRepoMasterCleanSynchronized.Combine("Assets", "TestDocument.txt");
            testDocumentTxt.WriteAllText("foobar");

            await TaskManager.Wait();
            RepositoryManager.WaitForEvents();
            WaitForNotBusy(repositoryManagerEvents);
            WaitForStatusUpdated(repositoryManagerEvents);

            repositoryManagerListener.Received().OnIsBusyChanged(Args.Bool);
            repositoryManagerListener.Received().OnStatusUpdated(Args.GitStatus);
            repositoryManagerListener.DidNotReceive().OnLocksUpdated(Args.EnumerableGitLock);
            repositoryManagerListener.DidNotReceive().OnHeadUpdated(Args.String);
            repositoryManagerListener.DidNotReceive().OnLocalBranchListUpdated(Arg.Any<Dictionary<string, ConfigBranch>>());
            repositoryManagerListener.DidNotReceive().OnRemoteBranchListUpdated(Arg.Any<Dictionary<string, Dictionary<string, ConfigBranch>>>());
            repositoryManagerListener.DidNotReceive().OnLocalBranchUpdated(Args.String);
            repositoryManagerListener.DidNotReceive().OnLocalBranchAdded(Args.String);
            repositoryManagerListener.DidNotReceive().OnLocalBranchRemoved(Args.String);
            repositoryManagerListener.DidNotReceive().OnRemoteBranchAdded(Args.String, Args.String);
            repositoryManagerListener.DidNotReceive().OnRemoteBranchRemoved(Args.String, Args.String);

            result.AssertEqual(expectedAfterChanges);

            repositoryManagerListener.ClearReceivedCalls();
            repositoryManagerEvents.Reset();

            await RepositoryManager
                .CommitAllFiles("IntegrationTest Commit", string.Empty)
                .StartAsAsync();

            await TaskManager.Wait();
            RepositoryManager.WaitForEvents();
            WaitForNotBusy(repositoryManagerEvents);

            repositoryManagerListener.Received().OnIsBusyChanged(Args.Bool);
            repositoryManagerListener.DidNotReceive().OnStatusUpdated(Args.GitStatus);
            repositoryManagerListener.DidNotReceive().OnLocksUpdated(Args.EnumerableGitLock);
            repositoryManagerListener.DidNotReceive().OnHeadUpdated(Args.String);
            repositoryManagerListener.DidNotReceive().OnLocalBranchListUpdated(Arg.Any<Dictionary<string, ConfigBranch>>());
            repositoryManagerListener.DidNotReceive().OnRemoteBranchListUpdated(Arg.Any<Dictionary<string, Dictionary<string, ConfigBranch>>>());
            repositoryManagerListener.Received().OnLocalBranchUpdated(expectedLocalBranch);
            repositoryManagerListener.DidNotReceive().OnLocalBranchAdded(Args.String);
            repositoryManagerListener.DidNotReceive().OnLocalBranchRemoved(Args.String);
            repositoryManagerListener.DidNotReceive().OnRemoteBranchAdded(Args.String, Args.String);
            repositoryManagerListener.DidNotReceive().OnRemoteBranchRemoved(Args.String, Args.String);
        }

        [Test]
        public async Task ShouldDetectBranchChange()
        {
            await Initialize(TestRepoMasterCleanSynchronized);

            var repositoryManagerListener = Substitute.For<IRepositoryManagerListener>();
            repositoryManagerListener.AttachListener(RepositoryManager, repositoryManagerEvents);

            var expectedLocalBranch = "feature/document";
            var expected = new GitStatus {
                LocalBranch = expectedLocalBranch,
                RemoteBranch = "origin/feature/document",
                Entries = new List<GitStatusEntry>()
            };

            var result = new GitStatus();
            RepositoryManager.OnStatusUpdated += status => { result = status; };

            Logger.Trace("Starting test");

            await RepositoryManager.SwitchBranch(expectedLocalBranch).StartAsAsync();

            await TaskManager.Wait();
            RepositoryManager.WaitForEvents();
            WaitForNotBusy(repositoryManagerEvents);
            WaitForStatusUpdated(repositoryManagerEvents);

            repositoryManagerListener.Received().OnIsBusyChanged(Args.Bool);
            repositoryManagerListener.Received().OnStatusUpdated(Args.GitStatus);
            repositoryManagerListener.DidNotReceive().OnLocksUpdated(Args.EnumerableGitLock);
            repositoryManagerListener.Received().OnHeadUpdated(Args.String);
            repositoryManagerListener.DidNotReceive().OnLocalBranchListUpdated(Arg.Any<Dictionary<string, ConfigBranch>>());
            repositoryManagerListener.DidNotReceive().OnRemoteBranchListUpdated(Arg.Any<Dictionary<string, Dictionary<string, ConfigBranch>>>());
            repositoryManagerListener.DidNotReceive().OnLocalBranchUpdated(Args.String);
            repositoryManagerListener.DidNotReceive().OnLocalBranchAdded(Args.String);
            repositoryManagerListener.DidNotReceive().OnLocalBranchRemoved(Args.String);
            repositoryManagerListener.DidNotReceive().OnRemoteBranchAdded(Args.String, Args.String);
            repositoryManagerListener.DidNotReceive().OnRemoteBranchRemoved(Args.String, Args.String);

            result.AssertEqual(expected);
        }

        [Test]
        public async Task ShouldDetectBranchDelete()
        {
            await Initialize(TestRepoMasterCleanSynchronized);

            var repositoryManagerListener = Substitute.For<IRepositoryManagerListener>();
            repositoryManagerListener.AttachListener(RepositoryManager, repositoryManagerEvents);

            var deletedBranch = "feature/document";
            await RepositoryManager.DeleteBranch(deletedBranch, true).StartAsAsync();
            await TaskManager.Wait();
            RepositoryManager.WaitForEvents();
            WaitForNotBusy(repositoryManagerEvents);

            repositoryManagerListener.Received().OnIsBusyChanged(Args.Bool);
            repositoryManagerListener.DidNotReceive().OnStatusUpdated(Args.GitStatus);
            repositoryManagerListener.DidNotReceive().OnLocksUpdated(Args.EnumerableGitLock);
            repositoryManagerListener.DidNotReceive().OnHeadUpdated(Args.String);
            repositoryManagerListener.Received().OnLocalBranchListUpdated(Arg.Any<Dictionary<string, ConfigBranch>>());
            repositoryManagerListener.Received().OnRemoteBranchListUpdated(Arg.Any<Dictionary<string, Dictionary<string, ConfigBranch>>>());
            repositoryManagerListener.DidNotReceive().OnLocalBranchUpdated(Args.String);
            repositoryManagerListener.DidNotReceive().OnLocalBranchAdded(Args.String);
            repositoryManagerListener.Received().OnLocalBranchRemoved(deletedBranch);
            repositoryManagerListener.DidNotReceive().OnRemoteBranchAdded(Args.String, Args.String);
            repositoryManagerListener.DidNotReceive().OnRemoteBranchRemoved(Args.String, Args.String);
        }

        [Test]
        public async Task ShouldDetectBranchCreate()
        {
            await Initialize(TestRepoMasterCleanSynchronized);

            var repositoryManagerListener = Substitute.For<IRepositoryManagerListener>();
            repositoryManagerListener.AttachListener(RepositoryManager, repositoryManagerEvents);

            var createdBranch1 = "feature/document2";
            await RepositoryManager.CreateBranch(createdBranch1, "feature/document").StartAsAsync();
            await TaskManager.Wait();
            RepositoryManager.WaitForEvents();
            WaitForNotBusy(repositoryManagerEvents);

            repositoryManagerListener.Received().OnIsBusyChanged(Args.Bool);
            repositoryManagerListener.DidNotReceive().OnStatusUpdated(Args.GitStatus);
            repositoryManagerListener.DidNotReceive().OnLocksUpdated(Args.EnumerableGitLock);
            repositoryManagerListener.DidNotReceive().OnHeadUpdated(Args.String);
            repositoryManagerListener.DidNotReceive().OnLocalBranchListUpdated(Arg.Any<Dictionary<string, ConfigBranch>>());
            repositoryManagerListener.DidNotReceive().OnRemoteBranchListUpdated(Arg.Any<Dictionary<string, Dictionary<string, ConfigBranch>>>());
            repositoryManagerListener.DidNotReceive().OnLocalBranchUpdated(Args.String);
            repositoryManagerListener.Received().OnLocalBranchAdded(createdBranch1);
            repositoryManagerListener.DidNotReceive().OnLocalBranchRemoved(Args.String);
            repositoryManagerListener.DidNotReceive().OnRemoteBranchAdded(Args.String, Args.String);
            repositoryManagerListener.DidNotReceive().OnRemoteBranchRemoved(Args.String, Args.String);

            repositoryManagerListener.ClearReceivedCalls();
            repositoryManagerEvents.Reset();

            var createdBranch2 = "feature2/document2";
            await RepositoryManager.CreateBranch(createdBranch2, "feature/document").StartAsAsync();
            await TaskManager.Wait();
            RepositoryManager.WaitForEvents();
            WaitForNotBusy(repositoryManagerEvents);

            repositoryManagerListener.Received().OnIsBusyChanged(Args.Bool);
            repositoryManagerListener.DidNotReceive().OnStatusUpdated(Args.GitStatus);
            repositoryManagerListener.DidNotReceive().OnLocksUpdated(Args.EnumerableGitLock);
            repositoryManagerListener.DidNotReceive().OnHeadUpdated(Args.String);
            repositoryManagerListener.DidNotReceive().OnLocalBranchListUpdated(Arg.Any<Dictionary<string, ConfigBranch>>());
            repositoryManagerListener.DidNotReceive().OnRemoteBranchListUpdated(Arg.Any<Dictionary<string, Dictionary<string, ConfigBranch>>>());
            repositoryManagerListener.DidNotReceive().OnLocalBranchUpdated(Args.String);
            repositoryManagerListener.Received().OnLocalBranchAdded(createdBranch2);
            repositoryManagerListener.DidNotReceive().OnLocalBranchRemoved(Args.String);
            repositoryManagerListener.DidNotReceive().OnRemoteBranchAdded(Args.String, Args.String);
            repositoryManagerListener.DidNotReceive().OnRemoteBranchRemoved(Args.String, Args.String);
        }

        [Test]
        public async Task ShouldDetectChangesToRemotes()
        {
            await Initialize(TestRepoMasterCleanSynchronized);

            var repositoryManagerListener = Substitute.For<IRepositoryManagerListener>();
            repositoryManagerListener.AttachListener(RepositoryManager, repositoryManagerEvents);
            
            await RepositoryManager.RemoteRemove("origin").StartAsAsync();
            await TaskManager.Wait();
            RepositoryManager.WaitForEvents();
            WaitForNotBusy(repositoryManagerEvents);

            repositoryManagerListener.Received().OnIsBusyChanged(Args.Bool);
            repositoryManagerListener.DidNotReceive().OnStatusUpdated(Args.GitStatus);
            repositoryManagerListener.DidNotReceive().OnLocksUpdated(Args.EnumerableGitLock);
            repositoryManagerListener.DidNotReceive().OnHeadUpdated(Args.String);
            repositoryManagerListener.Received().OnLocalBranchListUpdated(Arg.Any<Dictionary<string, ConfigBranch>>());
            repositoryManagerListener.Received().OnRemoteBranchListUpdated(Arg.Any<Dictionary<string, Dictionary<string, ConfigBranch>>>());
            repositoryManagerListener.DidNotReceive().OnLocalBranchUpdated(Args.String);
            repositoryManagerListener.DidNotReceive().OnLocalBranchAdded(Args.String);
            repositoryManagerListener.DidNotReceive().OnLocalBranchRemoved(Args.String);
            repositoryManagerListener.DidNotReceive().OnRemoteBranchAdded(Args.String, Args.String);
            repositoryManagerListener.Received().OnRemoteBranchRemoved(Args.String, Args.String);

            repositoryManagerListener.ClearReceivedCalls();
            repositoryManagerEvents.Reset();

            await RepositoryManager.RemoteAdd("origin", "https://github.com/EvilShana/IOTestsRepo.git").StartAsAsync();
            await TaskManager.Wait();
            RepositoryManager.WaitForEvents();
            WaitForNotBusy(repositoryManagerEvents);

            repositoryManagerListener.Received().OnIsBusyChanged(Args.Bool);
            repositoryManagerListener.DidNotReceive().OnStatusUpdated(Args.GitStatus);
            repositoryManagerListener.DidNotReceive().OnLocksUpdated(Args.EnumerableGitLock);
            repositoryManagerListener.DidNotReceive().OnHeadUpdated(Args.String);
            repositoryManagerListener.Received().OnLocalBranchListUpdated(Arg.Any<Dictionary<string, ConfigBranch>>());
            repositoryManagerListener.Received().OnRemoteBranchListUpdated(Arg.Any<Dictionary<string, Dictionary<string, ConfigBranch>>>());
            repositoryManagerListener.DidNotReceive().OnLocalBranchUpdated(Args.String);
            repositoryManagerListener.DidNotReceive().OnLocalBranchAdded(Args.String);
            repositoryManagerListener.DidNotReceive().OnLocalBranchRemoved(Args.String);
            repositoryManagerListener.DidNotReceive().OnRemoteBranchAdded(Args.String, Args.String);
            repositoryManagerListener.DidNotReceive().OnRemoteBranchRemoved(Args.String, Args.String);
        }

        [Test]
        public async Task ShouldDetectChangesToRemotesWhenSwitchingBranches()
        {
            var expectedCloneUrl = "https://github.com/EvilStanleyGoldman/IOTestsRepo.git";

            await Initialize(TestRepoMasterTwoRemotes);

            var repositoryManagerListener = Substitute.For<IRepositoryManagerListener>();
            repositoryManagerListener.AttachListener(RepositoryManager, repositoryManagerEvents);

            await RepositoryManager.CreateBranch("branch2", "another/master")
                .StartAsAsync();

            await TaskManager.Wait();
            RepositoryManager.WaitForEvents();
            WaitForNotBusy(repositoryManagerEvents);

            repositoryManagerListener.Received().OnIsBusyChanged(Args.Bool);
            repositoryManagerListener.DidNotReceive().OnStatusUpdated(Args.GitStatus);
            repositoryManagerListener.DidNotReceive().OnLocksUpdated(Args.EnumerableGitLock);
            repositoryManagerListener.DidNotReceive().OnHeadUpdated(Args.String);
            repositoryManagerListener.Received().OnLocalBranchListUpdated(Arg.Any<Dictionary<string, ConfigBranch>>());
            repositoryManagerListener.Received().OnRemoteBranchListUpdated(Arg.Any<Dictionary<string, Dictionary<string, ConfigBranch>>>());
            repositoryManagerListener.DidNotReceive().OnLocalBranchUpdated(Args.String);
            repositoryManagerListener.Received().OnLocalBranchAdded(Args.String);
            repositoryManagerListener.DidNotReceive().OnLocalBranchRemoved(Args.String);
            repositoryManagerListener.DidNotReceive().OnRemoteBranchAdded(Args.String, Args.String);
            repositoryManagerListener.DidNotReceive().OnRemoteBranchRemoved(Args.String, Args.String);

            repositoryManagerListener.ClearReceivedCalls();
            repositoryManagerEvents.Reset();

            await RepositoryManager.SwitchBranch("branch2")
                                   .StartAsAsync();

            await TaskManager.Wait();
            RepositoryManager.WaitForEvents();
            WaitForNotBusy(repositoryManagerEvents);
            WaitForHeadUpdated(repositoryManagerEvents);

            repositoryManagerListener.Received().OnIsBusyChanged(Args.Bool);
            repositoryManagerListener.DidNotReceive().OnStatusUpdated(Args.GitStatus);
            repositoryManagerListener.DidNotReceive().OnLocksUpdated(Args.EnumerableGitLock);
            repositoryManagerListener.Received().OnHeadUpdated(Args.String);
            repositoryManagerListener.DidNotReceive().OnLocalBranchListUpdated(Arg.Any<Dictionary<string, ConfigBranch>>());
            repositoryManagerListener.DidNotReceive().OnRemoteBranchListUpdated(Arg.Any<Dictionary<string, Dictionary<string, ConfigBranch>>>());
            repositoryManagerListener.DidNotReceive().OnLocalBranchUpdated(Args.String);
            repositoryManagerListener.DidNotReceive().OnLocalBranchAdded(Args.String);
            repositoryManagerListener.DidNotReceive().OnLocalBranchRemoved(Args.String);
            repositoryManagerListener.DidNotReceive().OnRemoteBranchAdded(Args.String, Args.String);
            repositoryManagerListener.DidNotReceive().OnRemoteBranchRemoved(Args.String, Args.String);
        }

        [Test]
        public async Task ShouldDetectGitPull()
        {
            await Initialize(TestRepoMasterCleanSynchronized);

            var repositoryManagerListener = Substitute.For<IRepositoryManagerListener>();
            repositoryManagerListener.AttachListener(RepositoryManager, repositoryManagerEvents);

            var expected = new GitStatus {
                LocalBranch = "master",
                RemoteBranch = "origin/master",
                Entries = new List<GitStatusEntry>()
            };

            var result = new GitStatus();
            RepositoryManager.OnStatusUpdated += status => { result = status; };

            await RepositoryManager.Pull("origin", "master").StartAsAsync();
            await TaskManager.Wait();
            RepositoryManager.WaitForEvents();
            WaitForNotBusy(repositoryManagerEvents);
            WaitForStatusUpdated(repositoryManagerEvents);

            repositoryManagerListener.Received().OnIsBusyChanged(Args.Bool);
            repositoryManagerListener.Received().OnStatusUpdated(Args.GitStatus);
            repositoryManagerListener.DidNotReceive().OnLocksUpdated(Args.EnumerableGitLock);
            repositoryManagerListener.DidNotReceive().OnHeadUpdated(Args.String);
            repositoryManagerListener.DidNotReceive().OnLocalBranchListUpdated(Arg.Any<Dictionary<string, ConfigBranch>>());
            repositoryManagerListener.DidNotReceive().OnRemoteBranchListUpdated(Arg.Any<Dictionary<string, Dictionary<string, ConfigBranch>>>());
            repositoryManagerListener.Received().OnLocalBranchUpdated(Args.String);
            repositoryManagerListener.DidNotReceive().OnLocalBranchAdded(Args.String);
            repositoryManagerListener.DidNotReceive().OnLocalBranchRemoved(Args.String);
            repositoryManagerListener.DidNotReceive().OnRemoteBranchAdded(Args.String, Args.String);
            repositoryManagerListener.DidNotReceive().OnRemoteBranchRemoved(Args.String, Args.String);

            result.AssertEqual(expected);

            repositoryManagerEvents.Reset();
            WaitForNotBusy(repositoryManagerEvents);
        }

        [Test]
        public async Task ShouldDetectGitFetch()
        {
            await Initialize(TestRepoMasterCleanUnsynchronized);

            var repositoryManagerListener = Substitute.For<IRepositoryManagerListener>();
            repositoryManagerListener.AttachListener(RepositoryManager, repositoryManagerEvents);

            await RepositoryManager.Fetch("origin").StartAsAsync();
            await TaskManager.Wait();
            RepositoryManager.WaitForEvents();
            WaitForNotBusy(repositoryManagerEvents);

            repositoryManagerListener.Received().OnIsBusyChanged(Args.Bool);
            repositoryManagerListener.DidNotReceive().OnStatusUpdated(Args.GitStatus);
            repositoryManagerListener.DidNotReceive().OnLocksUpdated(Args.EnumerableGitLock);
            repositoryManagerListener.DidNotReceive().OnHeadUpdated(Args.String);
            repositoryManagerListener.DidNotReceive().OnLocalBranchListUpdated(Arg.Any<Dictionary<string, ConfigBranch>>());
            repositoryManagerListener.DidNotReceive().OnRemoteBranchListUpdated(Arg.Any<Dictionary<string, Dictionary<string, ConfigBranch>>>());
            repositoryManagerListener.DidNotReceive().OnLocalBranchUpdated(Args.String);
            repositoryManagerListener.DidNotReceive().OnLocalBranchAdded(Args.String);
            repositoryManagerListener.DidNotReceive().OnLocalBranchRemoved(Args.String);
            repositoryManagerListener.Received().OnRemoteBranchAdded(Args.String, Args.String);
            repositoryManagerListener.DidNotReceive().OnRemoteBranchRemoved(Args.String, Args.String);
        }

        private void WaitForNotBusy(RepositoryManagerEvents managerEvents, int seconds = 1)
        {
            managerEvents.OnIsBusy.WaitOne(TimeSpan.FromSeconds(seconds));
            managerEvents.OnIsNotBusy.WaitOne(TimeSpan.FromSeconds(seconds));
        }

        private void WaitForStatusUpdated(RepositoryManagerEvents managerEvents, int seconds = 1)
        {
            managerEvents.OnStatusUpdated.WaitOne(TimeSpan.FromSeconds(seconds));
        }

        private void WaitForHeadUpdated(RepositoryManagerEvents managerEvents, int seconds = 1)
        {
            managerEvents.OnHeadUpdated.WaitOne(TimeSpan.FromSeconds(seconds));
        }
    }
}
