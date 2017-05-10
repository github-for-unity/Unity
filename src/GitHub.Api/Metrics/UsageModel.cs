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
        private readonly Dictionary<DateTime, Usage> reports = new Dictionary<DateTime, Usage>();

        public IList<Usage> Reports
        {
            get { return reports.Values.ToList(); }
            set
            {
                reports.Clear();
                foreach (var usage in value)
                {
                    reports.Add(usage.Date.Date, usage);
                }
            }
        }

        public Usage GetCurrentUsage()
        {
            var date = DateTime.UtcNow.Date;

            Usage usage;
            if (!reports.TryGetValue(date, out usage))
            {
                usage= new Usage {
                   Date = date
                };
                reports[date] = usage;
            }

            return usage;
        }

        public void Clear()
        {
            reports.Clear();
        }
    }

    class UsageStore
    {
        public DateTimeOffset LastUpdated { get; set; } = DateTimeOffset.UtcNow;
        public UsageModel Model { get; set; } = new UsageModel();
    }
}
