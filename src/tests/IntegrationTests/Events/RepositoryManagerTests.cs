using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using FluentAssertions;
using GitHub.Unity;
using NSubstitute;
using NUnit.Framework;
using TestUtils;
using TestUtils.Events;
using System.Threading.Tasks;
using GitHub.Logging;

namespace IntegrationTests
{
    [TestFixture /*, Category("DoNotRunOnAppVeyor") */]
    class RepositoryManagerTests : BaseGitEnvironmentTest
    {
        private RepositoryManagerEvents repositoryManagerEvents;
        private TimeSpan Timeout = TimeSpan.FromMilliseconds(1200);

        public override void OnSetup()
        {
            base.OnSetup();
            repositoryManagerEvents = new RepositoryManagerEvents();
        }

        [Test]
        public async Task ShouldPerformBasicInitialize()
        {
            Stopwatch watch = null;
            ILogging logger = null;
            StartTest(out watch, out logger);

            try
            {
                var repositoryManagerListener = Substitute.For<IRepositoryManagerListener>();

                InitializePlatformAndEnvironment(TestRepoMasterCleanSynchronized,
                    onRepositoryManagerCreated: manager => {
                        repositoryManagerListener.AttachListener(manager, repositoryManagerEvents);
                    });

                repositoryManagerListener.AssertDidNotReceiveAnyCalls();

                await TaskManager.Wait();
                StartTrackTime(watch, logger, "RepositoryManager.WaitForEvents()");
                RepositoryManager.WaitForEvents();
                StopTrackTimeAndLog(watch, logger);

                repositoryManagerListener.DidNotReceive().OnIsBusyChanged(Args.Bool);
                repositoryManagerListener.DidNotReceive().CurrentBranchUpdated(Args.NullableConfigBranch, Args.NullableConfigRemote);
                repositoryManagerListener.DidNotReceive().GitAheadBehindStatusUpdated(Args.GitAheadBehindStatus);
                repositoryManagerListener.DidNotReceive().GitStatusUpdated(Args.GitStatus);
                repositoryManagerListener.DidNotReceive().GitLocksUpdated(Args.GitLocks);
                repositoryManagerListener.DidNotReceive().GitLogUpdated(Args.GitLogs);
                repositoryManagerListener.DidNotReceive().LocalBranchesUpdated(Args.LocalBranchDictionary);
                repositoryManagerListener.DidNotReceive().RemoteBranchesUpdated(Args.RemoteDictionary, Args.RemoteBranchDictionary);
            }
            finally
            {
                EndTest(logger);
            }
        }

        [Test]
        public async Task ShouldDetectFileChanges()
        {
            Stopwatch watch = null;
            ILogging logger = null;
            StartTest(out watch, out logger);

            try
            {
                var repositoryManagerListener = Substitute.For<IRepositoryManagerListener>();

                InitializePlatformAndEnvironment(TestRepoMasterCleanSynchronized,
                    onRepositoryManagerCreated: manager => {
                        repositoryManagerListener.AttachListener(manager, repositoryManagerEvents);
                    });

                repositoryManagerListener.AssertDidNotReceiveAnyCalls();

                var foobarTxt = TestRepoMasterCleanSynchronized.Combine("foobar.txt");
                foobarTxt.WriteAllText("foobar");

                await TaskManager.Wait();

                StartTrackTime(watch, logger, "RepositoryManager.WaitForEvents()");
                RepositoryManager.WaitForEvents();
                StopTrackTimeAndLog(watch, logger);

                StartTrackTime(watch, logger, "repositoryManagerEvents.WaitForNotBusy()");
                await repositoryManagerEvents.WaitForNotBusy();
                StopTrackTimeAndLog(watch, logger);

                (await TaskEx.WhenAny(repositoryManagerEvents.GitStatusUpdated, TaskEx.Delay(Timeout))).Should().BeAssignableTo<Task<bool>>();

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
                EndTest(logger);
            }
        }

