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

        public override string ToString()
        {
            var sb = new System.Text.StringBuilder();
            sb.Append(String.Format("LocalBranch:{0} RemoteBranch:{1} Ahead:{2} Behind:{3}", LocalBranch, RemoteBranch, Ahead, Behind));
            if (Entries != null)
            {
                foreach (var e in Entries)
                {
                    sb.AppendLine(e.ToString());
                }
            }
            return sb.ToString();
        }

    }
}