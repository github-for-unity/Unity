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
    [TestFixture, Category("DoNotRunOnAppVeyor")]
    class RepositoryManagerTests : BaseGitEnvironmentTest
    {
        private RepositoryManagerEvents repositoryManagerEvents;
        private TimeSpan Timeout = TimeSpan.FromSeconds(5);

        public override void OnSetup()
        {
            base.OnSetup();
            repositoryManagerEvents = new RepositoryManagerEvents();
        }

        [Test]
        public async Task ShouldPerformBasicInitialize()
        {
            Logger.Trace("Starting ShouldPerformBasicInitialize");

            try
            {
                var repositoryManagerListener = Substitute.For<IRepositoryManagerListener>();

                await Initialize(TestRepoMasterCleanSynchronized, initializeRepository: false,
                    onRepositoryManagerCreated: manager => {
                        repositoryManagerListener.AttachListener(manager, repositoryManagerEvents);
                    });

                repositoryManagerEvents.CurrentBranchUpdated.WaitOne(Timeout).Should().BeTrue();
                repositoryManagerEvents.LocalBranchesUpdated.WaitOne(Timeout).Should().BeTrue();
                repositoryManagerEvents.RemoteBranchesUpdated.WaitOne(Timeout).Should().BeTrue();
                repositoryManagerEvents.GitLogUpdated.WaitOne(Timeout).Should().BeTrue();
                repositoryManagerEvents.GitAheadBehindStatusUpdated.WaitOne(Timeout).Should().BeTrue();
                repositoryManagerEvents.IsBusy.WaitOne(Timeout).Should().BeTrue();
                repositoryManagerEvents.IsNotBusy.WaitOne(Timeout).Should().BeTrue();

                repositoryManagerListener.Received().OnIsBusyChanged(Args.Bool);
                repositoryManagerListener.Received().CurrentBranchUpdated(Args.NullableConfigBranch, Args.NullableConfigRemote);
                repositoryManagerListener.Received().GitAheadBehindStatusUpdated(Args.GitAheadBehindStatus);
                repositoryManagerListener.DidNotReceive().GitStatusUpdated(Args.GitStatus);
                repositoryManagerListener.DidNotReceive().GitLocksUpdated(Args.GitLocks);
                repositoryManagerListener.Received().GitLogUpdated(Args.GitLogs);
                repositoryManagerListener.Received().LocalBranchesUpdated(Args.LocalBranchDictionary);
                repositoryManagerListener.Received().RemoteBranchesUpdated(Args.RemoteDictionary, Args.RemoteBranchDictionary);
            }
            finally
            {
                Logger.Trace("Ending ShouldPerformBasicInitialize");
            }
        }

        [Test]
        public async Task ShouldDetectFileChanges()
        {
            Logger.Trace("Starting ShouldDetectFileChanges");

            try
            {
                var repositoryManagerListener = Substitute.For<IRepositoryManagerListener>();

                await Initialize(TestRepoMasterCleanSynchronized, initializeRepository: false,
                    onRepositoryManagerCreated: manager => {
                        repositoryManagerListener.AttachListener(manager, repositoryManagerEvents);
                    });

                repositoryManagerEvents.CurrentBranchUpdated.WaitOne(Timeout).Should().BeTrue();
                repositoryManagerEvents.LocalBranchesUpdated.WaitOne(Timeout).Should().BeTrue();
                repositoryManagerEvents.RemoteBranchesUpdated.WaitOne(Timeout).Should().BeTrue();
                repositoryManagerEvents.GitLogUpdated.WaitOne(Timeout).Should().BeTrue();
                repositoryManagerEvents.GitAheadBehindStatusUpdated.WaitOne(Timeout).Should().BeTrue();
                repositoryManagerEvents.IsBusy.WaitOne(Timeout).Should().BeTrue();
                repositoryManagerEvents.IsNotBusy.WaitOne(Timeout).Should().BeTrue();

                repositoryManagerListener.ClearReceivedCalls();
                repositoryManagerEvents.Reset();

                var foobarTxt = TestRepoMasterCleanSynchronized.Combine("foobar.txt");
                foobarTxt.WriteAllText("foobar");

                await TaskManager.Wait();

                RepositoryManager.WaitForEvents();
                repositoryManagerEvents.WaitForNotBusy();

                repositoryManagerEvents.GitStatusUpdated.WaitOne(Timeout).Should().BeTrue();

                repositoryManagerListener.Received().OnIsBusyChanged(Args.Bool);
                repositoryManagerListener.DidNotReceive().CurrentBranchUpdated(Args.NullableConfigBranch, Args.NullableConfigRemote);
                repositoryManagerListener.DidNotReceive().GitAheadBehindStatusUpdated(Args.GitAheadBehindStatus);
                repositoryManagerListener.Received().GitStatusUpdated(Args.GitStatus);
                repositoryManagerListener.DidNotReceive().GitLocksUpdated(Args.GitLocks);
                repositoryManagerListener.DidNotReceive().GitLogUpdated(Args.GitLogs);
                repositoryManagerListener.DidNotReceive().LocalBranchesUpdated(Args.LocalBranchDictionary);
                repositoryManagerListener.DidNotReceive().RemoteBranchesUpdated(Args.RemoteDictionary, Args.RemoteBranchDictionary);
            }
            finally
            {
                Logger.Trace("Ending ShouldDetectFileChanges");
            }
        }

        [Test]
        public async Task ShouldAddAndCommitFiles()
        {
            Logger.Trace("Starting ShouldAddAndCommitFiles");

            try
            {
                var repositoryManagerListener = Substitute.For<IRepositoryManagerListener>();

                await Initialize(TestRepoMasterCleanSynchronized, initializeRepository: false,
                    onRepositoryManagerCreated: manager => {
                        repositoryManagerListener.AttachListener(manager, repositoryManagerEvents);
                    });

                repositoryManagerEvents.CurrentBranchUpdated.WaitOne(Timeout).Should().BeTrue();
                repositoryManagerEvents.LocalBranchesUpdated.WaitOne(Timeout).Should().BeTrue();
                repositoryManagerEvents.RemoteBranchesUpdated.WaitOne(Timeout).Should().BeTrue();
                repositoryManagerEvents.GitLogUpdated.WaitOne(Timeout).Should().BeTrue();
                repositoryManagerEvents.GitAheadBehindStatusUpdated.WaitOne(Timeout).Should().BeTrue();
                repositoryManagerEvents.IsBusy.WaitOne(Timeout).Should().BeTrue();
                repositoryManagerEvents.IsNotBusy.WaitOne(Timeout).Should().BeTrue();

                repositoryManagerListener.ClearReceivedCalls();
                repositoryManagerEvents.Reset();

                var foobarTxt = TestRepoMasterCleanSynchronized.Combine("foobar.txt");
                foobarTxt.WriteAllText("foobar");

                var testDocumentTxt = TestRepoMasterCleanSynchronized.Combine("Assets", "TestDocument.txt");
                testDocumentTxt.WriteAllText("foobar");

                await TaskManager.Wait();

                RepositoryManager.WaitForEvents();
                repositoryManagerEvents.WaitForNotBusy();

                repositoryManagerEvents.GitStatusUpdated.WaitOne(Timeout).Should().BeTrue();

                repositoryManagerListener.Received().OnIsBusyChanged(Args.Bool);
                repositoryManagerListener.DidNotReceive().CurrentBranchUpdated(Args.NullableConfigBranch, Args.NullableConfigRemote);
                repositoryManagerListener.DidNotReceive().GitAheadBehindStatusUpdated(Args.GitAheadBehindStatus);
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

                repositoryManagerEvents.LocalBranchesUpdated.WaitOne(Timeout).Should().BeTrue();
                repositoryManagerEvents.GitStatusUpdated.WaitOne(Timeout).Should().BeTrue();
                repositoryManagerEvents.GitLogUpdated.WaitOne(Timeout).Should().BeTrue();
                repositoryManagerEvents.GitAheadBehindStatusUpdated.WaitOne(Timeout).Should().BeTrue();

                repositoryManagerListener.Received().OnIsBusyChanged(Args.Bool);
                repositoryManagerListener.DidNotReceive().CurrentBranchUpdated(Args.NullableConfigBranch, Args.NullableConfigRemote);
                repositoryManagerListener.Received().GitAheadBehindStatusUpdated(Args.GitAheadBehindStatus);
                repositoryManagerListener.Received().GitStatusUpdated(Args.GitStatus);
                repositoryManagerListener.DidNotReceive().GitLocksUpdated(Args.GitLocks);
                repositoryManagerListener.Received().GitLogUpdated(Args.GitLogs);
                repositoryManagerListener.Received().LocalBranchesUpdated(Args.LocalBranchDictionary);
                repositoryManagerListener.DidNotReceive().RemoteBranchesUpdated(Args.RemoteDictionary, Args.RemoteBranchDictionary);
            }
            finally
            {
                Logger.Trace("Ending ShouldAddAndCommitFiles");
            }
        }

        [Test]
        public async Task ShouldAddAndCommitAllFiles()
        {
            Logger.Trace("Starting ShouldAddAndCommitAllFiles");

            try
            {
                var repositoryManagerListener = Substitute.For<IRepositoryManagerListener>();

                await Initialize(TestRepoMasterCleanSynchronized, initializeRepository: false,
                    onRepositoryManagerCreated: manager => {
                        repositoryManagerListener.AttachListener(manager, repositoryManagerEvents);
                    });

                repositoryManagerEvents.CurrentBranchUpdated.WaitOne(Timeout).Should().BeTrue();
                repositoryManagerEvents.LocalBranchesUpdated.WaitOne(Timeout).Should().BeTrue();
                repositoryManagerEvents.RemoteBranchesUpdated.WaitOne(Timeout).Should().BeTrue();
                repositoryManagerEvents.GitLogUpdated.WaitOne(Timeout).Should().BeTrue();
                repositoryManagerEvents.GitAheadBehindStatusUpdated.WaitOne(Timeout).Should().BeTrue();
                repositoryManagerEvents.IsBusy.WaitOne(Timeout).Should().BeTrue();
                repositoryManagerEvents.IsNotBusy.WaitOne(Timeout).Should().BeTrue();

                repositoryManagerListener.ClearReceivedCalls();
                repositoryManagerEvents.Reset();

                var foobarTxt = TestRepoMasterCleanSynchronized.Combine("foobar.txt");
                foobarTxt.WriteAllText("foobar");

                var testDocumentTxt = TestRepoMasterCleanSynchronized.Combine("Assets", "TestDocument.txt");
                testDocumentTxt.WriteAllText("foobar");

                await TaskManager.Wait();

                RepositoryManager.WaitForEvents();
                repositoryManagerEvents.WaitForNotBusy();

                repositoryManagerEvents.GitStatusUpdated.WaitOne(Timeout).Should().BeTrue();

                repositoryManagerListener.Received().OnIsBusyChanged(Args.Bool);
                repositoryManagerListener.DidNotReceive().CurrentBranchUpdated(Args.NullableConfigBranch, Args.NullableConfigRemote);
                repositoryManagerListener.DidNotReceive().GitAheadBehindStatusUpdated(Args.GitAheadBehindStatus);
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

                repositoryManagerEvents.GitStatusUpdated.WaitOne(Timeout).Should().BeTrue();
                repositoryManagerEvents.LocalBranchesUpdated.WaitOne(Timeout).Should().BeTrue();
                repositoryManagerEvents.GitLogUpdated.WaitOne(Timeout).Should().BeTrue();
                repositoryManagerEvents.GitAheadBehindStatusUpdated.WaitOne(Timeout).Should().BeTrue();

                repositoryManagerListener.Received().OnIsBusyChanged(Args.Bool);
                repositoryManagerListener.DidNotReceive().CurrentBranchUpdated(Args.NullableConfigBranch, Args.NullableConfigRemote);
                repositoryManagerListener.Received().GitAheadBehindStatusUpdated(Args.GitAheadBehindStatus);
                repositoryManagerListener.Received().GitStatusUpdated(Args.GitStatus);
                repositoryManagerListener.DidNotReceive().GitLocksUpdated(Args.GitLocks);
                repositoryManagerListener.Received().GitLogUpdated(Args.GitLogs);
                repositoryManagerListener.Received().LocalBranchesUpdated(Args.LocalBranchDictionary);
                repositoryManagerListener.DidNotReceive().RemoteBranchesUpdated(Args.RemoteDictionary, Args.RemoteBranchDictionary);
            }
            finally
            {
                Logger.Trace("Ending ShouldAddAndCommitAllFiles");
            }
        }

        [Test]
        public async Task ShouldDetectBranchChange()
        {
            Logger.Trace("Starting ShouldDetectBranchChange");

            try
            {
                var repositoryManagerListener = Substitute.For<IRepositoryManagerListener>();

                await Initialize(TestRepoMasterCleanSynchronized, initializeRepository: false,
                    onRepositoryManagerCreated: manager => {
                        repositoryManagerListener.AttachListener(manager, repositoryManagerEvents);
                    });

                repositoryManagerEvents.CurrentBranchUpdated.WaitOne(Timeout).Should().BeTrue();
                repositoryManagerEvents.LocalBranchesUpdated.WaitOne(Timeout).Should().BeTrue();
                repositoryManagerEvents.RemoteBranchesUpdated.WaitOne(Timeout).Should().BeTrue();
                repositoryManagerEvents.GitLogUpdated.WaitOne(Timeout).Should().BeTrue();
                repositoryManagerEvents.GitAheadBehindStatusUpdated.WaitOne(Timeout).Should().BeTrue();
                repositoryManagerEvents.IsBusy.WaitOne(Timeout).Should().BeTrue();
                repositoryManagerEvents.IsNotBusy.WaitOne(Timeout).Should().BeTrue();

                repositoryManagerListener.ClearReceivedCalls();
                repositoryManagerEvents.Reset();

                await RepositoryManager.SwitchBranch("feature/document").StartAsAsync();
                await TaskManager.Wait();

                RepositoryManager.WaitForEvents();
                repositoryManagerEvents.WaitForNotBusy();

                repositoryManagerEvents.CurrentBranchUpdated.WaitOne(Timeout).Should().BeTrue();
                repositoryManagerEvents.GitStatusUpdated.WaitOne(Timeout).Should().BeTrue();
                repositoryManagerEvents.GitLogUpdated.WaitOne(Timeout).Should().BeTrue();

                repositoryManagerListener.Received().OnIsBusyChanged(Args.Bool);
                repositoryManagerListener.Received().CurrentBranchUpdated(Args.NullableConfigBranch, Args.NullableConfigRemote);
                repositoryManagerListener.DidNotReceive().GitAheadBehindStatusUpdated(Args.GitAheadBehindStatus);
                repositoryManagerListener.Received().GitStatusUpdated(Args.GitStatus);
                repositoryManagerListener.DidNotReceive().GitLocksUpdated(Args.GitLocks);
                repositoryManagerListener.Received().GitLogUpdated(Args.GitLogs);
                repositoryManagerListener.DidNotReceive().LocalBranchesUpdated(Args.LocalBranchDictionary);
                repositoryManagerListener.DidNotReceive().RemoteBranchesUpdated(Args.RemoteDictionary, Args.RemoteBranchDictionary);
            }
            finally
            {
                Logger.Trace("Ending ShouldDetectBranchChange");
            }
        }

        [Test]
        public async Task ShouldDetectBranchDelete()
        {
            Logger.Trace("Starting ShouldDetectBranchDelete");

            try
            {
                var repositoryManagerListener = Substitute.For<IRepositoryManagerListener>();

                await Initialize(TestRepoMasterCleanSynchronized, initializeRepository: false,
                    onRepositoryManagerCreated: manager => {
                        repositoryManagerListener.AttachListener(manager, repositoryManagerEvents);
                    });

                repositoryManagerEvents.CurrentBranchUpdated.WaitOne(Timeout).Should().BeTrue();
                repositoryManagerEvents.LocalBranchesUpdated.WaitOne(Timeout).Should().BeTrue();
                repositoryManagerEvents.RemoteBranchesUpdated.WaitOne(Timeout).Should().BeTrue();
                repositoryManagerEvents.GitLogUpdated.WaitOne(Timeout).Should().BeTrue();
                repositoryManagerEvents.GitAheadBehindStatusUpdated.WaitOne(Timeout).Should().BeTrue();
                repositoryManagerEvents.IsBusy.WaitOne(Timeout).Should().BeTrue();
                repositoryManagerEvents.IsNotBusy.WaitOne(Timeout).Should().BeTrue();

                repositoryManagerListener.ClearReceivedCalls();
                repositoryManagerEvents.Reset();

                await RepositoryManager.DeleteBranch("feature/document", true).StartAsAsync();
                await TaskManager.Wait();

                RepositoryManager.WaitForEvents();
                repositoryManagerEvents.WaitForNotBusy();

                repositoryManagerEvents.CurrentBranchUpdated.WaitOne(Timeout).Should().BeTrue();
                repositoryManagerEvents.LocalBranchesUpdated.WaitOne(Timeout).Should().BeTrue();
                repositoryManagerEvents.RemoteBranchesUpdated.WaitOne(Timeout).Should().BeTrue();
                repositoryManagerEvents.GitAheadBehindStatusUpdated.WaitOne(Timeout).Should().BeTrue();
                repositoryManagerEvents.GitLogUpdated.WaitOne(Timeout).Should().BeTrue();

                repositoryManagerListener.Received().OnIsBusyChanged(Args.Bool);
                repositoryManagerListener.Received().CurrentBranchUpdated(Args.NullableConfigBranch, Args.NullableConfigRemote);
                repositoryManagerListener.Received().GitAheadBehindStatusUpdated(Args.GitAheadBehindStatus);
                repositoryManagerListener.DidNotReceive().GitStatusUpdated(Args.GitStatus);
                repositoryManagerListener.DidNotReceive().GitLocksUpdated(Args.GitLocks);
                repositoryManagerListener.Received().GitLogUpdated(Args.GitLogs);
                repositoryManagerListener.Received().LocalBranchesUpdated(Args.LocalBranchDictionary);
                repositoryManagerListener.Received().RemoteBranchesUpdated(Args.RemoteDictionary, Args.RemoteBranchDictionary);
            }
            finally
            {
                Logger.Trace("Ending ShouldDetectBranchDelete");
            }
        }

        [Test]
        public async Task ShouldDetectBranchCreate()
        {
            Logger.Trace("Starting ShouldDetectBranchCreate");

            try
            {
                var repositoryManagerListener = Substitute.For<IRepositoryManagerListener>();

                await Initialize(TestRepoMasterCleanSynchronized, initializeRepository: false,
                    onRepositoryManagerCreated: manager => {
                        repositoryManagerListener.AttachListener(manager, repositoryManagerEvents);
                    });

                repositoryManagerEvents.CurrentBranchUpdated.WaitOne(Timeout).Should().BeTrue();
                repositoryManagerEvents.LocalBranchesUpdated.WaitOne(Timeout).Should().BeTrue();
                repositoryManagerEvents.RemoteBranchesUpdated.WaitOne(Timeout).Should().BeTrue();
                repositoryManagerEvents.GitLogUpdated.WaitOne(Timeout).Should().BeTrue();
                repositoryManagerEvents.GitAheadBehindStatusUpdated.WaitOne(Timeout).Should().BeTrue();
                repositoryManagerEvents.IsBusy.WaitOne(Timeout).Should().BeTrue();
                repositoryManagerEvents.IsNotBusy.WaitOne(Timeout).Should().BeTrue();

                repositoryManagerListener.ClearReceivedCalls();

                var createdBranch1 = "feature/document2";
                await RepositoryManager.CreateBranch(createdBranch1, "feature/document").StartAsAsync();
                await TaskManager.Wait();

                RepositoryManager.WaitForEvents();
                repositoryManagerEvents.WaitForNotBusy();

                repositoryManagerEvents.LocalBranchesUpdated.WaitOne(Timeout).Should().BeTrue();

                repositoryManagerListener.Received().OnIsBusyChanged(Args.Bool);
                repositoryManagerListener.DidNotReceive().CurrentBranchUpdated(Args.NullableConfigBranch, Args.NullableConfigRemote);
                repositoryManagerListener.DidNotReceive().GitAheadBehindStatusUpdated(Args.GitAheadBehindStatus);
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

                repositoryManagerEvents.LocalBranchesUpdated.WaitOne(Timeout).Should().BeTrue();
                repositoryManagerEvents.GitLogUpdated.WaitOne(Timeout).Should().BeTrue();

                repositoryManagerListener.Received().OnIsBusyChanged(Args.Bool);
                repositoryManagerListener.DidNotReceive().CurrentBranchUpdated(Args.NullableConfigBranch, Args.NullableConfigRemote);
                repositoryManagerListener.DidNotReceive().GitAheadBehindStatusUpdated(Args.GitAheadBehindStatus);
                repositoryManagerListener.DidNotReceive().GitStatusUpdated(Args.GitStatus);
                repositoryManagerListener.DidNotReceive().GitLocksUpdated(Args.GitLocks);
                repositoryManagerListener.Received().GitLogUpdated(Args.GitLogs);
                repositoryManagerListener.Received().LocalBranchesUpdated(Args.LocalBranchDictionary);
                repositoryManagerListener.DidNotReceive().RemoteBranchesUpdated(Args.RemoteDictionary, Args.RemoteBranchDictionary);
            }
            finally
            {
                Logger.Trace("Ending ShouldDetectBranchCreate");
            }
        }

        [Test]
        public async Task ShouldDetectChangesToRemotes()
        {
            Logger.Trace("Starting ShouldDetectChangesToRemotes");

            try
            {
                var repositoryManagerListener = Substitute.For<IRepositoryManagerListener>();

                await Initialize(TestRepoMasterCleanSynchronized, initializeRepository: false,
                    onRepositoryManagerCreated: manager => {
                        repositoryManagerListener.AttachListener(manager, repositoryManagerEvents);
                    });

                repositoryManagerEvents.CurrentBranchUpdated.WaitOne(Timeout).Should().BeTrue();
                repositoryManagerEvents.LocalBranchesUpdated.WaitOne(Timeout).Should().BeTrue();
                repositoryManagerEvents.RemoteBranchesUpdated.WaitOne(Timeout).Should().BeTrue();
                repositoryManagerEvents.GitLogUpdated.WaitOne(Timeout).Should().BeTrue();
                repositoryManagerEvents.GitAheadBehindStatusUpdated.WaitOne(Timeout).Should().BeTrue();
                repositoryManagerEvents.IsBusy.WaitOne(Timeout).Should().BeTrue();
                repositoryManagerEvents.IsNotBusy.WaitOne(Timeout).Should().BeTrue();

                repositoryManagerListener.ClearReceivedCalls();
                repositoryManagerEvents.Reset();

                await RepositoryManager.RemoteRemove("origin").StartAsAsync();
                await TaskManager.Wait();

                RepositoryManager.WaitForEvents();
                repositoryManagerEvents.WaitForNotBusy();

                repositoryManagerEvents.CurrentBranchUpdated.WaitOne(Timeout).Should().BeTrue();
                repositoryManagerEvents.RemoteBranchesUpdated.WaitOne(Timeout).Should().BeTrue();
                repositoryManagerEvents.LocalBranchesUpdated.WaitOne(Timeout).Should().BeTrue();
                repositoryManagerEvents.GitLogUpdated.WaitOne(Timeout).Should().BeTrue();
                repositoryManagerEvents.GitAheadBehindStatusUpdated.WaitOne(Timeout).Should().BeTrue();

                repositoryManagerListener.Received().OnIsBusyChanged(Args.Bool);
                repositoryManagerListener.Received().CurrentBranchUpdated(Args.NullableConfigBranch, Args.NullableConfigRemote);
                repositoryManagerListener.Received().GitAheadBehindStatusUpdated(Args.GitAheadBehindStatus);
                repositoryManagerListener.DidNotReceive().GitStatusUpdated(Args.GitStatus);
                repositoryManagerListener.DidNotReceive().GitLocksUpdated(Args.GitLocks);
                repositoryManagerListener.Received().GitLogUpdated(Args.GitLogs);
                repositoryManagerListener.Received().LocalBranchesUpdated(Args.LocalBranchDictionary);
                repositoryManagerListener.Received().RemoteBranchesUpdated(Args.RemoteDictionary, Args.RemoteBranchDictionary);

                repositoryManagerListener.ClearReceivedCalls();
                repositoryManagerEvents.Reset();

                await RepositoryManager.RemoteAdd("origin", "https://github.com/EvilShana/IOTestsRepo.git").StartAsAsync();
                await TaskManager.Wait();

                RepositoryManager.WaitForEvents();
                repositoryManagerEvents.WaitForNotBusy();

                repositoryManagerEvents.CurrentBranchUpdated.WaitOne(Timeout).Should().BeTrue();
                repositoryManagerEvents.RemoteBranchesUpdated.WaitOne(Timeout).Should().BeTrue();
                repositoryManagerEvents.LocalBranchesUpdated.WaitOne(Timeout).Should().BeTrue();
                repositoryManagerEvents.GitLogUpdated.WaitOne(Timeout).Should().BeTrue();
                repositoryManagerEvents.GitAheadBehindStatusUpdated.WaitOne(Timeout).Should().BeTrue();

                repositoryManagerListener.Received().OnIsBusyChanged(Args.Bool);
                repositoryManagerListener.Received().CurrentBranchUpdated(Args.NullableConfigBranch, Args.NullableConfigRemote);
                repositoryManagerListener.Received().GitAheadBehindStatusUpdated(Args.GitAheadBehindStatus);
                repositoryManagerListener.DidNotReceive().GitStatusUpdated(Args.GitStatus);
                repositoryManagerListener.DidNotReceive().GitLocksUpdated(Args.GitLocks);
                repositoryManagerListener.Received().GitLogUpdated(Args.GitLogs);
                repositoryManagerListener.Received().LocalBranchesUpdated(Args.LocalBranchDictionary);
                repositoryManagerListener.Received().RemoteBranchesUpdated(Args.RemoteDictionary, Args.RemoteBranchDictionary);
            }
            finally
            {
                Logger.Trace("Ending ShouldDetectChangesToRemotes");
            }
        }

        [Test]
        public async Task ShouldDetectChangesToRemotesWhenSwitchingBranches()
        {
            Logger.Trace("Starting ShouldDetectChangesToRemotesWhenSwitchingBranches");

            try
            {
                var repositoryManagerListener = Substitute.For<IRepositoryManagerListener>();

                await Initialize(TestRepoMasterTwoRemotes, initializeRepository: false,
                    onRepositoryManagerCreated: manager => {
                        repositoryManagerListener.AttachListener(manager, repositoryManagerEvents);
                    });

                repositoryManagerEvents.CurrentBranchUpdated.WaitOne(Timeout).Should().BeTrue();
                repositoryManagerEvents.LocalBranchesUpdated.WaitOne(Timeout).Should().BeTrue();
                repositoryManagerEvents.RemoteBranchesUpdated.WaitOne(Timeout).Should().BeTrue();
                repositoryManagerEvents.GitLogUpdated.WaitOne(Timeout).Should().BeTrue();
                repositoryManagerEvents.GitAheadBehindStatusUpdated.WaitOne(Timeout).Should().BeTrue();
                repositoryManagerEvents.IsBusy.WaitOne(Timeout).Should().BeTrue();
                repositoryManagerEvents.IsNotBusy.WaitOne(Timeout).Should().BeTrue();

                repositoryManagerListener.ClearReceivedCalls();
                repositoryManagerEvents.Reset();

                await RepositoryManager.CreateBranch("branch2", "another/master")
                                       .StartAsAsync();
                await TaskManager.Wait();

                RepositoryManager.WaitForEvents();
                repositoryManagerEvents.WaitForNotBusy();

                repositoryManagerEvents.CurrentBranchUpdated.WaitOne(Timeout).Should().BeTrue();
                repositoryManagerEvents.LocalBranchesUpdated.WaitOne(Timeout).Should().BeTrue();
                repositoryManagerEvents.RemoteBranchesUpdated.WaitOne(Timeout).Should().BeTrue();
                repositoryManagerEvents.GitAheadBehindStatusUpdated.WaitOne(Timeout).Should().BeTrue();
                repositoryManagerEvents.GitLogUpdated.WaitOne(Timeout).Should().BeTrue();

                repositoryManagerListener.Received().OnIsBusyChanged(Args.Bool);
                repositoryManagerListener.Received().CurrentBranchUpdated(Args.NullableConfigBranch, Args.NullableConfigRemote);
                repositoryManagerListener.Received().GitAheadBehindStatusUpdated(Args.GitAheadBehindStatus);
                repositoryManagerListener.DidNotReceive().GitStatusUpdated(Args.GitStatus);
                repositoryManagerListener.DidNotReceive().GitLocksUpdated(Args.GitLocks);
                repositoryManagerListener.Received().GitLogUpdated(Args.GitLogs);
                repositoryManagerListener.Received().LocalBranchesUpdated(Args.LocalBranchDictionary);
                repositoryManagerListener.Received().RemoteBranchesUpdated(Args.RemoteDictionary, Args.RemoteBranchDictionary);

                repositoryManagerListener.ClearReceivedCalls();
                repositoryManagerEvents.Reset();

                await RepositoryManager.SwitchBranch("branch2")
                                       .StartAsAsync();
                await TaskManager.Wait();

                RepositoryManager.WaitForEvents();
                repositoryManagerEvents.WaitForNotBusy();

                repositoryManagerEvents.CurrentBranchUpdated.WaitOne(Timeout).Should().BeTrue();
                repositoryManagerEvents.GitLogUpdated.WaitOne(Timeout).Should().BeTrue();
                repositoryManagerEvents.GitLogUpdated.WaitOne(Timeout).Should().BeTrue();

                repositoryManagerListener.Received().OnIsBusyChanged(Args.Bool);
                repositoryManagerListener.Received().CurrentBranchUpdated(Args.NullableConfigBranch, Args.NullableConfigRemote);
                repositoryManagerListener.DidNotReceive().GitAheadBehindStatusUpdated(Args.GitAheadBehindStatus);
                repositoryManagerListener.DidNotReceive().GitStatusUpdated(Args.GitStatus);
                repositoryManagerListener.DidNotReceive().GitLocksUpdated(Args.GitLocks);
                repositoryManagerListener.Received().GitLogUpdated(Args.GitLogs);
                repositoryManagerListener.DidNotReceive().LocalBranchesUpdated(Args.LocalBranchDictionary);
                repositoryManagerListener.DidNotReceive().RemoteBranchesUpdated(Args.RemoteDictionary, Args.RemoteBranchDictionary);
            }
            finally
            {
                Logger.Trace("Ending ShouldDetectChangesToRemotesWhenSwitchingBranches");
            }
        }

        [Test]
        public async Task ShouldDetectGitPull()
        {
            Logger.Trace("Starting ShouldDetectGitPull");

            try
            {
                var repositoryManagerListener = Substitute.For<IRepositoryManagerListener>();

                await Initialize(TestRepoMasterCleanSynchronized, initializeRepository: false,
                    onRepositoryManagerCreated: manager => {
                        repositoryManagerListener.AttachListener(manager, repositoryManagerEvents);
                    });

                repositoryManagerEvents.CurrentBranchUpdated.WaitOne(Timeout).Should().BeTrue();
                repositoryManagerEvents.LocalBranchesUpdated.WaitOne(Timeout).Should().BeTrue();
                repositoryManagerEvents.RemoteBranchesUpdated.WaitOne(Timeout).Should().BeTrue();
                repositoryManagerEvents.GitLogUpdated.WaitOne(Timeout).Should().BeTrue();
                repositoryManagerEvents.GitAheadBehindStatusUpdated.WaitOne(Timeout).Should().BeTrue();
                repositoryManagerEvents.IsBusy.WaitOne(Timeout).Should().BeTrue();
                repositoryManagerEvents.IsNotBusy.WaitOne(Timeout).Should().BeTrue();

                repositoryManagerListener.ClearReceivedCalls();

                await RepositoryManager.Pull("origin", "master").StartAsAsync();
                await TaskManager.Wait();

                RepositoryManager.WaitForEvents();
                repositoryManagerEvents.WaitForNotBusy();

                repositoryManagerEvents.LocalBranchesUpdated.WaitOne(Timeout).Should().BeTrue();
                repositoryManagerEvents.GitStatusUpdated.WaitOne(Timeout).Should().BeTrue();
                repositoryManagerEvents.GitLogUpdated.WaitOne(Timeout).Should().BeTrue();

                repositoryManagerListener.Received().OnIsBusyChanged(Args.Bool);
                repositoryManagerListener.DidNotReceive().CurrentBranchUpdated(Args.NullableConfigBranch, Args.NullableConfigRemote);
                repositoryManagerListener.DidNotReceive().GitAheadBehindStatusUpdated(Args.GitAheadBehindStatus);
                repositoryManagerListener.Received().GitStatusUpdated(Args.GitStatus);
                repositoryManagerListener.DidNotReceive().GitLocksUpdated(Args.GitLocks);
                repositoryManagerListener.Received().GitLogUpdated(Args.GitLogs);
                repositoryManagerListener.Received().LocalBranchesUpdated(Args.LocalBranchDictionary);
                repositoryManagerListener.DidNotReceive().RemoteBranchesUpdated(Args.RemoteDictionary, Args.RemoteBranchDictionary);
            }
            finally
            {
                Logger.Trace("Ending ShouldDetectGitPull");
            }
        }

        [Test]
        public async Task ShouldDetectGitFetch()
        {
            Logger.Trace("Starting ShouldDetectGitFetch");

            try
            {
                var repositoryManagerListener = Substitute.For<IRepositoryManagerListener>();

                await Initialize(TestRepoMasterCleanUnsynchronized, initializeRepository: false,
                    onRepositoryManagerCreated: manager => {
                        repositoryManagerListener.AttachListener(manager, repositoryManagerEvents);
                    });

                repositoryManagerEvents.CurrentBranchUpdated.WaitOne(Timeout).Should().BeTrue();
                repositoryManagerEvents.LocalBranchesUpdated.WaitOne(Timeout).Should().BeTrue();
                repositoryManagerEvents.RemoteBranchesUpdated.WaitOne(Timeout).Should().BeTrue();
                repositoryManagerEvents.GitLogUpdated.WaitOne(Timeout).Should().BeTrue();
                repositoryManagerEvents.GitAheadBehindStatusUpdated.WaitOne(Timeout).Should().BeTrue();
                repositoryManagerEvents.IsBusy.WaitOne(Timeout).Should().BeTrue();
                repositoryManagerEvents.IsNotBusy.WaitOne(Timeout).Should().BeTrue();

                repositoryManagerListener.ClearReceivedCalls();
                repositoryManagerEvents.Reset();

                await RepositoryManager.Fetch("origin").StartAsAsync();
                await TaskManager.Wait();

                RepositoryManager.WaitForEvents();
                repositoryManagerEvents.WaitForNotBusy();

                repositoryManagerEvents.RemoteBranchesUpdated.WaitOne(Timeout).Should().BeTrue();
                repositoryManagerEvents.GitLogUpdated.WaitOne(Timeout).Should().BeTrue();
                repositoryManagerEvents.GitAheadBehindStatusUpdated.WaitOne(Timeout).Should().BeTrue();

                repositoryManagerListener.Received().OnIsBusyChanged(Args.Bool);
                repositoryManagerListener.DidNotReceive().CurrentBranchUpdated(Args.NullableConfigBranch, Args.NullableConfigRemote);
                repositoryManagerListener.Received().GitAheadBehindStatusUpdated(Args.GitAheadBehindStatus);
                repositoryManagerListener.DidNotReceive().GitStatusUpdated(Args.GitStatus);
                repositoryManagerListener.DidNotReceive().GitLocksUpdated(Args.GitLocks);
                repositoryManagerListener.Received().GitLogUpdated(Args.GitLogs);
                repositoryManagerListener.DidNotReceive().LocalBranchesUpdated(Args.LocalBranchDictionary);
                repositoryManagerListener.Received().RemoteBranchesUpdated(Args.RemoteDictionary, Args.RemoteBranchDictionary);
            }
            finally
            {
                Logger.Trace("Ending ShouldDetectGitFetch");
            }
        }
    }
}
