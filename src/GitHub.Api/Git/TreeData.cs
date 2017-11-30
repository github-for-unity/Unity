using System;

namespace GitHub.Unity
{
    public interface ITreeData
    {
        string Path { get; }
        bool IsActive { get; }
        string CustomStringTag { get;}
        int CustomIntTag { get; }
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

        public string CustomStringTag => null;

        public int CustomIntTag => 0;
    }

    [Serializable]
    public struct GitStatusEntryTreeData : ITreeData
    {
        public static GitStatusEntryTreeData Default = new GitStatusEntryTreeData(GitStatusEntry.Default);

        public GitStatusEntry gitStatusEntry;

        public GitStatusEntryTreeData(GitStatusEntry gitStatusEntry)
        {
            this.gitStatusEntry = gitStatusEntry;
        }

        public string Path => gitStatusEntry.ProjectPath;
        public bool IsActive => false;
        public GitStatusEntry GitStatusEntry => gitStatusEntry;

        public string CustomStringTag => gitStatusEntry.ProjectPath;

        public int CustomIntTag => (int)gitStatusEntry.Status;
    }
}