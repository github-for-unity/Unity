namespace GitHub.Unity
{
    public interface IUsageTracker
    {
        bool Enabled { get; set; }
        void IncrementNumberOfStartups();
        void IncrementChangesViewButtonCommit();
        void IncrementHistoryViewToolbarFetch();
        void IncrementHistoryViewToolbarPush();
        void IncrementHistoryViewToolbarPull();
        void IncrementAuthenticationViewButtonAuthentication();
        void IncrementProjectsInitialized();
        void IncrementBranchesViewButtonCreateBranch();
        void IncrementBranchesViewButtonDeleteBranch();
        void IncrementBranchesViewButtonCheckoutLocalBranch();
        void IncrementBranchesViewButtonCheckoutRemoteBranch();
        void IncrementSettingsViewButtonLfsUnlock();
        void IncrementUnityProjectViewContextLfsLock();
        void IncrementUnityProjectViewContextLfsUnlock();
        void IncrementPublishViewButtonPublish();
        void IncrementApplicationMenuMenuItemCommandLine();
        void UpdateRepoSize(int kilobytes);
    }
}
