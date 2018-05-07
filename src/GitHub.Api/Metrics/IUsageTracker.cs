namespace GitHub.Unity
{
    public interface IUsageTracker
    {
        bool Enabled { get; set; }
        void IncrementNumberOfStartups();
        void ChangesViewButtonCommit();
        void HistoryViewToolbarButtonFetch();
        void HistoryViewToolbarButtonPush();
        void HistoryViewToolbarButtonPull();
        void AuthenticationViewButtonAuthentication();
        void ProjectsInitialized();
        void BranchesViewButtonCreateBranch();
        void BranchesViewButtonDeleteBranch();
        void BranchesViewButtonCheckoutLocalBranch();
        void BranchesViewButtonCheckoutRemoteBranch();
        void SettingsViewUnlockButtonLfsUnlock();
        void AssetExplorerContextMenuLfsLock();
        void AssetExplorerContextMenuLfsUnlock();
    }

    class NullUsageTracker : IUsageTracker
    {
        public bool Enabled { get; set; }
        public void IncrementNumberOfStartups() { }
        public void ChangesViewButtonCommit() { }
        public void HistoryViewToolbarButtonFetch() { }
        public void HistoryViewToolbarButtonPush() { }
        public void HistoryViewToolbarButtonPull() { }
        public void AuthenticationViewButtonAuthentication() { }
        public void ProjectsInitialized() { }
        public void BranchesViewButtonCreateBranch() { }
        public void BranchesViewButtonDeleteBranch() { }
        public void BranchesViewButtonCheckoutLocalBranch() { }
        public void BranchesViewButtonCheckoutRemoteBranch() { }
        public void SettingsViewUnlockButtonLfsUnlock() { }

        public void AssetExplorerContextMenuLfsLock() { }

        public void AssetExplorerContextMenuLfsUnlock() { }

        public void SetMetricsService(IMetricsService instance) { }
    }
}
