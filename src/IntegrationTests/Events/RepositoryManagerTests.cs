using System.Collections.Generic;
using System.Threading;
using FluentAssertions;
using GitHub.Unity;
using NSubstitute;
using NUnit.Framework;
using TestUtils;
using TestUtils.Events;

namespace IntegrationTests
{
    class RepositoryManagerTests : BaseGitEnvironmentTest
    {
        [Test]
        public void ShouldDetectFileChanges()
        {
            InitializeEnvironment(TestRepoMasterCleanSynchronized);

            var repositoryManager = CreateRepositoryManager(TestRepoMasterCleanSynchronized);
            repositoryManager.Initialize();
            repositoryManager.Start();

            var repositoryManagerListener = Substitute.For<IRepositoryManagerListener>();
            repositoryManagerListener.AttachListener(repositoryManager);

            try
            {
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
                repositoryManager.OnRepositoryChanged += status => { result = status; };

                Logger.Trace("Issuing Changes");

                var foobarTxt = TestRepoMasterCleanSynchronized.Combine("foobar.txt");
                foobarTxt.WriteAllText("foobar");

                Thread.Sleep(2000);

                Logger.Trace("Continue test");

                repositoryManagerListener.Received().OnRepositoryChanged(Args.GitStatus);
                result.AssertEqual(expected);

                repositoryManagerListener.DidNotReceive().OnIsBusyChanged(Args.Bool);
                repositoryManagerListener.DidNotReceive().OnActiveBranchChanged();
                repositoryManagerListener.DidNotReceive().OnActiveRemoteChanged();
                repositoryManagerListener.DidNotReceive().OnHeadChanged();
                repositoryManagerListener.DidNotReceive().OnLocalBranchListChanged();
                repositoryManagerListener.DidNotReceive().OnRemoteBranchListChanged();
                repositoryManagerListener.DidNotReceive().OnRemoteOrTrackingChanged();
            }
            finally
            {
                repositoryManager.Stop();
            }
        }

        [Test]
        public void ShouldAddAndCommitFiles()
        {
            InitializeEnvironment(TestRepoMasterCleanSynchronized);

            var repositoryManager = CreateRepositoryManager(TestRepoMasterCleanSynchronized);
            repositoryManager.Initialize();
            repositoryManager.Start();

            var repositoryManagerListener = Substitute.For<IRepositoryManagerListener>();
            repositoryManagerListener.AttachListener(repositoryManager);

            try
            {
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
                repositoryManager.OnRepositoryChanged += status => { result = status; };

                Logger.Trace("Issuing Changes");

                var foobarTxt = TestRepoMasterCleanSynchronized.Combine("foobar.txt");
                foobarTxt.WriteAllText("foobar");

                var testDocumentTxt = TestRepoMasterCleanSynchronized.Combine("Assets", "TestDocument.txt");
                testDocumentTxt.WriteAllText("foobar");

                Thread.Sleep(2000);

                Logger.Trace("Continue test");

                repositoryManagerListener.Received().OnRepositoryChanged(Args.GitStatus);
                result.AssertEqual(expectedAfterChanges);

                repositoryManagerListener.DidNotReceive().OnIsBusyChanged(Args.Bool);
                repositoryManagerListener.DidNotReceive().OnActiveBranchChanged();
                repositoryManagerListener.DidNotReceive().OnActiveRemoteChanged();
                repositoryManagerListener.DidNotReceive().OnHeadChanged();
                repositoryManagerListener.DidNotReceive().OnLocalBranchListChanged();
                repositoryManagerListener.DidNotReceive().OnRemoteBranchListChanged();
                repositoryManagerListener.DidNotReceive().OnRemoteOrTrackingChanged();

                repositoryManagerListener.ClearReceivedCalls();

                Logger.Trace("Issuing Command");

                repositoryManager.CommitFiles(new TaskResultDispatcher<string>(s => { }),
                    new List<string>() { "Assets\\TestDocument.txt", "foobar.txt" }, "IntegrationTest Commit",
                    string.Empty);

                Thread.Sleep(2000);

                Logger.Trace("Continue test");

                repositoryManagerListener.Received().OnRepositoryChanged(Args.GitStatus);
                result.AssertEqual(expectedAfterCommit);

                repositoryManagerListener.Received(1).OnActiveBranchChanged();
                repositoryManagerListener.DidNotReceive().OnActiveRemoteChanged();
                repositoryManagerListener.DidNotReceive().OnHeadChanged();
                repositoryManagerListener.DidNotReceive().OnLocalBranchListChanged();
                repositoryManagerListener.DidNotReceive().OnRemoteBranchListChanged();
                repositoryManagerListener.DidNotReceive().OnRemoteOrTrackingChanged();
            }
            finally
            {
                repositoryManager.Stop();
            }
        }

