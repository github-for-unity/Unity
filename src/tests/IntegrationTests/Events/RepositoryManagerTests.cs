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
    //[TestFixture, Category("DoNotRunOnAppVeyor")]
    [TestFixture, Category("RunOnAppVeyor")]
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

                // we expect these events
                await AssertReceivedEvent(repositoryManagerEvents.GitStatusUpdated);

                // we don't expect these events
                await AssertDidNotReceiveEvent(repositoryManagerEvents.LocalBranchesUpdated);
                await AssertDidNotReceiveEvent(repositoryManagerEvents.RemoteBranchesUpdated);
                await AssertDidNotReceiveEvent(repositoryManagerEvents.GitAheadBehindStatusUpdated);
                await AssertDidNotReceiveEvent(repositoryManagerEvents.GitLogUpdated);
                await AssertDidNotReceiveEvent(repositoryManagerEvents.GitLocksUpdated);
                await AssertDidNotReceiveEvent(repositoryManagerEvents.CurrentBranchUpdated);
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

                StartTrackTime(watch, logger, "RepositoryManager.WaitForEvents()");
                RepositoryManager.WaitForEvents();
                StopTrackTimeAndLog(watch, logger);

                StartTrackTime(watch, logger, "repositoryManagerEvents.WaitForNotBusy()");
                await repositoryManagerEvents.WaitForNotBusy();
                StopTrackTimeAndLog(watch, logger);

                // we expect these events
                await AssertReceivedEvent(repositoryManagerEvents.GitStatusUpdated);

                // we don't expect these events
                await AssertDidNotReceiveEvent(repositoryManagerEvents.LocalBranchesUpdated);
                await AssertDidNotReceiveEvent(repositoryManagerEvents.RemoteBranchesUpdated);
                await AssertDidNotReceiveEvent(repositoryManagerEvents.GitAheadBehindStatusUpdated);
                await AssertDidNotReceiveEvent(repositoryManagerEvents.GitLogUpdated);
                await AssertDidNotReceiveEvent(repositoryManagerEvents.GitLocksUpdated);
                await AssertDidNotReceiveEvent(repositoryManagerEvents.CurrentBranchUpdated);

                repositoryManagerListener.ClearReceivedCalls();
                repositoryManagerEvents.Reset();

                var filesToCommit = new List<string> { "foobar.txt" };
                var commitMessage = "IntegrationTest Commit";
                var commitBody = string.Empty;

                StartTrackTime(watch, logger, "CommitFiles");
                await RepositoryManager.CommitFiles(filesToCommit, commitMessage, commitBody).StartAsAsync();
                StopTrackTimeAndLog(watch, logger);
                await TaskManager.Wait();

                RepositoryManager.WaitForEvents();
                await repositoryManagerEvents.WaitForNotBusy();

                // we expect these events
                await AssertReceivedEvent(repositoryManagerEvents.LocalBranchesUpdated);
                await AssertReceivedEvent(repositoryManagerEvents.RemoteBranchesUpdated);
                await AssertReceivedEvent(repositoryManagerEvents.GitAheadBehindStatusUpdated);
                await AssertReceivedEvent(repositoryManagerEvents.GitLogUpdated);
                await AssertReceivedEvent(repositoryManagerEvents.GitStatusUpdated);
                await AssertReceivedEvent(repositoryManagerEvents.CurrentBranchUpdated);

                // we don't expect these events
                await AssertDidNotReceiveEvent(repositoryManagerEvents.GitLocksUpdated);
            }
            finally
            {
                EndTest(logger);
            }
        }

        private async Task AssertReceivedEvent(Task task)
        {
            (await TaskEx.WhenAny(task, TaskEx.Delay(Timeout))).Should().BeAssignableTo<Task<object>>("otherwise the event was not raised");
        }

        private async Task AssertDidNotReceiveEvent(Task task)
        {
            (await TaskEx.WhenAny(task, TaskEx.Delay(Timeout))).Should().BeAssignableTo<Task<bool>>("otherwise the event was raised");
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

                await TaskManager.Wait();

                StartTrackTime(watch, logger, "RepositoryManager.WaitForEvents()");
                RepositoryManager.WaitForEvents();
                StopTrackTimeAndLog(watch, logger);

                StartTrackTime(watch, logger, "repositoryManagerEvents.WaitForNotBusy()");
                await repositoryManagerEvents.WaitForNotBusy();
                StopTrackTimeAndLog(watch, logger);

                // we expect these events
                await AssertReceivedEvent(repositoryManagerEvents.GitStatusUpdated);

                // we don't expect these events
                await AssertDidNotReceiveEvent(repositoryManagerEvents.LocalBranchesUpdated);
                await AssertDidNotReceiveEvent(repositoryManagerEvents.RemoteBranchesUpdated);
                await AssertDidNotReceiveEvent(repositoryManagerEvents.GitAheadBehindStatusUpdated);
                await AssertDidNotReceiveEvent(repositoryManagerEvents.GitLogUpdated);
                await AssertDidNotReceiveEvent(repositoryManagerEvents.GitLocksUpdated);
                await AssertDidNotReceiveEvent(repositoryManagerEvents.CurrentBranchUpdated);

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

                // we expect these events
                await AssertReceivedEvent(repositoryManagerEvents.GitStatusUpdated);
                await AssertReceivedEvent(repositoryManagerEvents.LocalBranchesUpdated);
                await AssertReceivedEvent(repositoryManagerEvents.RemoteBranchesUpdated);
                await AssertReceivedEvent(repositoryManagerEvents.GitAheadBehindStatusUpdated);
                await AssertReceivedEvent(repositoryManagerEvents.GitLogUpdated);
                await AssertReceivedEvent(repositoryManagerEvents.CurrentBranchUpdated);

                // we don't expect these events
                await AssertDidNotReceiveEvent(repositoryManagerEvents.GitLocksUpdated);
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

                // we expect these events
                await AssertReceivedEvent(repositoryManagerEvents.GitStatusUpdated);
                await AssertReceivedEvent(repositoryManagerEvents.GitLogUpdated);
                await AssertReceivedEvent(repositoryManagerEvents.CurrentBranchUpdated);

                // we don't expect these events
                await AssertDidNotReceiveEvent(repositoryManagerEvents.LocalBranchesUpdated);
                await AssertDidNotReceiveEvent(repositoryManagerEvents.RemoteBranchesUpdated);
                await AssertDidNotReceiveEvent(repositoryManagerEvents.GitAheadBehindStatusUpdated);
                await AssertDidNotReceiveEvent(repositoryManagerEvents.GitLocksUpdated);
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

                // we expect these events
                await AssertReceivedEvent(repositoryManagerEvents.LocalBranchesUpdated);
                await AssertReceivedEvent(repositoryManagerEvents.RemoteBranchesUpdated);
                await AssertReceivedEvent(repositoryManagerEvents.GitAheadBehindStatusUpdated);
                await AssertReceivedEvent(repositoryManagerEvents.GitLogUpdated);
                await AssertReceivedEvent(repositoryManagerEvents.CurrentBranchUpdated);

                // we don't expect these events
                await AssertDidNotReceiveEvent(repositoryManagerEvents.GitStatusUpdated);
                await AssertDidNotReceiveEvent(repositoryManagerEvents.GitLocksUpdated);
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

                {
                    // prepopulate repository info cache
                    var b = Repository.CurrentBranch;
                    await TaskManager.Wait();
                    RepositoryManager.WaitForEvents();
                    await AssertReceivedEvent(repositoryManagerEvents.CurrentBranchUpdated);
                    repositoryManagerListener.ClearReceivedCalls();
                    repositoryManagerEvents.Reset();
                }

                var createdBranch1 = "feature/document2";
                await RepositoryManager.CreateBranch(createdBranch1, "feature/document").StartAsAsync();
                await TaskManager.Wait();

                RepositoryManager.WaitForEvents();
                await repositoryManagerEvents.WaitForNotBusy();

                // we expect these events
                await AssertReceivedEvent(repositoryManagerEvents.LocalBranchesUpdated);
                await AssertReceivedEvent(repositoryManagerEvents.RemoteBranchesUpdated);
                await AssertReceivedEvent(repositoryManagerEvents.GitAheadBehindStatusUpdated);

                // we don't expect these events
                // TODO: log should not be getting called, but it is because when branches get changed we're blindly calling log
                //await AssertDidNotReceiveEvent(repositoryManagerEvents.GitLogUpdated);
                await AssertDidNotReceiveEvent(repositoryManagerEvents.GitStatusUpdated);
                await AssertDidNotReceiveEvent(repositoryManagerEvents.CurrentBranchUpdated);
                await AssertDidNotReceiveEvent(repositoryManagerEvents.GitLocksUpdated);

                repositoryManagerListener.ClearReceivedCalls();
                repositoryManagerEvents.Reset();

                await RepositoryManager.CreateBranch("feature2/document2", "feature/document").StartAsAsync();
                await TaskManager.Wait();

                StartTrackTime(watch, logger, "CreateBranch");
                RepositoryManager.WaitForEvents();
                StopTrackTimeAndLog(watch, logger);

                await repositoryManagerEvents.WaitForNotBusy();

                // we expect these events
                await AssertReceivedEvent(repositoryManagerEvents.LocalBranchesUpdated);
                await AssertReceivedEvent(repositoryManagerEvents.RemoteBranchesUpdated);
                await AssertReceivedEvent(repositoryManagerEvents.GitAheadBehindStatusUpdated);

                // we don't expect these events
                await AssertDidNotReceiveEvent(repositoryManagerEvents.GitStatusUpdated);
                // TODO: log should not be getting called, but it is because when branches get changed we're blindly calling log
                //await AssertDidNotReceiveEvent(repositoryManagerEvents.GitLogUpdated);
                await AssertDidNotReceiveEvent(repositoryManagerEvents.CurrentBranchUpdated);
                await AssertDidNotReceiveEvent(repositoryManagerEvents.GitLocksUpdated);
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

                // we expect these events
                await AssertReceivedEvent(repositoryManagerEvents.LocalBranchesUpdated);
                await AssertReceivedEvent(repositoryManagerEvents.RemoteBranchesUpdated);
                await AssertReceivedEvent(repositoryManagerEvents.GitAheadBehindStatusUpdated);
                await AssertReceivedEvent(repositoryManagerEvents.GitLogUpdated);
                await AssertReceivedEvent(repositoryManagerEvents.CurrentBranchUpdated);

                // we don't expect these events
                await AssertDidNotReceiveEvent(repositoryManagerEvents.GitStatusUpdated);
                await AssertDidNotReceiveEvent(repositoryManagerEvents.GitLocksUpdated);

                repositoryManagerListener.ClearReceivedCalls();
                repositoryManagerEvents.Reset();

                await RepositoryManager.RemoteAdd("origin", "https://github.com/EvilShana/IOTestsRepo.git").StartAsAsync();
                await TaskManager.Wait();

                RepositoryManager.WaitForEvents();
                await repositoryManagerEvents.WaitForNotBusy();

                // we expect these events
                await AssertReceivedEvent(repositoryManagerEvents.LocalBranchesUpdated);
                await AssertReceivedEvent(repositoryManagerEvents.RemoteBranchesUpdated);
                await AssertReceivedEvent(repositoryManagerEvents.GitAheadBehindStatusUpdated);
                await AssertReceivedEvent(repositoryManagerEvents.GitLogUpdated);
                await AssertReceivedEvent(repositoryManagerEvents.CurrentBranchUpdated);

                // we don't expect these events
                await AssertDidNotReceiveEvent(repositoryManagerEvents.GitStatusUpdated);
                await AssertDidNotReceiveEvent(repositoryManagerEvents.GitLocksUpdated);
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

                // we expect these events
                await AssertReceivedEvent(repositoryManagerEvents.LocalBranchesUpdated);
                await AssertReceivedEvent(repositoryManagerEvents.RemoteBranchesUpdated);
                await AssertReceivedEvent(repositoryManagerEvents.GitAheadBehindStatusUpdated);
                await AssertReceivedEvent(repositoryManagerEvents.GitLogUpdated);
                await AssertReceivedEvent(repositoryManagerEvents.CurrentBranchUpdated);

                // we don't expect these events
                await AssertDidNotReceiveEvent(repositoryManagerEvents.GitStatusUpdated);
                await AssertDidNotReceiveEvent(repositoryManagerEvents.GitLocksUpdated);

                repositoryManagerListener.ClearReceivedCalls();
                repositoryManagerEvents.Reset();

                await RepositoryManager.SwitchBranch("branch2").StartAsAsync();
                await TaskManager.Wait();

                RepositoryManager.WaitForEvents();
                await repositoryManagerEvents.WaitForNotBusy();

                // we expect these events
                await AssertReceivedEvent(repositoryManagerEvents.GitStatusUpdated);
                await AssertReceivedEvent(repositoryManagerEvents.GitAheadBehindStatusUpdated);
                await AssertReceivedEvent(repositoryManagerEvents.GitLogUpdated);
                await AssertReceivedEvent(repositoryManagerEvents.CurrentBranchUpdated);

                // we don't expect these events
                await AssertDidNotReceiveEvent(repositoryManagerEvents.LocalBranchesUpdated);
                await AssertDidNotReceiveEvent(repositoryManagerEvents.RemoteBranchesUpdated);
                await AssertDidNotReceiveEvent(repositoryManagerEvents.GitLocksUpdated);
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

                // we expect these events
                await AssertReceivedEvent(repositoryManagerEvents.GitStatusUpdated);
                await AssertReceivedEvent(repositoryManagerEvents.LocalBranchesUpdated);
                await AssertReceivedEvent(repositoryManagerEvents.GitAheadBehindStatusUpdated);
                await AssertReceivedEvent(repositoryManagerEvents.GitLogUpdated);
                await AssertReceivedEvent(repositoryManagerEvents.CurrentBranchUpdated);

                // we don't expect these events
                // TODO: this should not happen but it's happening right now because when local branches get updated in the cache, remotes get updated too
                //await AssertDidNotReceiveEvent(repositoryManagerEvents.RemoteBranchesUpdated);
                await AssertDidNotReceiveEvent(repositoryManagerEvents.GitLocksUpdated);
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

                // we expect these events
                await AssertReceivedEvent(repositoryManagerEvents.LocalBranchesUpdated);
                await AssertReceivedEvent(repositoryManagerEvents.RemoteBranchesUpdated);
                await AssertReceivedEvent(repositoryManagerEvents.GitAheadBehindStatusUpdated);
                await AssertReceivedEvent(repositoryManagerEvents.CurrentBranchUpdated);

                // we don't expect these events
                await AssertDidNotReceiveEvent(repositoryManagerEvents.GitStatusUpdated);
                // TODO: log should not be getting called, but it is because when branches get changed we're blindly calling log
                //await AssertDidNotReceiveEvent(repositoryManagerEvents.GitLogUpdated);
                //await AssertDidNotReceiveEvent(repositoryManagerEvents.GitAheadBehindStatusUpdated);
                await AssertDidNotReceiveEvent(repositoryManagerEvents.GitLocksUpdated);
            }
            finally
            {
                EndTest(logger);
            }
        }
    }
}
