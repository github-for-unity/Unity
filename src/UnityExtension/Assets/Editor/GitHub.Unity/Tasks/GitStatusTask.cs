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
            //Tasks.Add(new GitStatusTask(onSuccess, onFailure));
        }

        private void ProcessOutput(string value)
        {
          
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

        protected override Action<string> OnSuccess { get { return onSuccess; } }
    }
}
