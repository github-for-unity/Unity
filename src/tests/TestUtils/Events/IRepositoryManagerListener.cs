using System.Collections.Generic;
using System.Linq;
using System.Threading;
using GitHub.Unity;
using NSubstitute;

namespace TestUtils.Events
{
    interface IRepositoryManagerListener
    {
        void OnIsBusyChanged(bool busy);
        void OnStatusUpdated(GitStatus status);
        void OnLocksUpdated(IEnumerable<GitLock> locks);
        void OnHeadUpdated(string head);
        void OnLocalBranchListUpdated(Dictionary<string, ConfigBranch> branchList);
        void OnRemoteBranchListUpdated(Dictionary<string, Dictionary<string, ConfigBranch>> remoteBranchList);
        void OnLocalBranchUpdated(string name);
        void OnLocalBranchAdded(string name);
        void OnLocalBranchRemoved(string name);
        void OnRemoteBranchAdded(string origin, string name);
        void OnRemoteBranchRemoved(string origin, string name);
        void OnGitUserLoaded(IUser user);
    }

    class RepositoryManagerEvents
    {
        public EventWaitHandle OnIsBusy { get; } = new ManualResetEvent(false);
        public EventWaitHandle OnIsNotBusy { get; } = new ManualResetEvent(false);
        public EventWaitHandle OnStatusUpdated { get; } = new ManualResetEvent(false);
        public EventWaitHandle OnLocksUpdated { get; } = new ManualResetEvent(false);
        public EventWaitHandle OnHeadUpdated { get; } = new ManualResetEvent(false);
        public EventWaitHandle OnLocalBranchListUpdated { get; } = new ManualResetEvent(false);
        public EventWaitHandle OnRemoteBranchListUpdated { get; } = new ManualResetEvent(false);
        public EventWaitHandle OnLocalBranchUpdated { get; } = new ManualResetEvent(false);
        public EventWaitHandle OnLocalBranchAdded { get; } = new ManualResetEvent(false);
        public EventWaitHandle OnLocalBranchRemoved { get; } = new ManualResetEvent(false);
        public EventWaitHandle OnRemoteBranchAdded { get; } = new ManualResetEvent(false);
        public EventWaitHandle OnRemoteBranchRemoved { get; } = new ManualResetEvent(false);
        public EventWaitHandle OnGitUserLoaded { get; } = new ManualResetEvent(false);

        public void Reset()
        {
            OnIsBusy.Reset();
            OnIsNotBusy.Reset();
            OnStatusUpdated.Reset();
            OnLocksUpdated.Reset();
            OnHeadUpdated.Reset();
            OnLocalBranchListUpdated.Reset();
            OnRemoteBranchListUpdated.Reset();
            OnLocalBranchUpdated.Reset();
            OnLocalBranchAdded.Reset();
            OnLocalBranchRemoved.Reset();
            OnRemoteBranchAdded.Reset();
            OnRemoteBranchRemoved.Reset();
            OnGitUserLoaded.Reset();
        }
    }

