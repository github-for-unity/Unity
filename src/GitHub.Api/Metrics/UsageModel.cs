using System;
using System.Collections.Generic;
using System.Linq;

namespace GitHub.Unity
{
    public class Usage
    {
        public DateTime Date { get; set; }
        public bool IsGitHubUser { get; set; }
        public bool IsEnterpriseUser { get; set; }
        public string AppVersion { get; set; }
        public string UnityVersion { get; set; }
        public string Lang { get; set; }
        public int NumberOfStartups { get; set; }
    }

    public class UsageModel
    {
        private List<Usage> reports = new List<Usage>();

        public IList<Usage> Reports
        {
            get { return reports; }
            set { reports = value.ToList(); }
        }

        private Usage currentUsage;

        public Usage GetCurrentUsage()
        {
            var date = DateTime.UtcNow.Date;
            if (currentUsage != null)
            {
                if (currentUsage.Date == date)
                {
                    return currentUsage;
                }

                currentUsage = null;
            }

            currentUsage = reports.FirstOrDefault(usage => usage.Date == date);

            if (currentUsage == null)
            {
                currentUsage = new Usage { Date = date };
                reports.Add(currentUsage);
            }

            return currentUsage;
        }

        public List<Usage> SelectReports(DateTime beforeDate)
        {
            return reports.Where(usage => usage.Date.Date != beforeDate.Date).ToList();
        }

        public void RemoveReports(DateTime beforeDate)
        {
            var reportsCopy = reports;

            var excludeUsage = reportsCopy.FirstOrDefault(usage => usage.Date.Date != beforeDate.Date);
            if (excludeUsage != null)
            {
                reports = new List<Usage> {
                    excludeUsage
                };

                reportsCopy.Remove(excludeUsage);
            }
            else
            {
                reports = new List<Usage>();
            }
        }
    }

    class UsageStore
    {
        public DateTimeOffset LastUpdated { get; set; } = DateTimeOffset.UtcNow;
        public UsageModel Model { get; set; } = new UsageModel();
    }
}
