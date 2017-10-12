using System;
using System.Collections.Generic;
using System.Linq;

namespace GitHub.Unity
{
    public class Usage
    {
        public string Guid { get; set; }
        public DateTime Date { get; set; }
        public string AppVersion { get; set; }
        public string UnityVersion { get; set; }
        public string Lang { get; set; }
        public int NumberOfStartups { get; set; }
    }

    class UsageModel
    {
        public List<Usage> Reports { get; set; } = new List<Usage>();
        public string Guid { get; set; }

        private Usage currentUsage;

        public Usage GetCurrentUsage()
        {
            var date = DateTime.UtcNow.Date;
            if (currentUsage == null)
            {
                currentUsage = Reports.FirstOrDefault(usage => usage.Date == date);
            }

            if (currentUsage?.Date == date)
            {
                // update any fields that might be missing, if we've changed the format
                if (currentUsage.Guid != Guid)
                    currentUsage.Guid = Guid;
            }
            else
            {
                currentUsage = new Usage { Date = date, Guid = Guid };
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
