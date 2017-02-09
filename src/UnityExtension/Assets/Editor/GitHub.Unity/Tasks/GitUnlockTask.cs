using System;
using System.Text;
using GitHub.Api;

namespace GitHub.Unity
{
    class GitUnlockTask : GitTask
    {
        private readonly string arguments;

        private GitUnlockTask(string path, Action onSuccess, Action onFailure = null, bool force = false)
            : base(str => onSuccess.SafeInvoke(), onFailure)
        {
            Guard.ArgumentNotNullOrWhiteSpace(path, "path");

            var stringBuilder = new StringBuilder("lfs unlock ");

            if (force)
            {
                stringBuilder.Append("--force ");
            }

            stringBuilder.Append(path);

            arguments = stringBuilder.ToString();
        }

        public static void Schedule(string path, Action onSuccess, Action onFailure = null, bool force = false)
        {
            Tasks.Add(new GitUnlockTask(path, onSuccess, onFailure, force));
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
            get { return "git lfs unlock"; }
        }

        protected override string ProcessArguments
        {
            get { return arguments; }
        }
    }
}
