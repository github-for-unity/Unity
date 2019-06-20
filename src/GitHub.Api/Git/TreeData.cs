using System;
using System.Security.Cryptography.X509Certificates;

namespace GitHub.Unity
{
    public interface ITreeData
    {
        string Path { get; }
        bool IsActive { get; }
        bool IsChecked { get; }
    }

    [Serializable]
    public struct GitBranchTreeData : ITreeData
    {
        public static GitBranchTreeData Default = new GitBranchTreeData(Unity.GitBranch.Default, false);

        public GitBranch GitBranch;
        public bool isActive;

        public GitBranchTreeData(GitBranch gitBranch, bool isActive)
        {
            GitBranch = gitBranch;
            this.isActive = isActive;
        }

        public override int GetHashCode()
        {
            int hash = 17;
            hash = hash * 23 + GitBranch.GetHashCode();
            hash = hash * 23 + isActive.GetHashCode();
            return hash;
        }

        public override bool Equals(object other)
        {
            if (other is GitBranchTreeData)
                return Equals((GitBranchTreeData)other);
            return false;
        }

        public bool Equals(GitBranchTreeData other)
        {
            return GitBranch.Equals(other.GitBranch) && IsActive == other.IsActive;
        }

        public static bool operator ==(GitBranchTreeData lhs, GitBranchTreeData rhs)
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

        public static bool operator !=(GitBranchTreeData lhs, GitBranchTreeData rhs)
        {
            return !(lhs == rhs);
        }

        public string Path => GitBranch.Name;
        public bool IsActive => isActive;
        public bool IsChecked => false;
    }

    [Serializable]
    public struct GitStatusEntryTreeData : ITreeData
    {
        public static GitStatusEntryTreeData Default = new GitStatusEntryTreeData(GitStatusEntry.Default);

        public GitStatusEntry gitStatusEntry;
        public bool isLocked;
        public bool isChecked;

        public GitStatusEntryTreeData(GitStatusEntry gitStatusEntry, bool isLocked = false)
        {
            this.isLocked = isLocked;
            this.gitStatusEntry = gitStatusEntry;
            isChecked = gitStatusEntry.Staged;
        }

        public override int GetHashCode()
        {
            int hash = 17;
            hash = hash * 23 + gitStatusEntry.GetHashCode();
            hash = hash * 23 + isLocked.GetHashCode();
            return hash;
        }

        public override bool Equals(object other)
        {
            if (other is GitStatusEntryTreeData)
                return Equals((GitStatusEntryTreeData)other);
            return false;
        }

        public bool Equals(GitStatusEntryTreeData other)
        {
            return
                gitStatusEntry.Equals(other.gitStatusEntry) &&
                isLocked == other.isLocked;
        }

        public static bool operator ==(GitStatusEntryTreeData lhs, GitStatusEntryTreeData rhs)
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

        public static bool operator !=(GitStatusEntryTreeData lhs, GitStatusEntryTreeData rhs)
        {
            return !(lhs == rhs);
        }

        public string Path => gitStatusEntry.Path;
        public string ProjectPath => gitStatusEntry.ProjectPath;
        public bool IsActive => false;
        public GitStatusEntry GitStatusEntry => gitStatusEntry;
        public GitFileStatus FileStatus => gitStatusEntry.Status;
        public bool IsLocked => isLocked;
        public bool IsChecked => isChecked;
    }
}
