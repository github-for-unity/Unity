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
    [TestFixture]
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

                await TaskEx.WhenAny(
                    TaskEx.WhenAll(
                        repositoryManagerEvents.GitLogUpdated,
                        repositoryManagerEvents.GitStatusUpdated,
                        repositoryManagerEvents.CurrentBranchUpdated,
                        repositoryManagerEvents.LocalBranchesUpdated,
                        repositoryManagerEvents.RemoteBranchesUpdated,
                        repositoryManagerEvents.GitAheadBehindStatusUpdated,
                        repositoryManagerEvents.GitLocksUpdated
                    ),
                    TaskEx.Delay(TimeSpan.FromSeconds(10))
                );

                // we expect these events
                await AssertReceivedEvent(nameof(repositoryManagerEvents.GitStatusUpdated), repositoryManagerEvents.GitStatusUpdated);
                await AssertReceivedEvent(nameof(repositoryManagerEvents.LocalBranchesUpdated), repositoryManagerEvents.LocalBranchesUpdated);
                await AssertReceivedEvent(nameof(repositoryManagerEvents.RemoteBranchesUpdated), repositoryManagerEvents.RemoteBranchesUpdated);
                await AssertReceivedEvent(nameof(repositoryManagerEvents.GitAheadBehindStatusUpdated), repositoryManagerEvents.GitAheadBehindStatusUpdated);
                await AssertReceivedEvent(nameof(repositoryManagerEvents.GitLocksUpdated), repositoryManagerEvents.GitLocksUpdated);
                await AssertReceivedEvent(nameof(repositoryManagerEvents.GitLogUpdated), repositoryManagerEvents.GitLogUpdated);
                await AssertReceivedEvent(nameof(repositoryManagerEvents.CurrentBranchUpdated), repositoryManagerEvents.CurrentBranchUpdated);
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

                await TaskEx.WhenAny(
                    TaskEx.WhenAll(
                        repositoryManagerEvents.GitStatusUpdated,
                        repositoryManagerEvents.GitLogUpdated,
                        repositoryManagerEvents.GitLocksUpdated,
                        repositoryManagerEvents.LocalBranchesUpdated,
                        repositoryManagerEvents.RemoteBranchesUpdated,
                        repositoryManagerEvents.GitAheadBehindStatusUpdated,
                        repositoryManagerEvents.CurrentBranchUpdated
                    ),
                    TaskEx.Delay(TimeSpan.FromSeconds(1))
                );

                // we expect these events
                await AssertReceivedEvent(nameof(repositoryManagerEvents.GitStatusUpdated), repositoryManagerEvents.GitStatusUpdated);
                await AssertReceivedEvent(nameof(repositoryManagerEvents.GitLogUpdated), repositoryManagerEvents.GitLogUpdated);
                await AssertReceivedEvent(nameof(repositoryManagerEvents.GitLocksUpdated), repositoryManagerEvents.GitLocksUpdated);
                await AssertReceivedEvent(nameof(repositoryManagerEvents.LocalBranchesUpdated), repositoryManagerEvents.LocalBranchesUpdated);
                await AssertReceivedEvent(nameof(repositoryManagerEvents.RemoteBranchesUpdated), repositoryManagerEvents.RemoteBranchesUpdated);
                await AssertReceivedEvent(nameof(repositoryManagerEvents.GitAheadBehindStatusUpdated), repositoryManagerEvents.GitAheadBehindStatusUpdated);
                await AssertReceivedEvent(nameof(repositoryManagerEvents.CurrentBranchUpdated), repositoryManagerEvents.CurrentBranchUpdated);

                repositoryManagerListener.ClearReceivedCalls();
                repositoryManagerEvents.Reset();

                var filesToCommit = new List<string> { "foobar.txt" };
                var commitMessage = "IntegrationTest Commit";
                var commitBody = string.Empty;

                StartTrackTime(watch, logger, "CommitFiles");
                await RepositoryManager.CommitFiles(filesToCommit, commitMessage, commitBody).StartAsAsync();
                StopTrackTimeAndLog(watch, logger);

                await TaskEx.WhenAny(
                    TaskEx.WhenAll(
                        repositoryManagerEvents.GitStatusUpdated,
                        repositoryManagerEvents.GitLogUpdated,
                        repositoryManagerEvents.LocalBranchesUpdated,
                        repositoryManagerEvents.RemoteBranchesUpdated,
                        repositoryManagerEvents.GitAheadBehindStatusUpdated
                    ),
                    TaskEx.Delay(TimeSpan.FromSeconds(1))
                );

                // we expect these events
                await AssertReceivedEvent(nameof(repositoryManagerEvents.GitStatusUpdated), repositoryManagerEvents.GitStatusUpdated);
                await AssertReceivedEvent(nameof(repositoryManagerEvents.GitLogUpdated), repositoryManagerEvents.GitLogUpdated);
                await AssertReceivedEvent(nameof(repositoryManagerEvents.LocalBranchesUpdated), repositoryManagerEvents.LocalBranchesUpdated);
                await AssertReceivedEvent(nameof(repositoryManagerEvents.RemoteBranchesUpdated), repositoryManagerEvents.RemoteBranchesUpdated);
                await AssertReceivedEvent(nameof(repositoryManagerEvents.GitAheadBehindStatusUpdated), repositoryManagerEvents.GitAheadBehindStatusUpdated);

                // we don't expect these events
                await AssertDidNotReceiveEvent(nameof(repositoryManagerEvents.GitLocksUpdated), repositoryManagerEvents.GitLocksUpdated);
                await AssertDidNotReceiveEvent(nameof(repositoryManagerEvents.CurrentBranchUpdated), repositoryManagerEvents.CurrentBranchUpdated);
            }
            finally
            {
                EndTest(logger);
            }
        }

        private async Task AssertReceivedEvent(string eventName, Task task)
        {
            try
            {
                (await TaskEx.WhenAny(task, TaskEx.Delay(Timeout))).Should().BeAssignableTo<Task<object>>("otherwise the event was not raised");
            }
            catch
            {
                throw new Exception($"Event {eventName} should have been raised");
            }
        }

        private async Task AssertDidNotReceiveEvent(string eventName, Task task)
        {
            try
            {
                (await TaskEx.WhenAny(task, TaskEx.Delay(Timeout))).Should().BeAssignableTo<Task<bool>>("otherwise the event was raised");
            }
            catch
            {
                throw new Exception($"Event {eventName} should not have been raised");
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

                await TaskEx.WhenAny(
                    TaskEx.WhenAll(
                        repositoryManagerEvents.GitStatusUpdated,
                        repositoryManagerEvents.GitLogUpdated,
                        repositoryManagerEvents.GitLocksUpdated,
                        repositoryManagerEvents.LocalBranchesUpdated,
                        repositoryManagerEvents.RemoteBranchesUpdated,
                        repositoryManagerEvents.GitAheadBehindStatusUpdated,
                        repositoryManagerEvents.CurrentBranchUpdated
                    ),
                    TaskEx.Delay(TimeSpan.FromSeconds(1))
                );

                // we expect these events
                await AssertReceivedEvent(nameof(repositoryManagerEvents.GitStatusUpdated), repositoryManagerEvents.GitStatusUpdated);
                await AssertReceivedEvent(nameof(repositoryManagerEvents.GitLogUpdated), repositoryManagerEvents.GitLogUpdated);
                await AssertReceivedEvent(nameof(repositoryManagerEvents.GitLocksUpdated), repositoryManagerEvents.GitLocksUpdated);
                await AssertReceivedEvent(nameof(repositoryManagerEvents.LocalBranchesUpdated), repositoryManagerEvents.LocalBranchesUpdated);
                await AssertReceivedEvent(nameof(repositoryManagerEvents.RemoteBranchesUpdated), repositoryManagerEvents.RemoteBranchesUpdated);
                await AssertReceivedEvent(nameof(repositoryManagerEvents.GitAheadBehindStatusUpdated), repositoryManagerEvents.GitAheadBehindStatusUpdated);
                await AssertReceivedEvent(nameof(repositoryManagerEvents.CurrentBranchUpdated), repositoryManagerEvents.CurrentBranchUpdated);

                repositoryManagerListener.ClearReceivedCalls();
                repositoryManagerEvents.Reset();

                StartTrackTime(watch, logger, "CommitAllFiles");
                await RepositoryManager.CommitAllFiles("IntegrationTest Commit", string.Empty).StartAsAsync();
                StopTrackTimeAndLog(watch, logger);

                await TaskEx.WhenAny(
                    TaskEx.WhenAll(
                        repositoryManagerEvents.GitStatusUpdated,
                        repositoryManagerEvents.GitLogUpdated,
                        repositoryManagerEvents.LocalBranchesUpdated,
                        repositoryManagerEvents.RemoteBranchesUpdated,
                        repositoryManagerEvents.GitAheadBehindStatusUpdated
                    ),
                    TaskEx.Delay(TimeSpan.FromSeconds(1))
                );

                // we expect these events
                await AssertReceivedEvent(nameof(repositoryManagerEvents.GitStatusUpdated), repositoryManagerEvents.GitStatusUpdated);
                await AssertReceivedEvent(nameof(repositoryManagerEvents.GitLogUpdated), repositoryManagerEvents.GitLogUpdated);
                await AssertReceivedEvent(nameof(repositoryManagerEvents.LocalBranchesUpdated), repositoryManagerEvents.LocalBranchesUpdated);
                await AssertReceivedEvent(nameof(repositoryManagerEvents.RemoteBranchesUpdated), repositoryManagerEvents.RemoteBranchesUpdated);
                await AssertReceivedEvent(nameof(repositoryManagerEvents.GitAheadBehindStatusUpdated), repositoryManagerEvents.GitAheadBehindStatusUpdated);

                // we don't expect these events
                await AssertDidNotReceiveEvent(nameof(repositoryManagerEvents.GitLocksUpdated), repositoryManagerEvents.GitLocksUpdated);
                await AssertDidNotReceiveEvent(nameof(repositoryManagerEvents.CurrentBranchUpdated), repositoryManagerEvents.CurrentBranchUpdated);
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

                await TaskEx.WhenAny(
                    TaskEx.WhenAll(
                        repositoryManagerEvents.GitStatusUpdated,
                        repositoryManagerEvents.GitLogUpdated,
                        repositoryManagerEvents.CurrentBranchUpdated,
                        repositoryManagerEvents.LocalBranchesUpdated,
                        repositoryManagerEvents.RemoteBranchesUpdated,
                        repositoryManagerEvents.GitLocksUpdated
                    ),
                    TaskEx.Delay(TimeSpan.FromSeconds(1))
                );

                // we expect these events
                await AssertReceivedEvent(nameof(repositoryManagerEvents.GitStatusUpdated), repositoryManagerEvents.GitStatusUpdated);
                await AssertReceivedEvent(nameof(repositoryManagerEvents.GitLogUpdated), repositoryManagerEvents.GitLogUpdated);
                await AssertReceivedEvent(nameof(repositoryManagerEvents.CurrentBranchUpdated), repositoryManagerEvents.CurrentBranchUpdated);
                await AssertReceivedEvent(nameof(repositoryManagerEvents.LocalBranchesUpdated), repositoryManagerEvents.LocalBranchesUpdated);
                await AssertReceivedEvent(nameof(repositoryManagerEvents.RemoteBranchesUpdated), repositoryManagerEvents.RemoteBranchesUpdated);
                await AssertReceivedEvent(nameof(repositoryManagerEvents.GitLocksUpdated), repositoryManagerEvents.GitLocksUpdated);

                // we don't expect these events
                await AssertDidNotReceiveEvent(nameof(repositoryManagerEvents.GitAheadBehindStatusUpdated), repositoryManagerEvents.GitAheadBehindStatusUpdated);
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

                await TaskEx.WhenAny(
                    TaskEx.WhenAll(
                        repositoryManagerEvents.GitLogUpdated,
                        repositoryManagerEvents.CurrentBranchUpdated,
                        repositoryManagerEvents.LocalBranchesUpdated,
                        repositoryManagerEvents.RemoteBranchesUpdated,
                        repositoryManagerEvents.GitAheadBehindStatusUpdated,
                        repositoryManagerEvents.GitLocksUpdated
                    ),
                    TaskEx.Delay(TimeSpan.FromSeconds(10))
                );

                // we expect these events
                await AssertReceivedEvent(nameof(repositoryManagerEvents.LocalBranchesUpdated), repositoryManagerEvents.LocalBranchesUpdated);
                await AssertReceivedEvent(nameof(repositoryManagerEvents.RemoteBranchesUpdated), repositoryManagerEvents.RemoteBranchesUpdated);
                await AssertReceivedEvent(nameof(repositoryManagerEvents.GitAheadBehindStatusUpdated), repositoryManagerEvents.GitAheadBehindStatusUpdated);
                await AssertReceivedEvent(nameof(repositoryManagerEvents.GitLogUpdated), repositoryManagerEvents.GitLogUpdated);
                await AssertReceivedEvent(nameof(repositoryManagerEvents.CurrentBranchUpdated), repositoryManagerEvents.CurrentBranchUpdated);
                await AssertReceivedEvent(nameof(repositoryManagerEvents.GitLocksUpdated), repositoryManagerEvents.GitLocksUpdated);

                // we don't expect these events
                await AssertDidNotReceiveEvent(nameof(repositoryManagerEvents.GitStatusUpdated), repositoryManagerEvents.GitStatusUpdated);
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
                    await AssertReceivedEvent(nameof(repositoryManagerEvents.CurrentBranchUpdated), repositoryManagerEvents.CurrentBranchUpdated);
                    repositoryManagerListener.ClearReceivedCalls();
                    repositoryManagerEvents.Reset();
                }

                var createdBranch1 = "feature/document2";
                await RepositoryManager.CreateBranch(createdBranch1, "feature/document").StartAsAsync();
                await TaskManager.Wait();

                await TaskEx.WhenAny(
                    TaskEx.WhenAll(
                        repositoryManagerEvents.GitLogUpdated,
                        repositoryManagerEvents.LocalBranchesUpdated,
                        repositoryManagerEvents.RemoteBranchesUpdated,
                        repositoryManagerEvents.GitAheadBehindStatusUpdated,
                        repositoryManagerEvents.GitLocksUpdated
                    ),
                    TaskEx.Delay(TimeSpan.FromSeconds(10))
                );

                // we expect these events
                await AssertReceivedEvent(nameof(repositoryManagerEvents.LocalBranchesUpdated), repositoryManagerEvents.LocalBranchesUpdated);
                await AssertReceivedEvent(nameof(repositoryManagerEvents.RemoteBranchesUpdated), repositoryManagerEvents.RemoteBranchesUpdated);
                await AssertReceivedEvent(nameof(repositoryManagerEvents.GitAheadBehindStatusUpdated), repositoryManagerEvents.GitAheadBehindStatusUpdated);
                await AssertReceivedEvent(nameof(repositoryManagerEvents.GitLogUpdated), repositoryManagerEvents.GitLogUpdated);
                await AssertReceivedEvent(nameof(repositoryManagerEvents.GitLocksUpdated), repositoryManagerEvents.GitLocksUpdated);

                // we don't expect these events
                await AssertDidNotReceiveEvent(nameof(repositoryManagerEvents.GitStatusUpdated), repositoryManagerEvents.GitStatusUpdated);
                await AssertDidNotReceiveEvent(nameof(repositoryManagerEvents.CurrentBranchUpdated), repositoryManagerEvents.CurrentBranchUpdated);

                repositoryManagerListener.ClearReceivedCalls();
                repositoryManagerEvents.Reset();

                await RepositoryManager.CreateBranch("feature2/document2", "feature/document").StartAsAsync();
                await TaskManager.Wait();

                StartTrackTime(watch, logger, "CreateBranch");
                RepositoryManager.WaitForEvents();
                StopTrackTimeAndLog(watch, logger);

                await repositoryManagerEvents.WaitForNotBusy();

                // we expect these events
                await AssertReceivedEvent(nameof(repositoryManagerEvents.LocalBranchesUpdated), repositoryManagerEvents.LocalBranchesUpdated);
                await AssertReceivedEvent(nameof(repositoryManagerEvents.RemoteBranchesUpdated), repositoryManagerEvents.RemoteBranchesUpdated);
                await AssertReceivedEvent(nameof(repositoryManagerEvents.GitAheadBehindStatusUpdated), repositoryManagerEvents.GitAheadBehindStatusUpdated);

                // we don't expect these events
                await AssertDidNotReceiveEvent(nameof(repositoryManagerEvents.GitStatusUpdated), repositoryManagerEvents.GitStatusUpdated);
                // TODO: log should not be getting called, but it is because when branches get changed we're blindly calling log
                //await AssertDidNotReceiveEvent(repositoryManagerEvents.GitLogUpdated);
                await AssertDidNotReceiveEvent(nameof(repositoryManagerEvents.CurrentBranchUpdated), repositoryManagerEvents.CurrentBranchUpdated);
                await AssertDidNotReceiveEvent(nameof(repositoryManagerEvents.GitLocksUpdated), repositoryManagerEvents.GitLocksUpdated);
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

                await TaskEx.WhenAny(
                    TaskEx.WhenAll(
                        repositoryManagerEvents.GitLogUpdated,
                        repositoryManagerEvents.CurrentBranchUpdated,
                        repositoryManagerEvents.LocalBranchesUpdated,
                        repositoryManagerEvents.RemoteBranchesUpdated,
                        repositoryManagerEvents.GitAheadBehindStatusUpdated,
                        repositoryManagerEvents.GitLocksUpdated
                    ),
                    TaskEx.Delay(TimeSpan.FromSeconds(10))
                );

                // we expect these events
                await AssertReceivedEvent(nameof(repositoryManagerEvents.LocalBranchesUpdated), repositoryManagerEvents.LocalBranchesUpdated);
                await AssertReceivedEvent(nameof(repositoryManagerEvents.RemoteBranchesUpdated), repositoryManagerEvents.RemoteBranchesUpdated);
                await AssertReceivedEvent(nameof(repositoryManagerEvents.GitAheadBehindStatusUpdated), repositoryManagerEvents.GitAheadBehindStatusUpdated);
                await AssertReceivedEvent(nameof(repositoryManagerEvents.GitLogUpdated), repositoryManagerEvents.GitLogUpdated);
                await AssertReceivedEvent(nameof(repositoryManagerEvents.CurrentBranchUpdated), repositoryManagerEvents.CurrentBranchUpdated);
                await AssertReceivedEvent(nameof(repositoryManagerEvents.GitLocksUpdated), repositoryManagerEvents.GitLocksUpdated);

                // we don't expect these events
                await AssertDidNotReceiveEvent(nameof(repositoryManagerEvents.GitStatusUpdated), repositoryManagerEvents.GitStatusUpdated);

                repositoryManagerListener.ClearReceivedCalls();
                repositoryManagerEvents.Reset();

                await RepositoryManager.RemoteAdd("origin", "https://github.com/EvilShana/IOTestsRepo.git").StartAsAsync();
                await TaskManager.Wait();

                await TaskEx.WhenAny(
                    TaskEx.WhenAll(
                        repositoryManagerEvents.GitLogUpdated,
                        repositoryManagerEvents.CurrentBranchUpdated,
                        repositoryManagerEvents.LocalBranchesUpdated,
                        repositoryManagerEvents.RemoteBranchesUpdated
                    ),
                    TaskEx.Delay(TimeSpan.FromSeconds(10))
                );

                // we expect these events
                await AssertReceivedEvent(nameof(repositoryManagerEvents.LocalBranchesUpdated), repositoryManagerEvents.LocalBranchesUpdated);
                await AssertReceivedEvent(nameof(repositoryManagerEvents.RemoteBranchesUpdated), repositoryManagerEvents.RemoteBranchesUpdated);
                await AssertReceivedEvent(nameof(repositoryManagerEvents.GitLogUpdated), repositoryManagerEvents.GitLogUpdated);
                await AssertReceivedEvent(nameof(repositoryManagerEvents.CurrentBranchUpdated), repositoryManagerEvents.CurrentBranchUpdated);

                // we don't expect these events
                await AssertDidNotReceiveEvent(nameof(repositoryManagerEvents.GitAheadBehindStatusUpdated), repositoryManagerEvents.GitAheadBehindStatusUpdated);
                await AssertDidNotReceiveEvent(nameof(repositoryManagerEvents.GitStatusUpdated), repositoryManagerEvents.GitStatusUpdated);
                await AssertDidNotReceiveEvent(nameof(repositoryManagerEvents.GitLocksUpdated), repositoryManagerEvents.GitLocksUpdated);
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

                await TaskEx.WhenAny(
                    TaskEx.WhenAll(
                        repositoryManagerEvents.GitLogUpdated,
                        repositoryManagerEvents.CurrentBranchUpdated,
                        repositoryManagerEvents.GitAheadBehindStatusUpdated,
                        repositoryManagerEvents.GitLocksUpdated,
                        repositoryManagerEvents.LocalBranchesUpdated,
                        repositoryManagerEvents.RemoteBranchesUpdated
                    ),
                    TaskEx.Delay(TimeSpan.FromSeconds(10))
                );

                // we expect these events
                await AssertReceivedEvent(nameof(repositoryManagerEvents.LocalBranchesUpdated), repositoryManagerEvents.LocalBranchesUpdated);
                await AssertReceivedEvent(nameof(repositoryManagerEvents.RemoteBranchesUpdated), repositoryManagerEvents.RemoteBranchesUpdated);
                await AssertReceivedEvent(nameof(repositoryManagerEvents.GitAheadBehindStatusUpdated), repositoryManagerEvents.GitAheadBehindStatusUpdated);
                await AssertReceivedEvent(nameof(repositoryManagerEvents.GitLogUpdated), repositoryManagerEvents.GitLogUpdated);
                await AssertReceivedEvent(nameof(repositoryManagerEvents.CurrentBranchUpdated), repositoryManagerEvents.CurrentBranchUpdated);
                await AssertReceivedEvent(nameof(repositoryManagerEvents.GitLocksUpdated), repositoryManagerEvents.GitLocksUpdated);

                // we don't expect these events
                await AssertDidNotReceiveEvent(nameof(repositoryManagerEvents.GitStatusUpdated), repositoryManagerEvents.GitStatusUpdated);

                repositoryManagerListener.ClearReceivedCalls();
                repositoryManagerEvents.Reset();

                await RepositoryManager.SwitchBranch("branch2").StartAsAsync();
                await TaskManager.Wait();

                RepositoryManager.WaitForEvents();
                await repositoryManagerEvents.WaitForNotBusy();

                // we expect these events
                await AssertReceivedEvent(nameof(repositoryManagerEvents.GitStatusUpdated), repositoryManagerEvents.GitStatusUpdated);
                await AssertReceivedEvent(nameof(repositoryManagerEvents.GitAheadBehindStatusUpdated), repositoryManagerEvents.GitAheadBehindStatusUpdated);
                await AssertReceivedEvent(nameof(repositoryManagerEvents.GitLogUpdated), repositoryManagerEvents.GitLogUpdated);
                await AssertReceivedEvent(nameof(repositoryManagerEvents.CurrentBranchUpdated), repositoryManagerEvents.CurrentBranchUpdated);

                // we don't expect these events
                await AssertDidNotReceiveEvent(nameof(repositoryManagerEvents.LocalBranchesUpdated), repositoryManagerEvents.LocalBranchesUpdated);
                await AssertDidNotReceiveEvent(nameof(repositoryManagerEvents.RemoteBranchesUpdated), repositoryManagerEvents.RemoteBranchesUpdated);
                await AssertDidNotReceiveEvent(nameof(repositoryManagerEvents.GitLocksUpdated), repositoryManagerEvents.GitLocksUpdated);
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

                await TaskEx.WhenAny(
                    TaskEx.WhenAll(
                        repositoryManagerEvents.GitLogUpdated,
                        repositoryManagerEvents.CurrentBranchUpdated,
                        repositoryManagerEvents.LocalBranchesUpdated,
                        repositoryManagerEvents.RemoteBranchesUpdated,
                        repositoryManagerEvents.GitAheadBehindStatusUpdated,
                        repositoryManagerEvents.GitLocksUpdated
                    ),
                    TaskEx.Delay(TimeSpan.FromSeconds(10))
                );

                // we expect these events
                await AssertReceivedEvent(nameof(repositoryManagerEvents.GitStatusUpdated), repositoryManagerEvents.GitStatusUpdated);
                await AssertReceivedEvent(nameof(repositoryManagerEvents.LocalBranchesUpdated), repositoryManagerEvents.LocalBranchesUpdated);
                await AssertReceivedEvent(nameof(repositoryManagerEvents.GitAheadBehindStatusUpdated), repositoryManagerEvents.GitAheadBehindStatusUpdated);
                await AssertReceivedEvent(nameof(repositoryManagerEvents.GitLogUpdated), repositoryManagerEvents.GitLogUpdated);
                await AssertReceivedEvent(nameof(repositoryManagerEvents.CurrentBranchUpdated), repositoryManagerEvents.CurrentBranchUpdated);
                await AssertReceivedEvent(nameof(repositoryManagerEvents.RemoteBranchesUpdated), repositoryManagerEvents.RemoteBranchesUpdated);
                await AssertReceivedEvent(nameof(repositoryManagerEvents.GitLocksUpdated), repositoryManagerEvents.GitLocksUpdated);
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

                await TaskEx.WhenAny(
                    TaskEx.WhenAll(
                        repositoryManagerEvents.GitLogUpdated,
                        repositoryManagerEvents.CurrentBranchUpdated,
                        repositoryManagerEvents.LocalBranchesUpdated,
                        repositoryManagerEvents.RemoteBranchesUpdated,
                        repositoryManagerEvents.GitAheadBehindStatusUpdated,
                        repositoryManagerEvents.GitLocksUpdated
                    ),
                    TaskEx.Delay(TimeSpan.FromSeconds(10))
                );

                // we expect these events
                await AssertReceivedEvent(nameof(repositoryManagerEvents.LocalBranchesUpdated), repositoryManagerEvents.LocalBranchesUpdated);
                await AssertReceivedEvent(nameof(repositoryManagerEvents.RemoteBranchesUpdated), repositoryManagerEvents.RemoteBranchesUpdated);
                await AssertReceivedEvent(nameof(repositoryManagerEvents.GitAheadBehindStatusUpdated), repositoryManagerEvents.GitAheadBehindStatusUpdated);
                await AssertReceivedEvent(nameof(repositoryManagerEvents.CurrentBranchUpdated), repositoryManagerEvents.CurrentBranchUpdated);
                await AssertReceivedEvent(nameof(repositoryManagerEvents.GitLogUpdated), repositoryManagerEvents.GitLogUpdated);
                await AssertReceivedEvent(nameof(repositoryManagerEvents.GitLocksUpdated), repositoryManagerEvents.GitLocksUpdated);

                // we don't expect these events
                await AssertDidNotReceiveEvent(nameof(repositoryManagerEvents.GitStatusUpdated), repositoryManagerEvents.GitStatusUpdated);
            }
            finally
            {
                EndTest(logger);
            }
        }
    }
}
