using System;
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
        void OnLocalBranchListUpdated(Dictionary<string, ConfigBranch> branchList);
        void OnRemoteBranchListUpdated(Dictionary<string, ConfigRemote> remotesList, Dictionary<string, Dictionary<string, ConfigBranch>> remoteBranchList);
        void OnLocalBranchUpdated(string name);
        void OnCurrentBranchAndRemoteUpdated(ConfigBranch? configBranch, ConfigRemote? configRemote);
    }

    class RepositoryManagerEvents
    {
        public EventWaitHandle OnIsBusy { get; } = new AutoResetEvent(false);
        public EventWaitHandle OnIsNotBusy { get; } = new AutoResetEvent(false);
        public EventWaitHandle OnStatusUpdated { get; } = new AutoResetEvent(false);
        public EventWaitHandle OnLocksUpdated { get; } = new AutoResetEvent(false);
        public EventWaitHandle OnCurrentBranchAndRemoteUpdated { get; } = new AutoResetEvent(false);
        public EventWaitHandle OnHeadUpdated { get; } = new AutoResetEvent(false);
        public EventWaitHandle OnLocalBranchListUpdated { get; } = new AutoResetEvent(false);
        public EventWaitHandle OnRemoteBranchListUpdated { get; } = new AutoResetEvent(false);
        public EventWaitHandle OnLocalBranchUpdated { get; } = new AutoResetEvent(false);
        public EventWaitHandle OnLocalBranchAdded { get; } = new AutoResetEvent(false);
        public EventWaitHandle OnLocalBranchRemoved { get; } = new AutoResetEvent(false);
        public EventWaitHandle OnRemoteBranchAdded { get; } = new AutoResetEvent(false);
        public EventWaitHandle OnRemoteBranchRemoved { get; } = new AutoResetEvent(false);

        public void Reset()
        {
            OnIsBusy.Reset();
            OnIsNotBusy.Reset();
            OnStatusUpdated.Reset();
            OnLocksUpdated.Reset();
            OnCurrentBranchAndRemoteUpdated.Reset();
            OnHeadUpdated.Reset();
            OnLocalBranchListUpdated.Reset();
            OnRemoteBranchListUpdated.Reset();
            OnLocalBranchUpdated.Reset();
            OnLocalBranchAdded.Reset();
            OnLocalBranchRemoved.Reset();
            OnRemoteBranchAdded.Reset();
            OnRemoteBranchRemoved.Reset();
        }

        public void WaitForNotBusy(int seconds = 1)
        {
            OnIsBusy.WaitOne(TimeSpan.FromSeconds(seconds));
            OnIsNotBusy.WaitOne(TimeSpan.FromSeconds(seconds));
        }

        public void WaitForStatusUpdated(int seconds = 1)
        {
            OnStatusUpdated.WaitOne(TimeSpan.FromSeconds(seconds));
        }

        public void WaitForHeadUpdated(int seconds = 1)
        {
            OnHeadUpdated.WaitOne(TimeSpan.FromSeconds(seconds));
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

            repositoryManager.OnCurrentBranchAndRemoteUpdated += (configBranch, configRemote) => {
                logger?.Trace("OnCurrentBranchAndRemoteUpdated");
                listener.OnCurrentBranchAndRemoteUpdated(configBranch, configRemote);
                managerEvents?.OnCurrentBranchAndRemoteUpdated.Set();
            };

            repositoryManager.OnLocalBranchListUpdated += branchList => {
                logger?.Trace("OnLocalBranchListUpdated");
                listener.OnLocalBranchListUpdated(branchList);
                managerEvents?.OnLocalBranchListUpdated.Set();
            };

            repositoryManager.OnRemoteBranchListUpdated += (remotesList, branchList) => {
                logger?.Trace("OnRemoteBranchListUpdated");
                listener.OnRemoteBranchListUpdated(remotesList, branchList);
                managerEvents?.OnRemoteBranchListUpdated.Set();
            };

            repositoryManager.OnLocalBranchUpdated += name => {
                logger?.Trace("OnLocalBranchUpdated Name:{0}", name);
                listener.OnLocalBranchUpdated(name);
                managerEvents?.OnLocalBranchUpdated.Set();
            };
        }

        public static void AssertDidNotReceiveAnyCalls(this IRepositoryManagerListener repositoryManagerListener)
        {
            repositoryManagerListener.DidNotReceive().OnIsBusyChanged(Args.Bool);
            repositoryManagerListener.DidNotReceive().OnStatusUpdated(Args.GitStatus);
            repositoryManagerListener.DidNotReceive().OnLocksUpdated(Args.EnumerableGitLock);
            repositoryManagerListener.DidNotReceive().OnCurrentBranchAndRemoteUpdated(Arg.Any<ConfigBranch?>(), Arg.Any<ConfigRemote?>());
            repositoryManagerListener.DidNotReceive().OnLocalBranchListUpdated(Arg.Any<Dictionary<string, ConfigBranch>>());
            repositoryManagerListener.DidNotReceive().OnRemoteBranchListUpdated(Arg.Any<Dictionary<string, ConfigRemote>>(), Arg.Any<Dictionary<string, Dictionary<string, ConfigBranch>>>());
            repositoryManagerListener.DidNotReceive().OnLocalBranchUpdated(Args.String);
        }
    }
};