        [Test]
        public void ShouldDetectBranchChange()
        {
            InitializeEnvironment(TestRepoMasterCleanSynchronized);

            var repositoryManager = CreateRepositoryManager(TestRepoMasterCleanSynchronized);
            repositoryManager.Initialize();
            repositoryManager.Start();

            var repositoryManagerListener = Substitute.For<IRepositoryManagerListener>();
            repositoryManagerListener.AttachListener(repositoryManager);

            try
            {
                var expected = new GitStatus {
                    LocalBranch = "feature/document",
                    RemoteBranch = "origin/feature/document",
                    Entries = new List<GitStatusEntry>()
                };

                var result = new GitStatus();
                repositoryManager.OnRepositoryChanged += status => { result = status; };

                Logger.Trace("Issuing Command");

                repositoryManager.SwitchBranch(new TaskResultDispatcher<string>(s => { }), "feature/document");

                Thread.Sleep(3000);

                Logger.Trace("Continue test");

                repositoryManagerListener.Received().OnRepositoryChanged(Args.GitStatus);
                result.AssertEqual(expected);
                repositoryManagerListener.Received(2).OnIsBusyChanged(Args.Bool);
                repositoryManagerListener.Received(1).OnActiveBranchChanged();
                repositoryManagerListener.Received(1).OnActiveRemoteChanged();
                repositoryManagerListener.Received(1).OnHeadChanged();
                repositoryManagerListener.DidNotReceive().OnLocalBranchListChanged();
                repositoryManagerListener.DidNotReceive().OnRemoteBranchListChanged();
                repositoryManagerListener.DidNotReceive().OnRemoteOrTrackingChanged();
            }
            finally
            {
                repositoryManager.Stop();
            }
        }

        [Test]
        public void ShouldDetectBranchDelete()
        {
            InitializeEnvironment(TestRepoMasterCleanSynchronized);

            var repositoryManager = CreateRepositoryManager(TestRepoMasterCleanSynchronized);
            repositoryManager.Initialize();
            repositoryManager.Start();

            var repositoryManagerListener = Substitute.For<IRepositoryManagerListener>();
            repositoryManagerListener.AttachListener(repositoryManager);

            try
            {
                Logger.Trace("Issuing Command");

                repositoryManager.DeleteBranch(new TaskResultDispatcher<string>(s => { }), "feature/document", true);

                Thread.Sleep(2000);

                Logger.Trace("Continue test");

                repositoryManagerListener.DidNotReceive().OnRepositoryChanged(Args.GitStatus);
                repositoryManagerListener.Received(2).OnIsBusyChanged(Args.Bool);
                repositoryManagerListener.DidNotReceive().OnActiveBranchChanged();
                repositoryManagerListener.DidNotReceive().OnActiveRemoteChanged();
                repositoryManagerListener.DidNotReceive().OnHeadChanged();
                repositoryManagerListener.Received(1).OnLocalBranchListChanged();
                repositoryManagerListener.DidNotReceive().OnRemoteBranchListChanged();
                repositoryManagerListener.DidNotReceive().OnRemoteOrTrackingChanged();
            }
            finally
            {
                repositoryManager.Stop();
            }
        }

