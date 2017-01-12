using System;
using System.Collections.Generic;
using System.IO;

namespace GitHub.Unity
{
    class GitListBranchesTask : GitTask
    {
        private const string LocalArguments = "branch -vv";
        private const string RemoteArguments = "branch -r";
        private const string UnmatchedLineError = "Unable to match the line '{0}'";
        private List<GitBranch> branches = new List<GitBranch>();
        private Mode mode;
        private Action<IEnumerable<GitBranch>> callback;
        private Action<string> onSuccess;

        private GitListBranchesTask(Mode mode, Action<IEnumerable<GitBranch>> onSuccess, Action onFailure = null)
            : base(null, onFailure)
        {
            this.mode = mode;
            this.callback = onSuccess;
            this.onSuccess = ParseOutput;
        }

        public static void ScheduleLocal(Action<IEnumerable<GitBranch>> onSuccess, Action onFailure = null)
        {
            Schedule(Mode.Local, onSuccess, onFailure);
        }

        public static void ScheduleRemote(Action<IEnumerable<GitBranch>> onSuccess, Action onFailure = null)
        {
            Schedule(Mode.Remote, onSuccess, onFailure);
        }

        private static void Schedule(Mode mode, Action<IEnumerable<GitBranch>> onSuccess, Action onFailure = null)
        {
            Tasks.Add(new GitListBranchesTask(mode, onSuccess, onFailure));
        }

        private void DeliverResult()
        {
            callback?.Invoke(branches);
        }

        private void ParseOutput(string value)
        {
            foreach (var line in value.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries))
            {
                var match = Utility.ListBranchesRegex.Match(line);

                if (!match.Success)
                {
                    Tasks.ReportFailure(FailureSeverity.Moderate, this, String.Format(UnmatchedLineError, line));
                    continue;
                }

                branches.Add(new GitBranch(match.Groups["name"].Value, match.Groups["tracking"].Value,
                    !string.IsNullOrEmpty(match.Groups["active"].Value)));
            }
        }

        public override bool Blocking
        {
            get { return false; }
        }

        public override TaskQueueSetting Queued
        {
            get { return TaskQueueSetting.Queue; }
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
            get { return "git branch"; }
        }

        protected override string ProcessArguments
        {
            get { return mode == Mode.Local ? LocalArguments : RemoteArguments; }
        }

        protected override Action<string> OnSuccess => onSuccess;

        private enum Mode
        {
            Local,
            Remote
        }
    }
}
