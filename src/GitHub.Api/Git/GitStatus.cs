using System;
using System.Collections.Generic;

namespace GitHub.Unity
{
    [Serializable]
    public struct GitStatus
    {
        public string LocalBranch;
        public string RemoteBranch;
        public int Ahead;
        public int Behind;
        public List<GitStatusEntry> Entries;

        public override string ToString()
        {
            var remoteBranchString = string.IsNullOrEmpty(RemoteBranch) ? "?" : string.Format("\"{0}\"", RemoteBranch);
            var entriesString = Entries == null ? "NULL" : Entries.Count.ToString();

            return string.Format("{{GitStatus: \"{0}\"->{1} +{2}/-{3} {4} entries}}", LocalBranch, remoteBranchString, Ahead,
                Behind, entriesString);
        }
    }
}