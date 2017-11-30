using System;

namespace GitHub.Unity
{
    public interface ITreeData
    {
        string FullPath { get; }
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

        public string FullPath => GitBranch.Name;
        public string Path => GitBranch.Name;
        public bool IsActive => GitBranch.IsActive;
    }
}