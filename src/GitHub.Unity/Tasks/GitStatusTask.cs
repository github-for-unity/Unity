using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace GitHub.Unity
{
    class GitStatusTask : GitTask
    {
        private const string BranchNamesSeparator = "...";

        private Action<GitStatus> callback;
        private Action<string> onSuccess;

        private GitStatusTask(Action<GitStatus> onSuccess = null, Action onFailure = null)
            : base(null, onFailure)
        {
            this.callback = onSuccess;
            this.onSuccess = ProcessOutput;
        }

        public static void Schedule(Action<GitStatus> onSuccess = null, Action onFailure = null)
        {
            Tasks.Add(new GitStatusTask(onSuccess, onFailure));
        }

        private void ProcessOutput(string value)
        {
            var status = new GitStatus();
            status.Entries = new List<GitStatusEntry>();

            foreach (var line in value.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries))
            {
                GitStatusEntry entry;

                // Grab change lines
                if (GitStatusEntry.TryParse(line, out entry))
                {
                    if (!status.Entries.Any(e => e.Path.Equals(entry.Path)) && !Directory.Exists(Utility.RepositoryPathToAbsolute(entry.Path)))
                    {
                        status.Entries.Add(entry);
                    }
                    continue;
                }

                // Grab local and remote branch
                if (Utility.StatusBranchLineValidRegex.Match(line).Success)
                {
                    var index = line.IndexOf(BranchNamesSeparator);

                    // Remote branch available
                    if (index >= 0)
                    {
                        status.LocalBranch = line.Substring(2, index - 2);
                        status.RemoteBranch = line.Substring(index + BranchNamesSeparator.Length);
                        index = status.RemoteBranch.IndexOf('[');

                        // Ahead and/or behind information available
                        if (index > 0)
                        {
                            var match = Utility.StatusAheadBehindRegex.Match(status.RemoteBranch.Substring(index - 1));

                            status.RemoteBranch = status.RemoteBranch.Substring(0, index).Trim();

                            string aheadString = match.Groups["ahead"].Value, behindString = match.Groups["behind"].Value;

                            status.Ahead = string.IsNullOrEmpty(aheadString) ? 0 : Int32.Parse(aheadString);
                            status.Behind = string.IsNullOrEmpty(behindString) ? 0 : Int32.Parse(behindString);
                        }
                        else
                        {
                            status.RemoteBranch = status.RemoteBranch.Trim();
                        }
                    }
                    // No remote branch
                    else
                    {
                        status.LocalBranch = line.Substring(2).Trim();
                    }
                }
            }
            Tasks.ScheduleMainThread(() => callback?.Invoke(status));
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
            get { return "git status"; }
        }

        protected override string ProcessArguments
        {
            get { return "status -b -u --porcelain"; }
        }

        protected override Action<string> OnSuccess => onSuccess;
    }
}