        [Test]
        public async Task ShouldAddAndCommitFiles()
        {
            Stopwatch watch = null;
            ILogging logger = null;
            StartTest(out watch, out logger);

            try
            {
                var repositoryManagerListener = Substitute.For<IRepositoryManagerListener>();

                InitializePlatformAndEnvironment(TestRepoMasterCleanSynchronized,
                    onRepositoryManagerCreated: manager => {
                        repositoryManagerListener.AttachListener(manager, repositoryManagerEvents);
                    });

                repositoryManagerListener.AssertDidNotReceiveAnyCalls();

                var foobarTxt = TestRepoMasterCleanSynchronized.Combine("foobar.txt");
                foobarTxt.WriteAllText("foobar");

                var testDocumentTxt = TestRepoMasterCleanSynchronized.Combine("Assets", "TestDocument.txt");
                testDocumentTxt.WriteAllText("foobar");

                await TaskManager.Wait();

                RepositoryManager.WaitForEvents();
                await repositoryManagerEvents.WaitForNotBusy();

                (await TaskEx.WhenAny(repositoryManagerEvents.GitStatusUpdated, TaskEx.Delay(Timeout))).Should().BeAssignableTo<Task<bool>>();

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

                var filesToCommit = new List<string> { "Assets\\TestDocument.txt", "foobar.txt" };
                var commitMessage = "IntegrationTest Commit";
                var commitBody = string.Empty;

                StartTrackTime(watch, logger, "CommitFiles");
                await RepositoryManager.CommitFiles(filesToCommit, commitMessage, commitBody).StartAsAsync();
                StopTrackTimeAndLog(watch, logger);
                await TaskManager.Wait();

                RepositoryManager.WaitForEvents();
                await repositoryManagerEvents.WaitForNotBusy();

                (await TaskEx.WhenAny(repositoryManagerEvents.LocalBranchesUpdated, TaskEx.Delay(Timeout))).Should().BeAssignableTo<Task<bool>>();
                (await TaskEx.WhenAny(repositoryManagerEvents.GitLogUpdated, TaskEx.Delay(Timeout))).Should().BeAssignableTo<Task<bool>>();
                (await TaskEx.WhenAny(repositoryManagerEvents.GitAheadBehindStatusUpdated, TaskEx.Delay(Timeout))).Should().BeAssignableTo<Task<bool>>();
                (await TaskEx.WhenAny(repositoryManagerEvents.GitStatusUpdated, TaskEx.Delay(Timeout))).Should().BeAssignableTo<Task<bool>>();

                repositoryManagerListener.Received().OnIsBusyChanged(Args.Bool);
                repositoryManagerListener.Received().CurrentBranchUpdated(Args.NullableConfigBranch, Args.NullableConfigRemote);
                repositoryManagerListener.Received().GitAheadBehindStatusUpdated(Args.GitAheadBehindStatus);
                repositoryManagerListener.Received().GitStatusUpdated(Args.GitStatus);
                repositoryManagerListener.DidNotReceive().GitLocksUpdated(Args.GitLocks);
                repositoryManagerListener.Received().GitLogUpdated(Args.GitLogs);
                repositoryManagerListener.Received().LocalBranchesUpdated(Args.LocalBranchDictionary);
                repositoryManagerListener.Received().RemoteBranchesUpdated(Args.RemoteDictionary, Args.RemoteBranchDictionary);
            }
            finally
            {
                EndTest(logger);
            }
        }

