namespace GitHub.Unity
{
    public interface IUsageTracker
    {
        bool Enabled { get; set; }
        void IncrementNumberOfStartups();
        void IncrementNumberOfCommits();
        void IncrementNumberOfFetches();
        void IncrementNumberOfPushes();
        void IncrementNumberOfPulls();
        void IncrementNumberOfAuthentications();
        void IncrementNumberOfProjectsInitialized();
        void IncrementNumberOfLocalBranchCreations();
        void IncrementNumberOfLocalBranchDeletions();
        void IncrementNumberOfLocalBranchCheckouts();
        void IncrementNumberOfRemoteBranchCheckouts();
    }

    class NullUsageTracker : IUsageTracker
    {
        public bool Enabled { get; set; }
        public void IncrementNumberOfStartups() { }
        public void IncrementNumberOfCommits() { }
        public void IncrementNumberOfFetches() { }
        public void IncrementNumberOfPushes() { }
        public void IncrementNumberOfPulls() { }
        public void IncrementNumberOfAuthentications() { }
        public void IncrementNumberOfProjectsInitialized() { }
        public void IncrementNumberOfLocalBranchCreations() { }
        public void IncrementNumberOfLocalBranchDeletions() { }
        public void IncrementNumberOfLocalBranchCheckouts() { }
        public void IncrementNumberOfRemoteBranchCheckouts() { }
        public void SetMetricsService(IMetricsService instance) { }
    }
}
