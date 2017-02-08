using GitHub.Api;
using System;
using System.Text;

namespace GitHub.Unity
{
    class GitPushTask : GitTask
    {
        private readonly string arguments;

        private GitPushTask(Action onSuccess, Action onFailure)
            : base(str => onSuccess.SafeInvoke(), onFailure)
        {
            arguments = "push";
        }

        private GitPushTask(string remote, string branch, bool setUpstream,
            Action onSuccess, Action onFailure)
            : base(str => onSuccess.SafeInvoke(), onFailure)
        {
            Guard.ArgumentNotNullOrWhiteSpace(remote, "remote");
            Guard.ArgumentNotNullOrWhiteSpace(branch, "branch");

            arguments = String.Format("push {0} {1} {2}:{2}",
                setUpstream ? "-u" : "",
                remote, branch);
        }

        public static void Schedule(Action onSuccess, Action onFailure = null)
        {
            Tasks.Add(new GitPushTask(onSuccess, onFailure));
        }

        public static void Schedule(string remote, bool setUpstream, string branch,
            Action onSuccess, Action onFailure = null)
        {
            Tasks.Add(new GitPushTask(remote, branch, setUpstream, onSuccess, onFailure));
        }

        public override bool Blocking { get { return false; } }
        public override bool Critical { get { return false; } }
        public override string Label { get { return "git push"; } }
        protected override string ProcessArguments { get { return arguments; } }
    }
}
