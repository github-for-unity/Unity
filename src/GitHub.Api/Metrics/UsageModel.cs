using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

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
        public DateTime Date { get; set; }
        public string AppVersion { get; set; }
        public string UnityVersion { get; set; }
        public string Lang { get; set; }
        public string CurrentLang { get; set; }
    }

    public class Measures
    {
        public int NumberOfStartups { get; set; }
        public int ChangesViewButtonCommit { get; set; }
        public int HistoryToolbarButtonFetch { get; set; }
        public int HistoryToolbarButtonPush { get; set; }
        public int HistoryToolbarButtonPull { get; set; }
        public int Initialized { get; set; }
        public int AuthenticationViewButtonAuthentication { get; set; }
        public int BranchesViewButtonCreateBranch { get; set; }
        public int BranchesViewButtonDeleteBranch { get; set; }
        public int BranchesViewButtonCheckoutLocalBranch { get; set; }
        public int BranchesViewButtonCheckoutRemoteBranch { get; set; }
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

            var date = DateTime.UtcNow.Date;
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
                        Date = date,
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
            return Reports.Where(usage => usage.Dimensions.Date.Date != beforeDate.Date).ToList();
        }

        public void RemoveReports(DateTime beforeDate)
        {
            Reports.RemoveAll(usage => usage.Dimensions.Date.Date != beforeDate.Date);
        }
    }

    class UsageStore
    {
        public DateTimeOffset LastUpdated { get; set; } = DateTimeOffset.UtcNow;
        public UsageModel Model { get; set; } = new UsageModel();
    }
}
