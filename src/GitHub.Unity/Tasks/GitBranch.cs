namespace GitHub.Unity
{
    struct GitBranch
    {
        public string Name { get; private set; }
        public string Tracking { get; private set; }
        public bool Active { get; private set; }

        public GitBranch(string name, string tracking, bool active)
        {
            Name = name;
            Tracking = tracking;
            Active = active;
        }
    }
}