        [Test]
        public async Task ShouldAddAndCommitAllFiles()
        {
            Stopwatch watch = null;
            ILogging logger = null;
            StartTest(out watch, out logger);

            try
            {
                var repositoryManagerListener = Substitute.For<IRepositoryManagerListener>();

                InitializePlatformAndEnvironment(TestRepoMasterCleanSynchronized,
                    onRepositoryManagerCreated: manager => {
                        repositoryManagerListener.AttachListener(manager, repositoryManagerEvents);
                    });

                repositoryManagerListener.AssertDidNotReceiveAnyCalls();

                logger.Trace("Add files");

                var foobarTxt = TestRepoMasterCleanSynchronized.Combine("foobar.txt");
                foobarTxt.WriteAllText("foobar");

                var testDocumentTxt = TestRepoMasterCleanSynchronized.Combine("Assets", "TestDocument.txt");
                testDocumentTxt.WriteAllText("foobar");

                await TaskManager.Wait();

                StartTrackTime(watch, logger, "RepositoryManager.WaitForEvents()");
                RepositoryManager.WaitForEvents();
                StopTrackTimeAndLog(watch, logger);

                StartTrackTime(watch, logger, "repositoryManagerEvents.WaitForNotBusy()");
                await repositoryManagerEvents.WaitForNotBusy();
                StopTrackTimeAndLog(watch, logger);

                (await TaskEx.WhenAny(repositoryManagerEvents.GitStatusUpdated, TaskEx.Delay(Timeout))).Should().BeAssignableTo<Task<bool>>();

                repositoryManagerListener.Received().OnIsBusyChanged(Args.Bool);
                repositoryManagerListener.Received().GitStatusUpdated(Args.GitStatus);

                repositoryManagerListener.DidNotReceive().CurrentBranchUpdated(Args.NullableConfigBranch, Args.NullableConfigRemote);
                repositoryManagerListener.DidNotReceive().GitAheadBehindStatusUpdated(Args.GitAheadBehindStatus);
                repositoryManagerListener.DidNotReceive().GitLocksUpdated(Args.GitLocks);
                repositoryManagerListener.DidNotReceive().GitLogUpdated(Args.GitLogs);
                repositoryManagerListener.DidNotReceive().LocalBranchesUpdated(Args.LocalBranchDictionary);
                repositoryManagerListener.DidNotReceive().RemoteBranchesUpdated(Args.RemoteDictionary, Args.RemoteBranchDictionary);

                repositoryManagerListener.ClearReceivedCalls();
                repositoryManagerEvents.Reset();

                StartTrackTime(watch, logger, "CommitAllFiles");
                await RepositoryManager.CommitAllFiles("IntegrationTest Commit", string.Empty).StartAsAsync();

                StopTrackTimeAndLog(watch, logger);
                await TaskManager.Wait();

                StartTrackTime(watch, logger, "RepositoryManager.WaitForEvents()");
                RepositoryManager.WaitForEvents();
                StopTrackTimeAndLog(watch, logger);

                StartTrackTime(watch, logger, "repositoryManagerEvents.WaitForNotBusy()");
                await repositoryManagerEvents.WaitForNotBusy();
                StopTrackTimeAndLog(watch, logger);

                (await TaskEx.WhenAny(repositoryManagerEvents.LocalBranchesUpdated, TaskEx.Delay(Timeout))).Should().BeAssignableTo<Task<bool>>();
                (await TaskEx.WhenAny(repositoryManagerEvents.GitLogUpdated, TaskEx.Delay(Timeout))).Should().BeAssignableTo<Task<bool>>();
                (await TaskEx.WhenAny(repositoryManagerEvents.GitAheadBehindStatusUpdated, TaskEx.Delay(Timeout))).Should().BeAssignableTo<Task<bool>>();
                (await TaskEx.WhenAny(repositoryManagerEvents.GitStatusUpdated, TaskEx.Delay(Timeout))).Should().BeAssignableTo<Task<bool>>();

                repositoryManagerListener.Received().CurrentBranchUpdated(Args.NullableConfigBranch, Args.NullableConfigRemote);
                repositoryManagerListener.Received().GitAheadBehindStatusUpdated(Args.GitAheadBehindStatus);
                repositoryManagerListener.Received().GitStatusUpdated(Args.GitStatus);
                repositoryManagerListener.DidNotReceive().GitLocksUpdated(Args.GitLocks);
                repositoryManagerListener.Received().GitLogUpdated(Args.GitLogs);
                repositoryManagerListener.Received().LocalBranchesUpdated(Args.LocalBranchDictionary);
                repositoryManagerListener.Received().RemoteBranchesUpdated(Args.RemoteDictionary, Args.RemoteBranchDictionary);
            }
            finally
            {
                EndTest(logger);
            }
        }

        [Test]
        public async Task ShouldDetectBranchChange()
        {
            Stopwatch watch = null;
            ILogging logger = null;
            StartTest(out watch, out logger);

            try
            {
                var repositoryManagerListener = Substitute.For<IRepositoryManagerListener>();

                InitializePlatformAndEnvironment(TestRepoMasterCleanSynchronized,
                    onRepositoryManagerCreated: manager => {
                        repositoryManagerListener.AttachListener(manager, repositoryManagerEvents);
                    });

                repositoryManagerListener.AssertDidNotReceiveAnyCalls();

                await RepositoryManager.SwitchBranch("feature/document").StartAsAsync();
                await TaskManager.Wait();

                RepositoryManager.WaitForEvents();
                await repositoryManagerEvents.WaitForNotBusy();

                (await TaskEx.WhenAny(repositoryManagerEvents.GitStatusUpdated, TaskEx.Delay(Timeout))).Should().BeAssignableTo<Task<bool>>();
                (await TaskEx.WhenAny(repositoryManagerEvents.CurrentBranchUpdated, TaskEx.Delay(Timeout))).Should().BeAssignableTo<Task<bool>>();
                (await TaskEx.WhenAny(repositoryManagerEvents.GitLogUpdated, TaskEx.Delay(Timeout))).Should().BeAssignableTo<Task<bool>>();
                (await TaskEx.WhenAny(repositoryManagerEvents.GitStatusUpdated, TaskEx.Delay(Timeout))).Should().BeAssignableTo<Task<bool>>();

                repositoryManagerListener.Received().OnIsBusyChanged(Args.Bool);
                repositoryManagerListener.Received().CurrentBranchUpdated(Args.NullableConfigBranch, Args.NullableConfigRemote);
                repositoryManagerListener.Received().GitStatusUpdated(Args.GitStatus);
                repositoryManagerListener.Received().GitLogUpdated(Args.GitLogs);

                repositoryManagerListener.DidNotReceive().GitAheadBehindStatusUpdated(Args.GitAheadBehindStatus);
                repositoryManagerListener.DidNotReceive().GitLocksUpdated(Args.GitLocks);
                repositoryManagerListener.DidNotReceive().LocalBranchesUpdated(Args.LocalBranchDictionary);
                repositoryManagerListener.DidNotReceive().RemoteBranchesUpdated(Args.RemoteDictionary, Args.RemoteBranchDictionary);
            }
            finally
            {
                EndTest(logger);
            }
        }

