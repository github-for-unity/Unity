using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using GitHub.Unity;
using NSubstitute;
using GitHub.Logging;
using NUnit.Framework;
using System.Threading.Tasks;

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
        internal TaskCompletionSource<bool> isBusy;
        public Task<bool> IsBusy => isBusy.Task;
        internal TaskCompletionSource<bool> isNotBusy;
        public Task<bool> IsNotBusy => isNotBusy.Task;
        internal TaskCompletionSource<bool> currentBranchUpdated;
        public Task<bool> CurrentBranchUpdated => currentBranchUpdated.Task;
        internal TaskCompletionSource<bool> gitAheadBehindStatusUpdated;
        public Task<bool> GitAheadBehindStatusUpdated => gitAheadBehindStatusUpdated.Task;
        internal TaskCompletionSource<bool> gitStatusUpdated;
        public Task<bool> GitStatusUpdated => gitStatusUpdated.Task;
        internal TaskCompletionSource<bool> gitLocksUpdated;
        public Task<bool> GitLocksUpdated => gitLocksUpdated.Task;
        internal TaskCompletionSource<bool> gitLogUpdated;
        public Task<bool> GitLogUpdated => gitLogUpdated.Task;
        internal TaskCompletionSource<bool> localBranchesUpdated;
        public Task<bool> LocalBranchesUpdated => localBranchesUpdated.Task;
        internal TaskCompletionSource<bool> remoteBranchesUpdated;
        public Task<bool> RemoteBranchesUpdated => remoteBranchesUpdated.Task;


        public RepositoryManagerEvents()
        {
            Reset();
        }

        public void Reset()
        {
            isBusy = new TaskCompletionSource<bool>();
            isNotBusy = new TaskCompletionSource<bool>();
            currentBranchUpdated = new TaskCompletionSource<bool>();
            gitAheadBehindStatusUpdated = new TaskCompletionSource<bool>();
            gitStatusUpdated = new TaskCompletionSource<bool>();
            gitLocksUpdated = new TaskCompletionSource<bool>();
            gitLogUpdated = new TaskCompletionSource<bool>();
            localBranchesUpdated = new TaskCompletionSource<bool>();
            remoteBranchesUpdated = new TaskCompletionSource<bool>();
        }

        public async Task WaitForNotBusy(int seconds = 1)
        {
            await TaskEx.WhenAny(IsBusy, TaskEx.Delay(TimeSpan.FromSeconds(seconds)));
            await TaskEx.WhenAny(IsNotBusy, TaskEx.Delay(TimeSpan.FromSeconds(seconds)));
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
                    managerEvents?.isBusy.TrySetResult(true);
                else
                    managerEvents?.isNotBusy.TrySetResult(true);
            };

            repositoryManager.CurrentBranchUpdated += (configBranch, configRemote) => {
                logger?.Trace("CurrentBranchUpdated");
                listener.CurrentBranchUpdated(configBranch, configRemote);
                managerEvents?.currentBranchUpdated.TrySetResult(true);
            };

            repositoryManager.GitLocksUpdated += gitLocks => {
                logger?.Trace("GitLocksUpdated");
                listener.GitLocksUpdated(gitLocks);
                managerEvents?.gitLocksUpdated.TrySetResult(true);
            };

            repositoryManager.GitAheadBehindStatusUpdated += gitAheadBehindStatus => {
                logger?.Trace("GitAheadBehindStatusUpdated");
                listener.GitAheadBehindStatusUpdated(gitAheadBehindStatus);
                managerEvents?.gitAheadBehindStatusUpdated.TrySetResult(true);
            };

            repositoryManager.GitStatusUpdated += gitStatus => {
                logger?.Trace("GitStatusUpdated");
                listener.GitStatusUpdated(gitStatus);
                managerEvents?.gitStatusUpdated.TrySetResult(true);
            };

            repositoryManager.GitLogUpdated += gitLogEntries => {
                logger?.Trace("GitLogUpdated");
                listener.GitLogUpdated(gitLogEntries);
                managerEvents?.gitLogUpdated.TrySetResult(true);
            };

            repositoryManager.LocalBranchesUpdated += branchList => {
                logger?.Trace("LocalBranchesUpdated");
                listener.LocalBranchesUpdated(branchList);
                managerEvents?.localBranchesUpdated.TrySetResult(true);
            };

            repositoryManager.RemoteBranchesUpdated += (remotesList, branchList) => {
                logger?.Trace("RemoteBranchesUpdated");
                listener.RemoteBranchesUpdated(remotesList, branchList);
                managerEvents?.remoteBranchesUpdated.TrySetResult(true);
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