using GitHub.Logging;
using GitHub.Unity;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;

namespace GitHub.Unity
{
    sealed class RepositoryInfoCacheData : IRepositoryInfoCacheData
    {
        public GitRemote? CurrentGitRemote { get; set; }
        public GitBranch? CurrentGitBranch { get; set; }
        public ConfigRemote? CurrentConfigRemote { get; set; }
        public ConfigBranch? CurrentConfigBranch { get; set; }
        public string CurrentHead { get; set; }
    }
}