        [Test]
        public async Task ShouldDetectBranchDelete()
        {
            Stopwatch watch = null;
            ILogging logger = null;
            StartTest(out watch, out logger);

            try
            {
                var repositoryManagerListener = Substitute.For<IRepositoryManagerListener>();

                InitializePlatformAndEnvironment(TestRepoMasterCleanSynchronized,
                    onRepositoryManagerCreated: manager => {
                        repositoryManagerListener.AttachListener(manager, repositoryManagerEvents);
                    });

                repositoryManagerListener.AssertDidNotReceiveAnyCalls();

                await RepositoryManager.DeleteBranch("feature/document", true).StartAsAsync();
                await TaskManager.Wait();

                RepositoryManager.WaitForEvents();
                await repositoryManagerEvents.WaitForNotBusy();

                await TaskEx.Delay(Timeout);

                (await TaskEx.WhenAny(repositoryManagerEvents.CurrentBranchUpdated, TaskEx.Delay(Timeout))).Should().BeAssignableTo<Task<bool>>();
                (await TaskEx.WhenAny(repositoryManagerEvents.GitAheadBehindStatusUpdated, TaskEx.Delay(Timeout))).Should().BeAssignableTo<Task<bool>>();
                (await TaskEx.WhenAny(repositoryManagerEvents.GitLogUpdated, TaskEx.Delay(Timeout))).Should().BeAssignableTo<Task<bool>>();
                (await TaskEx.WhenAny(repositoryManagerEvents.LocalBranchesUpdated, TaskEx.Delay(Timeout))).Should().BeAssignableTo<Task<bool>>();
                (await TaskEx.WhenAny(repositoryManagerEvents.RemoteBranchesUpdated, TaskEx.Delay(Timeout))).Should().BeAssignableTo<Task<bool>>();

                repositoryManagerListener.Received().OnIsBusyChanged(Args.Bool);
                repositoryManagerListener.Received().CurrentBranchUpdated(Args.NullableConfigBranch, Args.NullableConfigRemote);
                repositoryManagerListener.Received().GitAheadBehindStatusUpdated(Args.GitAheadBehindStatus);
                repositoryManagerListener.Received().GitLogUpdated(Args.GitLogs);
                repositoryManagerListener.Received().LocalBranchesUpdated(Args.LocalBranchDictionary);
                repositoryManagerListener.Received().RemoteBranchesUpdated(Args.RemoteDictionary, Args.RemoteBranchDictionary);

                repositoryManagerListener.DidNotReceive().GitStatusUpdated(Args.GitStatus);
                repositoryManagerListener.DidNotReceive().GitLocksUpdated(Args.GitLocks);
            }
            finally
            {
                EndTest(logger);
            }
        }

