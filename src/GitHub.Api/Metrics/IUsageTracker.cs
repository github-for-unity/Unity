using System.Threading.Tasks;

namespace GitHub.Unity
{
    public interface IUsageTracker
    {
        Task IncrementLaunchCount();
        Task IncrementCloneCount();
        Task IncrementCommitCount();
        Task IncrementCreateCount();
        Task IncrementPublishCount();
        Task IncrementOpenInGitHubCount();
        Task IncrementLinkToGitHubCount();
        Task IncrementCreateGistCount();
        Task IncrementUpstreamPullRequestCount();
        Task IncrementLoginCount();
        Task IncrementPullRequestCheckOutCount(bool fork);
        Task IncrementPullRequestPullCount(bool fork);
        Task IncrementPullRequestPushCount(bool fork);
        Task IncrementFetchCount();
        Task IncrementPullCount();
        Task IncrementPushCount();
        Task IncrementLockCount();
        Task IncrementUnlockCount();
    }

    class NullUsageTracker : IUsageTracker
    {
        private static ILogging logger = Logging.GetLogger<NullUsageTracker>();

        public Task IncrementLaunchCount()
        {
            return FromVoidResult();
        }

        public Task IncrementCloneCount()
        {
            logger.Trace("IncrementCloneCount");
            return FromVoidResult();
        }

        public Task IncrementCommitCount()
        {
            logger.Trace("IncrementCommitCount");
            return FromVoidResult();
        }

        public Task IncrementCreateCount()
        {
            logger.Trace("IncrementCreateCount");
            return FromVoidResult();
        }

        public Task IncrementPublishCount()
        {
            logger.Trace("IncrementPublishCount");
            return FromVoidResult();
        }

        public Task IncrementOpenInGitHubCount()
        {
            logger.Trace("IncrementOpenInGitHubCount");
            return FromVoidResult();
        }

        public Task IncrementLinkToGitHubCount()
        {
            logger.Trace("IncrementLinkToGitHubCount");
            return FromVoidResult();
        }

        public Task IncrementCreateGistCount()
        {
            logger.Trace("IncrementCreateGistCount");
            return FromVoidResult();
        }

        public Task IncrementUpstreamPullRequestCount()
        {
            logger.Trace("IncrementUpstreamPullRequestCount");
            return FromVoidResult();
        }

        public Task IncrementLoginCount()
        {
            logger.Trace("IncrementLoginCount");
            return FromVoidResult();
        }

        public Task IncrementPullRequestCheckOutCount(bool fork)
        {
            logger.Trace("IncrementPullRequestCheckOutCount");
            return FromVoidResult();
        }

        public Task IncrementPullRequestPullCount(bool fork)
        {
            logger.Trace("IncrementPullRequestPullCount");
            return FromVoidResult();
        }

        public Task IncrementPullRequestPushCount(bool fork)
        {
            logger.Trace("IncrementPullRequestPushCount");
            return FromVoidResult();
        }

        public Task IncrementFetchCount()
        {
            logger.Trace("IncrementFetchCount");
            return FromVoidResult();
        }

        public Task IncrementPullCount()
        {
            logger.Trace("IncrementPullCount");
            return FromVoidResult();
        }

        public Task IncrementPushCount()
        {
            logger.Trace("IncrementPushCount");
            return FromVoidResult();
        }

        public Task IncrementLockCount()
        {
            logger.Trace("IncrementLockCount");
            return FromVoidResult();
        }

        public Task IncrementUnlockCount()
        {
            logger.Trace("IncrementUnlockCount");
            return FromVoidResult();
        }

        private static Task FromVoidResult()
        {
            return TaskEx.FromResult(0).ContinueWith(task => task.Wait());
        }
    }
}
