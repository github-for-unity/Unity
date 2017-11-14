using System;
using System.Collections.Generic;
using System.Linq;
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
        public async Task ShouldPerformBasicInitialize()
        {
            var repositoryManagerListener = Substitute.For<IRepositoryManagerListener>();

            await Initialize(TestRepoMasterCleanSynchronized, initializeRepository: false,
                onRepositoryManagerCreated: manager => {
                    repositoryManagerListener.AttachListener(manager, repositoryManagerEvents);
                });

            repositoryManagerEvents.CurrentBranchUpdated.WaitOne(TimeSpan.FromSeconds(20)).Should().BeTrue();
            repositoryManagerEvents.LocalBranchesUpdated.WaitOne(TimeSpan.FromSeconds(20)).Should().BeTrue();
            repositoryManagerEvents.RemoteBranchesUpdated.WaitOne(TimeSpan.FromSeconds(20)).Should().BeTrue();

            repositoryManagerListener.DidNotReceive().OnIsBusyChanged(Args.Bool);
            repositoryManagerListener.Received().CurrentBranchUpdated(Args.NullableConfigBranch, Args.NullableConfigRemote);
            repositoryManagerListener.DidNotReceive().GitStatusUpdated(Args.GitStatus);
            repositoryManagerListener.DidNotReceive().GitLocksUpdated(Args.GitLocks);
            repositoryManagerListener.DidNotReceive().GitLogUpdated(Args.GitLogs);
            repositoryManagerListener.Received().LocalBranchesUpdated(Args.LocalBranchDictionary);
            repositoryManagerListener.Received().RemoteBranchesUpdated(Args.RemoteDictionary, Args.RemoteBranchDictionary);
        }

        [Test]
        public async Task ShouldDetectFileChanges()
        {
            var repositoryManagerListener = Substitute.For<IRepositoryManagerListener>();

            await Initialize(TestRepoMasterCleanSynchronized, initializeRepository: false,
                onRepositoryManagerCreated: manager => {
                    repositoryManagerListener.AttachListener(manager, repositoryManagerEvents);
                });

            repositoryManagerEvents.CurrentBranchUpdated.WaitOne(TimeSpan.FromSeconds(20)).Should().BeTrue();
            repositoryManagerEvents.LocalBranchesUpdated.WaitOne(TimeSpan.FromSeconds(20)).Should().BeTrue();
            repositoryManagerEvents.RemoteBranchesUpdated.WaitOne(TimeSpan.FromSeconds(20)).Should().BeTrue();

            repositoryManagerListener.ClearReceivedCalls();

            var foobarTxt = TestRepoMasterCleanSynchronized.Combine("foobar.txt");
            foobarTxt.WriteAllText("foobar");

            await TaskManager.Wait();

            RepositoryManager.WaitForEvents();
            repositoryManagerEvents.WaitForNotBusy();

            repositoryManagerEvents.GitStatusUpdated.WaitOne(TimeSpan.FromSeconds(20)).Should().BeTrue();

            repositoryManagerListener.Received().OnIsBusyChanged(Args.Bool);
            repositoryManagerListener.DidNotReceive().CurrentBranchUpdated(Args.NullableConfigBranch, Args.NullableConfigRemote);
            repositoryManagerListener.Received().GitStatusUpdated(Args.GitStatus);
            repositoryManagerListener.DidNotReceive().GitLocksUpdated(Args.GitLocks);
            repositoryManagerListener.DidNotReceive().GitLogUpdated(Args.GitLogs);
            repositoryManagerListener.DidNotReceive().LocalBranchesUpdated(Args.LocalBranchDictionary);
            repositoryManagerListener.DidNotReceive().RemoteBranchesUpdated(Args.RemoteDictionary, Args.RemoteBranchDictionary);
        }

        [Test]
        public async Task ShouldAddAndCommitFiles()
        {
            var repositoryManagerListener = Substitute.For<IRepositoryManagerListener>();

            await Initialize(TestRepoMasterCleanSynchronized, initializeRepository: false,
                onRepositoryManagerCreated: manager => {
                    repositoryManagerListener.AttachListener(manager, repositoryManagerEvents);
                });

            repositoryManagerEvents.CurrentBranchUpdated.WaitOne(TimeSpan.FromSeconds(20)).Should().BeTrue();
            repositoryManagerEvents.LocalBranchesUpdated.WaitOne(TimeSpan.FromSeconds(20)).Should().BeTrue();
            repositoryManagerEvents.RemoteBranchesUpdated.WaitOne(TimeSpan.FromSeconds(20)).Should().BeTrue();

            repositoryManagerListener.ClearReceivedCalls();

            var foobarTxt = TestRepoMasterCleanSynchronized.Combine("foobar.txt");
            foobarTxt.WriteAllText("foobar");

            var testDocumentTxt = TestRepoMasterCleanSynchronized.Combine("Assets", "TestDocument.txt");
            testDocumentTxt.WriteAllText("foobar");

            await TaskManager.Wait();

            RepositoryManager.WaitForEvents();
            repositoryManagerEvents.WaitForNotBusy();

            repositoryManagerEvents.GitStatusUpdated.WaitOne(TimeSpan.FromSeconds(20)).Should().BeTrue();

            repositoryManagerListener.Received().OnIsBusyChanged(Args.Bool);
            repositoryManagerListener.DidNotReceive().CurrentBranchUpdated(Args.NullableConfigBranch, Args.NullableConfigRemote);
            repositoryManagerListener.Received().GitStatusUpdated(Args.GitStatus);
            repositoryManagerListener.DidNotReceive().GitLocksUpdated(Args.GitLocks);
            repositoryManagerListener.DidNotReceive().GitLogUpdated(Args.GitLogs);
            repositoryManagerListener.DidNotReceive().LocalBranchesUpdated(Args.LocalBranchDictionary);
            repositoryManagerListener.DidNotReceive().RemoteBranchesUpdated(Args.RemoteDictionary, Args.RemoteBranchDictionary);
            repositoryManagerListener.ClearReceivedCalls();

            repositoryManagerEvents.Reset();

            await RepositoryManager
                .CommitFiles(new List<string> { "Assets\\TestDocument.txt", "foobar.txt" }, "IntegrationTest Commit", string.Empty)
                .StartAsAsync();
            await TaskManager.Wait();

            RepositoryManager.WaitForEvents();
            repositoryManagerEvents.WaitForNotBusy();

            repositoryManagerEvents.LocalBranchesUpdated.WaitOne(TimeSpan.FromSeconds(20)).Should().BeTrue();
            repositoryManagerEvents.GitStatusUpdated.WaitOne(TimeSpan.FromSeconds(20)).Should().BeTrue();
            repositoryManagerEvents.GitLogUpdated.WaitOne(TimeSpan.FromSeconds(20)).Should().BeTrue();

            repositoryManagerListener.Received().OnIsBusyChanged(Args.Bool);
            repositoryManagerListener.DidNotReceive().CurrentBranchUpdated(Args.NullableConfigBranch, Args.NullableConfigRemote);
            repositoryManagerListener.Received().GitStatusUpdated(Args.GitStatus);
            repositoryManagerListener.DidNotReceive().GitLocksUpdated(Args.GitLocks);
            repositoryManagerListener.Received().GitLogUpdated(Args.GitLogs);
            repositoryManagerListener.Received().LocalBranchesUpdated(Args.LocalBranchDictionary);
            repositoryManagerListener.DidNotReceive().RemoteBranchesUpdated(Args.RemoteDictionary, Args.RemoteBranchDictionary);
        }

        [Test]
        public async Task ShouldAddAndCommitAllFiles()
        {
            var repositoryManagerListener = Substitute.For<IRepositoryManagerListener>();

            await Initialize(TestRepoMasterCleanSynchronized, initializeRepository: false,
                onRepositoryManagerCreated: manager => {
                    repositoryManagerListener.AttachListener(manager, repositoryManagerEvents);
                });

            repositoryManagerEvents.CurrentBranchUpdated.WaitOne(TimeSpan.FromSeconds(20)).Should().BeTrue();
            repositoryManagerEvents.LocalBranchesUpdated.WaitOne(TimeSpan.FromSeconds(20)).Should().BeTrue();
            repositoryManagerEvents.RemoteBranchesUpdated.WaitOne(TimeSpan.FromSeconds(20)).Should().BeTrue();

            repositoryManagerListener.ClearReceivedCalls();

            var foobarTxt = TestRepoMasterCleanSynchronized.Combine("foobar.txt");
            foobarTxt.WriteAllText("foobar");

            var testDocumentTxt = TestRepoMasterCleanSynchronized.Combine("Assets", "TestDocument.txt");
            testDocumentTxt.WriteAllText("foobar");

            await TaskManager.Wait();

            RepositoryManager.WaitForEvents();
            repositoryManagerEvents.WaitForNotBusy();

            repositoryManagerEvents.GitStatusUpdated.WaitOne(TimeSpan.FromSeconds(20)).Should().BeTrue();

            repositoryManagerListener.Received().OnIsBusyChanged(Args.Bool);
            repositoryManagerListener.DidNotReceive().CurrentBranchUpdated(Args.NullableConfigBranch, Args.NullableConfigRemote);
            repositoryManagerListener.Received().GitStatusUpdated(Args.GitStatus);
            repositoryManagerListener.DidNotReceive().GitLocksUpdated(Args.GitLocks);
            repositoryManagerListener.DidNotReceive().GitLogUpdated(Args.GitLogs);
            repositoryManagerListener.DidNotReceive().LocalBranchesUpdated(Args.LocalBranchDictionary);
            repositoryManagerListener.DidNotReceive().RemoteBranchesUpdated(Args.RemoteDictionary, Args.RemoteBranchDictionary);

            repositoryManagerListener.ClearReceivedCalls();
            repositoryManagerEvents.Reset();

            await RepositoryManager
                .CommitAllFiles("IntegrationTest Commit", string.Empty)
                .StartAsAsync();
            await TaskManager.Wait();

            RepositoryManager.WaitForEvents();
            repositoryManagerEvents.WaitForNotBusy();

            repositoryManagerEvents.GitStatusUpdated.WaitOne(TimeSpan.FromSeconds(20)).Should().BeTrue();
            repositoryManagerEvents.LocalBranchesUpdated.WaitOne(TimeSpan.FromSeconds(20)).Should().BeTrue();

            repositoryManagerListener.Received().OnIsBusyChanged(Args.Bool);
            repositoryManagerListener.DidNotReceive().CurrentBranchUpdated(Args.NullableConfigBranch, Args.NullableConfigRemote);
            repositoryManagerListener.Received().GitStatusUpdated(Args.GitStatus);
            repositoryManagerListener.DidNotReceive().GitLocksUpdated(Args.GitLocks);
            repositoryManagerListener.DidNotReceive().GitLogUpdated(Args.GitLogs);
            repositoryManagerListener.Received().LocalBranchesUpdated(Args.LocalBranchDictionary);
            repositoryManagerListener.DidNotReceive().RemoteBranchesUpdated(Args.RemoteDictionary, Args.RemoteBranchDictionary);
        }

        [Test]
        public async Task ShouldDetectBranchChange()
        {
            var repositoryManagerListener = Substitute.For<IRepositoryManagerListener>();

            await Initialize(TestRepoMasterCleanSynchronized, initializeRepository: false,
                onRepositoryManagerCreated: manager => {
                    repositoryManagerListener.AttachListener(manager, repositoryManagerEvents);
                });

            repositoryManagerEvents.CurrentBranchUpdated.WaitOne(TimeSpan.FromSeconds(20)).Should().BeTrue();
            repositoryManagerEvents.LocalBranchesUpdated.WaitOne(TimeSpan.FromSeconds(20)).Should().BeTrue();
            repositoryManagerEvents.RemoteBranchesUpdated.WaitOne(TimeSpan.FromSeconds(20)).Should().BeTrue();

            repositoryManagerListener.ClearReceivedCalls();

            await RepositoryManager.SwitchBranch("feature/document").StartAsAsync();
            await TaskManager.Wait();

            RepositoryManager.WaitForEvents();
            repositoryManagerEvents.WaitForNotBusy();

            repositoryManagerEvents.CurrentBranchUpdated.WaitOne(TimeSpan.FromSeconds(2)).Should().BeTrue();

            repositoryManagerListener.Received().OnIsBusyChanged(Args.Bool);
            repositoryManagerListener.Received().CurrentBranchUpdated(Args.NullableConfigBranch, Args.NullableConfigRemote);
            repositoryManagerListener.DidNotReceive().GitStatusUpdated(Args.GitStatus);
            repositoryManagerListener.DidNotReceive().GitLocksUpdated(Args.GitLocks);
            repositoryManagerListener.DidNotReceive().GitLogUpdated(Args.GitLogs);
            repositoryManagerListener.DidNotReceive().LocalBranchesUpdated(Args.LocalBranchDictionary);
            repositoryManagerListener.DidNotReceive().RemoteBranchesUpdated(Args.RemoteDictionary, Args.RemoteBranchDictionary);
        }

        [Test]
        public async Task ShouldDetectBranchDelete()
        {
            var repositoryManagerListener = Substitute.For<IRepositoryManagerListener>();

            await Initialize(TestRepoMasterCleanSynchronized, initializeRepository: false,
                onRepositoryManagerCreated: manager => {
                    repositoryManagerListener.AttachListener(manager, repositoryManagerEvents);
                });

            repositoryManagerEvents.CurrentBranchUpdated.WaitOne(TimeSpan.FromSeconds(20)).Should().BeTrue();
            repositoryManagerEvents.LocalBranchesUpdated.WaitOne(TimeSpan.FromSeconds(20)).Should().BeTrue();
            repositoryManagerEvents.RemoteBranchesUpdated.WaitOne(TimeSpan.FromSeconds(20)).Should().BeTrue();

            repositoryManagerListener.ClearReceivedCalls();

            await RepositoryManager.DeleteBranch("feature/document", true).StartAsAsync();
            await TaskManager.Wait();

            RepositoryManager.WaitForEvents();
            repositoryManagerEvents.WaitForNotBusy();

            repositoryManagerEvents.CurrentBranchUpdated.WaitOne(TimeSpan.FromSeconds(2)).Should().BeTrue();
            repositoryManagerEvents.LocalBranchesUpdated.WaitOne(TimeSpan.FromSeconds(2)).Should().BeTrue();
            repositoryManagerEvents.RemoteBranchesUpdated.WaitOne(TimeSpan.FromSeconds(2)).Should().BeTrue();

            repositoryManagerListener.Received().OnIsBusyChanged(Args.Bool);
            repositoryManagerListener.Received().CurrentBranchUpdated(Args.NullableConfigBranch, Args.NullableConfigRemote);
            repositoryManagerListener.DidNotReceive().GitStatusUpdated(Args.GitStatus);
            repositoryManagerListener.DidNotReceive().GitLocksUpdated(Args.GitLocks);
            repositoryManagerListener.DidNotReceive().GitLogUpdated(Args.GitLogs);
            repositoryManagerListener.Received().LocalBranchesUpdated(Args.LocalBranchDictionary);
            repositoryManagerListener.Received().RemoteBranchesUpdated(Args.RemoteDictionary, Args.RemoteBranchDictionary);
        }

        [Test]
        public async Task ShouldDetectBranchCreate()
        {
            var repositoryManagerListener = Substitute.For<IRepositoryManagerListener>();

            await Initialize(TestRepoMasterCleanSynchronized, initializeRepository: false,
                onRepositoryManagerCreated: manager => {
                    repositoryManagerListener.AttachListener(manager, repositoryManagerEvents);
                });

            repositoryManagerEvents.CurrentBranchUpdated.WaitOne(TimeSpan.FromSeconds(20)).Should().BeTrue();
            repositoryManagerEvents.LocalBranchesUpdated.WaitOne(TimeSpan.FromSeconds(20)).Should().BeTrue();
            repositoryManagerEvents.RemoteBranchesUpdated.WaitOne(TimeSpan.FromSeconds(20)).Should().BeTrue();

            repositoryManagerListener.ClearReceivedCalls();

            var createdBranch1 = "feature/document2";
            await RepositoryManager.CreateBranch(createdBranch1, "feature/document").StartAsAsync();
            await TaskManager.Wait();

            RepositoryManager.WaitForEvents();
            repositoryManagerEvents.WaitForNotBusy();

            repositoryManagerEvents.LocalBranchesUpdated.WaitOne(TimeSpan.FromSeconds(2)).Should().BeTrue();

            repositoryManagerListener.Received().OnIsBusyChanged(Args.Bool);
            repositoryManagerListener.DidNotReceive().CurrentBranchUpdated(Args.NullableConfigBranch, Args.NullableConfigRemote);
            repositoryManagerListener.DidNotReceive().GitStatusUpdated(Args.GitStatus);
            repositoryManagerListener.DidNotReceive().GitLocksUpdated(Args.GitLocks);
            repositoryManagerListener.DidNotReceive().GitLogUpdated(Args.GitLogs);
            repositoryManagerListener.Received().LocalBranchesUpdated(Args.LocalBranchDictionary);
            repositoryManagerListener.DidNotReceive().RemoteBranchesUpdated(Args.RemoteDictionary, Args.RemoteBranchDictionary);

            repositoryManagerListener.ClearReceivedCalls();
            repositoryManagerEvents.Reset();

            await RepositoryManager.CreateBranch("feature2/document2", "feature/document").StartAsAsync();
            await TaskManager.Wait();

            RepositoryManager.WaitForEvents();
            repositoryManagerEvents.WaitForNotBusy();

            repositoryManagerEvents.LocalBranchesUpdated.WaitOne(TimeSpan.FromSeconds(2)).Should().BeTrue();

            repositoryManagerListener.Received().OnIsBusyChanged(Args.Bool);
            repositoryManagerListener.DidNotReceive().CurrentBranchUpdated(Args.NullableConfigBranch, Args.NullableConfigRemote);
            repositoryManagerListener.DidNotReceive().GitStatusUpdated(Args.GitStatus);
            repositoryManagerListener.DidNotReceive().GitLocksUpdated(Args.GitLocks);
            repositoryManagerListener.DidNotReceive().GitLogUpdated(Args.GitLogs);
            repositoryManagerListener.Received().LocalBranchesUpdated(Args.LocalBranchDictionary);
            repositoryManagerListener.DidNotReceive().RemoteBranchesUpdated(Args.RemoteDictionary, Args.RemoteBranchDictionary);
        }

        [Test]
        public async Task ShouldDetectChangesToRemotes()
        {
            var repositoryManagerListener = Substitute.For<IRepositoryManagerListener>();

            await Initialize(TestRepoMasterCleanSynchronized, initializeRepository: false,
                onRepositoryManagerCreated: manager => {
                    repositoryManagerListener.AttachListener(manager, repositoryManagerEvents);
                });

            repositoryManagerEvents.CurrentBranchUpdated.WaitOne(TimeSpan.FromSeconds(20)).Should().BeTrue();
            repositoryManagerEvents.LocalBranchesUpdated.WaitOne(TimeSpan.FromSeconds(20)).Should().BeTrue();
            repositoryManagerEvents.RemoteBranchesUpdated.WaitOne(TimeSpan.FromSeconds(20)).Should().BeTrue();

            repositoryManagerListener.ClearReceivedCalls();

            await RepositoryManager.RemoteRemove("origin").StartAsAsync();
            await TaskManager.Wait();

            RepositoryManager.WaitForEvents();
            repositoryManagerEvents.WaitForNotBusy();

            repositoryManagerEvents.CurrentBranchUpdated.WaitOne(TimeSpan.FromSeconds(2)).Should().BeTrue();
            repositoryManagerEvents.RemoteBranchesUpdated.WaitOne(TimeSpan.FromSeconds(2)).Should().BeTrue();
            repositoryManagerEvents.LocalBranchesUpdated.WaitOne(TimeSpan.FromSeconds(2)).Should().BeTrue();

            repositoryManagerListener.Received().OnIsBusyChanged(Args.Bool);
            repositoryManagerListener.Received().CurrentBranchUpdated(Args.NullableConfigBranch, Args.NullableConfigRemote);
            repositoryManagerListener.DidNotReceive().GitStatusUpdated(Args.GitStatus);
            repositoryManagerListener.DidNotReceive().GitLocksUpdated(Args.GitLocks);
            repositoryManagerListener.DidNotReceive().GitLogUpdated(Args.GitLogs);
            repositoryManagerListener.Received().LocalBranchesUpdated(Args.LocalBranchDictionary);
            repositoryManagerListener.Received().RemoteBranchesUpdated(Args.RemoteDictionary, Args.RemoteBranchDictionary);

            repositoryManagerListener.ClearReceivedCalls();
            repositoryManagerEvents.Reset();

            await RepositoryManager.RemoteAdd("origin", "https://github.com/EvilShana/IOTestsRepo.git").StartAsAsync();
            await TaskManager.Wait();

            RepositoryManager.WaitForEvents();
            repositoryManagerEvents.WaitForNotBusy();

            repositoryManagerEvents.CurrentBranchUpdated.WaitOne(TimeSpan.FromSeconds(2)).Should().BeTrue();
            repositoryManagerEvents.RemoteBranchesUpdated.WaitOne(TimeSpan.FromSeconds(2)).Should().BeTrue();
            repositoryManagerEvents.LocalBranchesUpdated.WaitOne(TimeSpan.FromSeconds(2)).Should().BeTrue();

            repositoryManagerListener.Received().OnIsBusyChanged(Args.Bool);
            repositoryManagerListener.Received().CurrentBranchUpdated(Args.NullableConfigBranch, Args.NullableConfigRemote);
            repositoryManagerListener.DidNotReceive().GitStatusUpdated(Args.GitStatus);
            repositoryManagerListener.DidNotReceive().GitLocksUpdated(Args.GitLocks);
            repositoryManagerListener.DidNotReceive().GitLogUpdated(Args.GitLogs);
            repositoryManagerListener.Received().LocalBranchesUpdated(Args.LocalBranchDictionary);
            repositoryManagerListener.Received().RemoteBranchesUpdated(Args.RemoteDictionary, Args.RemoteBranchDictionary);
        }

        [Test]
        public async Task ShouldDetectChangesToRemotesWhenSwitchingBranches()
        {
            var repositoryManagerListener = Substitute.For<IRepositoryManagerListener>();

            await Initialize(TestRepoMasterTwoRemotes, initializeRepository: false,
                onRepositoryManagerCreated: manager => {
                    repositoryManagerListener.AttachListener(manager, repositoryManagerEvents);
                });

            repositoryManagerEvents.CurrentBranchUpdated.WaitOne(TimeSpan.FromSeconds(20)).Should().BeTrue();
            repositoryManagerEvents.LocalBranchesUpdated.WaitOne(TimeSpan.FromSeconds(20)).Should().BeTrue();
            repositoryManagerEvents.RemoteBranchesUpdated.WaitOne(TimeSpan.FromSeconds(20)).Should().BeTrue();

            repositoryManagerListener.ClearReceivedCalls();

            await RepositoryManager.CreateBranch("branch2", "another/master")
                .StartAsAsync();
            await TaskManager.Wait();

            RepositoryManager.WaitForEvents();
            repositoryManagerEvents.WaitForNotBusy();

            repositoryManagerEvents.CurrentBranchUpdated.WaitOne(TimeSpan.FromSeconds(2)).Should().BeTrue();
            repositoryManagerEvents.LocalBranchesUpdated.WaitOne(TimeSpan.FromSeconds(2)).Should().BeTrue();
            repositoryManagerEvents.RemoteBranchesUpdated.WaitOne(TimeSpan.FromSeconds(2)).Should().BeTrue();

            repositoryManagerListener.Received().OnIsBusyChanged(Args.Bool);
            repositoryManagerListener.Received().CurrentBranchUpdated(Args.NullableConfigBranch, Args.NullableConfigRemote);
            repositoryManagerListener.DidNotReceive().GitStatusUpdated(Args.GitStatus);
            repositoryManagerListener.DidNotReceive().GitLocksUpdated(Args.GitLocks);
            repositoryManagerListener.DidNotReceive().GitLogUpdated(Args.GitLogs);
            repositoryManagerListener.Received().LocalBranchesUpdated(Args.LocalBranchDictionary);
            repositoryManagerListener.Received().RemoteBranchesUpdated(Args.RemoteDictionary, Args.RemoteBranchDictionary);

            repositoryManagerListener.ClearReceivedCalls();
            repositoryManagerEvents.Reset();

            await RepositoryManager.SwitchBranch("branch2")
                                   .StartAsAsync();
            await TaskManager.Wait();

            RepositoryManager.WaitForEvents();
            repositoryManagerEvents.WaitForNotBusy();

            repositoryManagerEvents.CurrentBranchUpdated.WaitOne(TimeSpan.FromSeconds(2)).Should().BeTrue();

            repositoryManagerListener.Received().OnIsBusyChanged(Args.Bool);
            repositoryManagerListener.Received().CurrentBranchUpdated(Args.NullableConfigBranch, Args.NullableConfigRemote);
            repositoryManagerListener.DidNotReceive().GitStatusUpdated(Args.GitStatus);
            repositoryManagerListener.DidNotReceive().GitLocksUpdated(Args.GitLocks);
            repositoryManagerListener.DidNotReceive().GitLogUpdated(Args.GitLogs);
            repositoryManagerListener.DidNotReceive().LocalBranchesUpdated(Args.LocalBranchDictionary);
            repositoryManagerListener.DidNotReceive().RemoteBranchesUpdated(Args.RemoteDictionary, Args.RemoteBranchDictionary);
        }

        [Test]
        public async Task ShouldDetectGitPull()
        {
            var repositoryManagerListener = Substitute.For<IRepositoryManagerListener>();

            await Initialize(TestRepoMasterCleanSynchronized, initializeRepository: false,
                onRepositoryManagerCreated: manager => {
                    repositoryManagerListener.AttachListener(manager, repositoryManagerEvents);
                });

            repositoryManagerEvents.CurrentBranchUpdated.WaitOne(TimeSpan.FromSeconds(20)).Should().BeTrue();
            repositoryManagerEvents.LocalBranchesUpdated.WaitOne(TimeSpan.FromSeconds(20)).Should().BeTrue();
            repositoryManagerEvents.RemoteBranchesUpdated.WaitOne(TimeSpan.FromSeconds(20)).Should().BeTrue();

            repositoryManagerListener.ClearReceivedCalls();

            await RepositoryManager.Pull("origin", "master").StartAsAsync();
            await TaskManager.Wait();

            RepositoryManager.WaitForEvents();
            repositoryManagerEvents.WaitForNotBusy();

            repositoryManagerEvents.LocalBranchesUpdated.WaitOne(TimeSpan.FromSeconds(2)).Should().BeTrue();

            repositoryManagerListener.Received().OnIsBusyChanged(Args.Bool);
            repositoryManagerListener.DidNotReceive().CurrentBranchUpdated(Args.NullableConfigBranch, Args.NullableConfigRemote);
            repositoryManagerListener.DidNotReceive().GitStatusUpdated(Args.GitStatus);
            repositoryManagerListener.DidNotReceive().GitLocksUpdated(Args.GitLocks);
            repositoryManagerListener.DidNotReceive().GitLogUpdated(Args.GitLogs);
            repositoryManagerListener.Received().LocalBranchesUpdated(Args.LocalBranchDictionary);
            repositoryManagerListener.DidNotReceive().RemoteBranchesUpdated(Args.RemoteDictionary, Args.RemoteBranchDictionary);
        }

        [Test]
        public async Task ShouldDetectGitFetch()
        {
            var repositoryManagerListener = Substitute.For<IRepositoryManagerListener>();

            await Initialize(TestRepoMasterCleanUnsynchronized, initializeRepository: false,
                onRepositoryManagerCreated: manager => {
                    repositoryManagerListener.AttachListener(manager, repositoryManagerEvents);
                });

            repositoryManagerEvents.CurrentBranchUpdated.WaitOne(TimeSpan.FromSeconds(20)).Should().BeTrue();
            repositoryManagerEvents.LocalBranchesUpdated.WaitOne(TimeSpan.FromSeconds(20)).Should().BeTrue();
            repositoryManagerEvents.RemoteBranchesUpdated.WaitOne(TimeSpan.FromSeconds(20)).Should().BeTrue();

            repositoryManagerListener.ClearReceivedCalls();

            await RepositoryManager.Fetch("origin").StartAsAsync();
            await TaskManager.Wait();

            RepositoryManager.WaitForEvents();
            repositoryManagerEvents.WaitForNotBusy();

            repositoryManagerEvents.LocalBranchesUpdated.WaitOne(TimeSpan.FromSeconds(2)).Should().BeTrue();
            repositoryManagerEvents.RemoteBranchesUpdated.WaitOne(TimeSpan.FromSeconds(2)).Should().BeTrue();

            repositoryManagerListener.Received().OnIsBusyChanged(Args.Bool);
            repositoryManagerListener.DidNotReceive().CurrentBranchUpdated(Args.NullableConfigBranch, Args.NullableConfigRemote);
            repositoryManagerListener.DidNotReceive().GitStatusUpdated(Args.GitStatus);
            repositoryManagerListener.DidNotReceive().GitLocksUpdated(Args.GitLocks);
            repositoryManagerListener.DidNotReceive().GitLogUpdated(Args.GitLogs);
            repositoryManagerListener.Received().LocalBranchesUpdated(Args.LocalBranchDictionary);
            repositoryManagerListener.Received().RemoteBranchesUpdated(Args.RemoteDictionary, Args.RemoteBranchDictionary);
        }
    }
}
