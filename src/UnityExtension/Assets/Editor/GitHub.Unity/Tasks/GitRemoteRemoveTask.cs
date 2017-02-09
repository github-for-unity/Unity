using System;
using GitHub.Api;

namespace GitHub.Unity
{
    class GitRemoteRemoveTask : GitTask
    {
        private readonly string arguments;

        private GitRemoteRemoveTask(string remote, Action onSuccess, Action onFailure)
            : base(str => onSuccess.SafeInvoke(), onFailure)
        {
            Guard.ArgumentNotNullOrWhiteSpace(remote, "remote");
            arguments = String.Format("remote rm {0}", remote);
        }

        public static void Schedule(string name, Action onSuccess, Action onFailure = null)
        {
            Tasks.Add(new GitRemoteRemoveTask(name, onSuccess, onFailure));
        }

        public override bool Blocking { get { return false; } }
        public override bool Critical { get { return false; } }
        public override string Label { get { return "git remote rm"; } }
        protected override string ProcessArguments { get { return arguments; } }
    }
}
