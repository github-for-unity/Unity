using System;

namespace GitHub.Unity
{
    public interface ITreeData
    {
        string Path { get; }
        bool IsActive { get; }
    }

    [Serializable]
    public struct GitBranchTreeData : ITreeData
    {
        public static GitBranchTreeData Default = new GitBranchTreeData(Unity.GitBranch.Default);

        public GitBranch GitBranch;

        public GitBranchTreeData(GitBranch gitBranch)
        {
            GitBranch = gitBranch;
        }

        public string Path => GitBranch.Name;
        public bool IsActive => GitBranch.IsActive;
    }

    [Serializable]
    public struct GitStatusEntryTreeData : ITreeData
    {
        public static GitStatusEntryTreeData Default = new GitStatusEntryTreeData(GitStatusEntry.Default);

        public GitStatusEntry gitStatusEntry;
        public bool isLocked;

        public GitStatusEntryTreeData(GitStatusEntry gitStatusEntry, bool isLocked = false)
        {
            this.isLocked = isLocked;
            this.gitStatusEntry = gitStatusEntry;
        }

        public string Path => gitStatusEntry.Path;
        public string ProjectPath => gitStatusEntry.ProjectPath;
        public bool IsActive => false;
        public GitStatusEntry GitStatusEntry => gitStatusEntry;
        public GitFileStatus FileStatus => gitStatusEntry.Status;
        public bool IsLocked => isLocked;
    }
}