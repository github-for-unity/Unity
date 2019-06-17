using System;

namespace GitHub.Unity
{
    [Serializable]
    public struct GitStatusEntry
    {
        public static GitStatusEntry Default = new GitStatusEntry();
        
        public string path;
        public string fullPath;
        public string projectPath;
        public string originalPath;
        public GitFileStatus indexStatus;
        public GitFileStatus workTreeStatus;

        public GitStatusEntry(string path, string fullPath, string projectPath,
            GitFileStatus indexStatus, GitFileStatus workTreeStatus,
            string originalPath = null)
        {
            Guard.ArgumentNotNullOrWhiteSpace(path, "path");
            Guard.ArgumentNotNullOrWhiteSpace(fullPath, "fullPath");

            this.path = path;
            this.indexStatus = indexStatus;
            this.workTreeStatus = workTreeStatus;
            this.fullPath = fullPath;
            this.projectPath = projectPath;
            this.originalPath = originalPath;
        }

        public override int GetHashCode()
        {
            int hash = 17;
            hash = hash * 23 + (path?.GetHashCode() ?? 0);
            hash = hash * 23 + (fullPath?.GetHashCode() ?? 0);
            hash = hash * 23 + (projectPath?.GetHashCode() ?? 0);
            hash = hash * 23 + (originalPath?.GetHashCode() ?? 0);
            hash = hash * 23 + indexStatus.GetHashCode();
            hash = hash * 23 + workTreeStatus.GetHashCode();
            return hash;
        }

        public override bool Equals(object other)
        {
            if (other is GitStatusEntry)
                return Equals((GitStatusEntry)other);
            return false;
        }

        public bool Equals(GitStatusEntry other)
        {
            return
                String.Equals(path, other.path) && 
                String.Equals(fullPath, other.fullPath) &&
                String.Equals(projectPath, other.projectPath) &&
                String.Equals(originalPath, other.originalPath) &&
                indexStatus == other.indexStatus &&
                workTreeStatus == other.workTreeStatus
                ;
        }

        public static bool operator ==(GitStatusEntry lhs, GitStatusEntry rhs)
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

        public static bool operator !=(GitStatusEntry lhs, GitStatusEntry rhs)
        {
            return !(lhs == rhs);
        }

        public static GitFileStatus ParseStatusMarker(char changeFlag)
        {
            GitFileStatus status = GitFileStatus.None;
            switch (changeFlag)
            {
                case 'M':
                    status = GitFileStatus.Modified;
                    break;
                case 'A':
                    status = GitFileStatus.Added;
                    break;
                case 'D':
                    status = GitFileStatus.Deleted;
                    break;
                case 'R':
                    status = GitFileStatus.Renamed;
                    break;
                case 'C':
                    status = GitFileStatus.Copied;
                    break;
                case 'U':
                    status = GitFileStatus.Unmerged;
                    break;
                case 'T':
                    status = GitFileStatus.TypeChange;
                    break;
                case 'X':
                    status = GitFileStatus.Unknown;
                    break;
                case 'B':
                    status = GitFileStatus.Broken;
                    break;
                case '?':
                    status = GitFileStatus.Untracked;
                    break;
                case '!':
                    status = GitFileStatus.Ignored;
                    break;
                default: break;
            }
            return status;
        }

        public string Path => path;

        public string FullPath => fullPath;

        public string ProjectPath => projectPath;

        public string OriginalPath => originalPath;

        public GitFileStatus Status => workTreeStatus != GitFileStatus.None ? workTreeStatus : indexStatus;
        public GitFileStatus IndexStatus => indexStatus;
        public GitFileStatus WorkTreeStatus => workTreeStatus;

        public bool Staged => indexStatus != GitFileStatus.None && !Unmerged && !Untracked && !Ignored;

        public bool Unmerged => (indexStatus == workTreeStatus && (indexStatus == GitFileStatus.Added || indexStatus == GitFileStatus.Deleted)) ||
                                 indexStatus == GitFileStatus.Unmerged || workTreeStatus == GitFileStatus.Unmerged;

        public bool Untracked => workTreeStatus == GitFileStatus.Untracked;
        public bool Ignored => workTreeStatus == GitFileStatus.Ignored;

        public override string ToString()
        {
            return $"Path:'{Path}' Status:'{Status}' FullPath:'{FullPath}' ProjectPath:'{ProjectPath}' OriginalPath:'{OriginalPath}' Staged:'{Staged}' Unmerged:'{Unmerged}' Status:'{IndexStatus}'  Status:'{WorkTreeStatus}' ";
        }
    }
}
