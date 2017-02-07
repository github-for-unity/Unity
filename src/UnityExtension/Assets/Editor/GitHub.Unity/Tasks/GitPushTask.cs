using System;
using System.Text;

namespace GitHub.Unity
{
    class GitPushTask : GitTask
    {
        private readonly string repository;
        private readonly bool? setUpstream;
        private readonly string branch;

        private GitPushTask(Action onSuccess, Action onFailure, string repository = null, bool? setUpstream = null, string branch = null)
            : base(str => onSuccess.SafeInvoke(), onFailure)
        {
            this.repository = repository;
            this.setUpstream = setUpstream;
            this.branch = branch;
        }

        public static void Schedule(Action onSuccess, string repository = null, bool? setUpstream = null, string branch = null, Action onFailure = null)
        {
            Tasks.Add(new GitPushTask(onSuccess, onFailure, repository, setUpstream, branch));
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
            get { return "git push"; }
        }

        protected override string ProcessArguments
        {
            get
            {
                var stringBuilder = new StringBuilder();
                stringBuilder.Append("push");

                if (setUpstream.HasValue)
                {
                    stringBuilder.Append(" -u");
                }

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