using System;

namespace GitHub.Unity
{
    public interface ITreeData
    {
        string Name { get; }
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

        public string Name => GitBranch.Name;
        public bool IsActive => GitBranch.IsActive;
    }
}