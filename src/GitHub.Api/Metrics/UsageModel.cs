using System;
using System.Collections.Generic;
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
        public int NumberOfCommits { get; set; }
        public int NumberOfFetches { get; set; }
        public int NumberOfPushes { get; set; }
        public int NumberOfPulls { get; set; }
        public int NumberOfProjectsInitialized { get; set; }
        public int NumberOfAuthentications { get; set; }
        public int NumberOfLocalBranchCreations { get; set; }
        public int NumberOfLocalBranchDeletion { get; set; }
        public int NumberOfLocalBranchCheckouts { get; set; }
        public int NumberOfRemoteBranchCheckouts { get; set; }
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

            if (currentUsage?.Dimensions.Date == date)
            {
                // update any fields that might be missing, if we've changed the format
                if (currentUsage.Dimensions.Guid != Guid)
                    currentUsage.Dimensions.Guid = Guid;
            }
            else
            {
                currentUsage = new Usage
                {
                    InstanceId = instanceId,
                    Dimensions = {
                        Date = date,
                        Guid = Guid,
                        AppVersion = appVersion,
                        UnityVersion = unityVersion
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
