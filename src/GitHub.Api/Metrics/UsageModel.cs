using System;

namespace GitHub.Models
{
    public class UsageModel
    {
        public bool IsGitHubUser { get; set; }
        public bool IsEnterpriseUser { get; set; }
        public string AppVersion { get; set; }
        public string VSVersion { get; set; }
        public string Lang { get; set; }
        public int NumberOfStartups { get; set; }
        public int NumberOfStartupsWeek { get; set; }
        public int NumberOfStartupsMonth { get; set; }
        public int NumberOfUpstreamPullRequests { get; set; }
        public int NumberOfClones { get; set; }
        public int NumberOfReposCreated { get; set; }
        public int NumberOfReposPublished { get; set; }
        public int NumberOfGists { get; set; }
        public int NumberOfOpenInGitHub { get; set; }
        public int NumberOfLinkToGitHub { get; set; }
        public int NumberOfLogins { get; set; }
        public int NumberOfPullRequestsOpened { get; set; }
        public int NumberOfLocalPullRequestsCheckedOut { get; set; }
        public int NumberOfLocalPullRequestPulls { get; set; }
        public int NumberOfLocalPullRequestPushes { get; set; }
        public int NumberOfForkPullRequestsCheckedOut { get; set; }
        public int NumberOfForkPullRequestPulls { get; set; }
        public int NumberOfForkPullRequestPushes { get; set; }

        public UsageModel Clone(bool includeWeekly, bool includeMonthly)
        {
            return new UsageModel
            {
                IsGitHubUser = IsGitHubUser,
                IsEnterpriseUser = IsEnterpriseUser,
                AppVersion = AppVersion,
                VSVersion = VSVersion,
                Lang = Lang,
                NumberOfStartups = NumberOfStartups,
                NumberOfStartupsWeek = includeWeekly ? NumberOfStartupsWeek : 0,
                NumberOfStartupsMonth = includeMonthly ? NumberOfStartupsMonth : 0,
                NumberOfUpstreamPullRequests = NumberOfUpstreamPullRequests,
                NumberOfClones = NumberOfClones,
                NumberOfReposCreated = NumberOfReposCreated,
                NumberOfReposPublished = NumberOfReposPublished,
                NumberOfGists = NumberOfGists,
                NumberOfOpenInGitHub = NumberOfOpenInGitHub,
                NumberOfLinkToGitHub = NumberOfLinkToGitHub,
                NumberOfLogins = NumberOfLogins,
                NumberOfPullRequestsOpened = NumberOfPullRequestsOpened,
                NumberOfLocalPullRequestsCheckedOut = NumberOfLocalPullRequestsCheckedOut,
                NumberOfLocalPullRequestPulls = NumberOfLocalPullRequestPulls,
                NumberOfLocalPullRequestPushes = NumberOfLocalPullRequestPushes,
                NumberOfForkPullRequestsCheckedOut = NumberOfForkPullRequestsCheckedOut,
                NumberOfForkPullRequestPulls = NumberOfForkPullRequestPulls,
                NumberOfForkPullRequestPushes = NumberOfForkPullRequestPushes,
            };
        }
    }
}
