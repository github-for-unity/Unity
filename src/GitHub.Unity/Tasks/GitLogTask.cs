using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace GitHub.Unity
{
    struct GitLogEntry
    {
        private const string Today = "Today";
        private const string Yesterday = "Yesterday";

        public string CommitID, MergeA, MergeB, AuthorName, AuthorEmail, Summary, Description;
        public DateTimeOffset Time;
        public List<GitStatusEntry> Changes;

        public string ShortID
        {
            get { return CommitID.Length < 7 ? CommitID : CommitID.Substring(0, 7); }
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

        public void Clear()
        {
            CommitID = MergeA = MergeB = AuthorName = AuthorEmail = Summary = Description = "";
            Time = DateTimeOffset.Now;
            Changes = new List<GitStatusEntry>();
        }

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

    class GitLogTask : GitTask
    {
        private const string UnhandledParsePhaseError = "Unhandled parse phase: '{0}'";
        private const string LineParseError = "Log parse error in line: '{0}', parse phase: '{1}'";
        private const string GitTimeFormat = "ddd MMM d HH:mm:ss yyyy zz";

        private static Action<IList<GitLogEntry>> onLogUpdate;

        private string arguments = "log --name-status";
        private bool completed = false;
        private List<GitLogEntry> entries = new List<GitLogEntry>();
        private GitLogEntry parsedEntry = new GitLogEntry();
        private ParsePhase parsePhase;

        private GitLogTask(string file = null)
        {
            parsedEntry.Clear();

            if (!string.IsNullOrEmpty(file))
            {
                arguments = String.Format("{0} --follow -m {1}", arguments, file);
            }
        }

        public static void RegisterCallback(Action<IList<GitLogEntry>> callback)
        {
            onLogUpdate += callback;
        }

        public static void UnregisterCallback(Action<IList<GitLogEntry>> callback)
        {
            onLogUpdate -= callback;
        }

        public static void Schedule(string file = null)
        {
            Tasks.Add(new GitLogTask(file));
        }

        protected override void OnProcessOutputUpdate()
        {
            Utility.ParseLines(OutputBuffer.GetStringBuilder(), ParseOutputLine, Done);

            if (Done && !completed)
            {
                completed = true;

                // Complete parsing on the last entry
                ParseOutputLine(null);

                // Handle failure / success
                var buffer = ErrorBuffer.GetStringBuilder();
                if (buffer.Length > 0)
                {
                    Tasks.ReportFailure(FailureSeverity.Moderate, this, buffer.ToString());
                }
                else
                {
                    Tasks.ScheduleMainThread(DeliverResult);
                }
            }
        }

        private void DeliverResult()
        {
            if (onLogUpdate != null)
            {
                onLogUpdate(entries);
            }

            entries.Clear();
        }

        private void ParseOutputLine(string line)
        {
            // Empty lines are section or commit dividers
            if (string.IsNullOrEmpty(line))
            {
                switch (parsePhase)
                {
                    case ParsePhase.Changes:
                        entries.Add(parsedEntry);
                        parsedEntry.Clear();
                        parsePhase = ParsePhase.Commit;
                        return;
                    default:
                        ++parsePhase;
                        return;
                }
            }

            Match match;

            switch (parsePhase)
            {
                case ParsePhase.Commit:
                    match = Utility.LogCommitRegex.Match(line);
                    if (match.Groups.Count == 2)
                    {
                        parsedEntry.CommitID = match.Groups[1].ToString();
                        ++parsePhase;
                        return;
                    }

                    break;
                case ParsePhase.Author:
                    // If this is a marge commit, merge info comes before author info, so we parse this and stay in the author phase
                    match = Utility.LogMergeRegex.Match(line);
                    if (match.Groups.Count == 3)
                    {
                        parsedEntry.MergeA = match.Groups[1].ToString();
                        parsedEntry.MergeB = match.Groups[2].ToString();
                        return;
                    }

                    match = Utility.LogAuthorRegex.Match(line);
                    if (match.Groups.Count == 3)
                    {
                        parsedEntry.AuthorName = match.Groups[1].ToString();
                        parsedEntry.AuthorEmail = match.Groups[2].ToString();
                        ++parsePhase;
                        return;
                    }

                    break;
                case ParsePhase.Time:
                    match = Utility.LogTimeRegex.Match(line);
                    if (match.Groups.Count == 2)
                    {
                        var time = match.Groups[1].ToString();

                        parsedEntry.Time = DateTimeOffset.ParseExact(time, GitTimeFormat, CultureInfo.InvariantCulture, DateTimeStyles.None);

                        if (DateTimeOffset.TryParseExact(time, GitTimeFormat, CultureInfo.InvariantCulture, DateTimeStyles.None,
                            out parsedEntry.Time))
                        {
                            // NOTE: Time is always last in the header, so we should not progress to next phase here - the divider will do that
                            return;
                        }
                    }

                    break;
                case ParsePhase.Description:
                    match = Utility.LogDescriptionRegex.Match(line);
                    if (match.Groups.Count == 2)
                    {
                        if (string.IsNullOrEmpty(parsedEntry.Summary))
                        {
                            parsedEntry.Summary = match.Groups[1].ToString();
                        }
                        else
                        {
                            parsedEntry.Description += match.Groups[1].ToString();
                        }
                        return;
                    }

                    break;
                case ParsePhase.Changes:
                    GitStatusEntry entry;

                    // Try to read the line as a change entry
                    if (GitStatusEntry.TryParse(line, out entry))
                    {
                        parsedEntry.Changes.Add(entry);
                        return;
                    }
                    // This commit had no changes, so complete parsing it and pass the next commit header into a new session
                    else if ((match = Utility.LogCommitRegex.Match(line)).Groups.Count == 2)
                    {
                        ParseOutputLine(null);
                        ParseOutputLine(line);
                        return;
                    }

                    break;
                default:
                    throw new TaskException(String.Format(UnhandledParsePhaseError, parsePhase));
            }

            // Garbled input. Eject!
            Debug.LogErrorFormat(LineParseError, line, parsePhase);
            Abort();
        }

        public override bool Blocking
        {
            get { return false; }
        }

        public virtual TaskQueueSetting Queued
        {
            get { return TaskQueueSetting.QueueSingle; }
        }

        public override bool Critical
        {
            get { return false; }
        }

        public override bool Cached
        {
            get { return false; }
        }

        public override string Label
        {
            get { return "git log"; }
        }

        protected override string ProcessArguments
        {
            get { return arguments; }
        }

        private enum ParsePhase
        {
            Commit,
            Author,
            Time,
            Description,
            Changes
        }
    }
}
