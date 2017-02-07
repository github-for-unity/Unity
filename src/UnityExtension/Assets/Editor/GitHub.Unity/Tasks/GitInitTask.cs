using System;

namespace GitHub.Unity
{
    class GitInitTask : GitTask
    {
        private GitInitTask(Action onSuccess, Action onFailure) : base(str => onSuccess.SafeInvoke(), onFailure)
        {}

        public static void Schedule(Action onSuccess, Action onFailure = null)
        {
            Tasks.Add(new GitInitTask(onSuccess, onFailure));
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
            get { return "git init"; }
        }

        protected override string ProcessArguments
        {
            get { return "init"; }
        }
    }
}
