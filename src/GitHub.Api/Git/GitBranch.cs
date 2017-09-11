using System;

namespace GitHub.Unity
{
    interface ITreeData
    {
        string Name { get; }
        bool IsActive { get; }
    }

    [Serializable]
    struct GitBranch : ITreeData
    {
        private string name;
        private string tracking;
        private bool active;
        public string Name { get { return name; } }
        public string Tracking { get { return tracking; } }
        public bool IsActive { get { return active; } }

        public GitBranch(string name, string tracking, bool active)
        {
            Guard.ArgumentNotNullOrWhiteSpace(name, "name");

            this.name = name;
            this.tracking = tracking;
            this.active = active;
        }

        public override string ToString()
        {
            return $"{Name} Tracking? {Tracking} Active? {IsActive}";
        }
    }
}