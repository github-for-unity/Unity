using System;
using System.Collections.Generic;
using System.Linq;

namespace GitHub.Unity
{
    class Usage
    {
        public DateTime Date { get; set; }
        public bool IsGitHubUser { get; set; }
        public bool IsEnterpriseUser { get; set; }
        public string AppVersion { get; set; }
        public string UnityVersion { get; set; }
        public string Lang { get; set; }
        public int NumberOfStartups { get; set; }
    }

    class UsageModel
    {
        public List<Usage> Reports { get; } = new List<Usage>();

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

            currentUsage = Reports.FirstOrDefault(usage => usage.Date == date);

            if (currentUsage == null)
            {
                currentUsage = new Usage { Date = date };
                Reports.Add(currentUsage);
            }

            return currentUsage;
        }

        public List<Usage> SelectReports(DateTime beforeDate)
        {
            return Reports.Where(usage => usage.Date.Date != beforeDate.Date).ToList();
        }

        public void RemoveReports(DateTime beforeDate)
        {
            Reports.RemoveAll(usage => usage.Date.Date != beforeDate.Date);
        }
    }

    class UsageStore
    {
        public DateTimeOffset LastUpdated { get; set; } = DateTimeOffset.UtcNow;
        public UsageModel Model { get; set; } = new UsageModel();
    }
}
