using System;

namespace GitHub.Unity
{
    public interface ITreeData
    {
        string Name { get; }
        bool IsActive { get; }
    }

    [Serializable]
    public struct GitBranch : ITreeData
    {
        public static GitBranch Default = new GitBranch();

        public string name;
        public string tracking;
        public bool isActive;

        public string Name { get { return name; } }
        public string Tracking { get { return tracking; } }
        public bool IsActive { get { return isActive; } }

        public GitBranch(string name, string tracking, bool active)
        {
            Guard.ArgumentNotNullOrWhiteSpace(name, "name");

            this.name = name;
            this.tracking = tracking;
            this.isActive = active;
        }

        public override string ToString()
        {
            return $"{Name} Tracking? {Tracking} Active? {IsActive}";
        }
    }
}