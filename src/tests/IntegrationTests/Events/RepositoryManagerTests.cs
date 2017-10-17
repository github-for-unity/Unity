﻿using System;
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
            Environment.Repository.OnStatusUpdated += status => { result = status; };

            var foobarTxt = TestRepoMasterCleanSynchronized.Combine("foobar.txt");
            foobarTxt.WriteAllText("foobar");

            await TaskManager.Wait();
            RepositoryManager.WaitForEvents();
            WaitForNotBusy(repositoryManagerEvents, 1);

            repositoryManagerListener.Received().OnStatusUpdate(Args.GitStatus);
            repositoryManagerListener.DidNotReceive().OnActiveBranchChanged(Arg.Any<ConfigBranch?>());
            repositoryManagerListener.DidNotReceive().OnActiveRemoteChanged(Arg.Any<ConfigRemote?>());
            repositoryManagerListener.DidNotReceive().OnLocalBranchListChanged();
            repositoryManagerListener.DidNotReceive().OnRemoteBranchListChanged();
            repositoryManagerListener.Received().OnIsBusyChanged(Args.Bool);
            repositoryManagerListener.DidNotReceive().OnLocksUpdated(Args.EnumerableGitLock);

            result.AssertEqual(expected);
        }

        [Test]
        public async Task ShouldAddAndCommitFiles()
        {
            await Initialize(TestRepoMasterCleanSynchronized);

            var repositoryManagerListener = Substitute.For<IRepositoryManagerListener>();
            repositoryManagerListener.AttachListener(RepositoryManager, repositoryManagerEvents);

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

            var result = new GitStatus();
            RepositoryManager.OnStatusUpdated += status => { result = status; };

            var foobarTxt = TestRepoMasterCleanSynchronized.Combine("foobar.txt");
            foobarTxt.WriteAllText("foobar");

            var testDocumentTxt = TestRepoMasterCleanSynchronized.Combine("Assets", "TestDocument.txt");
            testDocumentTxt.WriteAllText("foobar");
            await TaskManager.Wait();
            RepositoryManager.WaitForEvents();
            WaitForNotBusy(repositoryManagerEvents, 1);

            repositoryManagerListener.Received().OnStatusUpdate(Args.GitStatus);
            repositoryManagerListener.DidNotReceive().OnActiveBranchChanged(Arg.Any<ConfigBranch?>());
            repositoryManagerListener.DidNotReceive().OnActiveRemoteChanged(Arg.Any<ConfigRemote?>());
            repositoryManagerListener.DidNotReceive().OnLocalBranchListChanged();
            repositoryManagerListener.DidNotReceive().OnRemoteBranchListChanged();
            repositoryManagerListener.ReceivedWithAnyArgs().OnIsBusyChanged(Args.Bool);
            repositoryManagerListener.DidNotReceive().OnLocksUpdated(Args.EnumerableGitLock);

            result.AssertEqual(expectedAfterChanges);

            repositoryManagerListener.ClearReceivedCalls();
            repositoryManagerEvents.Reset();

            await RepositoryManager
                .CommitFiles(new List<string>() { "Assets\\TestDocument.txt", "foobar.txt" }, "IntegrationTest Commit", string.Empty)
                .StartAsAsync();

            await TaskManager.Wait();
            RepositoryManager.WaitForEvents();
            WaitForNotBusy(repositoryManagerEvents, 1);
            repositoryManagerEvents.OnStatusUpdate.WaitOne(TimeSpan.FromSeconds(1));

            repositoryManagerListener.Received().OnStatusUpdate(Args.GitStatus);
            repositoryManagerListener.DidNotReceive().OnActiveBranchChanged(Arg.Any<ConfigBranch?>());
            repositoryManagerListener.DidNotReceive().OnActiveRemoteChanged(Arg.Any<ConfigRemote?>());
            repositoryManagerListener.DidNotReceive().OnLocalBranchListChanged();
            repositoryManagerListener.DidNotReceive().OnRemoteBranchListChanged();
            repositoryManagerListener.Received().OnIsBusyChanged(Args.Bool);
            repositoryManagerListener.DidNotReceive().OnLocksUpdated(Args.EnumerableGitLock);
        }

        [Test]
        public async Task ShouldAddAndCommitAllFiles()
        {
            await Initialize(TestRepoMasterCleanSynchronized);

            var repositoryManagerListener = Substitute.For<IRepositoryManagerListener>();
            repositoryManagerListener.AttachListener(RepositoryManager, repositoryManagerEvents);

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

            var result = new GitStatus();
            RepositoryManager.OnStatusUpdated += status => { result = status; };

            var foobarTxt = TestRepoMasterCleanSynchronized.Combine("foobar.txt");
            foobarTxt.WriteAllText("foobar");

            var testDocumentTxt = TestRepoMasterCleanSynchronized.Combine("Assets", "TestDocument.txt");
            testDocumentTxt.WriteAllText("foobar");
            await TaskManager.Wait();
            WaitForNotBusy(repositoryManagerEvents, 1);
            RepositoryManager.WaitForEvents();

            repositoryManagerListener.Received().OnStatusUpdate(Args.GitStatus);
            repositoryManagerListener.DidNotReceive().OnActiveBranchChanged(Arg.Any<ConfigBranch?>());
            repositoryManagerListener.DidNotReceive().OnActiveRemoteChanged(Arg.Any<ConfigRemote?>());
            repositoryManagerListener.DidNotReceive().OnLocalBranchListChanged();
            repositoryManagerListener.DidNotReceive().OnRemoteBranchListChanged();
            repositoryManagerListener.ReceivedWithAnyArgs().OnIsBusyChanged(Args.Bool);
            repositoryManagerListener.DidNotReceive().OnLocksUpdated(Args.EnumerableGitLock);

            result.AssertEqual(expectedAfterChanges);

            repositoryManagerListener.ClearReceivedCalls();
            repositoryManagerEvents.Reset();

            await RepositoryManager
                .CommitAllFiles("IntegrationTest Commit", string.Empty)
                .StartAsAsync();

            await TaskManager.Wait();
            WaitForNotBusy(repositoryManagerEvents, 1);
            RepositoryManager.WaitForEvents();

            repositoryManagerListener.DidNotReceive().OnStatusUpdate(Args.GitStatus);
            repositoryManagerListener.DidNotReceive().OnActiveBranchChanged(Arg.Any<ConfigBranch?>());
            repositoryManagerListener.DidNotReceive().OnActiveRemoteChanged(Arg.Any<ConfigRemote?>());
            repositoryManagerListener.DidNotReceive().OnLocalBranchListChanged();
            repositoryManagerListener.DidNotReceive().OnRemoteBranchListChanged();
            repositoryManagerListener.Received(2).OnIsBusyChanged(Args.Bool);
            repositoryManagerListener.DidNotReceive().OnLocksUpdated(Args.EnumerableGitLock);
        }

        [Test]
        public async Task ShouldDetectBranchChange()
        {
            await Initialize(TestRepoMasterCleanSynchronized);

            var repositoryManagerListener = Substitute.For<IRepositoryManagerListener>();
            repositoryManagerListener.AttachListener(RepositoryManager, repositoryManagerEvents);

            var expected = new GitStatus {
                LocalBranch = "feature/document",
                RemoteBranch = "origin/feature/document",
                Entries = new List<GitStatusEntry>()
            };

            var result = new GitStatus();
            RepositoryManager.OnStatusUpdated += status => { result = status; };

            await RepositoryManager.SwitchBranch("feature/document").StartAsAsync();
            await TaskManager.Wait();
            RepositoryManager.WaitForEvents();
            WaitForNotBusy(repositoryManagerEvents, 5);
            repositoryManagerEvents.OnStatusUpdate.WaitOne(TimeSpan.FromSeconds(1));

            repositoryManagerListener.Received().OnStatusUpdate(Args.GitStatus);
            repositoryManagerListener.Received().OnActiveBranchChanged(Arg.Any<ConfigBranch?>());
            repositoryManagerListener.Received().OnActiveRemoteChanged(Arg.Any<ConfigRemote?>());
            repositoryManagerListener.DidNotReceive().OnLocalBranchListChanged();
            repositoryManagerListener.DidNotReceive().OnRemoteBranchListChanged();
            repositoryManagerListener.Received().OnIsBusyChanged(Args.Bool);
            repositoryManagerListener.DidNotReceive().OnLocksUpdated(Args.EnumerableGitLock);

            result.AssertEqual(expected);
        }

        [Test]
        public async Task ShouldDetectBranchDelete()
        {
            await Initialize(TestRepoMasterCleanSynchronized);

            var repositoryManagerListener = Substitute.For<IRepositoryManagerListener>();
            repositoryManagerListener.AttachListener(RepositoryManager, repositoryManagerEvents);

            await RepositoryManager.DeleteBranch("feature/document", true).StartAsAsync();
            await TaskManager.Wait();
            WaitForNotBusy(repositoryManagerEvents, 1);
            RepositoryManager.WaitForEvents();

            repositoryManagerListener.DidNotReceive().OnStatusUpdate(Args.GitStatus);
            //TODO: Deleting a branch causes a config reload, which raises OnActiveBranchChanged/OnActiveRemoteChanged
            repositoryManagerListener.Received().OnActiveBranchChanged(Arg.Any<ConfigBranch?>());
            repositoryManagerListener.Received().OnActiveRemoteChanged(Arg.Any<ConfigRemote?>());
            repositoryManagerListener.Received(1).OnLocalBranchListChanged();
            repositoryManagerListener.DidNotReceive().OnRemoteBranchListChanged();
            repositoryManagerListener.ReceivedWithAnyArgs().OnIsBusyChanged(Args.Bool);
            repositoryManagerListener.DidNotReceive().OnLocksUpdated(Args.EnumerableGitLock);
        }

        [Test]
        public async Task ShouldDetectBranchCreate()
        {
            await Initialize(TestRepoMasterCleanSynchronized);

            var repositoryManagerListener = Substitute.For<IRepositoryManagerListener>();
            repositoryManagerListener.AttachListener(RepositoryManager, repositoryManagerEvents);

            await RepositoryManager.CreateBranch("feature/document2", "feature/document").StartAsAsync();
            await TaskManager.Wait();
            RepositoryManager.WaitForEvents();

            repositoryManagerListener.DidNotReceive().OnStatusUpdate(Args.GitStatus);
            repositoryManagerListener.DidNotReceive().OnActiveBranchChanged(Arg.Any<ConfigBranch?>());
            repositoryManagerListener.DidNotReceive().OnActiveRemoteChanged(Arg.Any<ConfigRemote?>());
            repositoryManagerListener.Received(1).OnLocalBranchListChanged();
            repositoryManagerListener.DidNotReceive().OnRemoteBranchListChanged();
            repositoryManagerListener.ReceivedWithAnyArgs().OnIsBusyChanged(Args.Bool);
            repositoryManagerListener.DidNotReceive().OnLocksUpdated(Args.EnumerableGitLock);

            repositoryManagerListener.ClearReceivedCalls();
            repositoryManagerEvents.Reset();

            await RepositoryManager.CreateBranch("feature2/document2", "feature/document").StartAsAsync();
            await TaskManager.Wait();
            RepositoryManager.WaitForEvents();

            repositoryManagerListener.DidNotReceive().OnStatusUpdate(Args.GitStatus);
            repositoryManagerListener.DidNotReceive().OnActiveBranchChanged(Arg.Any<ConfigBranch?>());
            repositoryManagerListener.DidNotReceive().OnActiveRemoteChanged(Arg.Any<ConfigRemote?>());
            repositoryManagerListener.Received(1).OnLocalBranchListChanged();
            repositoryManagerListener.DidNotReceive().OnRemoteBranchListChanged();
            repositoryManagerListener.Received(2).OnIsBusyChanged(Args.Bool);
            repositoryManagerListener.DidNotReceive().OnLocksUpdated(Args.EnumerableGitLock);
        }

        [Test]
        public async Task ShouldDetectChangesToRemotes()
        {
            await Initialize(TestRepoMasterCleanSynchronized);

            var repositoryManagerListener = Substitute.For<IRepositoryManagerListener>();
            repositoryManagerListener.AttachListener(RepositoryManager, repositoryManagerEvents);

            Environment.Repository.CurrentRemote.HasValue.Should().BeTrue();
            Environment.Repository.CurrentRemote.Value.Name.Should().Be("origin");
            Environment.Repository.CurrentRemote.Value.Url.Should().Be("https://github.com/EvilStanleyGoldman/IOTestsRepo.git");

            Environment.Repository.CloneUrl.Should().Be("https://github.com/EvilStanleyGoldman/IOTestsRepo.git");
            Environment.Repository.Owner.Should().Be("EvilStanleyGoldman");

            await RepositoryManager.RemoteRemove("origin").StartAsAsync();
            await TaskManager.Wait();
            RepositoryManager.WaitForEvents();

            Environment.Repository.CurrentRemote.HasValue.Should().BeFalse();

            Environment.Repository.CloneUrl.Should().BeNull();
            Environment.Repository.Owner.Should().BeNull();

            repositoryManagerListener.DidNotReceive().OnStatusUpdate(Args.GitStatus);
            repositoryManagerListener.Received().OnActiveBranchChanged(Arg.Any<ConfigBranch?>());
            repositoryManagerListener.Received().OnActiveRemoteChanged(Arg.Any<ConfigRemote?>());
            repositoryManagerListener.DidNotReceive().OnLocalBranchListChanged();
            repositoryManagerListener.Received().OnRemoteBranchListChanged();
            repositoryManagerListener.ReceivedWithAnyArgs().OnIsBusyChanged(Args.Bool);
            repositoryManagerListener.DidNotReceive().OnLocksUpdated(Args.EnumerableGitLock);

            repositoryManagerListener.ClearReceivedCalls();
            repositoryManagerEvents.Reset();

            await RepositoryManager.RemoteAdd("origin", "https://github.com/EvilShana/IOTestsRepo.git").StartAsAsync();
            await TaskManager.Wait();
            RepositoryManager.WaitForEvents();

            Environment.Repository.CurrentRemote.HasValue.Should().BeTrue();
            Environment.Repository.CurrentRemote.Value.Name.Should().Be("origin");
            Environment.Repository.CurrentRemote.Value.Url.Should().Be("https://github.com/EvilShana/IOTestsRepo.git");

            Environment.Repository.CloneUrl.Should().Be("https://github.com/EvilShana/IOTestsRepo.git");
            Environment.Repository.Owner.Should().Be("EvilShana");

            repositoryManagerListener.DidNotReceive().OnStatusUpdate(Args.GitStatus);
            repositoryManagerListener.Received().OnActiveBranchChanged(Arg.Any<ConfigBranch?>());
            repositoryManagerListener.Received().OnActiveRemoteChanged(Arg.Any<ConfigRemote?>());
            repositoryManagerListener.DidNotReceive().OnLocalBranchListChanged();
            repositoryManagerListener.DidNotReceive().OnRemoteBranchListChanged();
            repositoryManagerListener.ReceivedWithAnyArgs().OnIsBusyChanged(Args.Bool);
            repositoryManagerListener.DidNotReceive().OnLocksUpdated(Args.EnumerableGitLock);
        }

        [Test]
        public async Task ShouldDetectChangesToRemotesWhenSwitchingBranches()
        {
            var expectedCloneUrl = "https://github.com/EvilStanleyGoldman/IOTestsRepo.git";

            await Initialize(TestRepoMasterTwoRemotes);

            var repositoryManagerListener = Substitute.For<IRepositoryManagerListener>();
            repositoryManagerListener.AttachListener(RepositoryManager, repositoryManagerEvents);

            Environment.Repository.CurrentRemote.HasValue.Should().BeTrue();
            Environment.Repository.CurrentRemote.Value.Name.Should().Be("origin");
            Environment.Repository.CurrentRemote.Value.Url.Should().Be(expectedCloneUrl);
            Environment.Repository.Owner.Should().Be("EvilStanleyGoldman");

            await RepositoryManager.CreateBranch("branch2", "another/master")
                //.Then(RepositoryManager.SwitchBranch("branch2"))
                .StartAsAsync();

            await TaskManager.Wait();
            RepositoryManager.WaitForEvents();

            repositoryManagerListener.DidNotReceive().OnStatusUpdate(Args.GitStatus);
            repositoryManagerListener.Received().OnActiveBranchChanged(Arg.Any<ConfigBranch?>());
            repositoryManagerListener.Received().OnActiveRemoteChanged(Arg.Any<ConfigRemote?>());
            repositoryManagerListener.Received().OnLocalBranchListChanged();
            repositoryManagerListener.DidNotReceive().OnRemoteBranchListChanged();
            repositoryManagerListener.ReceivedWithAnyArgs().OnIsBusyChanged(Args.Bool);
            repositoryManagerListener.DidNotReceive().OnLocksUpdated(Args.EnumerableGitLock);

            repositoryManagerListener.ClearReceivedCalls();
            repositoryManagerEvents.Reset();

            await RepositoryManager.SwitchBranch("branch2")
                                   .StartAsAsync();

            await TaskManager.Wait();
            RepositoryManager.WaitForEvents();

            Environment.Repository.CurrentRemote.HasValue.Should().BeTrue();
            Environment.Repository.CurrentRemote.Value.Name.Should().Be("another");

            var expectedRemoteUrl = "https://another.remote/Owner/Url.git";
            Environment.Repository.CurrentRemote.Value.Url.Should().Be(expectedRemoteUrl);
            Environment.Repository.CloneUrl.ToString().Should().Be(expectedRemoteUrl);
            Environment.Repository.Owner.Should().Be("Owner");

            repositoryManagerListener.DidNotReceive().OnStatusUpdate(Args.GitStatus);
            repositoryManagerListener.Received().OnActiveBranchChanged(Arg.Any<ConfigBranch?>());
            repositoryManagerListener.Received().OnActiveRemoteChanged(Arg.Any<ConfigRemote?>());
            repositoryManagerListener.DidNotReceive().OnLocalBranchListChanged();
            repositoryManagerListener.DidNotReceive().OnRemoteBranchListChanged();
            repositoryManagerListener.Received().OnIsBusyChanged(Args.Bool);
            repositoryManagerListener.DidNotReceive().OnLocksUpdated(Args.EnumerableGitLock);
        }

        [Test]
        public async Task ShouldUpdateCloneUrlIfRemoteIsDeleted()
        {
            await Initialize(TestRepoMasterCleanSynchronized);

            var repositoryManagerListener = Substitute.For<IRepositoryManagerListener>();
            repositoryManagerListener.AttachListener(RepositoryManager, repositoryManagerEvents);

            Environment.Repository.CurrentRemote.HasValue.Should().BeTrue();
            Environment.Repository.CurrentRemote.Value.Name.Should().Be("origin");
            Environment.Repository.CurrentRemote.Value.Url.Should().Be("https://github.com/EvilStanleyGoldman/IOTestsRepo.git");

            Environment.Repository.CloneUrl.Should().Be("https://github.com/EvilStanleyGoldman/IOTestsRepo.git");
            Environment.Repository.Owner.Should().Be("EvilStanleyGoldman");

            await RepositoryManager.RemoteRemove("origin").StartAsAsync();
            await TaskManager.Wait();
            RepositoryManager.WaitForEvents();

            Environment.Repository.CurrentRemote.HasValue.Should().BeFalse();

            Environment.Repository.CloneUrl.Should().BeNull();
            Environment.Repository.Owner.Should().BeNull();

            repositoryManagerListener.DidNotReceive().OnStatusUpdate(Args.GitStatus);
            repositoryManagerListener.ReceivedWithAnyArgs().OnIsBusyChanged(Args.Bool);
            repositoryManagerListener.Received().OnActiveBranchChanged(Arg.Any<ConfigBranch?>());
            repositoryManagerListener.Received().OnActiveRemoteChanged(Arg.Any<ConfigRemote?>());
            repositoryManagerListener.DidNotReceive().OnLocalBranchListChanged();
            repositoryManagerListener.Received().OnRemoteBranchListChanged();
            repositoryManagerListener.DidNotReceive().OnLocksUpdated(Args.EnumerableGitLock);

            repositoryManagerListener.ClearReceivedCalls();
            repositoryManagerEvents.Reset();

            await RepositoryManager.RemoteAdd("origin", "https://github.com/EvilShana/IOTestsRepo.git").StartAsAsync();
            await TaskManager.Wait();
            RepositoryManager.WaitForEvents();

            Environment.Repository.CurrentRemote.HasValue.Should().BeTrue();
            Environment.Repository.CurrentRemote.Value.Url.Should().Be("https://github.com/EvilShana/IOTestsRepo.git");

            Environment.Repository.CloneUrl.Should().Be("https://github.com/EvilShana/IOTestsRepo.git");
            Environment.Repository.Owner.Should().Be("EvilShana");

            repositoryManagerListener.DidNotReceive().OnStatusUpdate(Args.GitStatus);
            repositoryManagerListener.ReceivedWithAnyArgs().OnIsBusyChanged(Args.Bool);
            repositoryManagerListener.Received().OnActiveBranchChanged(Arg.Any<ConfigBranch?>());
            repositoryManagerListener.Received().OnActiveRemoteChanged(Arg.Any<ConfigRemote?>());
            repositoryManagerListener.DidNotReceive().OnLocalBranchListChanged();
            repositoryManagerListener.DidNotReceive().OnRemoteBranchListChanged();
            repositoryManagerListener.DidNotReceive().OnLocksUpdated(Args.EnumerableGitLock);
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

            repositoryManagerListener.Received().OnStatusUpdate(Args.GitStatus);
            repositoryManagerListener.ReceivedWithAnyArgs().OnIsBusyChanged(Args.Bool);
            repositoryManagerListener.DidNotReceive().OnActiveBranchChanged(Arg.Any<ConfigBranch?>());
            repositoryManagerListener.DidNotReceive().OnActiveRemoteChanged(Arg.Any<ConfigRemote?>());
            repositoryManagerListener.DidNotReceive().OnLocalBranchListChanged();
            repositoryManagerListener.DidNotReceive().OnRemoteBranchListChanged();
            repositoryManagerListener.DidNotReceive().OnLocksUpdated(Args.EnumerableGitLock);

            result.AssertEqual(expected);

            repositoryManagerEvents.Reset();
            WaitForNotBusy(repositoryManagerEvents, 1);
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

            repositoryManagerListener.DidNotReceive().OnStatusUpdate(Args.GitStatus);
            repositoryManagerListener.ReceivedWithAnyArgs().OnIsBusyChanged(Args.Bool);
            repositoryManagerListener.DidNotReceive().OnActiveBranchChanged(Arg.Any<ConfigBranch?>());
            repositoryManagerListener.DidNotReceive().OnActiveRemoteChanged(Arg.Any<ConfigRemote?>());
            repositoryManagerListener.DidNotReceive().OnLocalBranchListChanged();
            repositoryManagerListener.Received(2).OnRemoteBranchListChanged();
            repositoryManagerListener.DidNotReceive().OnLocksUpdated(Args.EnumerableGitLock);
        }

        private void WaitForNotBusy(RepositoryManagerEvents managerEvents, int seconds = 1)
        {
            managerEvents.OnIsBusy.WaitOne(TimeSpan.FromSeconds(seconds));
            managerEvents.OnIsNotBusy.WaitOne(TimeSpan.FromSeconds(seconds));
        }
    }
}
