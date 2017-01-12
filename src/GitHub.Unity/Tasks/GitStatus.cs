using System.Collections.Generic;

namespace GitHub.Unity
{
    struct GitStatus
    {
        public string LocalBranch;
        public string RemoteBranch;
        public int Ahead;
        public int Behind;
        public List<GitStatusEntry> Entries;
    }
}