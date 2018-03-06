using GitHub.Logging;
using GitHub.Unity;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;

namespace GitHub.Unity
{
    class DataCache : IManagedCache
    {
        public DataCache(CacheType cacheType)
        {
            CacheType = cacheType;
        }
        public CacheType CacheType { get; }

        public DateTimeOffset LastUpdatedAt { get; }

        public event Action CacheInvalidated;
        public event Action<DateTimeOffset> CacheUpdated;

        public void InvalidateData()
        {
        }

        public bool ValidateData() => true;
    }

    sealed class DataCache_RepositoryInfo : DataCache, IRepositoryInfoCache
    {
        public DataCache_RepositoryInfo() : base(CacheType.RepositoryInfo)
        { }

        public void UpdateData(IRepositoryInfoCache data)
        {
        }

        public GitRemote? CurrentGitRemote { get; set; }
        public GitBranch? CurrentGitBranch { get; set; }

        public ConfigRemote? CurrentConfigRemote { get; set; }

        public ConfigBranch? CurrentConfigBranch { get; set; }

        public TimeSpan DataTimeout { get { return TimeSpan.FromDays(1); } }
    }
}
