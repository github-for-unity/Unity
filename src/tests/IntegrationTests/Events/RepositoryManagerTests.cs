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

        private void StartTest(out Stopwatch watch, out ILogging logger, [CallerMemberName] string testName = "test")
        {
            watch = new Stopwatch();
            logger = LogHelper.GetLogger(testName);
            logger.Trace("Starting test");
        }

        private void EndTest(ILogging logger)
        {
            logger.Trace("Ending test");
        }

        private void StartTrackTime(Stopwatch watch, ILogging logger, string message = "")
        {
            if (!String.IsNullOrEmpty(message))
                logger.Trace(message);
            watch.Reset();
            watch.Start();
        }

        private void StopTrackTimeAndLog(Stopwatch watch, ILogging logger)
        {
            watch.Stop();
            logger.Trace($"Time: {watch.ElapsedMilliseconds}");
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

                //repositoryManagerListener.ClearReceivedCalls();
                //repositoryManagerEvents.Reset();
                repositoryManagerListener.AssertDidNotReceiveAnyCalls();

                var foobarTxt = TestRepoMasterCleanSynchronized.Combine("foobar.txt");
                foobarTxt.WriteAllText("foobar");

                await TaskManager.Wait();

                StartTrackTime(watch, logger, "RepositoryManager.WaitForEvents()");
                RepositoryManager.WaitForEvents();
                StopTrackTimeAndLog(watch, logger);

                StartTrackTime(watch, logger, "repositoryManagerEvents.WaitForNotBusy()");
                repositoryManagerEvents.WaitForNotBusy();
                StopTrackTimeAndLog(watch, logger);

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

                var foobarTxt = TestRepoMasterCleanSynchronized.Combine("foobar.txt");
                foobarTxt.WriteAllText("foobar");

                var testDocumentTxt = TestRepoMasterCleanSynchronized.Combine("Assets", "TestDocument.txt");
                testDocumentTxt.WriteAllText("foobar");

                await TaskManager.Wait();

                StartTrackTime(watch, logger, "RepositoryManager.WaitForEvents()");
                RepositoryManager.WaitForEvents();
                StopTrackTimeAndLog(watch, logger);

                StartTrackTime(watch, logger, "repositoryManagerEvents.WaitForNotBusy()");
                repositoryManagerEvents.WaitForNotBusy();
                StopTrackTimeAndLog(watch, logger);

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

                StartTrackTime(watch, logger, "CommitAllFiles");
                await RepositoryManager
                    .CommitAllFiles("IntegrationTest Commit", string.Empty)
                    .StartAsAsync();

                StopTrackTimeAndLog(watch, logger);
                await TaskManager.Wait();

                StartTrackTime(watch, logger, "RepositoryManager.WaitForEvents()");
                RepositoryManager.WaitForEvents();
                StopTrackTimeAndLog(watch, logger);

                StartTrackTime(watch, logger, "repositoryManagerEvents.WaitForNotBusy()");
                repositoryManagerEvents.WaitForNotBusy();
                StopTrackTimeAndLog(watch, logger);

                repositoryManagerEvents.GitStatusUpdated.WaitOne(Timeout).Should().BeTrue();
                repositoryManagerEvents.LocalBranchesUpdated.WaitOne(Timeout).Should().BeTrue();
                repositoryManagerEvents.GitLogUpdated.WaitOne(Timeout).Should().BeTrue();

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
                repositoryManagerEvents.WaitForNotBusy();

                repositoryManagerEvents.LocalBranchesUpdated.WaitOne(Timeout).Should().BeTrue();

                repositoryManagerListener.Received().OnIsBusyChanged(Args.Bool);
                repositoryManagerListener.Received().CurrentBranchUpdated(Args.NullableConfigBranch, Args.NullableConfigRemote);
                repositoryManagerListener.DidNotReceive().GitAheadBehindStatusUpdated(Args.GitAheadBehindStatus);
                repositoryManagerListener.DidNotReceive().GitStatusUpdated(Args.GitStatus);
                repositoryManagerListener.DidNotReceive().GitLocksUpdated(Args.GitLocks);
                repositoryManagerListener.DidNotReceive().GitLogUpdated(Args.GitLogs);
                repositoryManagerListener.Received().LocalBranchesUpdated(Args.LocalBranchDictionary);
                repositoryManagerListener.Received().RemoteBranchesUpdated(Args.RemoteDictionary, Args.RemoteBranchDictionary);

                repositoryManagerListener.ClearReceivedCalls();
                repositoryManagerEvents.Reset();

                await RepositoryManager.CreateBranch("feature2/document2", "feature/document").StartAsAsync();
                await TaskManager.Wait();

                StartTrackTime(watch, logger, "CreateBranch");
                RepositoryManager.WaitForEvents();
                StopTrackTimeAndLog(watch, logger);

                repositoryManagerEvents.WaitForNotBusy();

                repositoryManagerEvents.LocalBranchesUpdated.WaitOne(Timeout).Should().BeTrue();

                repositoryManagerListener.Received().OnIsBusyChanged(Args.Bool);
                repositoryManagerListener.DidNotReceive().CurrentBranchUpdated(Args.NullableConfigBranch, Args.NullableConfigRemote);
                repositoryManagerListener.Received().GitAheadBehindStatusUpdated(Args.GitAheadBehindStatus);
                repositoryManagerListener.DidNotReceive().GitStatusUpdated(Args.GitStatus);
                repositoryManagerListener.DidNotReceive().GitLocksUpdated(Args.GitLocks);
                repositoryManagerListener.DidNotReceive().GitLogUpdated(Args.GitLogs);
                repositoryManagerListener.Received().LocalBranchesUpdated(Args.LocalBranchDictionary);
                repositoryManagerListener.Received().RemoteBranchesUpdated(Args.RemoteDictionary, Args.RemoteBranchDictionary);
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

                repositoryManagerListener.Received().OnIsBusyChanged(Args.Bool);
                repositoryManagerListener.Received().CurrentBranchUpdated(Args.NullableConfigBranch, Args.NullableConfigRemote);
                repositoryManagerListener.Received().GitAheadBehindStatusUpdated(Args.GitAheadBehindStatus);
                repositoryManagerListener.DidNotReceive().GitStatusUpdated(Args.GitStatus);
                repositoryManagerListener.DidNotReceive().GitLocksUpdated(Args.GitLocks);
                repositoryManagerListener.Received().GitLogUpdated(Args.GitLogs);
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
                repositoryManagerEvents.WaitForNotBusy();

                repositoryManagerEvents.LocalBranchesUpdated.WaitOne(Timeout).Should().BeTrue();
                repositoryManagerEvents.GitStatusUpdated.WaitOne(Timeout).Should().BeTrue();

                repositoryManagerListener.Received().OnIsBusyChanged(Args.Bool);
                repositoryManagerListener.Received().CurrentBranchUpdated(Args.NullableConfigBranch, Args.NullableConfigRemote);
                repositoryManagerListener.Received().GitAheadBehindStatusUpdated(Args.GitAheadBehindStatus);
                repositoryManagerListener.Received().GitStatusUpdated(Args.GitStatus);
                repositoryManagerListener.DidNotReceive().GitLocksUpdated(Args.GitLocks);
                repositoryManagerListener.Received().GitLogUpdated(Args.GitLogs);
                repositoryManagerListener.Received().LocalBranchesUpdated(Args.LocalBranchDictionary);
                repositoryManagerListener.DidNotReceive().RemoteBranchesUpdated(Args.RemoteDictionary, Args.RemoteBranchDictionary);
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
                repositoryManagerEvents.WaitForNotBusy();

                repositoryManagerEvents.RemoteBranchesUpdated.WaitOne(Timeout).Should().BeTrue();
                repositoryManagerEvents.GitAheadBehindStatusUpdated.WaitOne(Timeout).Should().BeTrue();

                repositoryManagerListener.Received().OnIsBusyChanged(Args.Bool);
                repositoryManagerListener.Received().CurrentBranchUpdated(Args.NullableConfigBranch, Args.NullableConfigRemote);
                repositoryManagerListener.Received().GitAheadBehindStatusUpdated(Args.GitAheadBehindStatus);
                repositoryManagerListener.DidNotReceive().GitStatusUpdated(Args.GitStatus);
                repositoryManagerListener.DidNotReceive().GitLocksUpdated(Args.GitLocks);
                repositoryManagerListener.DidNotReceive().GitLogUpdated(Args.GitLogs);
                repositoryManagerListener.Received().LocalBranchesUpdated(Args.LocalBranchDictionary);
                repositoryManagerListener.Received().RemoteBranchesUpdated(Args.RemoteDictionary, Args.RemoteBranchDictionary);
            }
            finally
            {
                EndTest(logger);
            }
        }
    }
}
