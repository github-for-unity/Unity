using System.Collections.Generic;
using System.Linq;
using System.Threading;
using GitHub.Unity;
using NSubstitute;

namespace TestUtils.Events
{
    interface IRepositoryListener
    {
        void OnStatusChanged(GitStatus status);
        void OnCurrentBranchChanged(string branch);
        void OnCurrentRemoteChanged(string remote);
        void OnLocalBranchListChanged();
        void OnRemoteBranchListChanged();
        void OnHeadChanged();
        void OnLocksChanged(IEnumerable<GitLock> locks);
        void OnRepositoryInfoChanged();
    }

    class RepositoryEvents
    {
        public EventWaitHandle OnStatusChanged { get; } = new AutoResetEvent(false);
        public EventWaitHandle OnCurrentBranchChanged { get; } = new AutoResetEvent(false);
        public EventWaitHandle OnCurrentRemoteChanged { get; } = new AutoResetEvent(false);
        public EventWaitHandle OnLocalBranchListChanged { get; } = new AutoResetEvent(false);
        public EventWaitHandle OnRemoteBranchListChanged { get; } = new AutoResetEvent(false);
        public EventWaitHandle OnHeadChanged { get; } = new AutoResetEvent(false);
        public EventWaitHandle OnLocksChanged { get; } = new AutoResetEvent(false);
        public EventWaitHandle OnRepositoryInfoChanged { get; } = new AutoResetEvent(false);

        public void Reset()
        {
            OnStatusChanged.Reset();
            OnCurrentBranchChanged.Reset();
            OnCurrentRemoteChanged.Reset();
            OnLocalBranchListChanged.Reset();
            OnRemoteBranchListChanged.Reset();
            OnHeadChanged.Reset();
            OnLocksChanged.Reset();
            OnRepositoryInfoChanged.Reset();
        }
    }

    static class RepositoryListenerExtensions
    {
        public static void AttachListener(this IRepositoryListener listener,
            IRepository repository, RepositoryEvents repositoryEvents = null, bool trace = true)
        {
            var logger = trace ? Logging.GetLogger<IRepositoryListener>() : null;

            //TODO: Figure this out
            //repository.OnStatusChanged += gitStatus =>
            //{
            //    logger?.Trace("OnStatusChanged: {0}", gitStatus);
            //    listener.OnStatusChanged(gitStatus);
            //    repositoryEvents?.OnStatusChanged.Set();
            //};

            repository.OnCurrentBranchChanged += name =>
            {
                logger?.Debug("OnCurrentBranchChanged: {0}", name);
                listener.OnCurrentBranchChanged(name);
                repositoryEvents?.OnCurrentBranchChanged.Set();
            };

            repository.OnCurrentRemoteChanged += name =>
            {
                logger?.Debug("OnCurrentRemoteChanged: {0}", name);
                listener.OnCurrentRemoteChanged(name);
                repositoryEvents?.OnCurrentRemoteChanged.Set();
            };

            repository.OnLocalBranchListChanged += () =>
            {
                logger?.Debug("OnLocalBranchListChanged");
                listener.OnLocalBranchListChanged();
                repositoryEvents?.OnLocalBranchListChanged.Set();
            };

            repository.OnRemoteBranchListChanged += () =>
            {
                logger?.Debug("OnRemoteBranchListChanged");
                listener.OnRemoteBranchListChanged();
                repositoryEvents?.OnRemoteBranchListChanged.Set();
            };

            repository.OnCurrentBranchUpdated += () =>
            {
                logger?.Debug("OnHeadChanged");
                listener.OnHeadChanged();
                repositoryEvents?.OnHeadChanged.Set();
            };

            repository.OnLocksChanged += locks =>
            {
                logger?.Debug("OnLocksChanged: {0}", locks);
                listener.OnLocksChanged(locks);
                repositoryEvents?.OnLocksChanged.Set();
            };

            repository.OnRepositoryInfoChanged += () =>
            {
                logger?.Debug("OnRepositoryInfoChanged");
                listener.OnRepositoryInfoChanged();
                repositoryEvents?.OnRepositoryInfoChanged.Set();
            };
        }

        public static void AssertDidNotReceiveAnyCalls(this IRepositoryListener repositoryListener)
        {
            repositoryListener.DidNotReceive().OnStatusChanged(Args.GitStatus);
            repositoryListener.DidNotReceive().OnCurrentBranchChanged(Args.String);
            repositoryListener.DidNotReceive().OnCurrentRemoteChanged(Args.String);
            repositoryListener.DidNotReceive().OnLocalBranchListChanged();
            repositoryListener.DidNotReceive().OnHeadChanged();
            repositoryListener.DidNotReceive().OnLocksChanged(Arg.Any<IEnumerable<GitLock>>());
            repositoryListener.DidNotReceive().OnRepositoryInfoChanged();
        }
    }
};