        [Test]
        public void ShouldDetectBranchCreate()
        {
            InitializeEnvironment(TestRepoMasterCleanSynchronized);

            var repositoryManager = CreateRepositoryManager(TestRepoMasterCleanSynchronized);
            repositoryManager.Initialize();
            repositoryManager.Start();

            var repositoryManagerListener = Substitute.For<IRepositoryManagerListener>();
            repositoryManagerListener.AttachListener(repositoryManager);

            try
            {
                Logger.Trace("Issuing Command");

                repositoryManager.CreateBranch(new TaskResultDispatcher<string>(s => { }), "feature/document2",
                    "feature/document");

                Thread.Sleep(1000);

                Logger.Trace("Continue test");

                repositoryManagerListener.DidNotReceive().OnRepositoryChanged(Args.GitStatus);
                repositoryManagerListener.Received(2).OnIsBusyChanged(Args.Bool);
                repositoryManagerListener.DidNotReceive().OnActiveBranchChanged();
                repositoryManagerListener.DidNotReceive().OnActiveRemoteChanged();
                repositoryManagerListener.DidNotReceive().OnHeadChanged();
                repositoryManagerListener.Received(1).OnLocalBranchListChanged();
                repositoryManagerListener.DidNotReceive().OnRemoteBranchListChanged();
                repositoryManagerListener.DidNotReceive().OnRemoteOrTrackingChanged();

                repositoryManagerListener.ClearReceivedCalls();

                Logger.Trace("Issuing Command");

                repositoryManager.CreateBranch(new TaskResultDispatcher<string>(s => { }), "feature2/document2",
                    "feature/document");

                Thread.Sleep(1000);

                Logger.Trace("Continue test");

                repositoryManagerListener.DidNotReceive().OnRepositoryChanged(Args.GitStatus);
                repositoryManagerListener.DidNotReceive().OnActiveBranchChanged();
                repositoryManagerListener.DidNotReceive().OnActiveRemoteChanged();
                repositoryManagerListener.DidNotReceive().OnHeadChanged();
                repositoryManagerListener.Received(1).OnLocalBranchListChanged();
                repositoryManagerListener.DidNotReceive().OnRemoteBranchListChanged();
                repositoryManagerListener.DidNotReceive().OnRemoteOrTrackingChanged();

                repositoryManagerListener.ClearReceivedCalls();
            }
            finally
            {
                repositoryManager.Stop();
            }
        }

        [Test]
        public void ShouldDetectChangesToRemotes()
        {
            InitializeEnvironment(TestRepoMasterCleanSynchronized);

            var repositoryManager = CreateRepositoryManager(TestRepoMasterCleanSynchronized);
            repositoryManager.Initialize();
            repositoryManager.Start();

            var repositoryManagerListener = Substitute.For<IRepositoryManagerListener>();
            repositoryManagerListener.AttachListener(repositoryManager);

            try
            {
                Logger.Trace("Issuing Command");

                repositoryManager.RemoteRemove(new TaskResultDispatcher<string>(s => { }), "origin");

                Thread.Sleep(2000);

                Logger.Trace("Continue test");

                repositoryManagerListener.DidNotReceive().OnRepositoryChanged(Args.GitStatus);
                repositoryManagerListener.Received(2).OnIsBusyChanged(Args.Bool);
                repositoryManagerListener.DidNotReceive().OnActiveBranchChanged();
                repositoryManagerListener.DidNotReceive().OnActiveRemoteChanged();
                repositoryManagerListener.DidNotReceive().OnHeadChanged();
                repositoryManagerListener.DidNotReceive().OnLocalBranchListChanged();
                repositoryManagerListener.Received().OnRemoteBranchListChanged();
                repositoryManagerListener.DidNotReceive().OnRemoteOrTrackingChanged();

                repositoryManagerListener.ClearReceivedCalls();

                Logger.Trace("Issuing Command");

                repositoryManager.RemoteAdd(new TaskResultDispatcher<string>(s => { }), "origin",
                    "https://github.com/EvilStanleyGoldman/IOTestsRepo.git");

                Thread.Sleep(2000);

                Logger.Trace("Continue test");

                repositoryManagerListener.DidNotReceive().OnRepositoryChanged(Args.GitStatus);
                repositoryManagerListener.Received(2).OnIsBusyChanged(Args.Bool);
                repositoryManagerListener.DidNotReceive().OnActiveBranchChanged();
                repositoryManagerListener.DidNotReceive().OnActiveRemoteChanged();
                repositoryManagerListener.DidNotReceive().OnHeadChanged();
                repositoryManagerListener.DidNotReceive().OnLocalBranchListChanged();
                repositoryManagerListener.DidNotReceive().OnRemoteBranchListChanged();
                repositoryManagerListener.DidNotReceive().OnRemoteOrTrackingChanged();
            }
            finally
            {
                repositoryManager.Stop();
            }
        }

