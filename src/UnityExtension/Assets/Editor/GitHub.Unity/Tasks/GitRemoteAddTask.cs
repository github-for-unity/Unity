using System;
using System.IO;

namespace GitHub.Unity
{
    class GitRemoteAddTask : GitTask
    {
        private readonly string name;
        private readonly string url;

        private GitRemoteAddTask(string name, string url, Action onSuccess, Action onFailure)
            : base(str => onSuccess.SafeInvoke(), onFailure)
        {
            this.name = name;
            this.url = url;
        }

        public static void Schedule(string name, string url, Action onSuccess, Action onFailure = null)
        {
            Tasks.Add(new GitRemoteAddTask(name, url, onSuccess, onFailure));
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
            get { return "git remote add"; }
        }

        protected override string ProcessArguments
        {
            get { return String.Format("remote add {0} {1}", name, url); }
        }
    }
}
