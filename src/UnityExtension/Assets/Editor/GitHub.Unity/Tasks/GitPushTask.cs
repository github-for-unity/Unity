using GitHub.Unity;
using System;
using System.Text;

namespace GitHub.Unity
{
    class GitPushTask : GitTask
    {
        private readonly string arguments;

        private GitPushTask(IEnvironment environment, IProcessManager processManager, ITaskResultDispatcher resultDispatcher,
                            Action onSuccess, Action onFailure)
            : base(environment, processManager, resultDispatcher,
                    str => onSuccess.SafeInvoke(), onFailure)
        {
            arguments = "push";
        }

        private GitPushTask(
                IEnvironment environment, IProcessManager processManager, ITaskResultDispatcher resultDispatcher,
                string remote, string branch, bool setUpstream,
                Action onSuccess, Action onFailure)
            : base(environment, processManager, resultDispatcher,
                  str => onSuccess.SafeInvoke(), onFailure)
        {
            Guard.ArgumentNotNullOrWhiteSpace(remote, "remote");
            Guard.ArgumentNotNullOrWhiteSpace(branch, "branch");

            arguments = String.Format("push {0} {1} {2}:{2}",
                setUpstream ? "-u" : "",
                remote, branch);
        }

        public static void Schedule(Action onSuccess, Action onFailure = null)
        {
            Tasks.Add(new GitPushTask(
                EntryPoint.Environment, EntryPoint.ProcessManager, EntryPoint.TaskResultDispatcher,
                onSuccess, onFailure));
        }

        public static void Schedule(string remote, bool setUpstream, string branch,
            Action onSuccess, Action onFailure = null)
        {
            Tasks.Add(new GitPushTask(
                EntryPoint.Environment, EntryPoint.ProcessManager, EntryPoint.TaskResultDispatcher,
                remote, branch, setUpstream, onSuccess, onFailure));
        }

        public override bool Blocking { get { return false; } }
        public override bool Critical { get { return false; } }
        public override string Label { get { return "git push"; } }
        protected override string ProcessArguments { get { return arguments; } }
    }
}
