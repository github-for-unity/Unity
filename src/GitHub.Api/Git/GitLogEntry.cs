using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace GitHub.Unity
{
    [Serializable]
    public struct GitLogEntry
    {
        private const string Today = "Today";
        private const string Yesterday = "Yesterday";

        public static GitLogEntry Default = new GitLogEntry(String.Empty, String.Empty, String.Empty, String.Empty, String.Empty, String.Empty, String.Empty, DateTimeOffset.MinValue, DateTimeOffset.MinValue, new List<GitStatusEntry>(), String.Empty, String.Empty);

        public string commitID;
        public string mergeA;
        public string mergeB;
        public string authorName;
        public string authorEmail;
        public string commitEmail;
        public string commitName;
        public string summary;
        public string description;
        public string timeString;
        public string commitTimeString;
        public List<GitStatusEntry> changes;

        public GitLogEntry(string commitID, 
            string authorName, string authorEmail,
            string commitName, string commitEmail, 
            string summary,
            string description,
            DateTimeOffset time, DateTimeOffset commitTime,
            List<GitStatusEntry> changes, 
            string mergeA = null, string mergeB = null) : this()
        {
            Guard.ArgumentNotNull(commitID, "commitID");
            Guard.ArgumentNotNull(authorName, "authorName");
            Guard.ArgumentNotNull(authorEmail, "authorEmail");
            Guard.ArgumentNotNull(commitEmail, "commitEmail");
            Guard.ArgumentNotNull(commitName, "commitName");
            Guard.ArgumentNotNull(summary, "summary");
            Guard.ArgumentNotNull(description, "description");
            Guard.ArgumentNotNull(changes, "changes");

            this.commitID = commitID;
            this.authorName = authorName;
            this.authorEmail = authorEmail;
            this.commitEmail = commitEmail;
            this.commitName = commitName;
            this.summary = summary;
            this.description = description;

            Time = time;
            CommitTime = commitTime;

            this.changes = changes;

            this.mergeA = mergeA ?? string.Empty;
            this.mergeB = mergeB ?? string.Empty;
        }

        public string PrettyTimeString
        {
            get
            {
                DateTimeOffset now = DateTimeOffset.Now, relative = Time.ToLocalTime();

                return String.Format("{0}, {1:HH}:{1:mm}",
                    relative.DayOfYear == now.DayOfYear
                        ? Today
                        : relative.DayOfYear == now.DayOfYear - 1 ? Yesterday : relative.ToString("d MMM yyyy"), relative);
            }
        }

        [NonSerialized] private DateTimeOffset? timeValue;
        public DateTimeOffset Time
        {
            get
            {
                if (!timeValue.HasValue)
                {
                    DateTimeOffset result;
                    if (DateTimeOffset.TryParseExact(TimeString, Constants.Iso8601Format, CultureInfo.InvariantCulture,DateTimeStyles.None, out result))
                    {
                        timeValue = result;
                    }
                    else
                    {
                        Time = DateTimeOffset.MinValue;
                    }
                }
                
                return timeValue.Value;
            }
            private set
            {
                timeString = value.ToString(Constants.Iso8601Format);
                timeValue = value;
            }
        }

        [NonSerialized] private DateTimeOffset? commitTimeValue;
        public DateTimeOffset CommitTime
        {
            get
            {
                if (!commitTimeValue.HasValue)
                {
                    DateTimeOffset result;
                    if (DateTimeOffset.TryParseExact(CommitTimeString, Constants.Iso8601Format, CultureInfo.InvariantCulture, DateTimeStyles.None, out result))
                    {
                        commitTimeValue = result;
                    }
                    else
                    {
                        CommitTime = DateTimeOffset.MinValue;
                    }
                }

                return commitTimeValue.Value;
            }
            private set
            {
                commitTimeString = value.ToString(Constants.Iso8601Format);
                commitTimeValue = value;
            }
        }

        public string ShortID => CommitID.Length < 7 ? CommitID : CommitID.Substring(0, 7);

        public string CommitID => commitID;

        public string MergeA => mergeA;

        public string MergeB => mergeB;

        public string AuthorName => authorName;

        public string AuthorEmail => authorEmail;

        public string CommitEmail => commitEmail;

        public string CommitName => commitName;

        public string Summary => summary;

        public string Description => description;

        public string TimeString => timeString;

        public string CommitTimeString => commitTimeString;

        public List<GitStatusEntry> Changes => changes;

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine(String.Format("CommitID: {0}", CommitID));
            sb.AppendLine(String.Format("MergeA: {0}", MergeA));
            sb.AppendLine(String.Format("MergeB: {0}", MergeB));
            sb.AppendLine(String.Format("AuthorName: {0}", AuthorName));
            sb.AppendLine(String.Format("AuthorEmail: {0}", AuthorEmail));
            sb.AppendLine(String.Format("Time: {0}", Time.ToString()));
            sb.AppendLine(String.Format("Summary: {0}", Summary));
            sb.AppendLine(String.Format("Description: {0}", Description));
            return sb.ToString();
        }
    }
}