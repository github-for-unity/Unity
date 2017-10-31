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
        void OnLocksChanged(IEnumerable<GitLock> locks);
        void OnRepositoryInfoChanged();
    }

    class RepositoryEvents
    {
        public EventWaitHandle OnStatusChanged { get; } = new AutoResetEvent(false);
        public EventWaitHandle OnLocksChanged { get; } = new AutoResetEvent(false);
        public EventWaitHandle OnRepositoryInfoChanged { get; } = new AutoResetEvent(false);

        public void Reset()
        {
            OnStatusChanged.Reset();
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
            repositoryListener.DidNotReceive().OnLocksChanged(Arg.Any<IEnumerable<GitLock>>());
            repositoryListener.DidNotReceive().OnRepositoryInfoChanged();
        }
    }
};