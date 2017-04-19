namespace GitHub.Unity
{
    public class UsageModel
    {
        public bool IsGitHubUser { get; set; }
        public bool IsEnterpriseUser { get; set; }
        public string AppVersion { get; set; }
        public string UnityVersion { get; set; }
        public string Lang { get; set; }
        public int NumberOfStartups { get; set; }
        public int NumberOfStartupsWeek { get; set; }
        public int NumberOfStartupsMonth { get; set; }
        public UsageModel Clone(bool includeWeekly, bool includeMonthly)
        {
            return new UsageModel
            {
                IsGitHubUser = IsGitHubUser,
                IsEnterpriseUser = IsEnterpriseUser,
                AppVersion = AppVersion,
                UnityVersion = UnityVersion,
                Lang = Lang,
                NumberOfStartups = NumberOfStartups,
                NumberOfStartupsWeek = includeWeekly ? NumberOfStartupsWeek : 0,
                NumberOfStartupsMonth = includeMonthly ? NumberOfStartupsMonth : 0,
            };
        }
    }
}