        [Test]
        public async Task ShouldDetectBranchCreate()
        {
            Stopwatch watch = null;
            ILogging logger = null;
            StartTest(out watch, out logger);

            try
            {
                var repositoryManagerListener = Substitute.For<IRepositoryManagerListener>();

                InitializePlatformAndEnvironment(TestRepoMasterCleanSynchronized,
                    onRepositoryManagerCreated: manager => {
                        repositoryManagerListener.AttachListener(manager, repositoryManagerEvents);
                    });

                repositoryManagerListener.AssertDidNotReceiveAnyCalls();

                var createdBranch1 = "feature/document2";
                await RepositoryManager.CreateBranch(createdBranch1, "feature/document").StartAsAsync();
                await TaskManager.Wait();

                RepositoryManager.WaitForEvents();
                await repositoryManagerEvents.WaitForNotBusy();

                (await TaskEx.WhenAny(repositoryManagerEvents.CurrentBranchUpdated, TaskEx.Delay(Timeout))).Should().BeAssignableTo<Task<bool>>();
                (await TaskEx.WhenAny(repositoryManagerEvents.GitAheadBehindStatusUpdated, TaskEx.Delay(Timeout))).Should().BeAssignableTo<Task<bool>>();
                (await TaskEx.WhenAny(repositoryManagerEvents.LocalBranchesUpdated, TaskEx.Delay(Timeout))).Should().BeAssignableTo<Task<bool>>();
                (await TaskEx.WhenAny(repositoryManagerEvents.RemoteBranchesUpdated, TaskEx.Delay(Timeout))).Should().BeAssignableTo<Task<bool>>();

                repositoryManagerListener.Received().OnIsBusyChanged(Args.Bool);
                repositoryManagerListener.Received().CurrentBranchUpdated(Args.NullableConfigBranch, Args.NullableConfigRemote);
                repositoryManagerListener.Received().GitAheadBehindStatusUpdated(Args.GitAheadBehindStatus);
                repositoryManagerListener.Received().LocalBranchesUpdated(Args.LocalBranchDictionary);
                repositoryManagerListener.Received().RemoteBranchesUpdated(Args.RemoteDictionary, Args.RemoteBranchDictionary);

                repositoryManagerListener.DidNotReceive().GitStatusUpdated(Args.GitStatus);
                repositoryManagerListener.DidNotReceive().GitLocksUpdated(Args.GitLocks);
                // TODO: log should not be getting called, but it is because when branches get changed we're blindly calling log
                //repositoryManagerListener.DidNotReceive().GitLogUpdated(Args.GitLogs);

                repositoryManagerListener.ClearReceivedCalls();
                repositoryManagerEvents.Reset();

                await RepositoryManager.CreateBranch("feature2/document2", "feature/document").StartAsAsync();
                await TaskManager.Wait();

                StartTrackTime(watch, logger, "CreateBranch");
                RepositoryManager.WaitForEvents();
                StopTrackTimeAndLog(watch, logger);

                await repositoryManagerEvents.WaitForNotBusy();

                (await TaskEx.WhenAny(repositoryManagerEvents.GitAheadBehindStatusUpdated, TaskEx.Delay(Timeout))).Should().BeAssignableTo<Task<bool>>();
                (await TaskEx.WhenAny(repositoryManagerEvents.LocalBranchesUpdated, TaskEx.Delay(Timeout))).Should().BeAssignableTo<Task<bool>>();
                (await TaskEx.WhenAny(repositoryManagerEvents.RemoteBranchesUpdated, TaskEx.Delay(Timeout))).Should().BeAssignableTo<Task<bool>>();

                repositoryManagerListener.Received().OnIsBusyChanged(Args.Bool);
                repositoryManagerListener.Received().GitAheadBehindStatusUpdated(Args.GitAheadBehindStatus);
                repositoryManagerListener.Received().LocalBranchesUpdated(Args.LocalBranchDictionary);
                repositoryManagerListener.Received().RemoteBranchesUpdated(Args.RemoteDictionary, Args.RemoteBranchDictionary);

                repositoryManagerListener.DidNotReceive().CurrentBranchUpdated(Args.NullableConfigBranch, Args.NullableConfigRemote);
                repositoryManagerListener.DidNotReceive().GitStatusUpdated(Args.GitStatus);
                repositoryManagerListener.DidNotReceive().GitLocksUpdated(Args.GitLocks);
                // TODO: log should not be getting called, but it is because when branches get changed we're blindly calling log
                //repositoryManagerListener.DidNotReceive().GitLogUpdated(Args.GitLogs);
            }
            finally
            {
                EndTest(logger);
            }
        }

