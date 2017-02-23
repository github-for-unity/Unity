using System;

namespace GitHub.Unity
{
    class GitBranchDeleteTask : GitTask
    {
        private readonly string arguments;

        public GitBranchDeleteTask(IEnvironment environment, IProcessManager processManager,
            ITaskResultDispatcher<string> resultDispatcher,
            string branch, Action onSuccess, Action onFailure)
            : base(environment, processManager, resultDispatcher)
        {
            Guard.ArgumentNotNullOrWhiteSpace(branch, "branch");
            arguments = String.Format("branch -d {0}", branch);
        }

        public override bool Blocking { get { return false; } }
        public override bool Critical { get { return false; } }
        public override string Label { get { return "git branch"; } }
        protected override string ProcessArguments { get { return arguments; } }
    }
}
