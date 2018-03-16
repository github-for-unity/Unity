using System;
using System.Collections.Generic;
using System.Globalization;
using GitHub.Logging;

namespace GitHub.Unity
{
    public class CacheContainer : ICacheContainer
    {
        private static ILogging Logger = LogHelper.GetLogger<CacheContainer>();

        private Dictionary<CacheType, Lazy<IManagedCache>> caches = new Dictionary<CacheType, Lazy<IManagedCache>>();

        public event Action<CacheType> CacheInvalidated;
        public event Action<CacheType, DateTimeOffset> CacheUpdated;

        public void SetCacheInitializer(CacheType cacheType, Func<IManagedCache> initializer)
        {
            caches.Add(cacheType, new Lazy<IManagedCache>(() => SetupCache(initializer())));
        }

        public void ValidateAll()
        {
            // this can trigger invalidation requests fyi
            foreach (var cache in caches.Values)
                cache.Value.ValidateData();
        }

        public void InvalidateAll()
        {
            foreach (var cache in caches.Values)
            {
                // force an invalidation if the cache is valid, otherwise it will do it on its own
                if (cache.Value.ValidateData())
                    cache.Value.InvalidateData();
            }
        }

        private IManagedCache SetupCache(IManagedCache cache)
        {
            cache.CacheInvalidated += OnCacheInvalidated;
            cache.CacheUpdated += OnCacheUpdated;
            return cache;
        }

        public IManagedCache GetCache(CacheType cacheType)
        {
            return caches[cacheType].Value;
        }

        public void CheckAndRaiseEventsIfCacheNewer(CacheType cacheType, CacheUpdateEvent cacheUpdateEvent)
        {
            var cache = GetCache(cacheType);
            var needsInvalidation = cache.ValidateData();
            if (!cacheUpdateEvent.IsInitialized || !needsInvalidation || cache.LastUpdatedAt != cacheUpdateEvent.UpdatedTime)
            {
                OnCacheUpdated(cache.CacheType, cache.LastUpdatedAt);
            }
        }

        private void OnCacheUpdated(CacheType cacheType, DateTimeOffset datetime)
        {
            Logger.Trace("OnCacheUpdated cacheType:{0} datetime:{1}", cacheType, datetime);
            CacheUpdated.SafeInvoke(cacheType, datetime);
        }

        private void OnCacheInvalidated(CacheType cacheType)
        {
            Logger.Trace("OnCacheInvalidated cacheType:{0}", cacheType);
            CacheInvalidated.SafeInvoke(cacheType);
        }

        private bool disposed;
        private void Dispose(bool disposing)
        {
            if (disposed) return;
            disposed = true;

            if (disposing)
            {
                foreach (var cache in caches.Values)
                {
                    cache.Value.CacheInvalidated -= OnCacheInvalidated;
                    cache.Value.CacheUpdated -= OnCacheUpdated;
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public IBranchCache BranchCache { get { return (IBranchCache)caches[CacheType.Branches].Value; } }
        public IGitLogCache GitLogCache { get { return (IGitLogCache)caches[CacheType.GitLog].Value; } }
        public IGitAheadBehindCache GitTrackingStatusCache { get { return (IGitAheadBehindCache)caches[CacheType.GitAheadBehind].Value; } }
        public IGitStatusCache GitStatusEntriesCache { get { return (IGitStatusCache)caches[CacheType.GitStatus].Value; } }
        public IGitLocksCache GitLocksCache { get { return (IGitLocksCache)caches[CacheType.GitLocks].Value; } }
        public IGitUserCache GitUserCache { get { return (IGitUserCache)caches[CacheType.GitUser].Value; } }
        public IRepositoryInfoCache RepositoryInfoCache { get { return (IRepositoryInfoCache)caches[CacheType.RepositoryInfo].Value; } }
    }

    [Serializable]
    public struct CacheUpdateEvent
    {
        [NonSerialized] private DateTimeOffset? updatedTimeValue;
        public string updatedTimeString;
        public CacheType cacheType;

        public CacheUpdateEvent(CacheType type, DateTimeOffset when)
        {
            if (type == CacheType.None) throw new ArgumentOutOfRangeException(nameof(type));

            cacheType = type;
            updatedTimeValue = when;
            updatedTimeString = when.ToString(Constants.Iso8601Format);
        }

        public override int GetHashCode()
        {
            int hash = 17;
            hash = hash * 23 + cacheType.GetHashCode();
            hash = hash * 23 + (updatedTimeString?.GetHashCode() ?? 0);
            return hash;
        }

        public override bool Equals(object other)
        {
            if (other is CacheUpdateEvent)
                return Equals((CacheUpdateEvent)other);
            return false;
        }

        public bool Equals(CacheUpdateEvent other)
        {
            return
                cacheType == other.cacheType &&
                String.Equals(updatedTimeString, other.updatedTimeString)
                ;
        }

        public static bool operator ==(CacheUpdateEvent lhs, CacheUpdateEvent rhs)
        {
            // If both are null, or both are same instance, return true.
            if (ReferenceEquals(lhs, rhs))
                return true;

            // If one is null, but not both, return false.
            if (((object)lhs == null) || ((object)rhs == null))
                return false;

            // Return true if the fields match:
            return lhs.Equals(rhs);
        }

        public static bool operator !=(CacheUpdateEvent lhs, CacheUpdateEvent rhs)
        {
            return !(lhs == rhs);
        }

        public DateTimeOffset UpdatedTime
        {
            get
            {
                if (!updatedTimeValue.HasValue)
                {
                    DateTimeOffset result;
                    if (DateTimeOffset.TryParseExact(updatedTimeString, Constants.Iso8601Format, CultureInfo.InvariantCulture, DateTimeStyles.None, out result))
                    {
                        updatedTimeValue = result;
                    }
                    else
                    {
                        updatedTimeValue = DateTimeOffset.MinValue;
                        updatedTimeString = updatedTimeValue.Value.ToString(Constants.Iso8601Format);
                    }
                }

                return updatedTimeValue.Value;
            }
        }

        public bool IsInitialized => cacheType != CacheType.None;

        public string UpdatedTimeString => updatedTimeString;
    }
}
