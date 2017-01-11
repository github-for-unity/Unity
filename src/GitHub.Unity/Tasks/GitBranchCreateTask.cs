using System;
using System.IO;

namespace GitHub.Unity
{
    class GitBranchCreateTask : GitTask
    {
        private string baseBranch;
        private string newBranch;
        private Action onFailure;
        private Action onSuccess;

        private GitBranchCreateTask(string newBranch, string baseBranch, Action onSuccess, Action onFailure)
        {
            this.newBranch = newBranch;
            this.baseBranch = baseBranch;
            this.onSuccess = onSuccess;
            this.onFailure = onFailure;
        }

        public static void Schedule(string newBranch, string baseBranch, Action onSuccess, Action onFailure = null)
        {
            Tasks.Add(new GitBranchCreateTask(newBranch, baseBranch, onSuccess, onFailure));
        }

        protected override void OnProcessOutputUpdate()
        {
            if (Done)
            {
                // Handle failure / success
                var buffer = ErrorBuffer.GetStringBuilder();
                if (buffer.Length > 0)
                {
                    Tasks.ReportFailure(FailureSeverity.Critical, this, buffer.ToString());
                    if (onFailure != null)
                    {
                        Tasks.ScheduleMainThread(onFailure);
                    }

                    return;
                }

                if (onSuccess != null)
                {
                    Tasks.ScheduleMainThread(onSuccess);
                }
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
            get { return true; }
        }

        public override string Label
        {
            get { return "git branch"; }
        }

        protected override string ProcessArguments
        {
            get { return String.Format("branch {0} {1}", newBranch, baseBranch); }
        }
    }
}
