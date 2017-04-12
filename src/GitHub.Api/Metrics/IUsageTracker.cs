using System.Threading.Tasks;

namespace GitHub.Unity
{
    public interface IUsageTracker
    {
        Task IncrementLaunchCount();
        Task IncrementCloneCount();
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
    }
}
