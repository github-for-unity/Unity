namespace GitHub.Unity
{
    public interface IUsageTracker
    {
        bool Enabled { get; set; }
        void IncrementNumberOfStartups();
        void ChangesViewButtonCommit();
        void HistoryToolbarButtonFetch();
        void HistoryToolbarButtonPush();
        void HistoryToolbarButtonPull();
        void AuthenticationViewButtonAuthentication();
        void Initialized();
        void BranchesViewButtonCreateBranch();
        void BranchesViewButtonDeleteBranch();
        void BranchesViewButtonCheckoutLocalBranch();
        void BranchesViewButtonCheckoutRemoteBranch();
    }

    class NullUsageTracker : IUsageTracker
    {
        public bool Enabled { get; set; }
        public void IncrementNumberOfStartups() { }
        public void ChangesViewButtonCommit() { }
        public void HistoryToolbarButtonFetch() { }
        public void HistoryToolbarButtonPush() { }
        public void HistoryToolbarButtonPull() { }
        public void AuthenticationViewButtonAuthentication() { }
        public void Initialized() { }
        public void BranchesViewButtonCreateBranch() { }
        public void BranchesViewButtonDeleteBranch() { }
        public void BranchesViewButtonCheckoutLocalBranch() { }
        public void BranchesViewButtonCheckoutRemoteBranch() { }
        public void SetMetricsService(IMetricsService instance) { }
    }
}
