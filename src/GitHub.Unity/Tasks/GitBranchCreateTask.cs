using System;
using System.IO;

namespace GitHub.Unity
{
    class GitBranchCreateTask : GitTask
    {
        private string baseBranch;
        private string newBranch;

        private GitBranchCreateTask(string newBranch, string baseBranch, Action onSuccess, Action onFailure)
            : base(str => onSuccess?.Invoke(), onFailure)
        {
            this.newBranch = newBranch;
            this.baseBranch = baseBranch;
        }

        public static void Schedule(string newBranch, string baseBranch, Action onSuccess, Action onFailure = null)
        {
            Tasks.Add(new GitBranchCreateTask(newBranch, baseBranch, onSuccess, onFailure));
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
