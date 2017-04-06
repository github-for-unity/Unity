using GitHub.Unity;
using NSubstitute;

namespace TestUtils.Events
{
    interface IRepositoryManagerListener
    {
        void OnRepositoryChanged(GitStatus obj);
        void OnActiveBranchChanged();
        void OnActiveRemoteChanged();
        void OnHeadChanged();
        void OnLocalBranchListChanged();
        void OnRemoteBranchListChanged();
        void OnRemoteOrTrackingChanged();
        void OnIsBusyChanged(bool obj);
    }

    static class RepositoryManagerListenerExtensions
    {
        public static void AttachListener(this IRepositoryManagerListener listener, IRepositoryManager repositoryManager)
        {
            repositoryManager.OnIsBusyChanged += listener.OnIsBusyChanged;
            repositoryManager.OnRepositoryChanged += listener.OnRepositoryChanged;
            repositoryManager.OnActiveBranchChanged += listener.OnActiveBranchChanged;
            repositoryManager.OnActiveRemoteChanged += listener.OnActiveRemoteChanged;
            repositoryManager.OnHeadChanged += listener.OnHeadChanged;
            repositoryManager.OnLocalBranchListChanged += listener.OnLocalBranchListChanged;
            repositoryManager.OnRemoteBranchListChanged += listener.OnRemoteBranchListChanged;
            repositoryManager.OnRemoteOrTrackingChanged += listener.OnRemoteOrTrackingChanged;
        }

        public static void AssertDidNotReceiveAnyCalls(this IRepositoryManagerListener repositoryManagerListener)
        {
            repositoryManagerListener.DidNotReceive().OnRepositoryChanged(Args.GitStatus);
            repositoryManagerListener.DidNotReceive().OnActiveBranchChanged();
            repositoryManagerListener.DidNotReceive().OnActiveRemoteChanged();
            repositoryManagerListener.DidNotReceive().OnHeadChanged();
            repositoryManagerListener.DidNotReceive().OnLocalBranchListChanged();
            repositoryManagerListener.DidNotReceive().OnRemoteBranchListChanged();
            repositoryManagerListener.DidNotReceive().OnRemoteOrTrackingChanged();
        }
    }
}