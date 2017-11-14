using System;

namespace GitHub.Unity
{
    static class ManagedCacheExtensions
    {
        public static bool ShouldRaiseCacheEvent(this IManagedCache managedCache, CacheUpdateEvent cacheUpdateEvent)
        {
            bool raiseEvent;
            if (cacheUpdateEvent.UpdatedTimeString == null)
            {
                raiseEvent = managedCache.LastUpdatedAt != DateTimeOffset.MinValue;
            }
            else
            {
                raiseEvent = managedCache.LastUpdatedAt.ToString() != cacheUpdateEvent.UpdatedTimeString;
            }
            return raiseEvent;
        }
    }
}