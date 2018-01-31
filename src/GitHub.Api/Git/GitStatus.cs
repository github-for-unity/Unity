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

        public override int GetHashCode()
        {
            int hash = 17;
            hash = hash * 23 + (LocalBranch?.GetHashCode() ?? 0);
            hash = hash * 23 + (RemoteBranch?.GetHashCode() ?? 0);
            hash = hash * 23 + Ahead.GetHashCode();
            hash = hash * 23 + Behind.GetHashCode();
            hash = hash * 23 + (Entries?.GetHashCode() ?? 0);
            return hash;
        }

        public override bool Equals(object other)
        {
            if (other is GitStatus)
                return Equals((GitStatus)other);
            return false;
        }

        public bool Equals(GitStatus other)
        {
            return
                String.Equals(LocalBranch, other.LocalBranch) && 
                String.Equals(RemoteBranch, other.RemoteBranch) &&
                Ahead == other.Ahead &&
                Behind == other.Behind &&
                object.Equals(Entries, other.Entries)
                ;
        }

        public static bool operator ==(GitStatus lhs, GitStatus rhs)
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

        public static bool operator !=(GitStatus lhs, GitStatus rhs)
        {
            return !(lhs == rhs);
        }

        public override string ToString()
        {
            var remoteBranchString = string.IsNullOrEmpty(RemoteBranch) ? "?" : string.Format("\"{0}\"", RemoteBranch);
            var entriesString = Entries == null ? "NULL" : Entries.Count.ToString();

            return string.Format("{{GitStatus: \"{0}\"->{1} +{2}/-{3} {4} entries}}", LocalBranch, remoteBranchString, Ahead,
                Behind, entriesString);
        }
    }
}