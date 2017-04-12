using System.Threading.Tasks;
using GitHub.Services;

namespace GitHub.Unity
{
    public class UsageTrackerDispatcher : IUsageTracker
    {
        private readonly IUsageTracker usageTracker;

        public UsageTrackerDispatcher(IUsageTracker usageTracker)
        {
            this.usageTracker = usageTracker;
        }

        public Task IncrementCloneCount() => usageTracker.IncrementCloneCount();
        public Task IncrementCreateCount() => usageTracker.IncrementCreateCount();
        public Task IncrementCreateGistCount() => usageTracker.IncrementCreateGistCount();
        public Task IncrementLaunchCount() => usageTracker.IncrementLaunchCount();
        public Task IncrementLinkToGitHubCount() => usageTracker.IncrementLinkToGitHubCount();
        public Task IncrementLoginCount() => usageTracker.IncrementLoginCount();
        public Task IncrementOpenInGitHubCount() => usageTracker.IncrementOpenInGitHubCount();
        public Task IncrementPublishCount() => usageTracker.IncrementPublishCount();
        public Task IncrementUpstreamPullRequestCount() => usageTracker.IncrementUpstreamPullRequestCount();
        public Task IncrementPullRequestCheckOutCount(bool fork) => usageTracker.IncrementPullRequestCheckOutCount(fork);
        public Task IncrementPullRequestPullCount(bool fork) => usageTracker.IncrementPullRequestPullCount(fork);
        public Task IncrementPullRequestPushCount(bool fork) => usageTracker.IncrementPullRequestPushCount(fork);
    }
}