        [Test]
        public async Task ShouldDetectChangesToRemotes()
        {
            Stopwatch watch = null;
            ILogging logger = null;
            StartTest(out watch, out logger);

            try
            {
                var repositoryManagerListener = Substitute.For<IRepositoryManagerListener>();

                InitializePlatformAndEnvironment(TestRepoMasterCleanSynchronized,
                    onRepositoryManagerCreated: manager => {
                        repositoryManagerListener.AttachListener(manager, repositoryManagerEvents);
                    });

                repositoryManagerListener.AssertDidNotReceiveAnyCalls();

                await RepositoryManager.RemoteRemove("origin").StartAsAsync();
                await TaskManager.Wait();

                RepositoryManager.WaitForEvents();
                await repositoryManagerEvents.WaitForNotBusy();

                (await TaskEx.WhenAny(repositoryManagerEvents.CurrentBranchUpdated, TaskEx.Delay(Timeout))).Should().BeAssignableTo<Task<bool>>();
                (await TaskEx.WhenAny(repositoryManagerEvents.GitAheadBehindStatusUpdated, TaskEx.Delay(Timeout))).Should().BeAssignableTo<Task<bool>>();
                (await TaskEx.WhenAny(repositoryManagerEvents.GitLogUpdated, TaskEx.Delay(Timeout))).Should().BeAssignableTo<Task<bool>>();
                (await TaskEx.WhenAny(repositoryManagerEvents.LocalBranchesUpdated, TaskEx.Delay(Timeout))).Should().BeAssignableTo<Task<bool>>();
                (await TaskEx.WhenAny(repositoryManagerEvents.RemoteBranchesUpdated, TaskEx.Delay(Timeout))).Should().BeAssignableTo<Task<bool>>();

                repositoryManagerListener.Received().OnIsBusyChanged(Args.Bool);
                repositoryManagerListener.Received().CurrentBranchUpdated(Args.NullableConfigBranch, Args.NullableConfigRemote);
                repositoryManagerListener.Received().GitAheadBehindStatusUpdated(Args.GitAheadBehindStatus);
                repositoryManagerListener.Received().GitLogUpdated(Args.GitLogs);
                repositoryManagerListener.Received().LocalBranchesUpdated(Args.LocalBranchDictionary);
                repositoryManagerListener.Received().RemoteBranchesUpdated(Args.RemoteDictionary, Args.RemoteBranchDictionary);

                repositoryManagerListener.DidNotReceive().GitStatusUpdated(Args.GitStatus);
                repositoryManagerListener.DidNotReceive().GitLocksUpdated(Args.GitLocks);

                repositoryManagerListener.ClearReceivedCalls();
                repositoryManagerEvents.Reset();

                await RepositoryManager.RemoteAdd("origin", "https://github.com/EvilShana/IOTestsRepo.git").StartAsAsync();
                await TaskManager.Wait();

                RepositoryManager.WaitForEvents();
                await repositoryManagerEvents.WaitForNotBusy();

                (await TaskEx.WhenAny(repositoryManagerEvents.CurrentBranchUpdated, TaskEx.Delay(Timeout))).Should().BeAssignableTo<Task<bool>>();
                (await TaskEx.WhenAny(repositoryManagerEvents.GitAheadBehindStatusUpdated, TaskEx.Delay(Timeout))).Should().BeAssignableTo<Task<bool>>();
                (await TaskEx.WhenAny(repositoryManagerEvents.GitLogUpdated, TaskEx.Delay(Timeout))).Should().BeAssignableTo<Task<bool>>();
                (await TaskEx.WhenAny(repositoryManagerEvents.LocalBranchesUpdated, TaskEx.Delay(Timeout))).Should().BeAssignableTo<Task<bool>>();
                (await TaskEx.WhenAny(repositoryManagerEvents.RemoteBranchesUpdated, TaskEx.Delay(Timeout))).Should().BeAssignableTo<Task<bool>>();

                repositoryManagerListener.Received().OnIsBusyChanged(Args.Bool);
                repositoryManagerListener.Received().CurrentBranchUpdated(Args.NullableConfigBranch, Args.NullableConfigRemote);
                repositoryManagerListener.Received().GitAheadBehindStatusUpdated(Args.GitAheadBehindStatus);
                repositoryManagerListener.Received().GitLogUpdated(Args.GitLogs);
                repositoryManagerListener.Received().LocalBranchesUpdated(Args.LocalBranchDictionary);
                repositoryManagerListener.Received().RemoteBranchesUpdated(Args.RemoteDictionary, Args.RemoteBranchDictionary);

                repositoryManagerListener.DidNotReceive().GitStatusUpdated(Args.GitStatus);
                repositoryManagerListener.DidNotReceive().GitLocksUpdated(Args.GitLocks);
            }
            finally
            {
                EndTest(logger);
            }
        }

