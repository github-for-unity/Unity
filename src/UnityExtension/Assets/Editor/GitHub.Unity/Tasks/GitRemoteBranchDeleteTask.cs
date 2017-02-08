using System;
using GitHub.Api;

namespace GitHub.Unity
{
    class GitRemoteBranchDeleteTask : GitTask
    {
        private readonly string arguments;

        private GitRemoteBranchDeleteTask(string remote, string branch, Action onSuccess, Action onFailure)
            : base(str => onSuccess.SafeInvoke(), onFailure)
        {
            Guard.ArgumentNotNullOrWhiteSpace(remote, "remote");
            Guard.ArgumentNotNullOrWhiteSpace(branch, "branch");

            arguments = String.Format("push {0} --delete {1}", remote, branch);
        }

        public static void Schedule(string repository, string branch, Action onSuccess, Action onFailure = null)
        {
            Tasks.Add(new GitRemoteBranchDeleteTask(repository, branch, onSuccess, onFailure));
        }

        public override bool Blocking { get { return false; } }
        public override bool Critical { get { return false; } }
        public override string Label { get { return "git push --delete"; } }
        protected override string ProcessArguments { get { return arguments; } }
    }
}
