using System;
using System.Collections.Generic;

namespace GitHub.Unity
{
    [Serializable]
    struct GitStatus
    {
        public string LocalBranch;
        public string RemoteBranch;
        public int Ahead;
        public int Behind;
        public List<GitStatusEntry> Entries;
    }
}