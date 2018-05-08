namespace GitHub.Unity
{
    public interface IUsageTracker
    {
        bool Enabled { get; set; }
        void IncrementNumberOfStartups();
        void IncrementChangesViewButtonCommit();
        void IncrementHistoryViewToolbarButtonFetch();
        void IncrementHistoryViewToolbarButtonPush();
        void IncrementHistoryViewToolbarButtonPull();
        void IncrementAuthenticationViewButtonAuthentication();
        void IncrementProjectsInitialized();
        void IncrementBranchesViewButtonCreateBranch();
        void IncrementBranchesViewButtonDeleteBranch();
        void IncrementBranchesViewButtonCheckoutLocalBranch();
        void IncrementBranchesViewButtonCheckoutRemoteBranch();
        void IncrementSettingsViewUnlockButtonLfsUnlock();
        void IncrementAssetExplorerContextMenuLfsLock();
        void IncrementAssetExplorerContextMenuLfsUnlock();
    }
}
