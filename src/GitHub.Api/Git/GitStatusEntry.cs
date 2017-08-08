using System;

namespace GitHub.Unity
{
    [Serializable]
    struct GitStatusEntry
    {
        public string Path;
        public string FullPath;
        public string ProjectPath;
        public string OriginalPath;
        public GitFileStatus Status;
        public bool Staged;

        public GitStatusEntry(string path, string fullPath, string projectPath,
            GitFileStatus status,
            string originalPath = null, bool staged = false)
        {
            Path = path;
            Status = status;
            FullPath = fullPath;
            ProjectPath = projectPath;
            OriginalPath = originalPath;
            Staged = staged;
        }

        public override string ToString()
        {
            return String.Format("Path:'{0}' Status:'{1}' FullPath:'{2}' ProjectPath:'{3}' OriginalPath:'{4}' Staged:'{5}'", Path, Status, FullPath, ProjectPath, OriginalPath, Staged);
        }
    }
}