        [Test]
        public void ShouldDetectGitPull()
        {
            InitializeEnvironment(TestRepoMasterCleanSynchronized);

            var repositoryManager = CreateRepositoryManager(TestRepoMasterCleanSynchronized);
            repositoryManager.Initialize();
            repositoryManager.Start();

            var repositoryManagerListener = Substitute.For<IRepositoryManagerListener>();
            repositoryManagerListener.AttachListener(repositoryManager);

            try
            {
                var expected = new GitStatus {
                    LocalBranch = "master",
                    RemoteBranch = "origin/master",
                    Entries = new List<GitStatusEntry>()
                };

                var result = new GitStatus();
                repositoryManager.OnRepositoryChanged += status => { result = status; };

                Logger.Trace("Issuing Command");

                repositoryManager.Pull(new TaskResultDispatcher<string>(s => { }), "origin", "master");

                Thread.Sleep(7000);

                Logger.Trace("Continue test");

                repositoryManagerListener.Received().OnRepositoryChanged(Args.GitStatus);
                result.AssertEqual(expected);

                repositoryManagerListener.ReceivedWithAnyArgs(2).OnIsBusyChanged(Args.Bool);
                repositoryManager.IsBusy.Should().BeFalse();

                repositoryManagerListener.Received(1).OnActiveBranchChanged();
                repositoryManagerListener.DidNotReceive().OnActiveRemoteChanged();
                repositoryManagerListener.DidNotReceive().OnHeadChanged();
                repositoryManagerListener.DidNotReceive().OnLocalBranchListChanged();
                repositoryManagerListener.DidNotReceive().OnRemoteBranchListChanged();
                repositoryManagerListener.DidNotReceive().OnRemoteOrTrackingChanged();
            }
            finally
            {
                repositoryManager.Stop();
            }
        }

        [Test]
        public void ShouldDetectGitFetch()
        {
            InitializeEnvironment(TestRepoMasterCleanUnsynchronized);

            var repositoryManager = CreateRepositoryManager(TestRepoMasterCleanSynchronized);
            repositoryManager.Initialize();
            repositoryManager.Start();

            var repositoryManagerListener = Substitute.For<IRepositoryManagerListener>();
            repositoryManagerListener.AttachListener(repositoryManager);

            try
            {
                Logger.Trace("Issuing Command");

                repositoryManager.Fetch(new TaskResultDispatcher<string>(s => { }), "origin");

                Thread.Sleep(1000);

                Logger.Trace("Continue test");

                repositoryManagerListener.DidNotReceive().OnRepositoryChanged(Args.GitStatus);
                repositoryManagerListener.Received(2).OnIsBusyChanged(Args.Bool);
                repositoryManagerListener.DidNotReceive().OnActiveBranchChanged();
                repositoryManagerListener.DidNotReceive().OnActiveRemoteChanged();
                repositoryManagerListener.DidNotReceive().OnHeadChanged();
                repositoryManagerListener.DidNotReceive().OnLocalBranchListChanged();
                repositoryManagerListener.DidNotReceive().OnRemoteBranchListChanged();
                repositoryManagerListener.DidNotReceive().OnRemoteOrTrackingChanged();
            }
            finally
            {
                repositoryManager.Stop();
            }
        }

        private RepositoryManager CreateRepositoryManager(NPath path)
        {
            var repositoryManagerFactory = new RepositoryManagerFactory();
            var taskRunner = new TaskRunnerBase(new MainThreadSynchronizationContextBase(), CancellationToken.None);
            taskRunner.Run();
            return repositoryManagerFactory.CreateRepositoryManager(Platform, taskRunner, path, CancellationToken.None);
        }
    }
}
