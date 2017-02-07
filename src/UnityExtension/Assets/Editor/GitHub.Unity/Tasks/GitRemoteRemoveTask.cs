using System;

namespace GitHub.Unity
{
    class GitRemoteRemoveTask : GitTask
    {
        private readonly string name;

        private GitRemoteRemoveTask(string name, Action onSuccess, Action onFailure)
            : base(str => onSuccess.SafeInvoke(), onFailure)
        {
            this.name = name;
        }

        public static void Schedule(string name, Action onSuccess, Action onFailure = null)
        {
            Tasks.Add(new GitRemoteRemoveTask(name, onSuccess, onFailure));
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
            get { return "git remote rm"; }
        }

        protected override string ProcessArguments
        {
            get { return String.Format("remote rm {0}", name); }
        }
    }
}