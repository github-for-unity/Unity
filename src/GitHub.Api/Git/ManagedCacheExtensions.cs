using System;

namespace GitHub.Unity
{
    static class ManagedCacheExtensions
    {
        public static bool IsLastUpdatedTimeDifferent(this IManagedCache managedCache, CacheUpdateEvent cacheUpdateEvent)
        {
            bool isDifferent;
            if (cacheUpdateEvent.UpdatedTimeString == null)
            {
                isDifferent = managedCache.LastUpdatedAt != DateTimeOffset.MinValue;
            }
            else
            {
                isDifferent = managedCache.LastUpdatedAt.ToString() != cacheUpdateEvent.UpdatedTimeString;
            }
            return isDifferent;
        }
    }
}