using System;
using System.IO;

namespace GitHub.Unity
{
    class GitBranchDeleteTask : GitTask
    {
        private string branch;

        private GitBranchDeleteTask(string branch, Action onSuccess, Action onFailure)
            : base(str => onSuccess.SafeInvoke(), onFailure)
        {
            this.branch = branch;
        }

        public static void Schedule(string branch, Action onSuccess, Action onFailure = null)
        {
            Tasks.Add(new GitBranchDeleteTask(branch, onSuccess, onFailure));
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
            get { return String.Format("branch -d {0}", branch); }
        }
    }
}
