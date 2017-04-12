using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel.Composition;
using Task = System.Threading.Tasks.Task;

namespace GitHub.Services
{
    [Export(typeof(IUsageTracker))]
    [PartCreationPolicy(CreationPolicy.NonShared)]
    public class UsageTrackerDispatcher : IUsageTracker
    {
        readonly IUsageTracker inner;

        [ImportingConstructor]
        public UsageTrackerDispatcher([Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider)
        {
            inner = serviceProvider.GetService(typeof(IUsageTracker)) as IUsageTracker;
        }

        public Task IncrementCloneCount() => inner.IncrementCloneCount();
        public Task IncrementCreateCount() => inner.IncrementCreateCount();
        public Task IncrementCreateGistCount() => inner.IncrementCreateGistCount();
        public Task IncrementLaunchCount() => inner.IncrementLaunchCount();
        public Task IncrementLinkToGitHubCount() => inner.IncrementLinkToGitHubCount();
        public Task IncrementLoginCount() => inner.IncrementLoginCount();
        public Task IncrementOpenInGitHubCount() => inner.IncrementOpenInGitHubCount();
        public Task IncrementPublishCount() => inner.IncrementPublishCount();
        public Task IncrementUpstreamPullRequestCount() => inner.IncrementUpstreamPullRequestCount();
        public Task IncrementPullRequestCheckOutCount(bool fork) => inner.IncrementPullRequestCheckOutCount(fork);
        public Task IncrementPullRequestPullCount(bool fork) => inner.IncrementPullRequestPullCount(fork);
        public Task IncrementPullRequestPushCount(bool fork) => inner.IncrementPullRequestPushCount(fork);
    }
}
