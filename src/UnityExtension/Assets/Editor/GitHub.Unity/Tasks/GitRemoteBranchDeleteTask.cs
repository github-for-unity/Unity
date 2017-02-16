using System;
using GitHub.Unity;

namespace GitHub.Unity
{
    class GitRemoteBranchDeleteTask : GitTask
    {
        private readonly string arguments;

        private GitRemoteBranchDeleteTask(
                IEnvironment environment, IProcessManager processManager, ITaskResultDispatcher resultDispatcher,
                string remote, string branch,
                Action onSuccess, Action onFailure)
            : base(environment, processManager, resultDispatcher,
                  str => onSuccess.SafeInvoke(), onFailure)
        {
            Guard.ArgumentNotNullOrWhiteSpace(remote, "remote");
            Guard.ArgumentNotNullOrWhiteSpace(branch, "branch");

            arguments = String.Format("push {0} --delete {1}", remote, branch);
        }

        public static void Schedule(string repository, string branch, Action onSuccess, Action onFailure = null)
        {
            Tasks.Add(new GitRemoteBranchDeleteTask(
                EntryPoint.Environment, EntryPoint.ProcessManager, EntryPoint.TaskResultDispatcher,
                repository, branch, onSuccess, onFailure));
        }

        public override bool Blocking { get { return false; } }
        public override bool Critical { get { return false; } }
        public override string Label { get { return "git push --delete"; } }
        protected override string ProcessArguments { get { return arguments; } }
    }
}