    static class RepositoryManagerListenerExtensions
    {
        public static void AttachListener(this IRepositoryManagerListener listener,
            IRepositoryManager repositoryManager, RepositoryManagerEvents managerEvents = null, bool trace = true)
        {
            var logger = trace ? Logging.GetLogger<IRepositoryManagerListener>() : null;

            repositoryManager.OnIsBusyChanged += isBusy => {
                logger?.Trace("OnIsBusyChanged: {0}", isBusy);
                listener.OnIsBusyChanged(isBusy);
                if (isBusy)
                    managerEvents?.OnIsBusy.Set();
                else
                    managerEvents?.OnIsNotBusy.Set();
            };

            repositoryManager.OnStatusUpdated += status => {
                logger?.Debug("OnStatusUpdated: {0}", status);
                listener.OnStatusUpdated(status);
                managerEvents?.OnStatusUpdated.Set();
            };

            repositoryManager.OnLocksUpdated += locks => {
                var lockArray = locks.ToArray();
                logger?.Trace("OnLocksUpdated Count:{0}", lockArray.Length);
                listener.OnLocksUpdated(lockArray);
                managerEvents?.OnLocksUpdated.Set();
            };

            repositoryManager.OnHeadUpdated += head => {
                logger?.Trace("OnHeadUpdated");
                listener.OnHeadUpdated(head);
                managerEvents?.OnHeadUpdated.Set();
            };

            repositoryManager.OnLocalBranchListUpdated += branchList => {
                logger?.Trace("OnLocalBranchListUpdated");
                listener.OnLocalBranchListUpdated(branchList);
                managerEvents?.OnLocalBranchListUpdated.Set();
            };

            repositoryManager.OnRemoteBranchListUpdated += branchList => {
                logger?.Trace("OnRemoteBranchListUpdated");
                listener.OnRemoteBranchListUpdated(branchList);
                managerEvents?.OnRemoteBranchListUpdated.Set();
            };

            repositoryManager.OnLocalBranchUpdated += name => {
                logger?.Trace("OnLocalBranchUpdated Name:{0}", name);
                listener.OnLocalBranchUpdated(name);
                managerEvents?.OnLocalBranchUpdated.Set();
            };

            repositoryManager.OnLocalBranchAdded += name => {
                logger?.Trace("OnLocalBranchAdded Name:{0}", name);
                listener.OnLocalBranchAdded(name);
                managerEvents?.OnLocalBranchAdded.Set();
            };

            repositoryManager.OnLocalBranchRemoved += name => {
                logger?.Trace("OnLocalBranchRemoved Name:{0}", name);
                listener.OnLocalBranchRemoved(name);
                managerEvents?.OnLocalBranchRemoved.Set();
            };

            repositoryManager.OnRemoteBranchAdded += (origin, name) => {
                logger?.Trace("OnRemoteBranchAdded Origin:{0} Name:{1}", origin, name);
                listener.OnRemoteBranchAdded(origin, name);
                managerEvents?.OnRemoteBranchAdded.Set();
            };

            repositoryManager.OnRemoteBranchRemoved += (origin, name) => {
                logger?.Trace("OnRemoteBranchRemoved Origin:{0} Name:{1}", origin, name);
                listener.OnRemoteBranchRemoved(origin, name);
                managerEvents?.OnRemoteBranchRemoved.Set();
            };

            repositoryManager.OnGitUserLoaded += user => {
                logger?.Trace("OnGitUserLoaded Name:{0}", user);
                listener.OnGitUserLoaded(user);
                managerEvents?.OnGitUserLoaded.Set();
            };
        }

        public static void AssertDidNotReceiveAnyCalls(this IRepositoryManagerListener repositoryManagerListener)
        {
            repositoryManagerListener.DidNotReceive().OnIsBusyChanged(Args.Bool);
            repositoryManagerListener.DidNotReceive().OnStatusUpdated(Args.GitStatus);
            repositoryManagerListener.DidNotReceive().OnLocksUpdated(Args.EnumerableGitLock);
            repositoryManagerListener.DidNotReceive().OnHeadUpdated(Args.String);
            repositoryManagerListener.DidNotReceive().OnLocalBranchListUpdated(Arg.Any<Dictionary<string, ConfigBranch>>());
            repositoryManagerListener.DidNotReceive().OnRemoteBranchListUpdated(Arg.Any<Dictionary<string, Dictionary<string, ConfigBranch>>>());
            repositoryManagerListener.DidNotReceive().OnLocalBranchUpdated(Args.String);
            repositoryManagerListener.DidNotReceive().OnLocalBranchAdded(Args.String);
            repositoryManagerListener.DidNotReceive().OnLocalBranchRemoved(Args.String);
            repositoryManagerListener.DidNotReceive().OnRemoteBranchAdded(Args.String, Args.String);
            repositoryManagerListener.DidNotReceive().OnRemoteBranchRemoved(Args.String, Args.String);
            repositoryManagerListener.DidNotReceive().OnGitUserLoaded(Arg.Any<IUser>());
        }
    }
};