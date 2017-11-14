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
        void LocalBranchesUpdated(Dictionary<string, ConfigBranch> branchList);
        void RemoteBranchesUpdated(Dictionary<string, ConfigRemote> remotesList, Dictionary<string, Dictionary<string, ConfigBranch>> remoteBranchList);
        void CurrentBranchUpdated(ConfigBranch? configBranch, ConfigRemote? configRemote);
        void GitStatusUpdated(GitStatus gitStatus);
        void GitLogUpdated(List<GitLogEntry> gitLogEntries);
    }

    class RepositoryManagerEvents
    {
        public EventWaitHandle IsBusy { get; } = new AutoResetEvent(false);
        public EventWaitHandle IsNotBusy { get; } = new AutoResetEvent(false);
        public EventWaitHandle CurrentBranchUpdated { get; } = new AutoResetEvent(false);
        public EventWaitHandle GitStatusUpdated { get; } = new AutoResetEvent(false);
        public EventWaitHandle GitLogUpdated { get; } = new AutoResetEvent(false);
        public EventWaitHandle LocalBranchesUpdated { get; } = new AutoResetEvent(false);
        public EventWaitHandle RemoteBranchesUpdated { get; } = new AutoResetEvent(false);

        public void Reset()
        {
            IsBusy.Reset();
            IsNotBusy.Reset();
            CurrentBranchUpdated.Reset();
            GitStatusUpdated.Reset();
            GitLogUpdated.Reset();
            LocalBranchesUpdated.Reset();
            RemoteBranchesUpdated.Reset();
        }

        public void WaitForNotBusy(int seconds = 1)
        {
            IsBusy.WaitOne(TimeSpan.FromSeconds(seconds));
            IsNotBusy.WaitOne(TimeSpan.FromSeconds(seconds));
        }
    }

    static class RepositoryManagerListenerExtensions
    {
        public static void AttachListener(this IRepositoryManagerListener listener,
            IRepositoryManager repositoryManager, RepositoryManagerEvents managerEvents = null, bool trace = true)
        {
            var logger = trace ? Logging.GetLogger<IRepositoryManagerListener>() : null;

            repositoryManager.IsBusyChanged += isBusy => {
                logger?.Trace("OnIsBusyChanged: {0}", isBusy);
                listener.OnIsBusyChanged(isBusy);
                if (isBusy)
                    managerEvents?.IsBusy.Set();
                else
                    managerEvents?.IsNotBusy.Set();
            };

            repositoryManager.CurrentBranchUpdated += (configBranch, configRemote) => {
                logger?.Trace("CurrentBranchUpdated");
                listener.CurrentBranchUpdated(configBranch, configRemote);
                managerEvents?.CurrentBranchUpdated.Set();
            };

            repositoryManager.GitStatusUpdated += gitStatus => {
                logger?.Trace("GitStatusUpdated");
                listener.GitStatusUpdated(gitStatus);
                managerEvents?.GitStatusUpdated.Set();
            };

            repositoryManager.GitLogUpdated += gitLogEntries => {
                logger?.Trace("GitLogUpdated");
                listener.GitLogUpdated(gitLogEntries);
                managerEvents?.GitLogUpdated.Set();
            };

            repositoryManager.LocalBranchesUpdated += branchList => {
                logger?.Trace("LocalBranchesUpdated");
                listener.LocalBranchesUpdated(branchList);
                managerEvents?.LocalBranchesUpdated.Set();
            };

            repositoryManager.RemoteBranchesUpdated += (remotesList, branchList) => {
                logger?.Trace("RemoteBranchesUpdated");
                listener.RemoteBranchesUpdated(remotesList, branchList);
                managerEvents?.RemoteBranchesUpdated.Set();
            };
        }

        public static void AssertDidNotReceiveAnyCalls(this IRepositoryManagerListener repositoryManagerListener)
        {
            repositoryManagerListener.DidNotReceive().OnIsBusyChanged(Args.Bool);
            repositoryManagerListener.DidNotReceive().CurrentBranchUpdated(Arg.Any<ConfigBranch?>(), Arg.Any<ConfigRemote?>());
            repositoryManagerListener.DidNotReceive().GitStatusUpdated(Args.GitStatus);
            repositoryManagerListener.DidNotReceive().GitLogUpdated(Args.GitLogs);
            repositoryManagerListener.DidNotReceive().LocalBranchesUpdated(Arg.Any<Dictionary<string, ConfigBranch>>());
            repositoryManagerListener.DidNotReceive().RemoteBranchesUpdated(Arg.Any<Dictionary<string, ConfigRemote>>(), Arg.Any<Dictionary<string, Dictionary<string, ConfigBranch>>>());
        }
    }
};