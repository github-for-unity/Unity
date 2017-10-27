using System;

namespace GitHub.Unity
{
    [Serializable]
    public struct GitBranch
    {
        public string Name;
        public string Tracking;
        public bool IsActive;

        public override string ToString()
        {
            return $"{Name} Tracking? {Tracking} Active? {IsActive}";
        }
    }
}