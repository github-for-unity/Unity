using GitHub.Logging;
using GitHub.Unity;
using NSubstitute;
using NUnit.Framework;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace TestUtils.Events
{
    interface ICacheListener
    {
        void CacheInvalidated();
        void CacheUpdated(DateTimeOffset dateTimeOffset);
    }

    class CacheEvents
    {
        internal TaskCompletionSource<object> repositoryInfoCacheInvalidated;
        public Task RepositoryInfoCacheInvalidated => repositoryInfoCacheInvalidated.Task;

        internal TaskCompletionSource<object> repositoryInfoCacheUpdated;
        public Task RepositoryInfoCacheUpdated => repositoryInfoCacheUpdated.Task;

        internal TaskCompletionSource<object> branchesCacheInvalidated;
        public Task BranchesCacheInvalidated => branchesCacheInvalidated.Task;

        internal TaskCompletionSource<object> branchesCacheUpdated;
        public Task BranchesCacheUpdated => branchesCacheUpdated.Task;

        internal TaskCompletionSource<object> gitLogCacheInvalidated;
        public Task GitLogCacheInvalidated => gitLogCacheInvalidated.Task;

        internal TaskCompletionSource<object> gitLogCacheUpdated;
        public Task GitLogCacheUpdated => gitLogCacheUpdated.Task;

        internal TaskCompletionSource<object> gitAheadBehindCacheInvalidated;
        public Task GitAheadBehindCacheInvalidated => gitAheadBehindCacheInvalidated.Task;

        internal TaskCompletionSource<object> gitAheadBehindCacheUpdated;
        public Task GitAheadBehindCacheUpdated => gitAheadBehindCacheUpdated.Task;

        internal TaskCompletionSource<object> gitStatusCacheInvalidated;
        public Task GitStatusCacheInvalidated => gitStatusCacheInvalidated.Task;

        internal TaskCompletionSource<object> gitStatusCacheUpdated;
        public Task GitStatusCacheUpdated => gitStatusCacheUpdated.Task;

        internal TaskCompletionSource<object> gitLocksCacheInvalidated;
        public Task GitLocksCacheInvalidated => gitLocksCacheInvalidated.Task;

        internal TaskCompletionSource<object> gitLocksCacheUpdated;
        public Task GitLocksCacheUpdated => gitLocksCacheUpdated.Task;

        internal TaskCompletionSource<object> gitUserCacheInvalidated;
        public Task GitUserCacheInvalidated => gitUserCacheInvalidated.Task;

        internal TaskCompletionSource<object> gitUserCacheUpdated;
        public Task GitUserCacheUpdated => gitUserCacheUpdated.Task;

        public CacheEvents(IRepositoryInfoCache repositoryInfoCache, IBranchCache branchCache, IGitLogCache gitLogCache,
            IGitAheadBehindCache gitAheadBehindCache, IGitStatusCache gitStatusCache, IGitLocksCache gitLocksCache,
            IGitUserCache gitUserCache)
        {
            repositoryInfoCache.CacheInvalidated += _ => repositoryInfoCacheInvalidated.TrySetResult(true);
            repositoryInfoCache.CacheUpdated += (_, __) => repositoryInfoCacheUpdated.TrySetResult(true);

            branchCache.CacheInvalidated += _ => branchesCacheInvalidated.TrySetResult(true);
            branchCache.CacheUpdated += (_, __) => branchesCacheUpdated.TrySetResult(true);

            gitLogCache.CacheInvalidated += _ => gitLogCacheInvalidated.TrySetResult(true);
            gitLogCache.CacheUpdated += (_, __) => gitLogCacheUpdated.TrySetResult(true);

            gitAheadBehindCache.CacheInvalidated += _ => gitAheadBehindCacheInvalidated.TrySetResult(true);
            gitAheadBehindCache.CacheUpdated += (_, __) => gitAheadBehindCacheUpdated.TrySetResult(true);

            gitStatusCache.CacheInvalidated += _ => gitStatusCacheInvalidated.TrySetResult(true);
            gitStatusCache.CacheUpdated += (_, __) => gitStatusCacheUpdated.TrySetResult(true);

            gitLocksCache.CacheInvalidated += _ => gitLocksCacheInvalidated.TrySetResult(true);
            gitLocksCache.CacheUpdated += (_, __) => gitLocksCacheUpdated.TrySetResult(true);

            gitLocksCache.CacheInvalidated += _ => gitLocksCacheInvalidated.TrySetResult(true);
            gitLocksCache.CacheUpdated += (_, __) => gitLocksCacheUpdated.TrySetResult(true);

            gitUserCache.CacheInvalidated += _ => gitUserCacheInvalidated.TrySetResult(true);
            gitUserCache.CacheUpdated += (_, __) => gitUserCacheUpdated.TrySetResult(true);

            Reset();
        }

        public void Reset()
        {
            repositoryInfoCacheInvalidated = new TaskCompletionSource<object>();
            repositoryInfoCacheUpdated = new TaskCompletionSource<object>();
            branchesCacheInvalidated = new TaskCompletionSource<object>();
            branchesCacheUpdated = new TaskCompletionSource<object>();
            gitLogCacheInvalidated = new TaskCompletionSource<object>();
            gitLogCacheUpdated = new TaskCompletionSource<object>();
            gitAheadBehindCacheInvalidated = new TaskCompletionSource<object>();
            gitAheadBehindCacheUpdated = new TaskCompletionSource<object>();
            gitStatusCacheInvalidated = new TaskCompletionSource<object>();
            gitStatusCacheUpdated = new TaskCompletionSource<object>();
            gitLocksCacheInvalidated = new TaskCompletionSource<object>();
            gitLocksCacheUpdated = new TaskCompletionSource<object>();
            gitUserCacheInvalidated = new TaskCompletionSource<object>();
            gitUserCacheUpdated = new TaskCompletionSource<object>();
        }
    }
};
