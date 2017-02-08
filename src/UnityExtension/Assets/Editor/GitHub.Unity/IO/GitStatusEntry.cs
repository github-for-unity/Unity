using System;

namespace GitHub.Unity
{
    struct GitStatusEntry
    {
        public readonly string Path;
        public readonly string FullPath;
        public readonly string ProjectPath;
        public readonly string OriginalPath;
        public readonly GitFileStatus Status;
        public readonly bool Staged;

        public GitStatusEntry(string path, string fullPath, string projectPath, GitFileStatus status,
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
            return String.Format("'{0}': {1}", Path, Status);
        }
    }
}