        [Test]
        public async Task ShouldDetectChangesToRemotesWhenSwitchingBranches()
        {
            Stopwatch watch = null;
            ILogging logger = null;
            StartTest(out watch, out logger);

            try
            {
                var repositoryManagerListener = Substitute.For<IRepositoryManagerListener>();

                InitializePlatformAndEnvironment(TestRepoMasterTwoRemotes,
                    onRepositoryManagerCreated: manager => {
                        repositoryManagerListener.AttachListener(manager, repositoryManagerEvents);
                    });

                repositoryManagerListener.AssertDidNotReceiveAnyCalls();

                await RepositoryManager.CreateBranch("branch2", "another/master").StartAsAsync();
                await TaskManager.Wait();

                RepositoryManager.WaitForEvents();
                await repositoryManagerEvents.WaitForNotBusy();

                (await TaskEx.WhenAny(repositoryManagerEvents.CurrentBranchUpdated, TaskEx.Delay(Timeout))).Should().BeAssignableTo<Task<bool>>();
                (await TaskEx.WhenAny(repositoryManagerEvents.GitAheadBehindStatusUpdated, TaskEx.Delay(Timeout))).Should().BeAssignableTo<Task<bool>>();
                (await TaskEx.WhenAny(repositoryManagerEvents.GitLogUpdated, TaskEx.Delay(Timeout))).Should().BeAssignableTo<Task<bool>>();
                (await TaskEx.WhenAny(repositoryManagerEvents.LocalBranchesUpdated, TaskEx.Delay(Timeout))).Should().BeAssignableTo<Task<bool>>();
                (await TaskEx.WhenAny(repositoryManagerEvents.RemoteBranchesUpdated, TaskEx.Delay(Timeout))).Should().BeAssignableTo<Task<bool>>();

                repositoryManagerListener.Received().OnIsBusyChanged(Args.Bool);
                repositoryManagerListener.Received().CurrentBranchUpdated(Args.NullableConfigBranch, Args.NullableConfigRemote);
                repositoryManagerListener.Received().GitAheadBehindStatusUpdated(Args.GitAheadBehindStatus);
                repositoryManagerListener.Received().GitLogUpdated(Args.GitLogs);
                repositoryManagerListener.Received().LocalBranchesUpdated(Args.LocalBranchDictionary);
                repositoryManagerListener.Received().RemoteBranchesUpdated(Args.RemoteDictionary, Args.RemoteBranchDictionary);

                repositoryManagerListener.DidNotReceive().GitStatusUpdated(Args.GitStatus);
                repositoryManagerListener.DidNotReceive().GitLocksUpdated(Args.GitLocks);

                repositoryManagerListener.ClearReceivedCalls();
                repositoryManagerEvents.Reset();

                await RepositoryManager.SwitchBranch("branch2").StartAsAsync();
                await TaskManager.Wait();

                RepositoryManager.WaitForEvents();
                await repositoryManagerEvents.WaitForNotBusy();

                (await TaskEx.WhenAny(repositoryManagerEvents.CurrentBranchUpdated, TaskEx.Delay(Timeout))).Should().BeAssignableTo<Task<bool>>();
                (await TaskEx.WhenAny(repositoryManagerEvents.GitAheadBehindStatusUpdated, TaskEx.Delay(Timeout))).Should().BeAssignableTo<Task<bool>>();
                (await TaskEx.WhenAny(repositoryManagerEvents.GitStatusUpdated, TaskEx.Delay(Timeout))).Should().BeAssignableTo<Task<bool>>();
                (await TaskEx.WhenAny(repositoryManagerEvents.GitLogUpdated, TaskEx.Delay(Timeout))).Should().BeAssignableTo<Task<bool>>();

                repositoryManagerListener.Received().OnIsBusyChanged(Args.Bool);
                repositoryManagerListener.Received().CurrentBranchUpdated(Args.NullableConfigBranch, Args.NullableConfigRemote);
                repositoryManagerListener.Received().GitAheadBehindStatusUpdated(Args.GitAheadBehindStatus);
                repositoryManagerListener.Received().GitStatusUpdated(Args.GitStatus);
                repositoryManagerListener.Received().GitLogUpdated(Args.GitLogs);

                repositoryManagerListener.DidNotReceive().GitLocksUpdated(Args.GitLocks);
                repositoryManagerListener.DidNotReceive().LocalBranchesUpdated(Args.LocalBranchDictionary);
                repositoryManagerListener.DidNotReceive().RemoteBranchesUpdated(Args.RemoteDictionary, Args.RemoteBranchDictionary);
            }
            finally
            {
                EndTest(logger);
            }
        }

