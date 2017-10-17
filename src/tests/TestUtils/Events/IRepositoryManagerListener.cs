using System.Collections.Generic;
using System.Linq;
using System.Threading;
using GitHub.Unity;
using NSubstitute;

namespace TestUtils.Events
{
    interface IRepositoryManagerListener
    {
        void OnStatusUpdate(GitStatus status);
        void OnActiveBranchChanged(ConfigBranch? branch);
        void OnActiveRemoteChanged(ConfigRemote? remote);
        void OnLocalBranchListChanged();
        void OnRemoteBranchListChanged();
        void OnIsBusyChanged(bool busy);
        void OnLocksUpdated(IEnumerable<GitLock> locks);
    }

    class RepositoryManagerEvents
    {
        public EventWaitHandle OnIsBusy { get; } = new ManualResetEvent(false);
        public EventWaitHandle OnIsNotBusy { get; } = new ManualResetEvent(false);
        public AutoResetEvent OnStatusUpdate { get; } = new AutoResetEvent(false);
        public EventWaitHandle OnActiveBranchChanged { get; } = new ManualResetEvent(false);
        public EventWaitHandle OnActiveRemoteChanged { get; } = new ManualResetEvent(false);
        public EventWaitHandle OnLocalBranchListChanged { get; } = new ManualResetEvent(false);
        public EventWaitHandle OnRemoteBranchListChanged { get; } = new ManualResetEvent(false);
        public EventWaitHandle OnLocksUpdated { get; } = new ManualResetEvent(false);

        public void Reset()
        {
            OnIsBusy.Reset();
            OnIsNotBusy.Reset();
            OnStatusUpdate.Reset();
            OnActiveBranchChanged.Reset();
            OnActiveRemoteChanged.Reset();
            OnLocalBranchListChanged.Reset();
            OnRemoteBranchListChanged.Reset();
            OnLocksUpdated.Reset();
        }
    }

    static class RepositoryManagerListenerExtensions
    {
        public static void AttachListener(this IRepositoryManagerListener listener, IRepositoryManager repositoryManager,
            RepositoryManagerEvents managerEvents = null, bool trace = true)
        {
            var logger = trace ? Logging.GetLogger<IRepositoryManagerListener>() : null;

            repositoryManager.OnIsBusyChanged += b => {
                logger?.Trace("OnIsBusyChanged: {0}", b);
                listener.OnIsBusyChanged(b);
                if (b)
                    managerEvents?.OnIsBusy.Set();
                else
                    managerEvents?.OnIsNotBusy.Set();
            };

            repositoryManager.OnStatusUpdated += status => {
                logger?.Debug("OnStatusUpdated: {0}", status);
                listener.OnStatusUpdate(status);
                managerEvents?.OnStatusUpdate.Set();
            };

            repositoryManager.OnActiveBranchChanged += (branch) => {
                logger?.Trace($"OnActiveBranchChanged {branch}");
                listener.OnActiveBranchChanged(branch);
                managerEvents?.OnActiveBranchChanged.Set();
            };

            repositoryManager.OnActiveRemoteChanged += (remote) => {
                logger?.Trace($"OnActiveRemoteChanged {(remote.HasValue ? remote.Value.Name : null)}");
                listener.OnActiveRemoteChanged(remote);
                managerEvents?.OnActiveRemoteChanged.Set();
            };

            repositoryManager.OnLocalBranchListChanged += () => {
                logger?.Trace("OnLocalBranchListChanged");
                listener.OnLocalBranchListChanged();
                managerEvents?.OnLocalBranchListChanged.Set();
            };

            repositoryManager.OnRemoteBranchListChanged += () => {
                logger?.Trace("OnRemoteBranchListChanged");
                listener.OnRemoteBranchListChanged();
                managerEvents?.OnRemoteBranchListChanged.Set();
            };

            repositoryManager.OnLocksUpdated += locks => {
                var lockArray = locks.ToArray();
                logger?.Trace("OnLocksUpdated Count:{0}", lockArray.Length);
                listener.OnLocksUpdated(lockArray);
                managerEvents?.OnLocksUpdated.Set();
            };
        }

        public static void AssertDidNotReceiveAnyCalls(this IRepositoryManagerListener repositoryManagerListener)
        {
            repositoryManagerListener.DidNotReceive().OnStatusUpdate(Args.GitStatus);
            repositoryManagerListener.DidNotReceive().OnActiveBranchChanged(Arg.Any<ConfigBranch?>());
            repositoryManagerListener.DidNotReceive().OnActiveRemoteChanged(Arg.Any<ConfigRemote?>());
            repositoryManagerListener.DidNotReceive().OnLocalBranchListChanged();
            repositoryManagerListener.DidNotReceive().OnRemoteBranchListChanged();
            repositoryManagerListener.DidNotReceive().OnLocksUpdated(Args.EnumerableGitLock);
        }
    }
}