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
        public async Task ShouldDoNothingOnInitialize()
        {
            await Initialize(TestRepoMasterCleanSynchronized, initializeRepository: false);

            var repositoryManagerListener = Substitute.For<IRepositoryManagerListener>();
            repositoryManagerListener.AttachListener(RepositoryManager, repositoryManagerEvents);

            RepositoryManager.WaitForEvents();
            repositoryManagerEvents.WaitForNotBusy();

            repositoryManagerListener.AssertDidNotReceiveAnyCalls();
        }

        [Test]
        public async Task ShouldDetectFileChanges()
        {
            await Initialize(TestRepoMasterCleanSynchronized, initializeRepository: false);

            var repositoryManagerListener = Substitute.For<IRepositoryManagerListener>();
            repositoryManagerListener.AttachListener(RepositoryManager, repositoryManagerEvents);

            var foobarTxt = TestRepoMasterCleanSynchronized.Combine("foobar.txt");
            foobarTxt.WriteAllText("foobar");

            await TaskManager.Wait();
            RepositoryManager.WaitForEvents();
            repositoryManagerEvents.WaitForNotBusy();

            repositoryManagerListener.Received().OnIsBusyChanged(Args.Bool);
            repositoryManagerListener.DidNotReceive().OnCurrentBranchAndRemoteUpdated(Arg.Any<ConfigBranch?>(), Arg.Any<ConfigRemote?>());
            repositoryManagerListener.DidNotReceive().OnLocalBranchListUpdated(Arg.Any<Dictionary<string, ConfigBranch>>());
            repositoryManagerListener.DidNotReceive().OnRemoteBranchListUpdated(Arg.Any<Dictionary<string, ConfigRemote>>(), Arg.Any<Dictionary<string, Dictionary<string, ConfigBranch>>>());
            repositoryManagerListener.DidNotReceive().OnLocalBranchUpdated(Args.String);
            repositoryManagerListener.DidNotReceive().OnLocalBranchAdded(Args.String);
            repositoryManagerListener.DidNotReceive().OnLocalBranchRemoved(Args.String);
            repositoryManagerListener.DidNotReceive().OnRemoteBranchAdded(Args.String, Args.String);
            repositoryManagerListener.DidNotReceive().OnRemoteBranchRemoved(Args.String, Args.String);
        }

        [Test]
        public async Task ShouldAddAndCommitFiles()
        {
            await Initialize(TestRepoMasterCleanSynchronized, initializeRepository: false);

            var repositoryManagerListener = Substitute.For<IRepositoryManagerListener>();
            repositoryManagerListener.AttachListener(RepositoryManager, repositoryManagerEvents);

            var expectedLocalBranch = "master";

            var foobarTxt = TestRepoMasterCleanSynchronized.Combine("foobar.txt");
            foobarTxt.WriteAllText("foobar");

            var testDocumentTxt = TestRepoMasterCleanSynchronized.Combine("Assets", "TestDocument.txt");
            testDocumentTxt.WriteAllText("foobar");

            await TaskManager.Wait();

            //Intentionally wait two cycles, in case the first cycle did not pick up all events
            RepositoryManager.WaitForEvents();
            repositoryManagerEvents.WaitForNotBusy();

            RepositoryManager.WaitForEvents();
            repositoryManagerEvents.WaitForNotBusy();
            repositoryManagerListener.Received().OnIsBusyChanged(Args.Bool);
            repositoryManagerListener.DidNotReceive().OnCurrentBranchAndRemoteUpdated(Arg.Any<ConfigBranch?>(), Arg.Any<ConfigRemote?>());
            repositoryManagerListener.DidNotReceive().OnLocalBranchListUpdated(Arg.Any<Dictionary<string, ConfigBranch>>());
            repositoryManagerListener.DidNotReceive().OnRemoteBranchListUpdated(Arg.Any<Dictionary<string, ConfigRemote>>(), Arg.Any<Dictionary<string, Dictionary<string, ConfigBranch>>>());
            repositoryManagerListener.DidNotReceive().OnLocalBranchUpdated(Args.String);
            repositoryManagerListener.DidNotReceive().OnLocalBranchAdded(Args.String);
            repositoryManagerListener.DidNotReceive().OnLocalBranchRemoved(Args.String);
            repositoryManagerListener.DidNotReceive().OnRemoteBranchAdded(Args.String, Args.String);
            repositoryManagerListener.DidNotReceive().OnRemoteBranchRemoved(Args.String, Args.String);

            repositoryManagerListener.ClearReceivedCalls();
            repositoryManagerEvents.Reset();

            await RepositoryManager
                .CommitFiles(new List<string> { "Assets\\TestDocument.txt", "foobar.txt" }, "IntegrationTest Commit", string.Empty)
                .StartAsAsync();

            await TaskManager.Wait();
            RepositoryManager.WaitForEvents();
            repositoryManagerEvents.WaitForNotBusy();

            repositoryManagerListener.Received().OnIsBusyChanged(Args.Bool);
            repositoryManagerListener.DidNotReceive().OnCurrentBranchAndRemoteUpdated(Arg.Any<ConfigBranch?>(), Arg.Any<ConfigRemote?>());
            repositoryManagerListener.DidNotReceive().OnLocalBranchListUpdated(Arg.Any<Dictionary<string, ConfigBranch>>());
            repositoryManagerListener.DidNotReceive().OnRemoteBranchListUpdated(Arg.Any<Dictionary<string, ConfigRemote>>(), Arg.Any<Dictionary<string, Dictionary<string, ConfigBranch>>>());
            repositoryManagerListener.Received().OnLocalBranchUpdated(expectedLocalBranch);
            repositoryManagerListener.DidNotReceive().OnLocalBranchAdded(Args.String);
            repositoryManagerListener.DidNotReceive().OnLocalBranchRemoved(Args.String);
            repositoryManagerListener.DidNotReceive().OnRemoteBranchAdded(Args.String, Args.String);
            repositoryManagerListener.DidNotReceive().OnRemoteBranchRemoved(Args.String, Args.String);
        }

        [Test]
        public async Task ShouldAddAndCommitAllFiles()
        {
            await Initialize(TestRepoMasterCleanSynchronized, initializeRepository: false);

            var repositoryManagerListener = Substitute.For<IRepositoryManagerListener>();
            repositoryManagerListener.AttachListener(RepositoryManager, repositoryManagerEvents);

            var expectedLocalBranch = "master";

            var foobarTxt = TestRepoMasterCleanSynchronized.Combine("foobar.txt");
            foobarTxt.WriteAllText("foobar");

            var testDocumentTxt = TestRepoMasterCleanSynchronized.Combine("Assets", "TestDocument.txt");
            testDocumentTxt.WriteAllText("foobar");

            await TaskManager.Wait();
            RepositoryManager.WaitForEvents();
            repositoryManagerEvents.WaitForNotBusy();

            repositoryManagerListener.Received().OnIsBusyChanged(Args.Bool);
            repositoryManagerListener.DidNotReceive().OnCurrentBranchAndRemoteUpdated(Arg.Any<ConfigBranch?>(), Arg.Any<ConfigRemote?>());
            repositoryManagerListener.DidNotReceive().OnLocalBranchListUpdated(Arg.Any<Dictionary<string, ConfigBranch>>());
            repositoryManagerListener.DidNotReceive().OnRemoteBranchListUpdated(Arg.Any<Dictionary<string, ConfigRemote>>(), Arg.Any<Dictionary<string, Dictionary<string, ConfigBranch>>>());
            repositoryManagerListener.DidNotReceive().OnLocalBranchUpdated(Args.String);
            repositoryManagerListener.DidNotReceive().OnLocalBranchAdded(Args.String);
            repositoryManagerListener.DidNotReceive().OnLocalBranchRemoved(Args.String);
            repositoryManagerListener.DidNotReceive().OnRemoteBranchAdded(Args.String, Args.String);
            repositoryManagerListener.DidNotReceive().OnRemoteBranchRemoved(Args.String, Args.String);

            repositoryManagerListener.ClearReceivedCalls();
            repositoryManagerEvents.Reset();

            await RepositoryManager
                .CommitAllFiles("IntegrationTest Commit", string.Empty)
                .StartAsAsync();

            await TaskManager.Wait();
            RepositoryManager.WaitForEvents();
            repositoryManagerEvents.WaitForNotBusy();

            repositoryManagerListener.Received().OnIsBusyChanged(Args.Bool);
            repositoryManagerListener.DidNotReceive().OnCurrentBranchAndRemoteUpdated(Arg.Any<ConfigBranch?>(), Arg.Any<ConfigRemote?>());
            repositoryManagerListener.DidNotReceive().OnLocalBranchListUpdated(Arg.Any<Dictionary<string, ConfigBranch>>());
            repositoryManagerListener.DidNotReceive().OnRemoteBranchListUpdated(Arg.Any<Dictionary<string, ConfigRemote>>(), Arg.Any<Dictionary<string, Dictionary<string, ConfigBranch>>>());
            repositoryManagerListener.Received().OnLocalBranchUpdated(expectedLocalBranch);
            repositoryManagerListener.DidNotReceive().OnLocalBranchAdded(Args.String);
            repositoryManagerListener.DidNotReceive().OnLocalBranchRemoved(Args.String);
            repositoryManagerListener.DidNotReceive().OnRemoteBranchAdded(Args.String, Args.String);
            repositoryManagerListener.DidNotReceive().OnRemoteBranchRemoved(Args.String, Args.String);
        }

        [Test]
        public async Task ShouldDetectBranchChange()
        {
            await Initialize(TestRepoMasterCleanSynchronized, initializeRepository: false);

            var repositoryManagerListener = Substitute.For<IRepositoryManagerListener>();
            repositoryManagerListener.AttachListener(RepositoryManager, repositoryManagerEvents);

            var expectedLocalBranch = "feature/document";

            Logger.Trace("Starting test");

            await RepositoryManager.SwitchBranch(expectedLocalBranch).StartAsAsync();

            await TaskManager.Wait();
            RepositoryManager.WaitForEvents();
            repositoryManagerEvents.WaitForNotBusy();

            repositoryManagerListener.Received().OnIsBusyChanged(Args.Bool);
            repositoryManagerListener.Received().OnCurrentBranchAndRemoteUpdated(Arg.Any<ConfigBranch?>(), Arg.Any<ConfigRemote?>());
            repositoryManagerListener.DidNotReceive().OnLocalBranchListUpdated(Arg.Any<Dictionary<string, ConfigBranch>>());
            repositoryManagerListener.DidNotReceive().OnRemoteBranchListUpdated(Arg.Any<Dictionary<string, ConfigRemote>>(), Arg.Any<Dictionary<string, Dictionary<string, ConfigBranch>>>());
            repositoryManagerListener.DidNotReceive().OnLocalBranchUpdated(Args.String);
            repositoryManagerListener.DidNotReceive().OnLocalBranchAdded(Args.String);
            repositoryManagerListener.DidNotReceive().OnLocalBranchRemoved(Args.String);
            repositoryManagerListener.DidNotReceive().OnRemoteBranchAdded(Args.String, Args.String);
            repositoryManagerListener.DidNotReceive().OnRemoteBranchRemoved(Args.String, Args.String);
        }

        [Test]
        public async Task ShouldDetectBranchDelete()
        {
            await Initialize(TestRepoMasterCleanSynchronized, initializeRepository: false);

            var repositoryManagerListener = Substitute.For<IRepositoryManagerListener>();
            repositoryManagerListener.AttachListener(RepositoryManager, repositoryManagerEvents);

            var deletedBranch = "feature/document";
            await RepositoryManager.DeleteBranch(deletedBranch, true).StartAsAsync();
            await TaskManager.Wait();
            RepositoryManager.WaitForEvents();
            repositoryManagerEvents.WaitForNotBusy();

            repositoryManagerListener.Received().OnIsBusyChanged(Args.Bool);
            repositoryManagerListener.Received().OnCurrentBranchAndRemoteUpdated(Arg.Any<ConfigBranch?>(), Arg.Any<ConfigRemote?>());
            repositoryManagerListener.Received().OnLocalBranchListUpdated(Arg.Any<Dictionary<string, ConfigBranch>>());
            repositoryManagerListener.Received().OnRemoteBranchListUpdated(Arg.Any<Dictionary<string, ConfigRemote>>(), Arg.Any<Dictionary<string, Dictionary<string, ConfigBranch>>>());
            repositoryManagerListener.DidNotReceive().OnLocalBranchUpdated(Args.String);
            repositoryManagerListener.DidNotReceive().OnLocalBranchAdded(Args.String);
            repositoryManagerListener.Received().OnLocalBranchRemoved(deletedBranch);
            repositoryManagerListener.DidNotReceive().OnRemoteBranchAdded(Args.String, Args.String);
            repositoryManagerListener.DidNotReceive().OnRemoteBranchRemoved(Args.String, Args.String);
        }

        [Test]
        public async Task ShouldDetectBranchCreate()
        {
            await Initialize(TestRepoMasterCleanSynchronized, initializeRepository: false);

            var repositoryManagerListener = Substitute.For<IRepositoryManagerListener>();
            repositoryManagerListener.AttachListener(RepositoryManager, repositoryManagerEvents);

            var createdBranch1 = "feature/document2";
            await RepositoryManager.CreateBranch(createdBranch1, "feature/document").StartAsAsync();
            await TaskManager.Wait();
            RepositoryManager.WaitForEvents();
            repositoryManagerEvents.WaitForNotBusy();

            repositoryManagerListener.Received().OnIsBusyChanged(Args.Bool);
            repositoryManagerListener.DidNotReceive().OnCurrentBranchAndRemoteUpdated(Arg.Any<ConfigBranch?>(), Arg.Any<ConfigRemote?>());
            repositoryManagerListener.DidNotReceive().OnLocalBranchListUpdated(Arg.Any<Dictionary<string, ConfigBranch>>());
            repositoryManagerListener.DidNotReceive().OnRemoteBranchListUpdated(Arg.Any<Dictionary<string, ConfigRemote>>(), Arg.Any<Dictionary<string, Dictionary<string, ConfigBranch>>>());
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
            repositoryManagerEvents.WaitForNotBusy();

            repositoryManagerListener.Received().OnIsBusyChanged(Args.Bool);
            repositoryManagerListener.DidNotReceive().OnCurrentBranchAndRemoteUpdated(Arg.Any<ConfigBranch?>(), Arg.Any<ConfigRemote?>());
            repositoryManagerListener.DidNotReceive().OnLocalBranchListUpdated(Arg.Any<Dictionary<string, ConfigBranch>>());
            repositoryManagerListener.DidNotReceive().OnRemoteBranchListUpdated(Arg.Any<Dictionary<string, ConfigRemote>>(), Arg.Any<Dictionary<string, Dictionary<string, ConfigBranch>>>());
            repositoryManagerListener.DidNotReceive().OnLocalBranchUpdated(Args.String);
            repositoryManagerListener.Received().OnLocalBranchAdded(createdBranch2);
            repositoryManagerListener.DidNotReceive().OnLocalBranchRemoved(Args.String);
            repositoryManagerListener.DidNotReceive().OnRemoteBranchAdded(Args.String, Args.String);
            repositoryManagerListener.DidNotReceive().OnRemoteBranchRemoved(Args.String, Args.String);
        }

        [Test]
        public async Task ShouldDetectChangesToRemotes()
        {
            await Initialize(TestRepoMasterCleanSynchronized, initializeRepository: false);

            var repositoryManagerListener = Substitute.For<IRepositoryManagerListener>();
            repositoryManagerListener.AttachListener(RepositoryManager, repositoryManagerEvents);

            await RepositoryManager.RemoteRemove("origin").StartAsAsync();
            await TaskManager.Wait();

            RepositoryManager.WaitForEvents();
            repositoryManagerEvents.WaitForNotBusy();

            repositoryManagerEvents.OnRemoteBranchListUpdated.WaitOne(TimeSpan.FromSeconds(1));
            repositoryManagerEvents.OnLocalBranchListUpdated.WaitOne(TimeSpan.FromSeconds(1));

            repositoryManagerListener.Received().OnIsBusyChanged(Args.Bool);
            repositoryManagerListener.Received().OnCurrentBranchAndRemoteUpdated(Arg.Any<ConfigBranch?>(), Arg.Any<ConfigRemote?>());
            repositoryManagerListener.Received().OnLocalBranchListUpdated(Arg.Any<Dictionary<string, ConfigBranch>>());
            repositoryManagerListener.Received().OnRemoteBranchListUpdated(Arg.Any<Dictionary<string, ConfigRemote>>(), Arg.Any<Dictionary<string, Dictionary<string, ConfigBranch>>>());
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
            repositoryManagerEvents.WaitForNotBusy();
            repositoryManagerEvents.OnRemoteBranchListUpdated.WaitOne(TimeSpan.FromSeconds(1));
            repositoryManagerEvents.OnLocalBranchListUpdated.WaitOne(TimeSpan.FromSeconds(1));

            repositoryManagerListener.Received().OnIsBusyChanged(Args.Bool);
            repositoryManagerListener.Received().OnCurrentBranchAndRemoteUpdated(Arg.Any<ConfigBranch?>(), Arg.Any<ConfigRemote?>());
            repositoryManagerListener.Received().OnLocalBranchListUpdated(Arg.Any<Dictionary<string, ConfigBranch>>());
            repositoryManagerListener.Received().OnRemoteBranchListUpdated(Arg.Any<Dictionary<string, ConfigRemote>>(), Arg.Any<Dictionary<string, Dictionary<string, ConfigBranch>>>());
            repositoryManagerListener.DidNotReceive().OnLocalBranchUpdated(Args.String);
            repositoryManagerListener.DidNotReceive().OnLocalBranchAdded(Args.String);
            repositoryManagerListener.DidNotReceive().OnLocalBranchRemoved(Args.String);
            repositoryManagerListener.DidNotReceive().OnRemoteBranchAdded(Args.String, Args.String);
            repositoryManagerListener.DidNotReceive().OnRemoteBranchRemoved(Args.String, Args.String);
        }

        [Test]
        public async Task ShouldDetectChangesToRemotesWhenSwitchingBranches()
        {
            await Initialize(TestRepoMasterTwoRemotes, initializeRepository: false);

            var repositoryManagerListener = Substitute.For<IRepositoryManagerListener>();
            repositoryManagerListener.AttachListener(RepositoryManager, repositoryManagerEvents);

            await RepositoryManager.CreateBranch("branch2", "another/master")
                .StartAsAsync();

            await TaskManager.Wait();
            RepositoryManager.WaitForEvents();
            repositoryManagerEvents.WaitForNotBusy();

            repositoryManagerListener.Received().OnIsBusyChanged(Args.Bool);
            repositoryManagerListener.Received().OnCurrentBranchAndRemoteUpdated(Arg.Any<ConfigBranch?>(), Arg.Any<ConfigRemote?>());
            repositoryManagerListener.Received().OnLocalBranchListUpdated(Arg.Any<Dictionary<string, ConfigBranch>>());
            repositoryManagerListener.Received().OnRemoteBranchListUpdated(Arg.Any<Dictionary<string, ConfigRemote>>(), Arg.Any<Dictionary<string, Dictionary<string, ConfigBranch>>>());
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
            repositoryManagerEvents.WaitForNotBusy();
            repositoryManagerEvents.WaitForHeadUpdated();

            repositoryManagerListener.Received().OnIsBusyChanged(Args.Bool);
            repositoryManagerListener.Received().OnCurrentBranchAndRemoteUpdated(Arg.Any<ConfigBranch?>(), Arg.Any<ConfigRemote?>());
            repositoryManagerListener.DidNotReceive().OnLocalBranchListUpdated(Arg.Any<Dictionary<string, ConfigBranch>>());
            repositoryManagerListener.DidNotReceive().OnRemoteBranchListUpdated(Arg.Any<Dictionary<string, ConfigRemote>>(), Arg.Any<Dictionary<string, Dictionary<string, ConfigBranch>>>());
            repositoryManagerListener.DidNotReceive().OnLocalBranchUpdated(Args.String);
            repositoryManagerListener.DidNotReceive().OnLocalBranchAdded(Args.String);
            repositoryManagerListener.DidNotReceive().OnLocalBranchRemoved(Args.String);
            repositoryManagerListener.DidNotReceive().OnRemoteBranchAdded(Args.String, Args.String);
            repositoryManagerListener.DidNotReceive().OnRemoteBranchRemoved(Args.String, Args.String);
        }

        [Test]
        public async Task ShouldDetectGitPull()
        {
            await Initialize(TestRepoMasterCleanSynchronized, initializeRepository: false);

            var repositoryManagerListener = Substitute.For<IRepositoryManagerListener>();
            repositoryManagerListener.AttachListener(RepositoryManager, repositoryManagerEvents);

            await RepositoryManager.Pull("origin", "master").StartAsAsync();
            await TaskManager.Wait();
            RepositoryManager.WaitForEvents();
            repositoryManagerEvents.WaitForNotBusy();

            repositoryManagerListener.Received().OnIsBusyChanged(Args.Bool);
            repositoryManagerListener.DidNotReceive().OnCurrentBranchAndRemoteUpdated(Arg.Any<ConfigBranch?>(), Arg.Any<ConfigRemote?>());
            repositoryManagerListener.DidNotReceive().OnLocalBranchListUpdated(Arg.Any<Dictionary<string, ConfigBranch>>());
            repositoryManagerListener.DidNotReceive().OnRemoteBranchListUpdated(Arg.Any<Dictionary<string, ConfigRemote>>(), Arg.Any<Dictionary<string, Dictionary<string, ConfigBranch>>>());
            repositoryManagerListener.Received().OnLocalBranchUpdated(Args.String);
            repositoryManagerListener.DidNotReceive().OnLocalBranchAdded(Args.String);
            repositoryManagerListener.DidNotReceive().OnLocalBranchRemoved(Args.String);
            repositoryManagerListener.DidNotReceive().OnRemoteBranchAdded(Args.String, Args.String);
            repositoryManagerListener.DidNotReceive().OnRemoteBranchRemoved(Args.String, Args.String);

            repositoryManagerEvents.Reset();
            repositoryManagerEvents.WaitForNotBusy();
        }

        [Test]
        public async Task ShouldDetectGitFetch()
        {
            await Initialize(TestRepoMasterCleanUnsynchronized, initializeRepository: false);

            var repositoryManagerListener = Substitute.For<IRepositoryManagerListener>();
            repositoryManagerListener.AttachListener(RepositoryManager, repositoryManagerEvents);

            await RepositoryManager.Fetch("origin").StartAsAsync();
            await TaskManager.Wait();
            RepositoryManager.WaitForEvents();
            repositoryManagerEvents.WaitForNotBusy();

            repositoryManagerListener.Received().OnIsBusyChanged(Args.Bool);
            repositoryManagerListener.DidNotReceive().OnCurrentBranchAndRemoteUpdated(Arg.Any<ConfigBranch?>(), Arg.Any<ConfigRemote?>());
            repositoryManagerListener.DidNotReceive().OnLocalBranchListUpdated(Arg.Any<Dictionary<string, ConfigBranch>>());
            repositoryManagerListener.DidNotReceive().OnRemoteBranchListUpdated(Arg.Any<Dictionary<string, ConfigRemote>>(), Arg.Any<Dictionary<string, Dictionary<string, ConfigBranch>>>());
            repositoryManagerListener.DidNotReceive().OnLocalBranchUpdated(Args.String);
            repositoryManagerListener.DidNotReceive().OnLocalBranchAdded(Args.String);
            repositoryManagerListener.DidNotReceive().OnLocalBranchRemoved(Args.String);
            repositoryManagerListener.Received().OnRemoteBranchAdded(Args.String, Args.String);
            repositoryManagerListener.DidNotReceive().OnRemoteBranchRemoved(Args.String, Args.String);
        }
    }
}
