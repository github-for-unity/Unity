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
    class RepositoryManagerDetectionTests : BaseGitEnvironmentTest
    {
        private IBranchCache branchCache;
        private IGitAheadBehindCache gitAheadBehindCache;
        private IGitLocksCache gitLockCache;
        private IGitLogCache gitLogCache;
        private IGitStatusCache gitStatusCache;
        private IGitUserCache gitUserCache;
        private IRepositoryInfoCache repositoryInfoCache;
        private CacheEvents cacheEvents;

        protected override ICacheContainer CreateCacheContainer()
        {
            branchCache = CreateMockCache<IBranchCache>(CacheType.Branches);
            gitAheadBehindCache = CreateMockCache<IGitAheadBehindCache>(CacheType.GitAheadBehind);
            gitLockCache = CreateMockCache<IGitLocksCache>(CacheType.GitLocks);
            gitLogCache = CreateMockCache<IGitLogCache>(CacheType.GitLog);
            gitStatusCache = CreateMockCache<IGitStatusCache>(CacheType.GitStatus);
            gitUserCache = CreateMockCache<IGitUserCache>(CacheType.GitUser);
            repositoryInfoCache = CreateMockCache<IRepositoryInfoCache>(CacheType.RepositoryInfo);

            var cacheContainer = Substitute.For<ICacheContainer>();
            cacheContainer.BranchCache.Returns(branchCache);
            cacheContainer.GitTrackingStatusCache.Returns(gitAheadBehindCache);
            cacheContainer.GitLocksCache.Returns(gitLockCache);
            cacheContainer.GitLogCache.Returns(gitLogCache);
            cacheContainer.GitStatusEntriesCache.Returns(gitStatusCache);
            cacheContainer.GitUserCache.Returns(gitUserCache);
            cacheContainer.RepositoryInfoCache.Returns(repositoryInfoCache);

            cacheContainer.GetCache(Args.CacheType).Returns(info => {
                var cacheType = info.Arg<CacheType>();
                switch (cacheType)
                {
                    case CacheType.RepositoryInfo:
                        return repositoryInfoCache;

                    case CacheType.Branches:
                        return branchCache;

                    case CacheType.GitLog:
                        return gitLogCache;

                    case CacheType.GitAheadBehind:
                        return gitAheadBehindCache;

                    case CacheType.GitStatus:
                        return gitStatusCache;

                    case CacheType.GitLocks:
                        return gitLockCache;

                    case CacheType.GitUser:
                        return gitUserCache;

                    default:
                        throw new ArgumentException("Unknown CacheType" + cacheType.ToString(), "cacheType");
                }
            });

            cacheEvents = new CacheEvents(repositoryInfoCache, branchCache, gitLogCache, gitAheadBehindCache, gitStatusCache, gitLockCache, gitUserCache);

            return cacheContainer;
        }

        private void ClearReceivedCalls()
        {
            this.cacheEvents.Reset();

            this.branchCache.ClearReceivedCalls();
            this.gitAheadBehindCache.ClearReceivedCalls();
            this.gitLockCache.ClearReceivedCalls();
            this.gitLogCache.ClearReceivedCalls();
            this.gitStatusCache.ClearReceivedCalls();
            this.gitUserCache.ClearReceivedCalls();
            this.repositoryInfoCache.ClearReceivedCalls();
        }

        private static T CreateMockCache<T>(CacheType cacheType) where T : class, IManagedCache
        {
            var cache = Substitute.For<T>();

            cache
                .When(c => c.InvalidateData())
                .Do(_ => cache.CacheInvalidated += Raise.Event<Action<CacheType>>(cacheType));

            return cache;
        }

        [Test]
        public async Task ShouldDetectFileChanges()
        {
            Stopwatch watch = null;
            ILogging logger = null;
            StartTest(out watch, out logger);

            try
            {
                InitializePlatformAndEnvironment(TestRepoMasterCleanSynchronized);

                var foobarTxt = TestRepoMasterCleanSynchronized.Combine("foobar.txt");
                foobarTxt.WriteAllText("foobar");

                await TaskEx.WhenAny(
                    TaskEx.WhenAll(
                        cacheEvents.GitStatusCacheInvalidated
                    ),
                    TaskEx.Delay(TimeSpan.FromSeconds(10))
                );

                branchCache.DidNotReceive().InvalidateData();
                gitAheadBehindCache.DidNotReceive().InvalidateData();
                gitLockCache.DidNotReceive().InvalidateData();
                gitLogCache.DidNotReceive().InvalidateData();
                gitStatusCache.Received(1).InvalidateData();
                gitUserCache.DidNotReceive().InvalidateData();
                repositoryInfoCache.DidNotReceive().InvalidateData();

                ClearReceivedCalls();
            }
            finally
            {
                EndTest(logger);
            }
        }

        [Test]
        public async Task ShouldDetectFileChangeAndCommit()
        {
            Stopwatch watch = null;
            ILogging logger = null;
            StartTest(out watch, out logger);

            try
            {
                InitializePlatformAndEnvironment(TestRepoMasterCleanSynchronized);

                var foobarTxt = TestRepoMasterCleanSynchronized.Combine("foobar.txt");
                foobarTxt.WriteAllText("foobar");

                await TaskEx.WhenAny(
                    TaskEx.WhenAll(
                        cacheEvents.GitStatusCacheInvalidated
                    ),
                    TaskEx.Delay(TimeSpan.FromSeconds(10))
                );

                branchCache.DidNotReceive().InvalidateData();
                gitAheadBehindCache.DidNotReceive().InvalidateData();
                gitLockCache.DidNotReceive().InvalidateData();
                gitLogCache.DidNotReceive().InvalidateData();
                gitStatusCache.Received(1).InvalidateData();
                gitUserCache.DidNotReceive().InvalidateData();
                repositoryInfoCache.DidNotReceive().InvalidateData();

                ClearReceivedCalls();

                var filesToCommit = new List<string> { "foobar.txt" };
                var commitMessage = "IntegrationTest Commit";
                var commitBody = string.Empty;

                StartTrackTime(watch, logger, "CommitFiles");
                await RepositoryManager.CommitFiles(filesToCommit, commitMessage, commitBody).StartAsAsync();
                StopTrackTimeAndLog(watch, logger);

                await TaskEx.WhenAny(
                    TaskEx.WhenAll(
                        cacheEvents.BranchesCacheInvalidated,
                        cacheEvents.GitAheadBehindCacheInvalidated,
                        cacheEvents.GitLogCacheInvalidated,
                        cacheEvents.GitStatusCacheInvalidated
                    ),
                    TaskEx.Delay(TimeSpan.FromSeconds(10))
                );

                branchCache.Received(1).InvalidateData();
                gitAheadBehindCache.Received(1).InvalidateData();
                gitLockCache.DidNotReceive().InvalidateData();
                gitLogCache.Received(1).InvalidateData();
                gitStatusCache.Received(1).InvalidateData();
                gitUserCache.DidNotReceive().InvalidateData();
                repositoryInfoCache.DidNotReceive().InvalidateData();

                ClearReceivedCalls();
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
                InitializePlatformAndEnvironment(TestRepoMasterCleanSynchronized);
        
                await RepositoryManager.SwitchBranch("feature/document").StartAsAsync();

                await TaskEx.WhenAny(
                    TaskEx.WhenAll(
                        cacheEvents.BranchesCacheInvalidated,
                        cacheEvents.GitStatusCacheInvalidated,
                        cacheEvents.GitAheadBehindCacheInvalidated,
                        cacheEvents.GitLogCacheInvalidated,
                        cacheEvents.RepositoryInfoCacheInvalidated
                    ),
                    TaskEx.Delay(TimeSpan.FromSeconds(10))
                );

                branchCache.Received(1).InvalidateData();
                gitAheadBehindCache.Received(2).InvalidateData();
                gitLockCache.DidNotReceive().InvalidateData();
                gitLogCache.Received(2).InvalidateData();
                gitStatusCache.Received(1).InvalidateData();
                gitUserCache.DidNotReceive().InvalidateData();
                repositoryInfoCache.Received(1).InvalidateData();
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
                InitializePlatformAndEnvironment(TestRepoMasterCleanSynchronized);
        
                await RepositoryManager.DeleteBranch("feature/document", true).StartAsAsync();

                await TaskEx.WhenAny(
                    TaskEx.WhenAll(
                        cacheEvents.BranchesCacheInvalidated,
                        cacheEvents.GitAheadBehindCacheInvalidated,
                        cacheEvents.GitLogCacheInvalidated,
                        cacheEvents.RepositoryInfoCacheInvalidated
                    ),
                    TaskEx.Delay(TimeSpan.FromSeconds(10))
                );

                branchCache.Received(2).InvalidateData();
                gitAheadBehindCache.Received(1).InvalidateData();
                gitLockCache.DidNotReceive().InvalidateData();
                gitLogCache.Received(2).InvalidateData();
                gitStatusCache.DidNotReceive().InvalidateData();
                gitUserCache.DidNotReceive().InvalidateData();
                repositoryInfoCache.Received(1).InvalidateData();
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
                InitializePlatformAndEnvironment(TestRepoMasterCleanSynchronized);
        
                var createdBranch1 = "feature/document2";
                await RepositoryManager.CreateBranch(createdBranch1, "feature/document").StartAsAsync();

                await TaskEx.WhenAny(
                    TaskEx.WhenAll(
                        cacheEvents.BranchesCacheInvalidated,
                        cacheEvents.GitAheadBehindCacheInvalidated,
                        cacheEvents.GitLogCacheInvalidated
                    ),
                    TaskEx.Delay(TimeSpan.FromSeconds(10))
                );

                branchCache.Received(1).InvalidateData();
                gitAheadBehindCache.Received(1).InvalidateData();
                gitLockCache.DidNotReceive().InvalidateData();
                gitLogCache.Received(1).InvalidateData();
                gitStatusCache.DidNotReceive().InvalidateData();
                gitUserCache.DidNotReceive().InvalidateData();
                repositoryInfoCache.DidNotReceive().InvalidateData();

                ClearReceivedCalls();

                await RepositoryManager.CreateBranch("feature2/document2", "feature/document").StartAsAsync();

                await TaskEx.WhenAny(
                    TaskEx.WhenAll(
                        cacheEvents.BranchesCacheInvalidated,
                        cacheEvents.GitAheadBehindCacheInvalidated,
                        cacheEvents.GitLogCacheInvalidated
                    ),
                    TaskEx.Delay(TimeSpan.FromSeconds(10))
                );

                branchCache.Received(1).InvalidateData();
                gitAheadBehindCache.Received(1).InvalidateData();
                gitLockCache.DidNotReceive().InvalidateData();
                gitLogCache.Received(1).InvalidateData();
                gitStatusCache.DidNotReceive().InvalidateData();
                gitUserCache.DidNotReceive().InvalidateData();
                repositoryInfoCache.DidNotReceive().InvalidateData();
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
                InitializePlatformAndEnvironment(TestRepoMasterCleanSynchronized);
        
                await RepositoryManager.RemoteRemove("origin").StartAsAsync();

                await TaskEx.WhenAny(
                    TaskEx.WhenAll(
                        cacheEvents.BranchesCacheInvalidated,
                        cacheEvents.GitAheadBehindCacheInvalidated,
                        cacheEvents.GitLogCacheInvalidated,
                        cacheEvents.RepositoryInfoCacheInvalidated
                    ),
                    TaskEx.Delay(TimeSpan.FromSeconds(10))
                );

                branchCache.Received(2).InvalidateData();
                gitAheadBehindCache.Received(1).InvalidateData();
                gitLockCache.DidNotReceive().InvalidateData();
                gitLogCache.Received(2).InvalidateData();
                gitStatusCache.DidNotReceive().InvalidateData();
                gitUserCache.DidNotReceive().InvalidateData();
                repositoryInfoCache.Received(1).InvalidateData();

                ClearReceivedCalls();

                await RepositoryManager.RemoteAdd("origin", "https://github.com/EvilShana/IOTestsRepo.git").StartAsAsync();

                await TaskEx.WhenAny(
                    TaskEx.WhenAll(
                        cacheEvents.BranchesCacheInvalidated,
                        cacheEvents.GitLogCacheInvalidated,
                        cacheEvents.RepositoryInfoCacheInvalidated
                    ),
                    TaskEx.Delay(TimeSpan.FromSeconds(10))
                );

                branchCache.Received(2).InvalidateData();
                gitAheadBehindCache.DidNotReceive().InvalidateData();
                gitLockCache.DidNotReceive().InvalidateData();
                gitLogCache.Received(1).InvalidateData();
                gitStatusCache.DidNotReceive().InvalidateData();
                gitUserCache.DidNotReceive().InvalidateData();
                repositoryInfoCache.Received(1).InvalidateData();

                ClearReceivedCalls();
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
                InitializePlatformAndEnvironment(TestRepoMasterTwoRemotes);
        
                await RepositoryManager.CreateBranch("branch2", "another/master").StartAsAsync();

                await TaskEx.WhenAny(
                    TaskEx.WhenAll(
                        cacheEvents.BranchesCacheInvalidated,
                        cacheEvents.GitAheadBehindCacheInvalidated,
                        cacheEvents.GitLogCacheInvalidated,
                        cacheEvents.RepositoryInfoCacheInvalidated
                    ),
                    TaskEx.Delay(TimeSpan.FromSeconds(10))
                );

                branchCache.Received(2).InvalidateData();
                gitAheadBehindCache.Received(1).InvalidateData();
                gitLockCache.DidNotReceive().InvalidateData();
                gitLogCache.Received(2).InvalidateData();
                gitStatusCache.DidNotReceive().InvalidateData();
                gitUserCache.DidNotReceive().InvalidateData();
                repositoryInfoCache.Received(1).InvalidateData();

                ClearReceivedCalls();

                await RepositoryManager.SwitchBranch("branch2").StartAsAsync();

                await TaskEx.WhenAny(
                    TaskEx.WhenAll(
                        cacheEvents.GitAheadBehindCacheInvalidated,
                        cacheEvents.GitLogCacheInvalidated,
                        cacheEvents.RepositoryInfoCacheInvalidated
                    ),
                    TaskEx.Delay(TimeSpan.FromSeconds(10))
                );

                branchCache.DidNotReceive().InvalidateData();
                gitAheadBehindCache.Received(1).InvalidateData();
                gitLockCache.DidNotReceive().InvalidateData();
                gitLogCache.Received(1).InvalidateData();
                gitStatusCache.DidNotReceive().InvalidateData();
                gitUserCache.DidNotReceive().InvalidateData();
                repositoryInfoCache.Received(1).InvalidateData();

                ClearReceivedCalls();
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
                InitializePlatformAndEnvironment(TestRepoMasterCleanSynchronized);
        
                await RepositoryManager.Pull("origin", "master").StartAsAsync();
                await TaskManager.Wait();

                await TaskEx.WhenAny(
                    TaskEx.WhenAll(
                        cacheEvents.BranchesCacheInvalidated,
                        cacheEvents.GitAheadBehindCacheInvalidated,
                        cacheEvents.GitLogCacheInvalidated,
                        cacheEvents.GitStatusCacheInvalidated
                    ),
                    TaskEx.Delay(TimeSpan.FromSeconds(10))
                );

                branchCache.Received(2).InvalidateData();
                gitAheadBehindCache.Received(1).InvalidateData();
                gitLockCache.DidNotReceive().InvalidateData();
                gitLogCache.Received(1).InvalidateData();
                gitStatusCache.Received().InvalidateData();
                gitUserCache.DidNotReceive().InvalidateData();
                repositoryInfoCache.DidNotReceive().InvalidateData();
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
                InitializePlatformAndEnvironment(TestRepoMasterCleanUnsynchronized);
        
                await RepositoryManager.Fetch("origin").StartAsAsync();

                await TaskEx.WhenAny(
                    TaskEx.WhenAll(
                        cacheEvents.BranchesCacheInvalidated,
                        cacheEvents.GitAheadBehindCacheInvalidated,
                        cacheEvents.GitLogCacheInvalidated
                    ),
                    TaskEx.Delay(TimeSpan.FromSeconds(10))
                );

                branchCache.Received().InvalidateData();
                gitAheadBehindCache.Received(1).InvalidateData();
                gitLockCache.DidNotReceive().InvalidateData();
                gitLogCache.Received(1).InvalidateData();
                gitStatusCache.DidNotReceive().InvalidateData();
                gitUserCache.DidNotReceive().InvalidateData();
                repositoryInfoCache.DidNotReceive().InvalidateData();
            }
            finally
            {
                EndTest(logger);
            }
        }
    }
}
