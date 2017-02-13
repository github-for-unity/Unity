using System;
using GitHub.Api;

namespace GitHub.Unity
{
    class GitLockTask : GitTask
    {
        private readonly string arguments;

        private GitLockTask(string path, Action onSuccess, Action onFailure = null)
            : base(str => onSuccess.SafeInvoke(), onFailure)
        {
            Guard.ArgumentNotNullOrWhiteSpace(path, "path");

            arguments = String.Format("lfs lock {0}", path);
        }

        public static void Schedule(string path, Action onSuccess, Action onFailure = null)
        {
            Tasks.Add(new GitLockTask(path, onSuccess, onFailure));
        }

        public override bool Blocking
        {
            get { return false; }
        }

        public override bool Critical
        {
            get { return false; }
        }

        public override string Label
        {
            get { return "git lfs lock"; }
        }

        protected override string ProcessArguments
        {
            get { return arguments; }
        }
    }
}
