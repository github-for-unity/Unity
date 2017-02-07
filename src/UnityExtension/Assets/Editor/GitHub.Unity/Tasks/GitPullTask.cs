using System;
using System.Text;

namespace GitHub.Unity
{
    class GitPullTask : GitTask
    {
        private readonly string repository;
        private readonly string branch;

        private GitPullTask(Action onSuccess, Action onFailure, string repository = null, string branch = null)
            : base(str => onSuccess.SafeInvoke(), onFailure)
        {
            this.repository = repository;
            this.branch = branch;
        }

        public static void Schedule(Action onSuccess, string repository = null, string branch = null, Action onFailure = null)
        {
            Tasks.Add(new GitPullTask(onSuccess, onFailure, repository, branch));
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
            get { return "git pull"; }
        }

        protected override string ProcessArguments
        {
            get
            {
                var stringBuilder = new StringBuilder();
                stringBuilder.Append("pull");

                if (repository != null)
                {
                    stringBuilder.Append(" ");
                    stringBuilder.Append(repository);
                }

                if (!string.IsNullOrEmpty(branch))
                {
                    stringBuilder.Append(" ");
                    stringBuilder.Append(branch);
                }

                return stringBuilder.ToString();
            }
        }
    }
}