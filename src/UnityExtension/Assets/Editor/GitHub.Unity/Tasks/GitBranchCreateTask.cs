using GitHub.Api;
using System;
using System.IO;

namespace GitHub.Unity
{
    class GitBranchCreateTask : GitTask
    {
        private readonly string arguments;

        private GitBranchCreateTask(IEnvironment environment, IProcessManager processManager, ITaskResultDispatcher resultDispatcher,
                                    string newBranch, string baseBranch, Action onSuccess, Action onFailure)
            : base(environment, processManager, resultDispatcher,
                   str => onSuccess.SafeInvoke(), onFailure)
        {
            Guard.ArgumentNotNullOrWhiteSpace(newBranch, "newBranch");
            Guard.ArgumentNotNullOrWhiteSpace(baseBranch, "baseBranch");

            arguments = String.Format("branch {0} {1}", newBranch, baseBranch);
        }

        public static void Schedule(string newBranch, string baseBranch, Action onSuccess, Action onFailure = null)
        {
            Tasks.Add(new GitBranchCreateTask(
                EntryPoint.Environment, EntryPoint.ProcessManager, EntryPoint.TaskResultDispatcher,
                newBranch, baseBranch, onSuccess, onFailure));
        }

        public override bool Blocking { get { return false; } }
        public override bool Critical { get { return false; } }
        public override string Label { get { return "git branch"; } }
        protected override string ProcessArguments { get { return arguments; } }
    }
}
