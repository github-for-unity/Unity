using System;
using System.Collections.Generic;
using System.IO;

namespace GitHub.Unity
{
    struct GitBranch
    {
        public string Name { get; private set; }
        public string Tracking { get; private set; }
        public bool Active { get; private set; }

        public GitBranch(string name, string tracking, bool active)
        {
            Name = name;
            Tracking = tracking;
            Active = active;
        }
    }

    class GitListBranchesTask : GitTask
    {
        private const string LocalArguments = "branch -vv";
        private const string RemoteArguments = "branch -r";
        private const string UnmatchedLineError = "Unable to match the line '{0}'";
        private List<GitBranch> branches = new List<GitBranch>();
        private Mode mode;
        private Action onFailure;
        private Action<IEnumerable<GitBranch>> onSuccess;

        private GitListBranchesTask(Mode mode, Action<IEnumerable<GitBranch>> onSuccess, Action onFailure = null)
        {
            this.mode = mode;
            this.onSuccess = onSuccess;
            this.onFailure = onFailure;
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

        protected override void OnProcessOutputUpdate()
        {
            Utility.ParseLines(OutputBuffer.GetStringBuilder(), ParseOutputLine, Done);

            if (Done)
            {
                // Handle failure / success
                var buffer = ErrorBuffer.GetStringBuilder();
                if (buffer.Length > 0)
                {
                    Tasks.ReportFailure(FailureSeverity.Moderate, this, buffer.ToString());
                    if (onFailure != null)
                    {
                        Tasks.ScheduleMainThread(() => onFailure());
                    }
                }
                else
                {
                    Tasks.ScheduleMainThread(DeliverResult);
                }
            }
        }

        private void DeliverResult()
        {
            if (onSuccess == null)
            {
                return;
            }

            onSuccess(branches);
        }

        private void ParseOutputLine(string line)
        {
            var match = Utility.ListBranchesRegex.Match(line);

            if (!match.Success)
            {
                Tasks.ReportFailure(FailureSeverity.Moderate, this, String.Format(UnmatchedLineError, line));
                return;
            }

            branches.Add(new GitBranch(match.Groups["name"].Value, match.Groups["tracking"].Value,
                !string.IsNullOrEmpty(match.Groups["active"].Value)));
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

        private enum Mode
        {
            Local,
            Remote
        }
    }
}
