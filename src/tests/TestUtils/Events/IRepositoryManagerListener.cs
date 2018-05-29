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
        internal TaskCompletionSource<object> isBusy;
        public Task IsBusy => isBusy.Task;
        internal TaskCompletionSource<object> isNotBusy;
        public Task IsNotBusy => isNotBusy.Task;
        internal TaskCompletionSource<object> currentBranchUpdated;
        public Task CurrentBranchUpdated => currentBranchUpdated.Task;
        internal TaskCompletionSource<object> gitAheadBehindStatusUpdated;
        public Task GitAheadBehindStatusUpdated => gitAheadBehindStatusUpdated.Task;
        internal TaskCompletionSource<object> gitStatusUpdated;
        public Task GitStatusUpdated => gitStatusUpdated.Task;
        internal TaskCompletionSource<object> gitLocksUpdated;
        public Task GitLocksUpdated => gitLocksUpdated.Task;
        internal TaskCompletionSource<object> gitLogUpdated;
        public Task GitLogUpdated => gitLogUpdated.Task;
        internal TaskCompletionSource<object> localBranchesUpdated;
        public Task LocalBranchesUpdated => localBranchesUpdated.Task;
        internal TaskCompletionSource<object> remoteBranchesUpdated;
        public Task RemoteBranchesUpdated => remoteBranchesUpdated.Task;


        public RepositoryManagerEvents()
        {
            Reset();
        }

        public void Reset()
        {
            isBusy = new TaskCompletionSource<object>();
            isNotBusy = new TaskCompletionSource<object>();
            currentBranchUpdated = new TaskCompletionSource<object>();
            gitAheadBehindStatusUpdated = new TaskCompletionSource<object>();
            gitStatusUpdated = new TaskCompletionSource<object>();
            gitLocksUpdated = new TaskCompletionSource<object>();
            gitLogUpdated = new TaskCompletionSource<object>();
            localBranchesUpdated = new TaskCompletionSource<object>();
            remoteBranchesUpdated = new TaskCompletionSource<object>();
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

            repositoryManager.CurrentBranchUpdated += (configBranch, configRemote, head) => {
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