        [Test]
        public async Task ShouldDetectGitPull()
        {
            Stopwatch watch = null;
            ILogging logger = null;
            StartTest(out watch, out logger);

            try
            {
                var repositoryManagerListener = Substitute.For<IRepositoryManagerListener>();

                InitializePlatformAndEnvironment(TestRepoMasterCleanSynchronized,
                    onRepositoryManagerCreated: manager => {
                        repositoryManagerListener.AttachListener(manager, repositoryManagerEvents);
                    });

                repositoryManagerListener.AssertDidNotReceiveAnyCalls();

                await RepositoryManager.Pull("origin", "master").StartAsAsync();
                await TaskManager.Wait();

                RepositoryManager.WaitForEvents();
                await repositoryManagerEvents.WaitForNotBusy();

                (await TaskEx.WhenAny(repositoryManagerEvents.CurrentBranchUpdated, TaskEx.Delay(Timeout))).Should().BeAssignableTo<Task<bool>>();
                (await TaskEx.WhenAny(repositoryManagerEvents.GitAheadBehindStatusUpdated, TaskEx.Delay(Timeout))).Should().BeAssignableTo<Task<bool>>();
                (await TaskEx.WhenAny(repositoryManagerEvents.GitStatusUpdated, TaskEx.Delay(Timeout))).Should().BeAssignableTo<Task<bool>>();
                (await TaskEx.WhenAny(repositoryManagerEvents.GitLogUpdated, TaskEx.Delay(Timeout))).Should().BeAssignableTo<Task<bool>>();
                (await TaskEx.WhenAny(repositoryManagerEvents.LocalBranchesUpdated, TaskEx.Delay(Timeout))).Should().BeAssignableTo<Task<bool>>();

                repositoryManagerListener.Received().OnIsBusyChanged(Args.Bool);
                repositoryManagerListener.Received().CurrentBranchUpdated(Args.NullableConfigBranch, Args.NullableConfigRemote);
                repositoryManagerListener.Received().GitAheadBehindStatusUpdated(Args.GitAheadBehindStatus);
                repositoryManagerListener.Received().GitStatusUpdated(Args.GitStatus);
                repositoryManagerListener.Received().GitLogUpdated(Args.GitLogs);
                repositoryManagerListener.Received().LocalBranchesUpdated(Args.LocalBranchDictionary);

                repositoryManagerListener.DidNotReceive().GitLocksUpdated(Args.GitLocks);
                // TODO: this should not happen but it's happening right now because when local branches get updated in the cache, remotes get updated too
                //repositoryManagerListener.DidNotReceive().RemoteBranchesUpdated(Args.RemoteDictionary, Args.RemoteBranchDictionary);
            }
            finally
            {
                EndTest(logger);
            }
        }

        [Test]
        public async Task ShouldDetectGitFetch()
        {
            Stopwatch watch = null;
            ILogging logger = null;
            StartTest(out watch, out logger);

            try
            {
                var repositoryManagerListener = Substitute.For<IRepositoryManagerListener>();

                InitializePlatformAndEnvironment(TestRepoMasterCleanUnsynchronized,
                    onRepositoryManagerCreated: manager => {
                        repositoryManagerListener.AttachListener(manager, repositoryManagerEvents);
                    });

                repositoryManagerListener.AssertDidNotReceiveAnyCalls();

                await RepositoryManager.Fetch("origin").StartAsAsync();
                await TaskManager.Wait();

                RepositoryManager.WaitForEvents();
                await repositoryManagerEvents.WaitForNotBusy();

                (await TaskEx.WhenAny(repositoryManagerEvents.CurrentBranchUpdated, TaskEx.Delay(Timeout))).Should().BeAssignableTo<Task<bool>>();
                (await TaskEx.WhenAny(repositoryManagerEvents.GitAheadBehindStatusUpdated, TaskEx.Delay(Timeout))).Should().BeAssignableTo<Task<bool>>();
                (await TaskEx.WhenAny(repositoryManagerEvents.LocalBranchesUpdated, TaskEx.Delay(Timeout))).Should().BeAssignableTo<Task<bool>>();
                (await TaskEx.WhenAny(repositoryManagerEvents.RemoteBranchesUpdated, TaskEx.Delay(Timeout))).Should().BeAssignableTo<Task<bool>>();

                repositoryManagerListener.Received().OnIsBusyChanged(Args.Bool);
                repositoryManagerListener.Received().CurrentBranchUpdated(Args.NullableConfigBranch, Args.NullableConfigRemote);
                repositoryManagerListener.Received().GitAheadBehindStatusUpdated(Args.GitAheadBehindStatus);
                repositoryManagerListener.Received().LocalBranchesUpdated(Args.LocalBranchDictionary);
                repositoryManagerListener.Received().RemoteBranchesUpdated(Args.RemoteDictionary, Args.RemoteBranchDictionary);

                repositoryManagerListener.DidNotReceive().GitStatusUpdated(Args.GitStatus);
                repositoryManagerListener.DidNotReceive().GitLocksUpdated(Args.GitLocks);
                // TODO: log should not be getting called, but it is because when branches get changed we're blindly calling log
                //repositoryManagerListener.DidNotReceive().GitLogUpdated(Args.GitLogs);
            }
            finally
            {
                EndTest(logger);
            }
        }
    }
}
