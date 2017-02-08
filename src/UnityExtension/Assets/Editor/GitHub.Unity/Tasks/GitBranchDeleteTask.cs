using GitHub.Api;
using System;

namespace GitHub.Unity
{
    class GitBranchDeleteTask : GitTask
    {
        private readonly string arguments;

        private GitBranchDeleteTask(string branch, Action onSuccess, Action onFailure)
            : base(str => onSuccess.SafeInvoke(), onFailure)
        {
            Guard.ArgumentNotNullOrWhiteSpace(branch, "branch");
            arguments = String.Format("branch -d {0}", branch);
        }

        public static void Schedule(string branch, Action onSuccess, Action onFailure = null)
        {
            Tasks.Add(new GitBranchDeleteTask(branch, onSuccess, onFailure));
        }

        public override bool Blocking { get { return false; } }
        public override bool Critical { get { return false; } }
        public override string Label { get { return "git branch"; } }
        protected override string ProcessArguments { get { return arguments; } }
    }
}
