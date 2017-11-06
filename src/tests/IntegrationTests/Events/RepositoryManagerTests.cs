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
    [TestFixture, Ignore]
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
            repositoryManagerEvents.WaitForNotBusy(2);

            repositoryManagerListener.AssertDidNotReceiveAnyCalls();

            Repository.Name.Should().Be("IOTestsRepo");
            Repository.CloneUrl.ToString().Should().Be("https://github.com/EvilStanleyGoldman/IOTestsRepo.git");
            Repository.Owner.Should().Be("EvilStanleyGoldman");
            Repository.LocalPath.Should().Be(TestRepoMasterCleanSynchronized);
            Repository.IsGitHub.Should().BeTrue();
            Repository.CurrentBranchName.Should().Be("master");
            Repository.CurrentBranch.HasValue.Should().BeTrue();
            Repository.CurrentBranch.Value.Name.Should().Be("master");
            Repository.CurrentRemote.HasValue.Should().BeTrue();
            Repository.CurrentRemote.Value.Name.Should().Be("origin");
            Repository.CurrentRemote.Value.Url.Should().Be("https://github.com/EvilStanleyGoldman/IOTestsRepo.git");
            Repository.LocalBranches.Should().BeEquivalentTo(new[] {
                new GitBranch("master", "origin/master", true),
                new GitBranch("feature/document", "origin/feature/document", false),
                new GitBranch("feature/other-feature", "origin/feature/other-feature", false),
            });
            Repository.Remotes.Should().BeEquivalentTo(new GitRemote("origin", "https://github.com/EvilStanleyGoldman/IOTestsRepo.git"));
            Repository.RemoteBranches.Should().BeEquivalentTo(new[] {
                new GitBranch("origin/master", "[None]", false),
                new GitBranch("origin/feature/document-2", "[None]", false),
                new GitBranch("origin/feature/other-feature", "[None]", false),
            });
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
            //TODO: Figure this out
            //Environment.Repository.OnStatusChanged += status => { result = status; };

            var foobarTxt = TestRepoMasterCleanSynchronized.Combine("foobar.txt");
            foobarTxt.WriteAllText("foobar");

            await TaskManager.Wait();
            RepositoryManager.WaitForEvents();
            repositoryManagerEvents.WaitForNotBusy();
            repositoryManagerEvents.WaitForStatusUpdated();

            repositoryManagerListener.Received().OnIsBusyChanged(Args.Bool);
            repositoryManagerListener.Received().OnStatusUpdated(Args.GitStatus);
            repositoryManagerListener.DidNotReceive().OnLocksUpdated(Args.EnumerableGitLock);
            repositoryManagerListener.DidNotReceive().OnCurrentBranchAndRemoteUpdated(Arg.Any<ConfigBranch?>(), Arg.Any<ConfigRemote?>());
            repositoryManagerListener.DidNotReceive().OnLocalBranchListUpdated(Arg.Any<Dictionary<string, ConfigBranch>>());
            repositoryManagerListener.DidNotReceive().OnRemoteBranchListUpdated(Arg.Any<Dictionary<string, ConfigRemote>>(), Arg.Any<Dictionary<string, Dictionary<string, ConfigBranch>>>());
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
            //RepositoryManager.OnStatusUpdated += status => { result = status; };

            var foobarTxt = TestRepoMasterCleanSynchronized.Combine("foobar.txt");
            foobarTxt.WriteAllText("foobar");

            var testDocumentTxt = TestRepoMasterCleanSynchronized.Combine("Assets", "TestDocument.txt");
            testDocumentTxt.WriteAllText("foobar");

            await TaskManager.Wait();

            //Intentionally wait two cycles, in case the first cycle did not pick up all events
            RepositoryManager.WaitForEvents();
            repositoryManagerEvents.WaitForNotBusy();
            repositoryManagerEvents.WaitForStatusUpdated();

            RepositoryManager.WaitForEvents();
            repositoryManagerEvents.WaitForNotBusy();
            repositoryManagerEvents.WaitForStatusUpdated();

            repositoryManagerListener.Received().OnIsBusyChanged(Args.Bool);
            repositoryManagerListener.Received().OnStatusUpdated(Args.GitStatus);
            repositoryManagerListener.DidNotReceive().OnLocksUpdated(Args.EnumerableGitLock);
            repositoryManagerListener.DidNotReceive().OnCurrentBranchAndRemoteUpdated(Arg.Any<ConfigBranch?>(), Arg.Any<ConfigRemote?>());
            repositoryManagerListener.DidNotReceive().OnLocalBranchListUpdated(Arg.Any<Dictionary<string, ConfigBranch>>());
            repositoryManagerListener.DidNotReceive().OnRemoteBranchListUpdated(Arg.Any<Dictionary<string, ConfigRemote>>(), Arg.Any<Dictionary<string, Dictionary<string, ConfigBranch>>>());
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
            repositoryManagerEvents.WaitForNotBusy();

            repositoryManagerListener.Received().OnIsBusyChanged(Args.Bool);
            repositoryManagerListener.DidNotReceive().OnStatusUpdated(Args.GitStatus);
            repositoryManagerListener.DidNotReceive().OnLocksUpdated(Args.EnumerableGitLock);
            repositoryManagerListener.DidNotReceive().OnCurrentBranchAndRemoteUpdated(Arg.Any<ConfigBranch?>(), Arg.Any<ConfigRemote?>());
            repositoryManagerListener.DidNotReceive().OnLocalBranchListUpdated(Arg.Any<Dictionary<string, ConfigBranch>>());
            repositoryManagerListener.DidNotReceive().OnRemoteBranchListUpdated(Arg.Any<Dictionary<string, ConfigRemote>>(), Arg.Any<Dictionary<string, Dictionary<string, ConfigBranch>>>());
            repositoryManagerListener.Received().OnLocalBranchUpdated(expectedLocalBranch);
            repositoryManagerListener.DidNotReceive().OnLocalBranchAdded(Args.String);
            repositoryManagerListener.DidNotReceive().OnLocalBranchRemoved(Args.String);
            repositoryManagerListener.DidNotReceive().OnRemoteBranchAdded(Args.String, Args.String);
            repositoryManagerListener.DidNotReceive().OnRemoteBranchRemoved(Args.String, Args.String);
        }

        [Test, Ignore("Fails often")]
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
            //TODO: Figure this out
            //RepositoryManager.OnStatusUpdated += status => { result = status; };

            var foobarTxt = TestRepoMasterCleanSynchronized.Combine("foobar.txt");
            foobarTxt.WriteAllText("foobar");

            var testDocumentTxt = TestRepoMasterCleanSynchronized.Combine("Assets", "TestDocument.txt");
            testDocumentTxt.WriteAllText("foobar");

            await TaskManager.Wait();
            RepositoryManager.WaitForEvents();
            repositoryManagerEvents.WaitForNotBusy();
            repositoryManagerEvents.WaitForStatusUpdated();

            repositoryManagerListener.Received().OnIsBusyChanged(Args.Bool);
            repositoryManagerListener.Received().OnStatusUpdated(Args.GitStatus);
            repositoryManagerListener.DidNotReceive().OnLocksUpdated(Args.EnumerableGitLock);
            repositoryManagerListener.DidNotReceive().OnCurrentBranchAndRemoteUpdated(Arg.Any<ConfigBranch?>(), Arg.Any<ConfigRemote?>());
            repositoryManagerListener.DidNotReceive().OnLocalBranchListUpdated(Arg.Any<Dictionary<string, ConfigBranch>>());
            repositoryManagerListener.DidNotReceive().OnRemoteBranchListUpdated(Arg.Any<Dictionary<string, ConfigRemote>>(), Arg.Any<Dictionary<string, Dictionary<string, ConfigBranch>>>());
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
            repositoryManagerEvents.WaitForNotBusy();

            repositoryManagerListener.Received().OnIsBusyChanged(Args.Bool);
            repositoryManagerListener.DidNotReceive().OnStatusUpdated(Args.GitStatus);
            repositoryManagerListener.DidNotReceive().OnLocksUpdated(Args.EnumerableGitLock);
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
            //TODO: Figure this out
            //RepositoryManager.OnStatusUpdated += status => { result = status; };

            Logger.Trace("Starting test");

            await RepositoryManager.SwitchBranch(expectedLocalBranch).StartAsAsync();

            await TaskManager.Wait();
            RepositoryManager.WaitForEvents();
            repositoryManagerEvents.WaitForNotBusy();
            repositoryManagerEvents.WaitForStatusUpdated();

            repositoryManagerListener.Received().OnIsBusyChanged(Args.Bool);
            repositoryManagerListener.Received().OnStatusUpdated(Args.GitStatus);
            repositoryManagerListener.DidNotReceive().OnLocksUpdated(Args.EnumerableGitLock);
            repositoryManagerListener.Received().OnCurrentBranchAndRemoteUpdated(Arg.Any<ConfigBranch?>(), Arg.Any<ConfigRemote?>());
            repositoryManagerListener.DidNotReceive().OnLocalBranchListUpdated(Arg.Any<Dictionary<string, ConfigBranch>>());
            repositoryManagerListener.DidNotReceive().OnRemoteBranchListUpdated(Arg.Any<Dictionary<string, ConfigRemote>>(), Arg.Any<Dictionary<string, Dictionary<string, ConfigBranch>>>());
            repositoryManagerListener.DidNotReceive().OnLocalBranchUpdated(Args.String);
            repositoryManagerListener.DidNotReceive().OnLocalBranchAdded(Args.String);
            repositoryManagerListener.DidNotReceive().OnLocalBranchRemoved(Args.String);
            repositoryManagerListener.DidNotReceive().OnRemoteBranchAdded(Args.String, Args.String);
            repositoryManagerListener.DidNotReceive().OnRemoteBranchRemoved(Args.String, Args.String);

            result.AssertEqual(expected);

            Repository.Name.Should().Be("IOTestsRepo");
            Repository.CloneUrl.ToString().Should().Be("https://github.com/EvilStanleyGoldman/IOTestsRepo.git");
            Repository.Owner.Should().Be("EvilStanleyGoldman");
            Repository.LocalPath.Should().Be(TestRepoMasterCleanSynchronized);
            Repository.IsGitHub.Should().BeTrue();
            Repository.CurrentBranchName.Should().Be("feature/document");
            Repository.CurrentBranch.HasValue.Should().BeTrue();
            Repository.CurrentBranch.Value.Name.Should().Be("feature/document");
            Repository.CurrentRemote.HasValue.Should().BeTrue();
            Repository.CurrentRemote.Value.Name.Should().Be("origin");
            Repository.CurrentRemote.Value.Url.Should().Be("https://github.com/EvilStanleyGoldman/IOTestsRepo.git");
            Repository.LocalBranches.Should().BeEquivalentTo(new[] {
                new GitBranch("master", "origin/master", false),
                new GitBranch("feature/document", "origin/feature/document", true),
                new GitBranch("feature/other-feature", "origin/feature/other-feature", false),
            });
            Repository.Remotes.Should().BeEquivalentTo(new GitRemote("origin", "https://github.com/EvilStanleyGoldman/IOTestsRepo.git"));
            Repository.RemoteBranches.Should().BeEquivalentTo(new[] {
                new GitBranch("origin/master", "[None]", false),
                new GitBranch("origin/feature/document-2", "[None]", false),
                new GitBranch("origin/feature/other-feature", "[None]", false),
            });
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
            repositoryManagerEvents.WaitForNotBusy();

            repositoryManagerListener.Received().OnIsBusyChanged(Args.Bool);
            repositoryManagerListener.DidNotReceive().OnStatusUpdated(Args.GitStatus);
            repositoryManagerListener.DidNotReceive().OnLocksUpdated(Args.EnumerableGitLock);
            repositoryManagerListener.Received().OnCurrentBranchAndRemoteUpdated(Arg.Any<ConfigBranch?>(), Arg.Any<ConfigRemote?>());
            repositoryManagerListener.Received().OnLocalBranchListUpdated(Arg.Any<Dictionary<string, ConfigBranch>>());
            repositoryManagerListener.Received().OnRemoteBranchListUpdated(Arg.Any<Dictionary<string, ConfigRemote>>(), Arg.Any<Dictionary<string, Dictionary<string, ConfigBranch>>>());
            repositoryManagerListener.DidNotReceive().OnLocalBranchUpdated(Args.String);
            repositoryManagerListener.DidNotReceive().OnLocalBranchAdded(Args.String);
            repositoryManagerListener.Received().OnLocalBranchRemoved(deletedBranch);
            repositoryManagerListener.DidNotReceive().OnRemoteBranchAdded(Args.String, Args.String);
            repositoryManagerListener.DidNotReceive().OnRemoteBranchRemoved(Args.String, Args.String);

            Repository.Name.Should().Be("IOTestsRepo");
            Repository.CloneUrl.ToString().Should().Be("https://github.com/EvilStanleyGoldman/IOTestsRepo.git");
            Repository.Owner.Should().Be("EvilStanleyGoldman");
            Repository.LocalPath.Should().Be(TestRepoMasterCleanSynchronized);
            Repository.IsGitHub.Should().BeTrue();
            Repository.CurrentBranchName.Should().Be("master");
            Repository.CurrentBranch.HasValue.Should().BeTrue();
            Repository.CurrentBranch.Value.Name.Should().Be("master");
            Repository.CurrentRemote.HasValue.Should().BeTrue();
            Repository.CurrentRemote.Value.Name.Should().Be("origin");
            Repository.CurrentRemote.Value.Url.Should().Be("https://github.com/EvilStanleyGoldman/IOTestsRepo.git");
            Repository.LocalBranches.Should().BeEquivalentTo(new[] {
                new GitBranch("master", "origin/master", true),
                new GitBranch("feature/other-feature", "origin/feature/other-feature", false),
            });
            Repository.Remotes.Should().BeEquivalentTo(new GitRemote("origin","https://github.com/EvilStanleyGoldman/IOTestsRepo.git"));
            Repository.RemoteBranches.Should().BeEquivalentTo(new[] {
                new GitBranch("origin/master", "[None]", false),
                new GitBranch("origin/feature/document-2", "[None]", false),
                new GitBranch("origin/feature/other-feature", "[None]", false),
            });
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
            repositoryManagerEvents.WaitForNotBusy();

            repositoryManagerListener.Received().OnIsBusyChanged(Args.Bool);
            repositoryManagerListener.DidNotReceive().OnStatusUpdated(Args.GitStatus);
            repositoryManagerListener.DidNotReceive().OnLocksUpdated(Args.EnumerableGitLock);
            repositoryManagerListener.DidNotReceive().OnCurrentBranchAndRemoteUpdated(Arg.Any<ConfigBranch?>(), Arg.Any<ConfigRemote?>());
            repositoryManagerListener.DidNotReceive().OnLocalBranchListUpdated(Arg.Any<Dictionary<string, ConfigBranch>>());
            repositoryManagerListener.DidNotReceive().OnRemoteBranchListUpdated(Arg.Any<Dictionary<string, ConfigRemote>>(), Arg.Any<Dictionary<string, Dictionary<string, ConfigBranch>>>());
            repositoryManagerListener.DidNotReceive().OnLocalBranchUpdated(Args.String);
            repositoryManagerListener.Received().OnLocalBranchAdded(createdBranch1);
            repositoryManagerListener.DidNotReceive().OnLocalBranchRemoved(Args.String);
            repositoryManagerListener.DidNotReceive().OnRemoteBranchAdded(Args.String, Args.String);
            repositoryManagerListener.DidNotReceive().OnRemoteBranchRemoved(Args.String, Args.String);

            Repository.Name.Should().Be("IOTestsRepo");
            Repository.CloneUrl.ToString().Should().Be("https://github.com/EvilStanleyGoldman/IOTestsRepo.git");
            Repository.Owner.Should().Be("EvilStanleyGoldman");
            Repository.LocalPath.Should().Be(TestRepoMasterCleanSynchronized);
            Repository.IsGitHub.Should().BeTrue();
            Repository.CurrentBranchName.Should().Be("master");
            Repository.CurrentBranch.HasValue.Should().BeTrue();
            Repository.CurrentBranch.Value.Name.Should().Be("master");
            Repository.CurrentRemote.HasValue.Should().BeTrue();
            Repository.CurrentRemote.Value.Name.Should().Be("origin");
            Repository.CurrentRemote.Value.Url.Should().Be("https://github.com/EvilStanleyGoldman/IOTestsRepo.git");
            Repository.LocalBranches.Should().BeEquivalentTo(new[] {
                new GitBranch("master", "origin/master", true),
                new GitBranch("feature/document", "origin/feature/document", false),
                new GitBranch("feature/document2", "[None]", false),
                new GitBranch("feature/other-feature", "origin/feature/other-feature", false),
            });
            Repository.Remotes.Should().BeEquivalentTo(new GitRemote("origin","https://github.com/EvilStanleyGoldman/IOTestsRepo.git"));
            Repository.RemoteBranches.Should().BeEquivalentTo(new[] {
                new GitBranch("origin/master", "[None]", false),
                new GitBranch("origin/feature/document-2", "[None]", false),
                new GitBranch("origin/feature/other-feature", "[None]", false),
            });

            repositoryManagerListener.ClearReceivedCalls();
            repositoryManagerEvents.Reset();

            var createdBranch2 = "feature2/document2";
            await RepositoryManager.CreateBranch(createdBranch2, "feature/document").StartAsAsync();
            await TaskManager.Wait();
            RepositoryManager.WaitForEvents();
            repositoryManagerEvents.WaitForNotBusy();

            repositoryManagerListener.Received().OnIsBusyChanged(Args.Bool);
            repositoryManagerListener.DidNotReceive().OnStatusUpdated(Args.GitStatus);
            repositoryManagerListener.DidNotReceive().OnLocksUpdated(Args.EnumerableGitLock);
            repositoryManagerListener.DidNotReceive().OnCurrentBranchAndRemoteUpdated(Arg.Any<ConfigBranch?>(), Arg.Any<ConfigRemote?>());
            repositoryManagerListener.DidNotReceive().OnLocalBranchListUpdated(Arg.Any<Dictionary<string, ConfigBranch>>());
            repositoryManagerListener.DidNotReceive().OnRemoteBranchListUpdated(Arg.Any<Dictionary<string, ConfigRemote>>(), Arg.Any<Dictionary<string, Dictionary<string, ConfigBranch>>>());
            repositoryManagerListener.DidNotReceive().OnLocalBranchUpdated(Args.String);
            repositoryManagerListener.Received().OnLocalBranchAdded(createdBranch2);
            repositoryManagerListener.DidNotReceive().OnLocalBranchRemoved(Args.String);
            repositoryManagerListener.DidNotReceive().OnRemoteBranchAdded(Args.String, Args.String);
            repositoryManagerListener.DidNotReceive().OnRemoteBranchRemoved(Args.String, Args.String);

            Repository.Name.Should().Be("IOTestsRepo");
            Repository.CloneUrl.ToString().Should().Be("https://github.com/EvilStanleyGoldman/IOTestsRepo.git");
            Repository.Owner.Should().Be("EvilStanleyGoldman");
            Repository.LocalPath.Should().Be(TestRepoMasterCleanSynchronized);
            Repository.IsGitHub.Should().BeTrue();
            Repository.CurrentBranchName.Should().Be("master");
            Repository.CurrentBranch.HasValue.Should().BeTrue();
            Repository.CurrentBranch.Value.Name.Should().Be("master");
            Repository.CurrentRemote.HasValue.Should().BeTrue();
            Repository.CurrentRemote.Value.Name.Should().Be("origin");
            Repository.CurrentRemote.Value.Url.Should().Be("https://github.com/EvilStanleyGoldman/IOTestsRepo.git");
            Repository.LocalBranches.Should().BeEquivalentTo(new[] {
                new GitBranch("master", "origin/master", true),
                new GitBranch("feature/document", "origin/feature/document", false),
                new GitBranch("feature/document2", "[None]", false),
                new GitBranch("feature2/document2", "[None]", false),
                new GitBranch("feature/other-feature", "origin/feature/other-feature", false),
            });
            Repository.Remotes.Should().BeEquivalentTo(new GitRemote("origin","https://github.com/EvilStanleyGoldman/IOTestsRepo.git"));
            Repository.RemoteBranches.Should().BeEquivalentTo(new[] {
                new GitBranch("origin/master", "[None]", false),
                new GitBranch("origin/feature/document-2", "[None]", false),
                new GitBranch("origin/feature/other-feature", "[None]", false),
            });
        }

        [Test]
        public async Task ShouldDetectChangesToRemotes()
        {
            await Initialize(TestRepoMasterCleanSynchronized);

            var repositoryManagerListener = Substitute.For<IRepositoryManagerListener>();
            repositoryManagerListener.AttachListener(RepositoryManager, repositoryManagerEvents);

            RepositoryManager.WaitForEvents();
            repositoryManagerEvents.WaitForNotBusy(2);

            repositoryManagerListener.AssertDidNotReceiveAnyCalls();

            Repository.Name.Should().Be("IOTestsRepo");
            Repository.CloneUrl.ToString().Should().Be("https://github.com/EvilStanleyGoldman/IOTestsRepo.git");
            Repository.Owner.Should().Be("EvilStanleyGoldman");
            Repository.LocalPath.Should().Be(TestRepoMasterCleanSynchronized);
            Repository.IsGitHub.Should().BeTrue();
            Repository.CurrentBranchName.Should().Be("master");
            Repository.CurrentBranch.HasValue.Should().BeTrue();
            Repository.CurrentBranch.Value.Name.Should().Be("master");
            Repository.CurrentRemote.HasValue.Should().BeTrue();
            Repository.CurrentRemote.Value.Name.Should().Be("origin");
            Repository.CurrentRemote.Value.Url.Should().Be("https://github.com/EvilStanleyGoldman/IOTestsRepo.git");
            Repository.LocalBranches.Should().BeEquivalentTo(new[] {
                new GitBranch("master", "origin/master", true),
                new GitBranch("feature/document", "origin/feature/document", false),
                new GitBranch("feature/other-feature", "origin/feature/other-feature", false),
            });
            Repository.Remotes.Should().BeEquivalentTo(new GitRemote("origin","https://github.com/EvilStanleyGoldman/IOTestsRepo.git"));
            Repository.RemoteBranches.Should().BeEquivalentTo(new[] {
                new GitBranch("origin/master", "[None]", false),
                new GitBranch("origin/feature/document-2", "[None]", false),
                new GitBranch("origin/feature/other-feature", "[None]", false),
            });

            await RepositoryManager.RemoteRemove("origin").StartAsAsync();
            await TaskManager.Wait();

            RepositoryManager.WaitForEvents();
            repositoryManagerEvents.WaitForNotBusy();

            repositoryManagerEvents.OnRemoteBranchListUpdated.WaitOne(TimeSpan.FromSeconds(1));
            repositoryManagerEvents.OnLocalBranchListUpdated.WaitOne(TimeSpan.FromSeconds(1));

            repositoryManagerListener.Received().OnIsBusyChanged(Args.Bool);
            repositoryManagerListener.DidNotReceive().OnStatusUpdated(Args.GitStatus);
            repositoryManagerListener.DidNotReceive().OnLocksUpdated(Args.EnumerableGitLock);
            repositoryManagerListener.Received().OnCurrentBranchAndRemoteUpdated(Arg.Any<ConfigBranch?>(), Arg.Any<ConfigRemote?>());
            repositoryManagerListener.Received().OnLocalBranchListUpdated(Arg.Any<Dictionary<string, ConfigBranch>>());
            repositoryManagerListener.Received().OnRemoteBranchListUpdated(Arg.Any<Dictionary<string, ConfigRemote>>(), Arg.Any<Dictionary<string, Dictionary<string, ConfigBranch>>>());
            repositoryManagerListener.DidNotReceive().OnLocalBranchUpdated(Args.String);
            repositoryManagerListener.DidNotReceive().OnLocalBranchAdded(Args.String);
            repositoryManagerListener.DidNotReceive().OnLocalBranchRemoved(Args.String);
            repositoryManagerListener.DidNotReceive().OnRemoteBranchAdded(Args.String, Args.String);
            repositoryManagerListener.Received().OnRemoteBranchRemoved(Args.String, Args.String);

            Repository.Name.Should().Be("IOTestsRepo_master_clean_sync");
            Repository.CloneUrl.Should().BeNull();
            Repository.Owner.Should().BeNull();
            Repository.LocalPath.Should().Be(TestRepoMasterCleanSynchronized);
            Repository.IsGitHub.Should().BeFalse();
            Repository.CurrentBranchName.Should().Be("master");
            Repository.CurrentBranch.HasValue.Should().BeTrue();
            Repository.CurrentBranch.Value.Name.Should().Be("master");
            Repository.CurrentRemote.HasValue.Should().BeFalse();
            Repository.Remotes.Should().BeEquivalentTo();
            Repository.RemoteBranches.Should().BeEmpty();

            repositoryManagerListener.ClearReceivedCalls();
            repositoryManagerEvents.Reset();

            await RepositoryManager.RemoteAdd("origin", "https://github.com/EvilShana/IOTestsRepo.git").StartAsAsync();
            await TaskManager.Wait();
            RepositoryManager.WaitForEvents();
            repositoryManagerEvents.WaitForNotBusy();
            repositoryManagerEvents.OnRemoteBranchListUpdated.WaitOne(TimeSpan.FromSeconds(1));
            repositoryManagerEvents.OnLocalBranchListUpdated.WaitOne(TimeSpan.FromSeconds(1));

            repositoryManagerListener.Received().OnIsBusyChanged(Args.Bool);
            repositoryManagerListener.DidNotReceive().OnStatusUpdated(Args.GitStatus);
            repositoryManagerListener.DidNotReceive().OnLocksUpdated(Args.EnumerableGitLock);
            repositoryManagerListener.Received().OnCurrentBranchAndRemoteUpdated(Arg.Any<ConfigBranch?>(), Arg.Any<ConfigRemote?>());
            repositoryManagerListener.Received().OnLocalBranchListUpdated(Arg.Any<Dictionary<string, ConfigBranch>>());
            repositoryManagerListener.Received().OnRemoteBranchListUpdated(Arg.Any<Dictionary<string, ConfigRemote>>(), Arg.Any<Dictionary<string, Dictionary<string, ConfigBranch>>>());
            repositoryManagerListener.DidNotReceive().OnLocalBranchUpdated(Args.String);
            repositoryManagerListener.DidNotReceive().OnLocalBranchAdded(Args.String);
            repositoryManagerListener.DidNotReceive().OnLocalBranchRemoved(Args.String);
            repositoryManagerListener.DidNotReceive().OnRemoteBranchAdded(Args.String, Args.String);
            repositoryManagerListener.DidNotReceive().OnRemoteBranchRemoved(Args.String, Args.String);

            Repository.Name.Should().Be("IOTestsRepo");
            Repository.CloneUrl.ToString().Should().Be("https://github.com/EvilShana/IOTestsRepo.git");
            Repository.Owner.Should().Be("EvilShana");
            Repository.LocalPath.Should().Be(TestRepoMasterCleanSynchronized);
            Repository.IsGitHub.Should().BeTrue();
            Repository.CurrentBranchName.Should().Be("master");
            Repository.CurrentBranch.HasValue.Should().BeTrue();
            Repository.CurrentBranch.Value.Name.Should().Be("master");
            Repository.CurrentRemote.HasValue.Should().BeTrue();
            Repository.CurrentRemote.Value.Name.Should().Be("origin");
            Repository.CurrentRemote.Value.Url.Should().Be("https://github.com/EvilShana/IOTestsRepo.git");
            Repository.LocalBranches.Should().BeEquivalentTo(new[] {
                new GitBranch("master", "[None]", true),
                new GitBranch("feature/document", "[None]", false),
                new GitBranch("feature/other-feature", "[None]", false),
            });
            Repository.Remotes.Should().BeEquivalentTo(new GitRemote("origin","https://github.com/EvilShana/IOTestsRepo.git"));
            Repository.RemoteBranches.Should().BeEmpty();
        }

        [Test]
        public async Task ShouldDetectChangesToRemotesWhenSwitchingBranches()
        {
            await Initialize(TestRepoMasterTwoRemotes);

            var repositoryManagerListener = Substitute.For<IRepositoryManagerListener>();
            repositoryManagerListener.AttachListener(RepositoryManager, repositoryManagerEvents);

            RepositoryManager.WaitForEvents();
            repositoryManagerEvents.WaitForNotBusy(2);

            repositoryManagerListener.AssertDidNotReceiveAnyCalls();

            Repository.Name.Should().Be("IOTestsRepo");
            Repository.CloneUrl.ToString().Should().Be("https://github.com/EvilStanleyGoldman/IOTestsRepo.git");
            Repository.Owner.Should().Be("EvilStanleyGoldman");
            Repository.LocalPath.Should().Be(TestRepoMasterTwoRemotes);
            Repository.IsGitHub.Should().BeTrue();
            Repository.CurrentBranchName.Should().Be("master");
            Repository.CurrentBranch.HasValue.Should().BeTrue();
            Repository.CurrentBranch.Value.Name.Should().Be("master");
            Repository.CurrentRemote.HasValue.Should().BeTrue();
            Repository.CurrentRemote.Value.Name.Should().Be("origin");
            Repository.CurrentRemote.Value.Url.Should().Be("https://github.com/EvilStanleyGoldman/IOTestsRepo.git");
            Repository.LocalBranches.Should().BeEquivalentTo(new[] {
                new GitBranch("master", "origin/master", true),
                new GitBranch("feature/document", "origin/feature/document", false),
                new GitBranch("feature/other-feature", "origin/feature/other-feature", false),
            });
            Repository.Remotes.Should().BeEquivalentTo(
                new GitRemote("origin","https://github.com/EvilStanleyGoldman/IOTestsRepo.git"), 
                new GitRemote("another","https://another.remote/Owner/Url.git"));
            Repository.RemoteBranches.Should().BeEquivalentTo(new[] {
                new GitBranch("origin/master", "[None]", false),
                new GitBranch("origin/feature/document-2", "[None]", false),
                new GitBranch("origin/feature/other-feature", "[None]", false),
                new GitBranch("another/master", "[None]", false),
                new GitBranch("another/feature/document-2", "[None]", false),
                new GitBranch("another/feature/other-feature", "[None]", false),
            });

            await RepositoryManager.CreateBranch("branch2", "another/master")
                .StartAsAsync();

            await TaskManager.Wait();
            RepositoryManager.WaitForEvents();
            repositoryManagerEvents.WaitForNotBusy();

            repositoryManagerListener.Received().OnIsBusyChanged(Args.Bool);
            repositoryManagerListener.DidNotReceive().OnStatusUpdated(Args.GitStatus);
            repositoryManagerListener.DidNotReceive().OnLocksUpdated(Args.EnumerableGitLock);
            repositoryManagerListener.Received().OnCurrentBranchAndRemoteUpdated(Arg.Any<ConfigBranch?>(), Arg.Any<ConfigRemote?>());
            repositoryManagerListener.Received().OnLocalBranchListUpdated(Arg.Any<Dictionary<string, ConfigBranch>>());
            repositoryManagerListener.Received().OnRemoteBranchListUpdated(Arg.Any<Dictionary<string, ConfigRemote>>(), Arg.Any<Dictionary<string, Dictionary<string, ConfigBranch>>>());
            repositoryManagerListener.DidNotReceive().OnLocalBranchUpdated(Args.String);
            repositoryManagerListener.Received().OnLocalBranchAdded(Args.String);
            repositoryManagerListener.DidNotReceive().OnLocalBranchRemoved(Args.String);
            repositoryManagerListener.DidNotReceive().OnRemoteBranchAdded(Args.String, Args.String);
            repositoryManagerListener.DidNotReceive().OnRemoteBranchRemoved(Args.String, Args.String);

            Repository.Name.Should().Be("IOTestsRepo");
            Repository.CloneUrl.ToString().Should().Be("https://github.com/EvilStanleyGoldman/IOTestsRepo.git");
            Repository.Owner.Should().Be("EvilStanleyGoldman");
            Repository.LocalPath.Should().Be(TestRepoMasterTwoRemotes);
            Repository.IsGitHub.Should().BeTrue();
            Repository.CurrentBranchName.Should().Be("master");
            Repository.CurrentBranch.HasValue.Should().BeTrue();
            Repository.CurrentBranch.Value.Name.Should().Be("master");
            Repository.CurrentRemote.HasValue.Should().BeTrue();
            Repository.CurrentRemote.Value.Name.Should().Be("origin");
            Repository.CurrentRemote.Value.Url.Should().Be("https://github.com/EvilStanleyGoldman/IOTestsRepo.git");
            Repository.LocalBranches.Should().BeEquivalentTo(new[] {
                new GitBranch("master", "origin/master", true),
                new GitBranch("branch2", "another/branch2", false),
                new GitBranch("feature/document", "origin/feature/document", false),
                new GitBranch("feature/other-feature", "origin/feature/other-feature", false),
            });
            Repository.Remotes.Should().BeEquivalentTo(
                new GitRemote("origin","https://github.com/EvilStanleyGoldman/IOTestsRepo.git"),
                new GitRemote("another","https://another.remote/Owner/Url.git"));
            Repository.RemoteBranches.Should().BeEquivalentTo(new[] {
                new GitBranch("origin/master", "[None]", false),
                new GitBranch("origin/feature/document-2", "[None]", false),
                new GitBranch("origin/feature/other-feature", "[None]", false),
                new GitBranch("another/master", "[None]", false),
                new GitBranch("another/feature/document-2", "[None]", false),
                new GitBranch("another/feature/other-feature", "[None]", false),
            });

            repositoryManagerListener.ClearReceivedCalls();
            repositoryManagerEvents.Reset();

            await RepositoryManager.SwitchBranch("branch2")
                                   .StartAsAsync();

            await TaskManager.Wait();
            RepositoryManager.WaitForEvents();
            repositoryManagerEvents.WaitForNotBusy();
            repositoryManagerEvents.WaitForHeadUpdated();

            repositoryManagerListener.Received().OnIsBusyChanged(Args.Bool);
            repositoryManagerListener.Received().OnStatusUpdated(Args.GitStatus);
            repositoryManagerListener.DidNotReceive().OnLocksUpdated(Args.EnumerableGitLock);
            repositoryManagerListener.Received().OnCurrentBranchAndRemoteUpdated(Arg.Any<ConfigBranch?>(), Arg.Any<ConfigRemote?>());
            repositoryManagerListener.DidNotReceive().OnLocalBranchListUpdated(Arg.Any<Dictionary<string, ConfigBranch>>());
            repositoryManagerListener.DidNotReceive().OnRemoteBranchListUpdated(Arg.Any<Dictionary<string, ConfigRemote>>(), Arg.Any<Dictionary<string, Dictionary<string, ConfigBranch>>>());
            repositoryManagerListener.DidNotReceive().OnLocalBranchUpdated(Args.String);
            repositoryManagerListener.DidNotReceive().OnLocalBranchAdded(Args.String);
            repositoryManagerListener.DidNotReceive().OnLocalBranchRemoved(Args.String);
            repositoryManagerListener.DidNotReceive().OnRemoteBranchAdded(Args.String, Args.String);
            repositoryManagerListener.DidNotReceive().OnRemoteBranchRemoved(Args.String, Args.String);

            Repository.Name.Should().Be("Url");
            Repository.CloneUrl.ToString().Should().Be("https://another.remote/Owner/Url.git");
            Repository.Owner.Should().Be("Owner");
            Repository.LocalPath.Should().Be(TestRepoMasterTwoRemotes);
            Repository.IsGitHub.Should().BeFalse();
            Repository.CurrentBranchName.Should().Be("branch2");
            Repository.CurrentBranch.HasValue.Should().BeTrue();
            Repository.CurrentBranch.Value.Name.Should().Be("branch2");
            Repository.CurrentRemote.HasValue.Should().BeTrue();
            Repository.CurrentRemote.Value.Name.Should().Be("another");
            Repository.CurrentRemote.Value.Url.Should().Be("https://another.remote/Owner/Url.git");
            Repository.LocalBranches.Should().BeEquivalentTo(new[] {
                new GitBranch("master", "origin/master", false),
                new GitBranch("branch2", "another/branch2", true),
                new GitBranch("feature/document", "origin/feature/document", false),
                new GitBranch("feature/other-feature", "origin/feature/other-feature", false),
            });
            Repository.Remotes.Should().BeEquivalentTo(
                new GitRemote("origin","https://github.com/EvilStanleyGoldman/IOTestsRepo.git"), 
                new GitRemote("another","https://another.remote/Owner/Url.git"));
            Repository.RemoteBranches.Should().BeEquivalentTo(new[] {
                new GitBranch("origin/master", "[None]", false),
                new GitBranch("origin/feature/document-2", "[None]", false),
                new GitBranch("origin/feature/other-feature", "[None]", false),
                new GitBranch("another/master", "[None]", false),
                new GitBranch("another/feature/document-2", "[None]", false),
                new GitBranch("another/feature/other-feature", "[None]", false),
            });
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
            //TODO: Figure this out
            //RepositoryManager.OnStatusUpdated += status => { result = status; };

            await RepositoryManager.Pull("origin", "master").StartAsAsync();
            await TaskManager.Wait();
            RepositoryManager.WaitForEvents();
            repositoryManagerEvents.WaitForNotBusy();
            repositoryManagerEvents.WaitForStatusUpdated();

            repositoryManagerListener.Received().OnIsBusyChanged(Args.Bool);
            repositoryManagerListener.Received().OnStatusUpdated(Args.GitStatus);
            repositoryManagerListener.DidNotReceive().OnLocksUpdated(Args.EnumerableGitLock);
            repositoryManagerListener.DidNotReceive().OnCurrentBranchAndRemoteUpdated(Arg.Any<ConfigBranch?>(), Arg.Any<ConfigRemote?>());
            repositoryManagerListener.DidNotReceive().OnLocalBranchListUpdated(Arg.Any<Dictionary<string, ConfigBranch>>());
            repositoryManagerListener.DidNotReceive().OnRemoteBranchListUpdated(Arg.Any<Dictionary<string, ConfigRemote>>(), Arg.Any<Dictionary<string, Dictionary<string, ConfigBranch>>>());
            repositoryManagerListener.Received().OnLocalBranchUpdated(Args.String);
            repositoryManagerListener.DidNotReceive().OnLocalBranchAdded(Args.String);
            repositoryManagerListener.DidNotReceive().OnLocalBranchRemoved(Args.String);
            repositoryManagerListener.DidNotReceive().OnRemoteBranchAdded(Args.String, Args.String);
            repositoryManagerListener.DidNotReceive().OnRemoteBranchRemoved(Args.String, Args.String);

            result.AssertEqual(expected);

            Repository.Name.Should().Be("IOTestsRepo");
            Repository.CloneUrl.ToString().Should().Be("https://github.com/EvilStanleyGoldman/IOTestsRepo.git");
            Repository.Owner.Should().Be("EvilStanleyGoldman");
            Repository.LocalPath.Should().Be(TestRepoMasterCleanSynchronized);
            Repository.IsGitHub.Should().BeTrue();
            Repository.CurrentBranchName.Should().Be("master");
            Repository.CurrentBranch.HasValue.Should().BeTrue();
            Repository.CurrentBranch.Value.Name.Should().Be("master");
            Repository.CurrentRemote.HasValue.Should().BeTrue();
            Repository.CurrentRemote.Value.Name.Should().Be("origin");
            Repository.CurrentRemote.Value.Url.Should().Be("https://github.com/EvilStanleyGoldman/IOTestsRepo.git");
            Repository.LocalBranches.Should().BeEquivalentTo(new[] {
                new GitBranch("master", "origin/master", true),
                new GitBranch("feature/document", "origin/feature/document", false),
                new GitBranch("feature/other-feature", "origin/feature/other-feature", false),
            });
            Repository.Remotes.Should().BeEquivalentTo(new GitRemote("origin","https://github.com/EvilStanleyGoldman/IOTestsRepo.git"));
            Repository.RemoteBranches.Should().BeEquivalentTo(new[] {
                new GitBranch("origin/master", "[None]", false),
                new GitBranch("origin/feature/document-2", "[None]", false),
                new GitBranch("origin/feature/other-feature", "[None]", false),
            });

            repositoryManagerEvents.Reset();
            repositoryManagerEvents.WaitForNotBusy();
        }

        [Test]
        public async Task ShouldDetectGitFetch()
        {
            await Initialize(TestRepoMasterCleanUnsynchronized);

            var repositoryManagerListener = Substitute.For<IRepositoryManagerListener>();
            repositoryManagerListener.AttachListener(RepositoryManager, repositoryManagerEvents);

            RepositoryManager.WaitForEvents();
            repositoryManagerEvents.WaitForNotBusy(2);

            repositoryManagerListener.AssertDidNotReceiveAnyCalls();

            Repository.Name.Should().Be("IOTestsRepo");
            Repository.CloneUrl.ToString().Should().Be("https://github.com/EvilStanleyGoldman/IOTestsRepo.git");
            Repository.Owner.Should().Be("EvilStanleyGoldman");
            Repository.LocalPath.Should().Be(TestRepoMasterCleanUnsynchronized);
            Repository.IsGitHub.Should().BeTrue();
            Repository.CurrentBranchName.Should().Be("master");
            Repository.CurrentBranch.HasValue.Should().BeTrue();
            Repository.CurrentBranch.Value.Name.Should().Be("master");
            Repository.CurrentRemote.HasValue.Should().BeTrue();
            Repository.CurrentRemote.Value.Name.Should().Be("origin");
            Repository.CurrentRemote.Value.Url.Should().Be("https://github.com/EvilStanleyGoldman/IOTestsRepo.git");
            Repository.LocalBranches.Should().BeEquivalentTo(new[] {
                new GitBranch("feature/document", "origin/feature/document", false),
            });
            Repository.Remotes.Should().BeEquivalentTo(new GitRemote("origin","https://github.com/EvilStanleyGoldman/IOTestsRepo.git"));
            Repository.RemoteBranches.Should().BeEquivalentTo(new[] {
                new GitBranch("origin/master", "[None]", false),
                new GitBranch("origin/feature/document", "[None]", false),
                new GitBranch("origin/feature/document-2", "[None]", false),
            });

            await RepositoryManager.Fetch("origin").StartAsAsync();
            await TaskManager.Wait();
            RepositoryManager.WaitForEvents();
            repositoryManagerEvents.WaitForNotBusy();

            repositoryManagerListener.Received().OnIsBusyChanged(Args.Bool);
            repositoryManagerListener.DidNotReceive().OnStatusUpdated(Args.GitStatus);
            repositoryManagerListener.DidNotReceive().OnLocksUpdated(Args.EnumerableGitLock);
            repositoryManagerListener.DidNotReceive().OnCurrentBranchAndRemoteUpdated(Arg.Any<ConfigBranch?>(), Arg.Any<ConfigRemote?>());
            repositoryManagerListener.DidNotReceive().OnLocalBranchListUpdated(Arg.Any<Dictionary<string, ConfigBranch>>());
            repositoryManagerListener.DidNotReceive().OnRemoteBranchListUpdated(Arg.Any<Dictionary<string, ConfigRemote>>(), Arg.Any<Dictionary<string, Dictionary<string, ConfigBranch>>>());
            repositoryManagerListener.DidNotReceive().OnLocalBranchUpdated(Args.String);
            repositoryManagerListener.DidNotReceive().OnLocalBranchAdded(Args.String);
            repositoryManagerListener.DidNotReceive().OnLocalBranchRemoved(Args.String);
            repositoryManagerListener.Received().OnRemoteBranchAdded(Args.String, Args.String);
            repositoryManagerListener.DidNotReceive().OnRemoteBranchRemoved(Args.String, Args.String);

            Repository.Name.Should().Be("IOTestsRepo");
            Repository.CloneUrl.ToString().Should().Be("https://github.com/EvilStanleyGoldman/IOTestsRepo.git");
            Repository.Owner.Should().Be("EvilStanleyGoldman");
            Repository.LocalPath.Should().Be(TestRepoMasterCleanUnsynchronized);
            Repository.IsGitHub.Should().BeTrue();
            Repository.CurrentBranchName.Should().Be("master");
            Repository.CurrentBranch.HasValue.Should().BeTrue();
            Repository.CurrentBranch.Value.Name.Should().Be("master");
            Repository.CurrentRemote.HasValue.Should().BeTrue();
            Repository.CurrentRemote.Value.Name.Should().Be("origin");
            Repository.CurrentRemote.Value.Url.Should().Be("https://github.com/EvilStanleyGoldman/IOTestsRepo.git");
            Repository.LocalBranches.Should().BeEquivalentTo(new[] {
                new GitBranch("feature/document", "origin/feature/document", false),
            });
            Repository.Remotes.Should().BeEquivalentTo(new GitRemote("origin","https://github.com/EvilStanleyGoldman/IOTestsRepo.git"));
            Repository.RemoteBranches.Should().BeEquivalentTo(new[] {
                new GitBranch("origin/master", "[None]", false),
                new GitBranch("origin/feature/document", "[None]", false),
                new GitBranch("origin/feature/document-2", "[None]", false),
                new GitBranch("origin/feature/new-feature", "[None]", false),
                new GitBranch("origin/feature/other-feature", "[None]", false),
            });
        }
    }
}
