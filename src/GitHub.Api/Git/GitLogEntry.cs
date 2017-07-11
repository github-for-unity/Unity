using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace GitHub.Unity
{
    [Serializable]
    struct GitLogEntry
    {
        private const string Today = "Today";
        private const string Yesterday = "Yesterday";

        public string CommitID;
        public string MergeA;
        public string MergeB;
        public string AuthorName;
        public string AuthorEmail;
        public string CommitEmail;
        public string CommitName;
        public string Summary;
        public string Description;

        public string TimeString;
        public string CommitTimeString;


        public List<GitStatusEntry> Changes;

        public string ShortID
        {
            get { return CommitID.Length < 7 ? CommitID : CommitID.Substring(0, 7); }
        }

        public string PrettyTimeString
        {
            get
            {
                DateTimeOffset now = DateTimeOffset.Now, relative = TimeValue.ToLocalTime();

                return String.Format("{0}, {1:HH}:{1:mm}",
                    relative.DayOfYear == now.DayOfYear
                        ? Today
                        : relative.DayOfYear == now.DayOfYear - 1 ? Yesterday : relative.ToString("d MMM yyyy"), relative);
            }
        }

        [NonSerialized] public DateTimeOffset? timeValue;
        public DateTimeOffset TimeValue
        {
            get
            {
                if (!timeValue.HasValue)
                {
                    timeValue = DateTimeOffset.Parse(TimeString);
                }

                return timeValue.Value;
            }
        }

        [NonSerialized] public DateTimeOffset? commitTimeValue;
        public DateTimeOffset? CommitTimeValue
        {
            get
            {
                if (!timeValue.HasValue && !string.IsNullOrEmpty(CommitTimeString))
                {
                    commitTimeValue = DateTimeOffset.Parse(CommitTimeString);
                }

                return commitTimeValue;
            }
        }

        public void Clear()
        {
            CommitID = MergeA = MergeB = AuthorName = AuthorEmail = Summary = Description = "";

            timeValue = DateTimeOffset.MinValue;
            TimeString = timeValue.Value.ToString(DateTimeFormatInfo.CurrentInfo);

            commitTimeValue = null;
            CommitTimeString = null;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine(String.Format("CommitID: {0}", CommitID));
            sb.AppendLine(String.Format("MergeA: {0}", MergeA));
            sb.AppendLine(String.Format("MergeB: {0}", MergeB));
            sb.AppendLine(String.Format("AuthorName: {0}", AuthorName));
            sb.AppendLine(String.Format("AuthorEmail: {0}", AuthorEmail));
            sb.AppendLine(String.Format("Time: {0}", TimeValue.ToString()));
            sb.AppendLine(String.Format("Summary: {0}", Summary));
            sb.AppendLine(String.Format("Description: {0}", Description));
            return sb.ToString();
        }
    }
}