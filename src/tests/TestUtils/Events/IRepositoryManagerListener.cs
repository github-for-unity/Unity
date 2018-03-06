using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using GitHub.Unity;
using NSubstitute;
using GitHub.Logging;
using NUnit.Framework;

namespace TestUtils.Events
{
    interface IRepositoryManagerListener
    {
        void OnIsBusyChanged(bool busy);
        void LocalBranchesUpdated(Dictionary<string, ConfigBranch> branchList);
        void RemoteBranchesUpdated(Dictionary<string, ConfigRemote> remotesList, Dictionary<string, Dictionary<string, ConfigBranch>> remoteBranchList);
        void CurrentBranchUpdated(ConfigBranch? configBranch, ConfigRemote? configRemote);
        void GitLocksUpdated(List<GitLock> gitLocks);
        void GitAheadBehindStatusUpdated(GitAheadBehindStatus gitAheadBehindStatus);
        void GitStatusUpdated(GitStatus gitStatus);
        void GitLogUpdated(List<GitLogEntry> gitLogEntries);
    }

    class RepositoryManagerEvents
    {
        public EventWaitHandle IsBusy { get; } = new AutoResetEvent(false);
        public EventWaitHandle IsNotBusy { get; } = new AutoResetEvent(false);
        public EventWaitHandle CurrentBranchUpdated { get; } = new AutoResetEvent(false);
        public EventWaitHandle GitAheadBehindStatusUpdated { get; } = new AutoResetEvent(false);
        public EventWaitHandle GitStatusUpdated { get; } = new AutoResetEvent(false);
        public EventWaitHandle GitLocksUpdated { get; } = new AutoResetEvent(false);
        public EventWaitHandle GitLogUpdated { get; } = new AutoResetEvent(false);
        public EventWaitHandle LocalBranchesUpdated { get; } = new AutoResetEvent(false);
        public EventWaitHandle RemoteBranchesUpdated { get; } = new AutoResetEvent(false);

        public void Reset()
        {
            IsBusy.Reset();
            IsNotBusy.Reset();
            CurrentBranchUpdated.Reset();
            GitAheadBehindStatusUpdated.Reset();
            GitStatusUpdated.Reset();
            GitLocksUpdated.Reset();
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
            var logger = trace ? LogHelper.GetLogger<IRepositoryManagerListener>() : null;

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

            repositoryManager.GitLocksUpdated += gitLocks => {
                logger?.Trace("GitLocksUpdated");
                listener.GitLocksUpdated(gitLocks);
                managerEvents?.GitLocksUpdated.Set();
            };

            repositoryManager.GitAheadBehindStatusUpdated += gitAheadBehindStatus => {
                logger?.Trace("GitAheadBehindStatusUpdated");
                listener.GitAheadBehindStatusUpdated(gitAheadBehindStatus);
                managerEvents?.GitAheadBehindStatusUpdated.Set();
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
            Assert.That(repositoryManagerListener.ReceivedCalls().Count() == 0);
            //repositoryManagerListener.DidNotReceive().OnIsBusyChanged(Args.Bool);
            //repositoryManagerListener.DidNotReceive().CurrentBranchUpdated(Args.NullableConfigBranch, Args.NullableConfigRemote);
            //repositoryManagerListener.DidNotReceive().GitAheadBehindStatusUpdated(Args.GitAheadBehindStatus);
            //repositoryManagerListener.DidNotReceive().GitStatusUpdated(Args.GitStatus);
            //repositoryManagerListener.DidNotReceive().GitLocksUpdated(Args.GitLocks);
            //repositoryManagerListener.DidNotReceive().GitLogUpdated(Args.GitLogs);
            //repositoryManagerListener.DidNotReceive().LocalBranchesUpdated(Args.LocalBranchDictionary);
            //repositoryManagerListener.DidNotReceive().RemoteBranchesUpdated(Args.RemoteDictionary, Args.RemoteBranchDictionary);
        }
    }
};