using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using GitHub.Unity.Logging;
using UnityEngine;

namespace GitHub.Unity
{
    class GitLogTask : GitTask
    {
        private const string UnhandledParsePhaseError = "Unhandled parse phase: '{0}'";
        private const string LineParseError = "Log parse error in line: '{0}', parse phase: '{1}'";
        private const string GitTimeFormat = "ddd MMM d HH:mm:ss yyyy zz";

        private static Action<IList<GitLogEntry>> onLogUpdate;

        private string arguments = "log --name-status";
        private List<GitLogEntry> entries = new List<GitLogEntry>();
        private GitLogEntry parsedEntry = new GitLogEntry();
        private ParsePhase parsePhase;
        private Action<string> onSuccess;

        private GitLogTask(string file = null)
            : base(null, null)
        {
            onSuccess = ProcessOutput;
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
            //Tasks.Add(new GitLogTask(file));
        }

        private void DeliverResult()
        {
            if (onLogUpdate != null)
                onLogUpdate(entries);

            entries.Clear();
        }

        private void ProcessOutput(string value)
        {
            foreach (var line in value.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries))
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
                            continue;
                        default:
                            ++parsePhase;
                            continue;
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
                            continue;
                        }

                        break;
                    case ParsePhase.Author:
                        // If this is a marge commit, merge info comes before author info, so we parse this and stay in the author phase
                        match = Utility.LogMergeRegex.Match(line);
                        if (match.Groups.Count == 3)
                        {
                            parsedEntry.MergeA = match.Groups[1].ToString();
                            parsedEntry.MergeB = match.Groups[2].ToString();
                            continue;
                        }

                        match = Utility.LogAuthorRegex.Match(line);
                        if (match.Groups.Count == 3)
                        {
                            parsedEntry.AuthorName = match.Groups[1].ToString();
                            parsedEntry.AuthorEmail = match.Groups[2].ToString();
                            ++parsePhase;
                            continue;
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
                                continue;
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
                            continue;
                        }

                        break;
                    case ParsePhase.Changes:
                        GitStatusEntry entry;

                        // Try to read the line as a change entry
                        if (GitStatusEntry.TryParse(line, out entry))
                        {
                            parsedEntry.Changes.Add(entry);
                            continue;
                        }
                        // This commit had no changes, so complete parsing it and pass the next commit header into a new session
                        else if ((match = Utility.LogCommitRegex.Match(line)).Groups.Count == 2)
                        {
                            continue;
                        }

                        break;
                    default:
                        throw new TaskException(String.Format(UnhandledParsePhaseError, parsePhase));
                }

                // Garbled input. Eject!
                Logger.Error(LineParseError, line, parsePhase);
                Abort();
            }
            InternalInvoke();
        }

        private void InternalInvoke()
        {
            Tasks.ScheduleMainThread(DeliverResult);
        }

        public override bool Blocking
        {
            get { return false; }
        }

        public override TaskQueueSetting Queued
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

        protected override Action<string> OnSuccess { get { return onSuccess; } }

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
