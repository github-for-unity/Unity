using GitHub.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace GitHub.Unity
{
    public class Usage
    {
        public string InstanceId { get; set; }
        public Dimensions Dimensions { get; set; } = new Dimensions();
        public Measures Measures { get; set; } = new Measures();
    }

    public class Dimensions
    {
        public string Guid { get; set; }
        public DateTimeOffset Date { get; set; }
        public string AppVersion { get; set; }
        public string UnityVersion { get; set; }
        public string Lang { get; set; }
        public string CurrentLang { get; set; }
        public string GitHubUser { get; set; }
    }

    public class Measures
    {
        public int NumberOfStartups { get; set; }
        public int ProjectsInitialized { get; set; }
        public int ChangesViewButtonCommit { get; set; }
        public int HistoryViewToolbarFetch { get; set; }
        public int HistoryViewToolbarPush { get; set; }
        public int HistoryViewToolbarPull { get; set; }
        public int AuthenticationViewButtonAuthentication { get; set; }
        public int BranchesViewButtonCreateBranch { get; set; }
        public int BranchesViewButtonDeleteBranch { get; set; }
        public int BranchesViewButtonCheckoutLocalBranch { get; set; }
        public int BranchesViewButtonCheckoutRemoteBranch { get; set; }
        public int SettingsViewButtonLfsUnlock { get; set; }
        public int UnityProjectViewContextLfsLock { get; set; }
        public int UnityProjectViewContextLfsUnlock { get; set; }
        public int PublishViewButtonPublish { get; set; }
        public int ApplicationMenuMenuItemCommandLine { get; set; }
        public int GitRepoSize { get; set; }
        public int LfsDiskUsage { get; set; }
    }

    class UsageModel
    {
        public List<Usage> Reports { get; set; } = new List<Usage>();
        public string Guid { get; set; }

        private Usage currentUsage;

        public Usage GetCurrentUsage(string appVersion, string unityVersion, string instanceId)
        {
            Guard.ArgumentNotNullOrWhiteSpace(appVersion, "appVersion");
            Guard.ArgumentNotNullOrWhiteSpace(unityVersion, "unityVersion");

            var now = DateTimeOffset.Now;
            if (currentUsage == null)
            {
                currentUsage = Reports
                    .FirstOrDefault(usage => usage.InstanceId == instanceId);
            }

            if (currentUsage == null)
            {
                currentUsage = new Usage
                {
                    InstanceId = instanceId,
                    Dimensions = {
                        Date = now,
                        Guid = Guid,
                        AppVersion = appVersion,
                        UnityVersion = unityVersion,
                        Lang = CultureInfo.InstalledUICulture.IetfLanguageTag,
                        CurrentLang = CultureInfo.CurrentCulture.IetfLanguageTag
                    }
                };
                Reports.Add(currentUsage);
            }

            return currentUsage;
        }

        public List<Usage> SelectReports(DateTime beforeDate)
        {
            return Reports.Where(usage => usage.Dimensions.Date.Date < beforeDate.Date).ToList();
        }

        public void RemoveReports(DateTime beforeDate)
        {
            Reports.RemoveAll(usage => usage.Dimensions.Date.Date < beforeDate.Date);
        }
    }

    class UsageStore
    {
        public DateTimeOffset LastSubmissionDate { get; set; } = DateTimeOffset.Now;
        public UsageModel Model { get; set; } = new UsageModel();

        public Measures GetCurrentMeasures(string appVersion, string unityVersion, string instanceId)
        {
            return Model.GetCurrentUsage(appVersion, unityVersion, instanceId).Measures;
        }
    }
}
