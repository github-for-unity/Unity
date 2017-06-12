using System.Collections.Generic;
using System.Linq;
using System.Threading;
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
        void OnLocksUpdated(IEnumerable<GitLock> locks);
    }

    class RepositoryManagerAutoResetEvent
    {
        public AutoResetEvent OnIsBusyChanged { get; } = new AutoResetEvent(false);
        public AutoResetEvent OnRepositoryChanged { get; } = new AutoResetEvent(false);
        public AutoResetEvent OnActiveBranchChanged { get; } = new AutoResetEvent(false);
        public AutoResetEvent OnActiveRemoteChanged { get; } = new AutoResetEvent(false);
        public AutoResetEvent OnHeadChanged { get; } = new AutoResetEvent(false);
        public AutoResetEvent OnLocalBranchListChanged { get; } = new AutoResetEvent(false);
        public AutoResetEvent OnRemoteBranchListChanged { get; } = new AutoResetEvent(false);
        public AutoResetEvent OnRemoteOrTrackingChanged { get; } = new AutoResetEvent(false);
        public AutoResetEvent OnLocksUpdated { get; } = new AutoResetEvent(false);
    }

    static class RepositoryManagerListenerExtensions
    {
        public static void AttachListener(this IRepositoryManagerListener listener, IRepositoryManager repositoryManager,
            RepositoryManagerAutoResetEvent managerAutoResetEvent = null, bool trace = false)
        {
            var logger = trace ? Logging.GetLogger<IRepositoryManagerListener>() : null;

            repositoryManager.OnIsBusyChanged += b => {
                logger?.Trace("OnIsBusyChanged: {0}", b);
                listener.OnIsBusyChanged(b);
                managerAutoResetEvent?.OnIsBusyChanged.Set();
            };

            repositoryManager.OnRepositoryChanged += status => {
                logger?.Debug("OnRepositoryChanged: {0}", status);
                listener.OnRepositoryChanged(status);
                managerAutoResetEvent?.OnRepositoryChanged.Set();
            };

            repositoryManager.OnActiveBranchChanged += () => {
                logger?.Trace("OnActiveBranchChanged");
                listener.OnActiveBranchChanged();
                managerAutoResetEvent?.OnActiveBranchChanged.Set();
            };

            repositoryManager.OnActiveRemoteChanged += () => {
                logger?.Trace("OnActiveRemoteChanged");
                listener.OnActiveRemoteChanged();
                managerAutoResetEvent?.OnActiveRemoteChanged.Set();
            };

            repositoryManager.OnHeadChanged += () => {
                logger?.Trace("OnHeadChanged");
                listener.OnHeadChanged();
                managerAutoResetEvent?.OnHeadChanged.Set();
            };

            repositoryManager.OnLocalBranchListChanged += () => {
                logger?.Trace("OnLocalBranchListChanged");
                listener.OnLocalBranchListChanged();
                managerAutoResetEvent?.OnLocalBranchListChanged.Set();
            };

            repositoryManager.OnRemoteBranchListChanged += () => {
                logger?.Trace("OnRemoteBranchListChanged");
                listener.OnRemoteBranchListChanged();
                managerAutoResetEvent?.OnRemoteBranchListChanged.Set();
            };

            repositoryManager.OnRemoteOrTrackingChanged += () => {
                logger?.Trace("OnRemoteOrTrackingChanged");
                listener.OnRemoteOrTrackingChanged();
                managerAutoResetEvent?.OnRemoteOrTrackingChanged.Set();
            };

            repositoryManager.OnLocksUpdated += locks => {
                var lockArray = locks.ToArray();
                logger?.Trace("OnLocksUpdated Count:{0}", lockArray.Length);
                listener.OnLocksUpdated(lockArray);
                managerAutoResetEvent?.OnLocksUpdated.Set();
            };
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
            repositoryManagerListener.DidNotReceive().OnLocksUpdated(Args.EnumerableGitLock);
        }
    }
}