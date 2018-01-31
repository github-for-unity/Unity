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
        public GitFileStatus status;
        public bool staged;

        public GitStatusEntry(string path, string fullPath, string projectPath,
            GitFileStatus status,
            string originalPath = null, bool staged = false)
        {
            Guard.ArgumentNotNullOrWhiteSpace(path, "path");
            Guard.ArgumentNotNullOrWhiteSpace(fullPath, "fullPath");

            this.path = path;
            this.status = status;
            this.fullPath = fullPath;
            this.projectPath = projectPath;
            this.originalPath = originalPath;
            this.staged = staged;
        }

        public override int GetHashCode()
        {
            int hash = 17;
            hash = hash * 23 + (path?.GetHashCode() ?? 0);
            hash = hash * 23 + (fullPath?.GetHashCode() ?? 0);
            hash = hash * 23 + (projectPath?.GetHashCode() ?? 0);
            hash = hash * 23 + (originalPath?.GetHashCode() ?? 0);
            hash = hash * 23 + status.GetHashCode();
            hash = hash * 23 + staged.GetHashCode();
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
                status == other.status &&
                staged == other.staged
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

        public string Path => path;

        public string FullPath => fullPath;

        public string ProjectPath => projectPath;

        public string OriginalPath => originalPath;

        public GitFileStatus Status => status;

        public bool Staged => staged;

        public override string ToString()
        {
            return $"Path:'{Path}' Status:'{Status}' FullPath:'{FullPath}' ProjectPath:'{ProjectPath}' OriginalPath:'{OriginalPath}' Staged:'{Staged}'";
        }
    }
}
