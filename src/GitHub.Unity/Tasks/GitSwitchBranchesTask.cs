using System;
using System.IO;

namespace GitHub.Unity
{
    class GitSwitchBranchesTask : GitTask
    {
        private const string SwitchConfirmedMessage = "Switched to branch '{0}'";

        private string branch;
        private Action onFailure;
        private Action onSuccess;

        private GitSwitchBranchesTask(string branch, Action onSuccess, Action onFailure = null)
        {
            this.branch = branch;
            this.onSuccess = onSuccess;
            this.onFailure = onFailure;
        }

        public static void Schedule(string branch, Action onSuccess, Action onFailure = null)
        {
            Tasks.Add(new GitSwitchBranchesTask(branch, onSuccess, onFailure));
        }

        protected override void OnProcessOutputUpdate()
        {
            if (Done)
            {
                // Handle failure / success
                var buffer = ErrorBuffer.GetStringBuilder();
                if (buffer.Length > 0)
                {
                    var message = buffer.ToString().Trim();

                    if (!message.Equals(String.Format(SwitchConfirmedMessage, branch)))
                    {
                        Tasks.ReportFailure(FailureSeverity.Critical, this, message);
                        if (onFailure != null)
                        {
                            Tasks.ScheduleMainThread(onFailure);
                        }

                        return;
                    }
                }

                if (onSuccess != null)
                {
                    Tasks.ScheduleMainThread(onSuccess);
                }
            }
        }

        public override bool Blocking
        {
            get { return true; }
        }

        public override TaskQueueSetting Queued
        {
            get { return TaskQueueSetting.QueueSingle; }
        }

        public override bool Critical
        {
            get { return true; }
        }

        public override bool Cached
        {
            get { return false; }
        }

        public override string Label
        {
            get { return "git checkout"; }
        }

        protected override string ProcessArguments
        {
            get { return String.Format("checkout {0}", branch); }
        }
    }
}
