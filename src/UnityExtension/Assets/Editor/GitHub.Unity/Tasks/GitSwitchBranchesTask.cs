using System;
using System.IO;

namespace GitHub.Unity
{
    class GitSwitchBranchesTask : GitTask
    {
        private const string SwitchConfirmedMessage = "Switched to branch '{0}'";

        private string branch;

        private GitSwitchBranchesTask(string branch, Action onSuccess, Action onFailure = null)
            : base(_ => onSuccess(), onFailure)
        {
            this.branch = branch;
        }

        public static void Schedule(string branch, Action onSuccess, Action onFailure = null)
        {
            Tasks.Add(new GitSwitchBranchesTask(branch, onSuccess, onFailure));
        }

        protected override void OnProcessOutputUpdate()
        {
            if (!Done)
            {
                return;
            }

            // Handle failure / success
            var buffer = ErrorBuffer.GetStringBuilder();
            if (buffer.Length > 0)
            {
                var message = buffer.ToString().Trim();

                if (!message.Equals(String.Format(SwitchConfirmedMessage, branch)))
                {
                    ReportFailure(message);
                    return;
                }
            }
            ReportSuccess(null);
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
